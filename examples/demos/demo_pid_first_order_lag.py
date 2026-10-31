"""Closed-loop demo: PIDController + standard FirstOrderLag.

Uses:
- python_runtime.components.PIDController (PID with anti-windup)
- python_runtime.components.FirstOrderLag (standard, has input port)

Note: Dahlquist FMU has no input, so it cannot be the plant of a closed loop.
For FMU-based closed loop, use Feedthrough.fmu or a plant-like FMU.
"""

import sys
from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt

ROOT = Path(__file__).parent.parent
sys.path.insert(0, str(ROOT / "src"))

from python_runtime.components import FirstOrderLag, PIDController  # noqa: E402


def main():
    dt = 0.01
    t_end = 10.0
    n_steps = int(round(t_end / dt))

    plant = FirstOrderLag("plant")
    plant.parameters["time_constant"] = 1.0

    pid = PIDController("pid")
    pid.parameters["kp"] = 2.5
    pid.parameters["ki"] = 0.75
    pid.parameters["kd"] = 0.1
    pid.parameters["setpoint"] = 1.0
    pid.parameters["out_min"] = 0.0
    pid.parameters["out_max"] = 2.0
    pid.parameters["anti_windup"] = True

    times, sp, y, u = [], [], [], []
    for i in range(n_steps + 1):
        t = i * dt
        measured = plant.outputs.get("output", 0.0)
        pid.inputs["measured"] = measured
        pid.update(t, dt, None)
        u_val = pid.outputs.get("control", 0.0)
        plant.inputs["input"] = u_val
        plant.update(t, dt, None)
        times.append(t)
        sp.append(pid.parameters.get("setpoint", 0.0))
        y.append(measured)
        u.append(u_val)

    fig, (ax1, ax2) = plt.subplots(2, 1, figsize=(12, 8), sharex=True)
    ax1.plot(times, sp, "k--", label="setpoint", linewidth=1.5)
    ax1.plot(times, y, "#FF5733", label="y (plant output)", linewidth=2)
    ax1.set_ylabel("y")
    ax1.legend()
    ax1.grid(True, alpha=0.3)
    ax1.set_title("Closed-loop: PIDController + FirstOrderLag (standard)")

    ax2.plot(times, u, "#33FF57", linewidth=2)
    ax2.set_ylabel("u (control)")
    ax2.set_xlabel("time [s]")
    ax2.grid(True, alpha=0.3)

    fig.tight_layout()
    out = ROOT / "tmp" / "closed_loop_demo.png"
    fig.savefig(out, dpi=120, facecolor="white", edgecolor="white")
    print(f"saved: {out}")
    print(f"final y = {y[-1]:.4f}  (target: 1.0)")
    print(f"final u = {u[-1]:.4f}")


if __name__ == "__main__":
    main()
