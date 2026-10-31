"""verify_pid.py - quantitative PID closed-loop verification (headless).

Runs OvenChamber + PIDController directly (same classes the GUI uses).
Prints per-combo metrics: steady-state, overshoot %, rise, settle, IAE.
"""
from __future__ import annotations
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from python_runtime.components import OvenChamber, PIDController


def run_closed_loop(kp, ki, kd, anti_windup, t_end=300.0, dt=0.05,
                    setpoint=200.0):
    oven = OvenChamber("oven")
    oven.parameters["thermal_resistance"] = 0.5
    oven.parameters["thermal_capacity"] = 200.0
    oven.parameters["ambient_temperature"] = 25.0
    oven.parameters["initial_temperature"] = 25.0
    oven.parameters["heater_power_max"] = 1000.0

    pid = PIDController("pid")
    pid.parameters["kp"] = kp
    pid.parameters["ki"] = ki
    pid.parameters["kd"] = kd
    pid.parameters["out_min"] = 0.0
    pid.parameters["out_max"] = 1.0
    pid.parameters["setpoint"] = setpoint
    pid.parameters["anti_windup"] = anti_windup

    n = int(round(t_end / dt))
    times, Ts, Us = [], [], []
    for i in range(n + 1):
        t = i * dt
        pid.inputs["measured"] = oven.outputs["oven_current_temp"]
        pid.update(t, dt, None)
        oven.inputs["heater_top_duty"] = pid.outputs["control"]
        oven.update(t, dt, None)
        times.append(t)
        Ts.append(oven.outputs["oven_current_temp"])
        Us.append(pid.outputs["control"])
    return times, Ts, Us


def metrics(times, values, target, dt):
    n = len(values)
    tail = values[int(n * 0.9):]
    ss = sum(tail) / len(tail)
    peak = max(values)
    overshoot = max(0.0, (peak - target) / target * 100.0)

    v0 = values[0]
    lo = v0 + 0.1 * (target - v0)
    hi = v0 + 0.9 * (target - v0)
    t10 = t90 = None
    for i, v in enumerate(values):
        if t10 is None and v >= lo:
            t10 = times[i]
        if t90 is None and v >= hi:
            t90 = times[i]
            break
    rise = (t90 - t10) if (t10 is not None and t90 is not None) else None

    band = 0.02 * abs(target)
    settle = 0.0
    for i in range(n - 1, -1, -1):
        if abs(values[i] - target) > band:
            settle = times[min(i + 1, n - 1)]
            break

    iae = sum(abs(target - v) * dt for v in values)
    return dict(ss=ss, err=target - ss, peak=peak,
                overshoot=overshoot, rise=rise, settle=settle, iae=iae)


def main():
    target = 200.0
    dt = 0.05
    combos = [
        ("current",       0.02,  0.001,    0.0, False),
        ("imc",           0.01,  0.0001,   0.0, True),
        ("aggressive",    0.02,  0.0002,   0.0, True),
        ("conservative",  0.005, 0.00005,  0.0, True),
        ("no_aw",         0.01,  0.0001,   0.0, False),
        ("aggressive_no_aw", 0.02, 0.0002, 0.0, False),
    ]
    hdr = (f"{'name':<14}{'kp':>7}{'ki':>9}{'aw':>6}"
           f"{'ss':>9}{'err':>8}{'ovs%':>8}{'rise':>7}{'settle':>8}{'IAE':>9}")
    print(hdr)
    print("-" * len(hdr))
    for name, kp, ki, kd, aw in combos:
        t, T, _ = run_closed_loop(kp, ki, kd, aw, t_end=300.0, dt=dt,
                                  setpoint=target)
        m = metrics(t, T, target, dt)
        rise = f"{m['rise']:.1f}" if m['rise'] is not None else "-"
        print(f"{name:<14}{kp:>7}{ki:>9}{str(aw):>6}"
              f"{m['ss']:>9.2f}{m['err']:>8.2f}{m['overshoot']:>8.2f}"
              f"{rise:>7}{m['settle']:>8.1f}{m['iae']:>9.0f}")


if __name__ == "__main__":
    main()