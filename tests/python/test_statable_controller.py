"""StaTableControllerBase / StaTableRuntime のテスト。"""

from __future__ import annotations

from python_runtime.statable import StaTableControllerBase, StaTableRuntime

# ======================================================================
# テスト用の層定義
# ======================================================================


class DriverInputController(StaTableControllerBase):
    LAYER_NAME = "DriverInput"
    LAYER_PRIORITY = 1
    INITIAL_STATE = "InIdle"
    TRANSITIONS = [
        ("InIdle", "SENSOR_TICK", None, None, "InSensing"),
        ("InSensing", "ADC_DONE", None, None, "InIdle"),
    ]
    EVENTS = {
        "SENSOR_TICK": {"id": 1, "delivery": "queue"},
        "ADC_DONE": {"id": 2, "delivery": "direct"},
    }

    def __init__(self, comp_id="driverinput"):
        super().__init__(
            comp_id=comp_id,
            comp_type="DriverInputController",
            layer_name=self.LAYER_NAME,
            layer_priority=self.LAYER_PRIORITY,
            initial_state=self.INITIAL_STATE,
            transitions=self.TRANSITIONS,
            events=self.EVENTS,
        )


class ApplicationController(StaTableControllerBase):
    LAYER_NAME = "Application"
    LAYER_PRIORITY = 5
    INITIAL_STATE = "Idle"
    TRANSITIONS = [
        ("Idle", "START", None, None, "Executing"),
        ("Executing", "DONE", None, None, "Completed"),
    ]
    EVENTS = {
        "START": {"id": 1, "delivery": "direct"},
        "DONE": {"id": 2, "delivery": "direct"},
    }

    def __init__(self, comp_id="application"):
        super().__init__(
            comp_id=comp_id,
            comp_type="ApplicationController",
            layer_name=self.LAYER_NAME,
            layer_priority=self.LAYER_PRIORITY,
            initial_state=self.INITIAL_STATE,
            transitions=self.TRANSITIONS,
            events=self.EVENTS,
        )


# ======================================================================
# StaTableControllerBase
# ======================================================================


class TestStaTableControllerBase:
    def test_initial_state(self):
        ctrl = DriverInputController()
        assert ctrl.state == "InIdle"
        assert ctrl.outputs["current_state"] == "InIdle"

    def test_fire_event_transitions(self):
        ctrl = DriverInputController()
        ok = ctrl.fire_event("SENSOR_TICK")
        assert ok is True
        assert ctrl.state == "InSensing"
        assert ctrl.outputs["current_state"] == "InSensing"

    def test_fire_event_unknown_event_returns_false(self):
        ctrl = DriverInputController()
        ok = ctrl.fire_event("NONEXISTENT")
        assert ok is False
        assert ctrl.state == "InIdle"

    def test_fire_event_wrong_state_returns_false(self):
        ctrl = DriverInputController()
        ok = ctrl.fire_event("ADC_DONE")
        assert ok is False
        assert ctrl.state == "InIdle"

    def test_multiple_transitions(self):
        ctrl = DriverInputController()
        ctrl.fire_event("SENSOR_TICK")
        ctrl.fire_event("ADC_DONE")
        assert ctrl.state == "InIdle"

    def test_reset(self):
        ctrl = DriverInputController()
        ctrl.fire_event("SENSOR_TICK")
        ctrl.reset()
        assert ctrl.state == "InIdle"
        assert ctrl.outputs["current_state"] == "InIdle"
        assert ctrl.pending_events == []

    def test_update_initializes_state(self):
        ctrl = DriverInputController()
        ctrl._initialized = False
        ctrl.state = "WRONG"
        ctrl.update(0.0, 0.001, None)
        assert ctrl.state == "InIdle"

    def test_queue_event_does_not_immediately_transition(self):
        ctrl = DriverInputController()
        ctrl.queue_event("SENSOR_TICK")
        assert ctrl.state == "InIdle"
        assert len(ctrl.pending_events) == 1

    def test_update_processes_pending_events(self):
        ctrl = DriverInputController()
        ctrl.queue_event("SENSOR_TICK")
        ctrl.update(0.0, 0.001, None)
        assert ctrl.state == "InSensing"
        assert ctrl.pending_events == []

    def test_get_state(self):
        ctrl = DriverInputController()
        assert ctrl.get_state() == "InIdle"

    def test_get_available_events(self):
        ctrl = DriverInputController()
        events = ctrl.get_available_events()
        assert events == ["SENSOR_TICK"]

    def test_get_available_events_after_transition(self):
        ctrl = DriverInputController()
        ctrl.fire_event("SENSOR_TICK")
        events = ctrl.get_available_events()
        assert events == ["ADC_DONE"]

    def test_layer_priority(self):
        ctrl = DriverInputController()
        assert ctrl.layer_priority == 1

    def test_layer_name(self):
        ctrl = DriverInputController()
        assert ctrl.layer_name == "DriverInput"


# ======================================================================
# StaTableRuntime
# ======================================================================


class TestStaTableRuntime:
    def test_layers_sorted_by_priority(self):
        rt = StaTableRuntime(
            [ApplicationController(), DriverInputController()],
            {},
        )
        assert rt.layers[0].layer_name == "DriverInput"
        assert rt.layers[1].layer_name == "Application"

    def test_layers_by_name(self):
        rt = StaTableRuntime(
            [ApplicationController(), DriverInputController()],
            {},
        )
        assert "DriverInput" in rt.layers_by_name
        assert "Application" in rt.layers_by_name

    def test_globals_are_copied(self):
        g = {"g_system_tick": 0}
        rt = StaTableRuntime([DriverInputController()], g)
        g["g_system_tick"] = 999
        assert rt.globals["g_system_tick"] == 0

    def test_tick_updates_all_layers(self):
        rt = StaTableRuntime([DriverInputController()], {})
        rt.tick(0.0, 0.001, None)
        assert rt.layers[0]._initialized is True

    def test_fire_event_on_layer(self):
        rt = StaTableRuntime([DriverInputController()], {})
        ok = rt.fire_event("DriverInput", "SENSOR_TICK")
        assert ok is True
        assert rt.layers_by_name["DriverInput"].state == "InSensing"

    def test_fire_event_unknown_layer(self):
        rt = StaTableRuntime([DriverInputController()], {})
        ok = rt.fire_event("Nonexistent", "SENSOR_TICK")
        assert ok is False

    def test_queue_event(self):
        rt = StaTableRuntime([DriverInputController()], {})
        ok = rt.queue_event("DriverInput", "SENSOR_TICK")
        assert ok is True
        layer = rt.layers_by_name["DriverInput"]
        assert len(layer.pending_events) == 1

    def test_reset_all_layers(self):
        rt = StaTableRuntime(
            [DriverInputController(), ApplicationController()],
            {},
        )
        rt.fire_event("DriverInput", "SENSOR_TICK")
        rt.fire_event("Application", "START")
        rt.reset()
        assert rt.layers_by_name["DriverInput"].state == "InIdle"
        assert rt.layers_by_name["Application"].state == "Idle"

    def test_get_layer_states(self):
        rt = StaTableRuntime([DriverInputController()], {})
        states = rt.get_layer_states()
        assert states == {"DriverInput": "InIdle"}
