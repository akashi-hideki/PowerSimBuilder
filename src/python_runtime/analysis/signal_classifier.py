"""Signal classifier: infer category + proposed component from waveform shape.

SPEC: docs/SPEC_PSBLOG_IMPORT.md §3
"""

from __future__ import annotations

import math
from dataclasses import dataclass

from python_runtime.psblog import SignalTrace


@dataclass
class Classification:
    """Result of classifying one variable from a SignalTrace."""

    var_name: str
    category: str
    confidence: float
    proposed_component: str | None
    reasoning: str


_DEFAULT = "_default_"


def _stddev(xs: list[float]) -> float:
    n = len(xs)
    if n < 2:
        return 0.0
    mean = sum(xs) / n
    var = sum((x - mean) ** 2 for x in xs) / n
    return math.sqrt(var)


def _linear_r2(ts: list[float], ys: list[float]) -> float:
    n = len(ts)
    if n < 2:
        return 0.0
    mean_t = sum(ts) / n
    mean_y = sum(ys) / n
    sxx = sum((t - mean_t) ** 2 for t in ts)
    sxy = sum((ts[i] - mean_t) * (ys[i] - mean_y) for i in range(n))
    syy = sum((y - mean_y) ** 2 for y in ys)
    if sxx == 0 or syy == 0:
        return 0.0
    return (sxy * sxy) / (sxx * syy)


def _detect_jumps(ys: list[float], threshold: float) -> list[int]:
    return [i for i in range(len(ys) - 1) if abs(ys[i + 1] - ys[i]) > threshold]


def _distinct_levels(ys: list[float]) -> int:
    return len(set(round(y, 4) for y in ys))


def _is_digital(ys: list[float]) -> bool:
    return all(abs(y) < 1e-9 or abs(y - 1) < 1e-9 for y in ys)


def _is_exponential(ts: list[float], ys: list[float]) -> tuple[bool, float]:
    if len(ts) < 5:
        return (False, 0.0)
    y0, y_n = ys[0], ys[-1]
    if abs(y_n - y0) < 1e-6:
        return (False, 0.0)
    # Reject if signal is already well-approximated by a straight line.
    # A ramp with sparse tail data can produce log(K-y) ~ linear (R²>0.98),
    # which would be misclassified as exponential. Prefer ramp/step here.
    if _linear_r2(ts, ys) > 0.99:
        return (False, 0.0)
    k = y_n
    eps = abs(y_n - y0) * 0.01 + 1e-9
    valid = [(t, k - y) for t, y in zip(ts, ys, strict=False) if (k - y) > eps]
    if len(valid) < 3:
        return (False, 0.0)
    try:
        ts_v = [t for t, _ in valid]
        log_v = [math.log(d) for _, d in valid]
    except (ValueError, ZeroDivisionError):
        return (False, 0.0)
    r2 = _linear_r2(ts_v, log_v)
    return (r2 > 0.98, r2)


def _classify_shape(ts: list[float], ys: list[float]) -> tuple[str, float, str]:
    if len(ts) < 2:
        return "constant", 0.9, "single sample"

    sd = _stddev(ys)
    if sd < 1e-6:
        return "constant", 0.9, f"stddev={sd:.2e}"

    if _is_digital(ys):
        if any(abs(ys[i + 1] - ys[i]) > 0.5 for i in range(len(ys) - 1)):
            return "digital", 0.95, "values in {0,1} with transitions"
        return "constant", 0.9, "values in {0,1} without transitions"

    rng = max(ys) - min(ys)
    jumps = _detect_jumps(ys, 0.05 * rng)

    # Exponential first: smooth curves have small "jumps" that would
    # otherwise match the step heuristic (SPEC §3: exponential has
    # strictest criterion R² > 0.98, so it takes priority).
    is_exp, r2_exp = _is_exponential(ts, ys)
    if is_exp:
        return "exponential", 0.85, f"1st-order fit R²={r2_exp:.4f}"

    if len(jumps) > 5 and _distinct_levels(ys) <= 4:
        return "pwm", 0.85, f"{len(jumps)} jumps, {_distinct_levels(ys)} levels"

    if 1 <= len(jumps) <= 3 and _distinct_levels(ys) <= 4:
        return "step", 0.85, f"{len(jumps)} jump(s) > 5% of range"

    r2_lin = _linear_r2(ts, ys)
    if r2_lin > 0.99:
        n = len(ts)
        mean_t = sum(ts) / n
        mean_y = sum(ys) / n
        sxx = sum((t - mean_t) ** 2 for t in ts)
        sxy = sum((ts[i] - mean_t) * (ys[i] - mean_y) for i in range(n))
        slope = sxy / sxx if sxx > 0 else 0.0
        if slope > 0:
            return "ramp", 0.9, f"linear R²={r2_lin:.4f}, slope={slope:.3f}"

    return "unknown", 0.3, "no heuristic matched"


def _role_override(
    role: str, base_cat: str, base_conf: float, base_reason: str
) -> tuple[str, float, str | None, str]:
    if role == "target":
        return "step", 1.0, "StepSource", "role=target"
    if role == "pid_out":
        return "pwm", 1.0, "PIDController", "role=pid_out"
    if role == "input":
        if base_cat == "digital":
            return base_cat, 1.0, "DigitalInput", "role=input + digital shape"
        if base_cat == "step":
            return base_cat, 1.0, "StepSource", "role=input + step shape"
        return base_cat, base_conf, None, "role=input, generic shape"
    if role in ("error", "state"):
        return base_cat, base_conf, None, f"role={role}"
    return base_cat, base_conf, _DEFAULT, base_reason


def _propose_component(cat: str) -> str | None:
    return {
        "constant": None,
        "ramp": "Integrator",
        "step": "StepSource",
        "exponential": "ThermalMass",
        "pwm": "PIDController",
        "digital": "DigitalInput",
        "unknown": None,
    }.get(cat)


def classify(trace: SignalTrace, var: str) -> Classification:
    meta = trace.vars.get(var)
    if meta is None:
        return Classification(
            var_name=var,
            category="unknown",
            confidence=0.0,
            proposed_component=None,
            reasoning=f"variable {var!r} not in VarTable",
        )
    series = trace.series(var)
    if not series:
        return Classification(
            var_name=var,
            category="unknown",
            confidence=0.0,
            proposed_component=None,
            reasoning="no data rows for variable",
        )
    ts = [t for t, _ in series]
    ys = [float(v) for _, v in series]

    base_cat, base_conf, base_reason = _classify_shape(ts, ys)
    cat, conf, prop_override, reason = _role_override(meta.role, base_cat, base_conf, base_reason)
    prop = _propose_component(cat) if prop_override is _DEFAULT else prop_override
    return Classification(
        var_name=var,
        category=cat,
        confidence=conf,
        proposed_component=prop,
        reasoning=reason,
    )


def classify_all(trace: SignalTrace) -> list[Classification]:
    return [classify(trace, name) for name in trace.vars]
