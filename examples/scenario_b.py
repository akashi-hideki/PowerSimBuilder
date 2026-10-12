"""Scenario B: ramp -> analog filter -> PID -> analog output.

Demonstrates:
    - first-order filter step response
    - proportional control output
"""

from __future__ import annotations

from python_runtime.components import AnalogInput, AnalogOutput, PIDController
from python_runtime.simulation_runner import SimulationRunner


def build() -> SimulationRunner:
    runner = SimulationRunner(dt=0.01)

    ain = AnalogInput("ain")
    ain.parameters["v_min"] = 0.0
    ain.parameters["v_max"] = 5.0
    ain.parameters["filter_alpha"] = 0.2

    pid = PIDController("pid")
    pid.parameters["kp"] = 1.0
    pid.parameters["ki"] = 0.0
    pid.parameters["kd"] = 0.0
    pid.parameters["setpoint"] = 0.5
    pid.parameters["out_min"] = 0.0
    pid.parameters["out_max"] = 1.0

    aout = AnalogOutput("aout")
    aout.parameters["v_ref"] = 5.0

    runner.add_component(ain)
    runner.add_component(pid)
    runner.add_component(aout)

    runner.add_connection("ain.outputs.filtered", "pid.inputs.measured")
    runner.add_connection("pid.outputs.control", "aout.inputs.value")

    runner.sim_data.set_stimulus(
        "ain.inputs.raw",
        lambda t: 0.0 if t < 0.1 else 5.0,
    )
    return runner


def run() -> None:
    runner = build()
    runner.run(t_end=0.5)
    for entry in runner.sim_data.logs:
        t = entry["time"]
        if abs(t - 0.10) < 1e-9 or abs(t - 0.20) < 1e-9 or abs(t - 0.50) < 1e-9:
            filt = entry["components"]["ain"]["outputs"]["filtered"]
            ctrl = entry["components"]["pid"]["outputs"]["control"]
            volt = entry["components"]["aout"]["outputs"]["voltage"]
            print(f"t={t:.2f} filtered={filt:.4f} control={ctrl:.4f} voltage={volt:.4f}")


if __name__ == "__main__":
    run()
