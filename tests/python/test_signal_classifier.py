"""Tests for signal_classifier."""

from __future__ import annotations

from pathlib import Path

from python_runtime.analysis.signal_classifier import (
    Classification,
    classify,
    classify_all,
)
from python_runtime.psblog import parse_psblog


def _write(tmp_path: Path, content: str) -> Path:
    p = tmp_path / "t.psblog"
    p.write_text(content, encoding="utf-8", newline="\n")
    return p


def _trace(tmp_path: Path, rows: str, var: str = "a", extra: str = "") -> Path:
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        f"@var {var} float32 - Hardware {extra}\n"
        f"{rows}"
    )
    return _write(tmp_path, content)


def _sample_path() -> Path:
    return (
        Path(__file__).resolve().parents[2] / "examples" / "logs" / "sample_cooking_heater.psblog"
    )


def test_constant(tmp_path: Path) -> None:
    p = _trace(tmp_path, "@0 a=5\n@10 a=5\n@20 a=5\n@30 a=5\n")
    c = classify(parse_psblog(p), "a")
    assert c.category == "constant"


def test_ramp(tmp_path: Path) -> None:
    p = _trace(tmp_path, "@0 a=0\n@10 a=1\n@20 a=2\n@30 a=3\n@40 a=4\n@50 a=5\n")
    c = classify(parse_psblog(p), "a")
    assert c.category == "ramp"


def test_step(tmp_path: Path) -> None:
    p = _trace(tmp_path, "@0 a=0\n@10 a=0\n@20 a=0\n@30 a=200\n@40 a=200\n@50 a=200\n")
    c = classify(parse_psblog(p), "a")
    assert c.category == "step"


def test_exponential(tmp_path: Path) -> None:
    p = _trace(
        tmp_path,
        "@0 a=0\n@1 a=0.632\n@2 a=0.865\n@3 a=0.950\n@4 a=0.982\n",
    )
    c = classify(parse_psblog(p), "a")
    assert c.category == "exponential"


def test_pwm(tmp_path: Path) -> None:
    rows = "\n".join(f"@{i * 10} a={100 if i % 2 else 0}" for i in range(12))
    p = _trace(tmp_path, rows + "\n")
    c = classify(parse_psblog(p), "a")
    assert c.category == "pwm"


def test_digital(tmp_path: Path) -> None:
    p = _trace(tmp_path, "@0 a=0\n@10 a=1\n@20 a=0\n@30 a=1\n@40 a=0\n@50 a=1\n")
    c = classify(parse_psblog(p), "a")
    assert c.category == "digital"


def test_role_override_target(tmp_path: Path) -> None:
    p = _trace(
        tmp_path,
        "@0 a=0\n@10 a=200\n@20 a=200\n",
        var="a",
        extra='"Target" role=target',
    )
    c = classify(parse_psblog(p), "a")
    assert c.category == "step"
    assert c.proposed_component == "StepSource"
    assert c.confidence == 1.0


def test_role_override_pid_out(tmp_path: Path) -> None:
    p = _trace(
        tmp_path,
        "@0 a=0\n@10 a=1\n@20 a=0\n@30 a=1\n@40 a=0\n@50 a=1\n",
        var="a",
        extra='"PWM" role=pid_out',
    )
    c = classify(parse_psblog(p), "a")
    assert c.category == "pwm"
    assert c.proposed_component == "PIDController"
    assert c.confidence == 1.0


def test_role_override_input_digital(tmp_path: Path) -> None:
    p = _trace(
        tmp_path,
        "@0 a=0\n@10 a=1\n@20 a=0\n@30 a=1\n",
        var="a",
        extra='"Door" role=input',
    )
    c = classify(parse_psblog(p), "a")
    assert c.category == "digital"
    assert c.proposed_component == "DigitalInput"
    assert c.confidence == 1.0


def test_role_override_input_step(tmp_path: Path) -> None:
    p = _trace(
        tmp_path,
        "@0 a=0\n@10 a=0\n@20 a=0\n@30 a=200\n@40 a=200\n",
        var="a",
        extra='"Setpoint" role=input',
    )
    c = classify(parse_psblog(p), "a")
    assert c.category == "step"
    assert c.proposed_component == "StepSource"
    assert c.confidence == 1.0


def test_confidence_full_match(tmp_path: Path) -> None:
    p = _trace(tmp_path, "@0 a=0\n@10 a=1\n@20 a=0\n@30 a=1\n@40 a=0\n")
    c = classify(parse_psblog(p), "a")
    assert c.category == "digital"
    assert c.confidence >= 0.9


def test_confidence_ambiguous(tmp_path: Path) -> None:
    p = _trace(
        tmp_path,
        "@0 a=0.3\n@10 a=0.7\n@20 a=0.1\n@30 a=0.9\n"
        "@40 a=0.5\n@50 a=0.2\n@60 a=0.8\n@70 a=0.4\n",
    )
    c = classify(parse_psblog(p), "a")
    assert c.category == "unknown"
    assert c.confidence <= 0.5


def test_classify_all_on_sample_log() -> None:
    trace = parse_psblog(_sample_path())
    results = classify_all(trace)
    assert len(results) == len(trace.vars)
    valid = {"constant", "ramp", "step", "exponential", "pwm", "digital", "oscillating", "unknown"}
    for r in results:
        assert isinstance(r, Classification)
        assert r.category in valid
        assert 0.0 <= r.confidence <= 1.0


def test_unknown_falls_back(tmp_path: Path) -> None:
    p = _trace(
        tmp_path,
        "@0 a=0.3\n@10 a=0.7\n@20 a=0.1\n@30 a=0.9\n"
        "@40 a=0.5\n@50 a=0.2\n@60 a=0.8\n@70 a=0.4\n",
    )
    c = classify(parse_psblog(p), "a")
    assert c.category == "unknown"
    assert c.proposed_component is None


def test_empty_series(tmp_path: Path) -> None:
    p = _write(
        tmp_path,
        "@schema psblog/v1\n" "@captured_at 2026-11-09T10:30:00+09:00\n" "@var a uint8_t - A\n",
    )
    c = classify(parse_psblog(p), "a")
    assert c.category == "unknown"
    assert c.confidence == 0.0


def test_ramp_not_exponential(tmp_path: Path) -> None:
    """Regression: a pure linear ramp must be 'ramp', not 'exponential'."""
    p = _trace(
        tmp_path,
        "@0 a=0\n@10 a=10\n@20 a=20\n@30 a=30\n@40 a=40\n@100 a=100\n@200 a=200\n",
    )
    c = classify(parse_psblog(p), "a")
    assert c.category == "ramp"
    assert c.proposed_component == "Integrator"


def test_oscillating(tmp_path: Path) -> None:
    """Triangle wave (3 cycles) → oscillating."""
    rows = "\n".join(
        [
            "@0 a=0",
            "@10 a=1",
            "@20 a=2",
            "@30 a=3",
            "@40 a=2",
            "@50 a=1",
            "@60 a=0",
            "@70 a=1",
            "@80 a=2",
            "@90 a=3",
            "@100 a=2",
            "@110 a=1",
            "@120 a=0",
            "@130 a=1",
            "@140 a=2",
            "@150 a=3",
        ]
    )
    p = _trace(tmp_path, rows + "\n")
    c = classify(parse_psblog(p), "a")
    assert c.category == "oscillating"
    assert c.confidence >= 0.5


def test_large_range_ramp_not_auto_selected(tmp_path: Path) -> None:
    """Large-range ramp (e.g. g_system_tick, rng > 10000) → confidence 0.3."""
    rows = "\n".join([f"@{i * 100} a={i * 1000}" for i in range(15)])
    p = _trace(tmp_path, rows + "\n")
    c = classify(parse_psblog(p), "a")
    assert c.category == "ramp"
    assert c.confidence < 0.5
