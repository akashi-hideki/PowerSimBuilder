"""Phase 12 generator: SimulationRunner + examples + tests."""

from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
F = {}

F[
    "src/python_runtime/simulation_runner.py"
] = '''"""SimulationRunner: main loop with connection propagation."""
from __future__ import annotations

from typing import Any

from .simulation_data import SimulationData


class SimulationRunner:
    """Runs the simulation loop.

    Components are registered by id. Connections propagate outputs to inputs
    after every update step, before logging.

    Attributes:
        components: dict of id -> Component.
        connections: list of {"from": "src.outputs.port",
                              "to": "dst.inputs.port"}.
        sim_data: SimulationData instance.
        dt: time step [s].
    """

    def __init__(self, dt: float = 0.001) -> None:
        self.components: dict[str, Any] = {}
        self.connections: list[dict[str, str]] = []
        self.sim_data = SimulationData(time_step=dt)
        self.dt = dt

    def add_component(self, comp: Any) -> None:
        self.components[comp.id] = comp

    def add_connection(self, src: str, dst: str) -> None:
        self.connections.append({"from": src, "to": dst})

    def step(self, t: float) -> None:
        """Perform one update cycle at time t."""
        self.sim_data.t = t
        self.sim_data.apply_stimulus(self.components, t)

        for comp in self.components.values():
            comp.update(t, self.dt, self.sim_data)

        self._propagate()

        self.sim_data.log(t, self.components)

    def run(self, t_end: float) -> SimulationData:
        """Run from t=0 to t_end inclusive (step = dt)."""
        t = 0.0
        n = int(round(t_end / self.dt))
        for i in range(n + 1):
            self.step(t)
            t = (i + 1) * self.dt
        return self.sim_data

    def _propagate(self) -> None:
        for conn in self.connections:
            src_path = conn["from"]
            dst_path = conn["to"]
            src_parts = src_path.split(".")
            dst_parts = dst_path.split(".")
            if len(src_parts) != 3 or len(dst_parts) != 3:
                continue
            src_id, src_sec, src_port = src_parts
            dst_id, dst_sec, dst_port = dst_parts
            src_comp = self.components.get(src_id)
            dst_comp = self.components.get(dst_id)
            if src_comp is None or dst_comp is None:
                continue
            if src_sec != "outputs" or dst_sec != "inputs":
                continue
            if src_port not in src_comp.outputs:
                continue
            dst_comp.inputs[dst_port] = src_comp.outputs[src_port]
'''

F["examples/__init__.py"] = '''"""Example scenarios for PowerSimBuilder runtime."""
'''

F["examples/scenario_a.py"] = '''"""Scenario A: button -> timer -> LED.

Timeline:
    t=0.00: button released
    t=0.10: button pressed
    t=0.60: LED turns ON (delay timer = 0.5s)
    t=1.00: button released
"""
from __future__ import annotations

from python_runtime.components import DigitalInput, DigitalOutput, Timer
from python_runtime.simulation_runner import SimulationRunner


def build() -> SimulationRunner:
    runner = SimulationRunner(dt=0.01)

    button = DigitalInput("button")
    timer = Timer("timer")
    timer.parameters["mode"] = "delay"
    timer.parameters["duration"] = 0.5

    led = DigitalOutput("led")
    led.parameters["active_low"] = False

    runner.add_component(button)
    runner.add_component(timer)
    runner.add_component(led)

    runner.add_connection("button.outputs.state", "timer.inputs.trigger")
    runner.add_connection("timer.outputs.done", "led.inputs.command")

    runner.sim_data.set_stimulus(
        "button.inputs.state",
        lambda t: 1 if 0.10 <= t < 1.00 else 0,
    )
    return runner


def run() -> None:
    runner = build()
    runner.run(t_end=1.2)
    for entry in runner.sim_data.logs:
        t = entry["time"]
        if abs(t - 0.10) < 1e-9 or abs(t - 0.60) < 1e-9 or abs(t - 1.00) < 1e-9:
            led_state = entry["components"]["led"]["outputs"]["state"]
            timer_done = entry["components"]["timer"]["outputs"]["done"]
            print(f"t={t:.2f} led={led_state} timer_done={timer_done}")


if __name__ == "__main__":
    run()
'''

F["examples/scenario_b.py"] = '''"""Scenario B: ramp -> analog filter -> PID -> analog output.

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
'''

F["examples/scenario_c.py"] = '''"""Scenario C: clock -> counter -> state machine.

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
'''

F["tests/python/test_simulation_runner.py"] = '''"""Unit tests for SimulationRunner."""
from python_runtime.component_base import Component
from python_runtime.simulation_runner import SimulationRunner


class Passthrough(Component):
    def __init__(self, cid, val=0.0):
        super().__init__(cid, "Passthrough")
        self.inputs = {"x": 0.0}
        self.outputs = {"y": val}

    def update(self, t, dt, sim_data):
        self.outputs["y"] = self.inputs.get("x", 0.0)


def test_add_component():
    r = SimulationRunner(dt=0.1)
    c = Passthrough("c1", 5.0)
    r.add_component(c)
    assert "c1" in r.components


def test_add_connection():
    r = SimulationRunner()
    r.add_connection("a.outputs.y", "b.inputs.x")
    assert r.connections == [{"from": "a.outputs.y", "to": "b.inputs.x"}]


def test_propagation_single_step():
    r = SimulationRunner(dt=0.1)
    src = Component("src", "Src")
    src.outputs["y"] = 1.0
    dst = Component("dst", "Dst")
    dst.inputs["x"] = 0.0
    r.add_component(src)
    r.add_component(dst)
    r.add_connection("src.outputs.y", "dst.inputs.x")

    r.step(0.0)
    assert dst.inputs["x"] == 1.0


def test_run_collects_logs():
    r = SimulationRunner(dt=0.1)
    c = Passthrough("c1", 3.0)
    r.add_component(c)
    r.run(t_end=1.0)
    assert len(r.sim_data.logs) == 11


def test_run_invokes_stimulus():
    r = SimulationRunner(dt=0.1)
    c = Passthrough("c1", 0.0)
    r.add_component(c)
    r.sim_data.set_stimulus("c1.inputs.x", lambda t: 1.0 if t >= 0.5 else 0.0)
    r.run(t_end=1.0)
    # find the log at t = 0.5 (index 5)
    entry = r.sim_data.logs[5]
    assert entry["components"]["c1"]["inputs"]["x"] == 1.0


def test_connection_missing_source_is_skipped():
    r = SimulationRunner(dt=0.1)
    dst = Passthrough("dst", 0.0)
    r.add_component(dst)
    r.add_connection("ghost.outputs.y", "dst.inputs.x")
    r.step(0.0)
    assert dst.inputs["x"] == 0.0


def test_connection_missing_port_is_skipped():
    r = SimulationRunner(dt=0.1)
    src = Passthrough("src", 1.0)
    dst = Passthrough("dst", 0.0)
    r.add_component(src)
    r.add_component(dst)
    r.add_connection("src.outputs.nope", "dst.inputs.x")
    r.step(0.0)
    assert dst.inputs["x"] == 0.0
'''

F["tests/python/test_scenarios.py"] = '''"""Integration tests for example scenarios."""
from examples import scenario_a, scenario_b, scenario_c


def test_scenario_a_button_timer_led():
    runner = scenario_a.build()
    runner.run(t_end=1.2)

    # Before t=0.6: LED off
    entry_05 = runner.sim_data.logs[50]
    assert entry_05["components"]["led"]["outputs"]["state"] == 0

    # After timer fires (accounting for one-step propagation delay):
    # timer.done becomes True at t=0.61, LED turns on at t=0.62.
    entry_06 = runner.sim_data.logs[62]
    assert entry_06["components"]["timer"]["outputs"]["done"] is True
    assert entry_06["components"]["led"]["outputs"]["state"] == 1


def test_scenario_b_filter_and_pid():
    runner = scenario_b.build()
    runner.run(t_end=0.5)

    # After step, filtered should rise
    entry_20 = runner.sim_data.logs[20]
    filtered = entry_20["components"]["ain"]["outputs"]["filtered"]
    assert 0.0 < filtered <= 1.0

    # PID output clamped to [0, 1]
    ctrl = entry_20["components"]["pid"]["outputs"]["control"]
    assert 0.0 <= ctrl <= 1.0


def test_scenario_c_counter_and_state():
    runner = scenario_c.build()
    runner.run(t_end=1.0)

    # State machine should eventually transition
    final_state = runner.sim_data.logs[-1]["components"]["sm"]["outputs"]["current_state"]
    assert final_state in ("IDLE", "DONE")
'''


def main() -> None:
    for rel, content in F.items():
        p = ROOT / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(content, encoding="utf-8")
        print(f"  + {rel}")
    print()
    print(f"Done. {len(F)} files written to {ROOT}")


if __name__ == "__main__":
    main()
