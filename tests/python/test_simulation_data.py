"""SimulationData の単体テスト。"""

from python_runtime.component_base import Component
from python_runtime.simulation_data import SimulationData

# --- 初期化 ---


def test_init_defaults():
    sd = SimulationData()
    assert sd.time_step == 0.001
    assert sd.t == 0.0
    assert sd.logs == []
    assert sd.stimulus == {}


def test_init_custom_time_step():
    sd = SimulationData(time_step=0.01)
    assert sd.time_step == 0.01


# --- stimulus (callable) ---


def test_set_callable_stimulus_and_get_value():
    sd = SimulationData()
    sd.set_stimulus("din1.inputs.state", lambda t: 1 if t >= 0.1 else 0)
    assert sd.get_stimulus_value("din1.inputs.state", 0.0) == 0
    assert sd.get_stimulus_value("din1.inputs.state", 0.1) == 1
    assert sd.get_stimulus_value("din1.inputs.state", 1.0) == 1


def test_get_stimulus_value_missing_returns_none():
    sd = SimulationData()
    assert sd.get_stimulus_value("nope.inputs.x", 0.0) is None


# --- stimulus (series) ---


def test_set_series_stimulus_sorted_and_step_interp():
    sd = SimulationData()
    sd.set_stimulus("din1.inputs.state", [(0.2, 1), (0.0, 0), (0.4, 0)])
    # t < 0.2 は 0
    assert sd.get_stimulus_value("din1.inputs.state", 0.1) == 0
    # 0.2 <= t < 0.4 は 1
    assert sd.get_stimulus_value("din1.inputs.state", 0.3) == 1
    # 0.4 <= t は 0
    assert sd.get_stimulus_value("din1.inputs.state", 0.5) == 0


# --- apply_stimulus ---


def test_apply_stimulus_updates_component_inputs():
    sd = SimulationData()
    c = Component("din1", "DigitalInput")
    sd.set_stimulus("din1.inputs.state", lambda t: int(t >= 0.5))
    sd.apply_stimulus({"din1": c}, 0.3)
    assert c.inputs["state"] == 0
    sd.apply_stimulus({"din1": c}, 0.7)
    assert c.inputs["state"] == 1


def test_apply_stimulus_ignores_missing_component():
    sd = SimulationData()
    sd.set_stimulus("ghost.inputs.state", lambda t: 1)
    # 例外なく通れば OK
    sd.apply_stimulus({}, 0.0)


def test_apply_stimulus_ignores_non_inputs_section():
    sd = SimulationData()
    c = Component("din1", "DigitalInput")
    sd.set_stimulus("din1.outputs.state", lambda t: 1)
    sd.apply_stimulus({"din1": c}, 0.0)
    assert "state" not in c.inputs


# --- logging ---


def test_log_appends_snapshot():
    sd = SimulationData()
    c = Component("c1", "Dummy")
    c.inputs["a"] = 1
    c.outputs["b"] = 2
    c.parameters["k"] = 3
    sd.log(0.5, {"c1": c})
    assert len(sd.logs) == 1
    entry = sd.logs[0]
    assert entry["time"] == 0.5
    assert entry["components"]["c1"]["inputs"] == {"a": 1}
    assert entry["components"]["c1"]["outputs"] == {"b": 2}
    assert entry["components"]["c1"]["parameters"] == {"k": 3}


def test_log_snapshot_is_deep_copy_of_containers():
    sd = SimulationData()
    c = Component("c1", "Dummy")
    c.outputs["y"] = 1
    sd.log(0.0, {"c1": c})
    c.outputs["y"] = 999
    assert sd.logs[0]["components"]["c1"]["outputs"]["y"] == 1


def test_clear_logs():
    sd = SimulationData()
    c = Component("c1", "Dummy")
    sd.log(0.0, {"c1": c})
    sd.clear_logs()
    assert sd.logs == []
