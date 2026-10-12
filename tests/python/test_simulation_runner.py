"""Unit tests for SimulationRunner."""
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
