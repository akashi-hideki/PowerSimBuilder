"""Scenario C: clock -> counter -> state machine.

Demonstrates:
    - rising-edge counting
    - state transition after overflow
"""

from __future__ import annotations

from python_runtime.components import Counter, StateMachine
from python_runtime.simulation_runner import SimulationRunner


def build() -> SimulationRunner:
    runner = SimulationRunner(dt=0.01)

    cnt = Counter("cnt")
    cnt.parameters["max_count"] = 5
    cnt.parameters["initial"] = 0

    sm = StateMachine("sm")
    sm.parameters["initial_state"] = "IDLE"
    # overflow is a boolean, so the transition key is "IDLE:True"
    sm.parameters["transitions"] = {
        "IDLE:True": "DONE",
    }

    runner.add_component(cnt)
    runner.add_component(sm)

    runner.add_connection("cnt.outputs.overflow", "sm.inputs.event")

    runner.sim_data.set_stimulus(
        "cnt.inputs.clock",
        lambda t: 1 if (t * 10) % 2 < 1 else 0,
    )
    return runner


def run() -> None:
    runner = build()
    runner.run(t_end=1.2)
    for entry in runner.sim_data.logs:
        t = entry["time"]
        if abs(t - 0.50) < 1e-9 or abs(t - 1.00) < 1e-9 or abs(t - 1.20) < 1e-9:
            count = entry["components"]["cnt"]["outputs"]["count"]
            state = entry["components"]["sm"]["outputs"]["current_state"]
            print(f"t={t:.2f} count={count} state={state}")


if __name__ == "__main__":
    run()
