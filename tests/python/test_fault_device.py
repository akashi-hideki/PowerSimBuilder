"""Tests for FaultDevice."""

from __future__ import annotations

from python_runtime.components import FaultDevice


def _run(fd: FaultDevice, t_end: float, dt: float = 0.001) -> None:
    n = int(round(t_end / dt))
    for i in range(n + 1):
        fd.update(i * dt, dt, None)


def test_no_trigger_when_condition_false():
    fd = FaultDevice("fd")
    fd.parameters["fault_type"] = "short"
    fd.parameters["condition_expr"] = "t >= 1.0"
    fd.parameters["duration"] = 0.0
    _run(fd, t_end=0.5)
    assert fd.outputs["short_circuit"] is False


def test_short_circuit_full_run():
    fd = FaultDevice("fd")
    fd.parameters["fault_type"] = "short"
    fd.parameters["condition_expr"] = "t >= 0.1"
    fd.parameters["duration"] = 0.05
    _run(fd, t_end=0.30)
    assert fd.outputs["short_circuit"] is True


def test_open_circuit_fault():
    fd = FaultDevice("fd")
    fd.parameters["fault_type"] = "open"
    fd.parameters["condition_expr"] = "t >= 0.1"
    fd.parameters["duration"] = 0.0
    _run(fd, t_end=0.2)
    assert fd.outputs["open_circuit"] is True


def test_inrush_fault():
    fd = FaultDevice("fd")
    fd.parameters["fault_type"] = "inrush"
    fd.parameters["condition_expr"] = "t >= 0.1"
    fd.parameters["duration"] = 0.0
    fd.parameters["inrush_current"] = 50.0
    _run(fd, t_end=0.2)
    assert fd.outputs["inrush_current"] == 50.0


def test_latch_persists_after_condition_clears():
    fd = FaultDevice("fd")
    fd.parameters["fault_type"] = "short"
    fd.parameters["condition_expr"] = "t >= 0.1 and t < 0.2"
    fd.parameters["duration"] = 0.0
    _run(fd, t_end=0.5)  # condition was true, then false
    assert fd.outputs["short_circuit"] is True  # latched


def test_reset_clears_latch():
    fd = FaultDevice("fd")
    fd.parameters["fault_type"] = "short"
    fd.parameters["condition_expr"] = "t >= 0.1"
    fd.parameters["duration"] = 0.0
    _run(fd, t_end=0.2)
    assert fd.outputs["short_circuit"] is True
    fd.reset()
    assert fd.outputs["short_circuit"] is False
    assert fd._scenario_state["triggered"] is False


def test_invalid_expression_treated_as_false():
    fd = FaultDevice("fd")
    fd.parameters["condition_expr"] = "undefined_name > 0"
    fd.parameters["duration"] = 0.0
    _run(fd, t_end=0.1)
    assert fd.outputs["short_circuit"] is False
    assert fd.outputs["open_circuit"] is False
