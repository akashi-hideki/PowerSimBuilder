"""Unit tests for StateMachine."""
from python_runtime.components import StateMachine


def _new(**params):
    c = StateMachine("sm1")
    c.parameters.update(params)
    return c


def test_initial_state():
    c = _new(initial_state="IDLE")
    c.update(0.0, 0.1, None)
    assert c.outputs["current_state"] == "IDLE"
    assert c.outputs["transitioned"] is False


def test_simple_transition():
    c = _new(
        initial_state="IDLE",
        transitions={"IDLE:start": "RUNNING"},
    )
    c.inputs["event"] = "start"
    c.update(0.0, 0.1, None)
    assert c.outputs["current_state"] == "RUNNING"
    assert c.outputs["prev_state"] == "IDLE"
    assert c.outputs["transitioned"] is True


def test_no_transition_for_unknown_event():
    c = _new(
        initial_state="IDLE",
        transitions={"IDLE:start": "RUNNING"},
    )
    c.inputs["event"] = "unknown"
    c.update(0.0, 0.1, None)
    assert c.outputs["current_state"] == "IDLE"
    assert c.outputs["transitioned"] is False


def test_multi_step_transition():
    c = _new(
        initial_state="IDLE",
        transitions={
            "IDLE:start": "RUNNING",
            "RUNNING:stop": "IDLE",
        },
    )
    c.inputs["event"] = "start"
    c.update(0.0, 0.1, None)
    assert c.outputs["current_state"] == "RUNNING"

    c.inputs["event"] = "stop"
    c.update(0.1, 0.1, None)
    assert c.outputs["current_state"] == "IDLE"


def test_event_none_does_nothing():
    c = _new(
        initial_state="A",
        transitions={"A:go": "B"},
    )
    c.inputs["event"] = None
    c.update(0.0, 0.1, None)
    assert c.outputs["current_state"] == "A"


def test_reset_input():
    c = _new(
        initial_state="IDLE",
        transitions={"IDLE:start": "RUNNING"},
    )
    c.inputs["event"] = "start"
    c.update(0.0, 0.1, None)
    assert c.outputs["current_state"] == "RUNNING"

    c.inputs["reset"] = True
    c.update(0.1, 0.1, None)
    assert c.outputs["current_state"] == "IDLE"


def test_transitioned_flag_is_one_shot():
    c = _new(
        initial_state="IDLE",
        transitions={"IDLE:start": "RUNNING"},
    )
    c.inputs["event"] = "start"
    c.update(0.0, 0.1, None)
    assert c.outputs["transitioned"] is True

    c.update(0.1, 0.1, None)
    assert c.outputs["transitioned"] is False
