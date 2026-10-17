"""StaTableControllerBase / StaTableRuntime のテスト。"""

from __future__ import annotations

import pytest

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


class ConditionalController(StaTableControllerBase):
    """Phase 2-B-2: condition / action 評価用のテスト層。"""

    LAYER_NAME = "Conditional"
    LAYER_PRIORITY = 3
    INITIAL_STATE = "Start"
    TRANSITIONS = [
        ("Start", "GO", 'self.globals["g_ready"]', None, "Running"),
        ("Start", "GO_FALLBACK", None, None, "Fallback"),
        (
            "Running",
            "STOP",
            None,
            'self.globals["g_count"] = self.globals.get("g_count", 0) + 1',
            "Start",
        ),
    ]
    EVENTS = {
        "GO": {"id": 1, "delivery": "direct"},
        "GO_FALLBACK": {"id": 2, "delivery": "direct"},
        "STOP": {"id": 3, "delivery": "direct"},
    }

    def __init__(self, comp_id="conditional"):
        super().__init__(
            comp_id=comp_id,
            comp_type="ConditionalController",
            layer_name=self.LAYER_NAME,
            layer_priority=self.LAYER_PRIORITY,
            initial_state=self.INITIAL_STATE,
            transitions=self.TRANSITIONS,
            events=self.EVENTS,
        )
        self.f_increment_called = False

    def F_increment(self):  # noqa: N802 - RoleFunction naming convention
        self.f_increment_called = True


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

    def test_update_marks_initialized(self):
        """初回 update() で初期化フラグが立ち、outputs が整う。

        Phase 2-A: update() は state を強制リセットしない。
        （queue 配送で遷移した state を保持するため）
        """
        ctrl = DriverInputController()
        ctrl._initialized = False
        ctrl.update(0.0, 0.001, None)
        assert ctrl._initialized is True
        assert ctrl.state == "InIdle"
        assert ctrl.outputs["current_state"] == "InIdle"

    def test_queue_event_does_not_immediately_transition(self):
        """runtime 未接続時のフォールバック（自分自身のみ）。"""
        ctrl = DriverInputController()
        ctrl.queue_event("SENSOR_TICK")
        assert ctrl.state == "InIdle"
        assert len(ctrl.pending_events) == 1

    def test_update_processes_pending_events(self):
        """runtime 未接続時のフォールバック更新。"""
        ctrl = DriverInputController()
        ctrl.queue_event("SENSOR_TICK")
        ctrl.update(0.0, 0.001, None)
        assert ctrl.state == "InSensing"
        assert ctrl.pending_events == []

    def test_queue_event_cross_layer_without_runtime_raises(self):
        """runtime 未接続で層間配送を要求すると RuntimeError。"""
        ctrl = DriverInputController()
        with pytest.raises(RuntimeError):
            ctrl.queue_event("START", target_layer="Application")

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
        """runtime.queue_event は配送キューに積み、次 tick で配送される。"""
        rt = StaTableRuntime([DriverInputController()], {})
        ok = rt.queue_event("DriverInput", "SENSOR_TICK")
        assert ok is True
        assert rt.pending_event_count == 1
        assert rt.layers_by_name["DriverInput"].pending_events == []
        assert rt.layers_by_name["DriverInput"].state == "InIdle"
        rt.tick(0.0, 0.001, None)
        assert rt.layers_by_name["DriverInput"].state == "InSensing"
        assert rt.pending_event_count == 0

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

    def test_reset_clears_event_queue(self):
        rt = StaTableRuntime([DriverInputController()], {})
        rt.queue_event("DriverInput", "SENSOR_TICK")
        assert rt.pending_event_count == 1
        rt.reset()
        assert rt.pending_event_count == 0

    def test_get_layer_states(self):
        rt = StaTableRuntime([DriverInputController()], {})
        states = rt.get_layer_states()
        assert states == {"DriverInput": "InIdle"}


# ======================================================================
# Phase 2-A: queue イベント層間配送
# ======================================================================


class TestStaTableCrossLayerQueue:
    def test_self_queue_via_runtime(self):
        """runtime 接続時は自分自身への queue も runtime 経由。"""
        rt = StaTableRuntime([DriverInputController()], {})
        driver = rt.layers_by_name["DriverInput"]
        driver.queue_event("SENSOR_TICK")
        assert driver.pending_events == []
        assert rt.pending_event_count == 1
        assert driver.state == "InIdle"
        rt.tick(0.0, 0.001, None)
        assert driver.state == "InSensing"

    def test_cross_layer_delivery(self):
        """DriverInput から Application へ queue 配送。"""
        rt = StaTableRuntime(
            [DriverInputController(), ApplicationController()],
            {},
        )
        driver = rt.layers_by_name["DriverInput"]
        app = rt.layers_by_name["Application"]

        driver.queue_event("START", target_layer="Application")
        assert app.state == "Idle"
        assert rt.pending_event_count == 1

        rt.tick(0.0, 0.001, None)
        assert app.state == "Executing"

    def test_broadcast_delivery(self):
        """target_layer="*" で送信元以外の全層へ配送。"""
        rt = StaTableRuntime(
            [DriverInputController(), ApplicationController()],
            {},
        )
        driver = rt.layers_by_name["DriverInput"]
        app = rt.layers_by_name["Application"]

        driver.queue_event("START", target_layer="*")
        rt.tick(0.0, 0.001, None)
        assert app.state == "Executing"
        assert driver.state == "InIdle"

    def test_cross_layer_delivery_via_runtime_queue_event(self):
        """runtime.queue_event 経由の層間配送。"""
        rt = StaTableRuntime(
            [DriverInputController(), ApplicationController()],
            {},
        )
        rt.enqueue("DriverInput", "START", None, "Application")
        rt.tick(0.0, 0.001, None)
        assert rt.layers_by_name["Application"].state == "Executing"

    def test_delivery_priority_order(self):
        """送信元 priority 昇順で配送される。"""
        rt = StaTableRuntime(
            [ApplicationController(), DriverInputController()],
            {},
        )
        rt.enqueue("Application", "DONE", None, "Application")
        rt.enqueue("DriverInput", "SENSOR_TICK", None, "DriverInput")

        rt.tick(0.0, 0.001, None)
        assert rt.layers_by_name["DriverInput"].state == "InSensing"
        assert rt.layers_by_name["Application"].state == "Idle"


# ======================================================================
# Phase 2-B-2: condition / action eval/exec
# ======================================================================


class TestStaTableConditionActionEval:
    """condition / action の eval/exec のテスト。"""

    # ------------------------------------------------------------------
    # condition
    # ------------------------------------------------------------------

    def test_condition_none_always_true(self):
        ctrl = ConditionalController()
        assert ctrl._evaluate_condition(None, None) is True

    def test_condition_empty_string_always_true(self):
        ctrl = ConditionalController()
        assert ctrl._evaluate_condition("", None) is True
        assert ctrl._evaluate_condition("   ", None) is True

    def test_condition_global_var_true(self):
        ctrl = ConditionalController()
        ctrl.globals["g_ready"] = True
        assert ctrl._evaluate_condition('self.globals["g_ready"]', None) is True

    def test_condition_global_var_false(self):
        ctrl = ConditionalController()
        ctrl.globals["g_ready"] = False
        assert ctrl._evaluate_condition('self.globals["g_ready"]', None) is False

    def test_condition_logical_and(self):
        ctrl = ConditionalController()
        ctrl.globals["a"] = True
        ctrl.globals["b"] = False
        assert ctrl._evaluate_condition('self.globals["a"] and self.globals["b"]', None) is False

    def test_condition_logical_or(self):
        ctrl = ConditionalController()
        ctrl.globals["a"] = False
        ctrl.globals["b"] = True
        assert ctrl._evaluate_condition('self.globals["a"] or self.globals["b"]', None) is True

    def test_condition_comparison(self):
        ctrl = ConditionalController()
        ctrl.globals["g_counter"] = 10
        assert ctrl._evaluate_condition('self.globals["g_counter"] > 5', None) is True
        assert ctrl._evaluate_condition('self.globals["g_counter"] < 5', None) is False

    def test_condition_syntax_error_returns_false(self):
        ctrl = ConditionalController()
        assert ctrl._evaluate_condition("this is not valid python !!!", None) is False
        assert len(ctrl.warnings) == 1

    def test_condition_sandbox_blocks_import(self):
        """__import__ は _SAFE_BUILTINS に無いので NameError。"""
        ctrl = ConditionalController()
        assert ctrl._evaluate_condition('__import__("os")', None) is False
        assert len(ctrl.warnings) == 1

    def test_condition_sandbox_blocks_open(self):
        """open も _SAFE_BUILTINS に無い。"""
        ctrl = ConditionalController()
        assert ctrl._evaluate_condition('open("secret.txt")', None) is False
        assert len(ctrl.warnings) == 1

    # ------------------------------------------------------------------
    # action
    # ------------------------------------------------------------------

    def test_action_none_noop(self):
        ctrl = ConditionalController()
        ctrl._execute_action(None, None)
        assert ctrl.warnings == []

    def test_action_empty_string_noop(self):
        ctrl = ConditionalController()
        ctrl._execute_action("", None)
        ctrl._execute_action("   ", None)
        assert ctrl.warnings == []

    def test_action_assign_global(self):
        ctrl = ConditionalController()
        ctrl.globals["g_count"] = 0
        ctrl._execute_action('self.globals["g_count"] = 42', None)
        assert ctrl.globals["g_count"] == 42

    def test_action_calls_role_function(self):
        ctrl = ConditionalController()
        ctrl._execute_action("self.F_increment()", None)
        assert ctrl.f_increment_called is True

    def test_action_syntax_error_warns(self):
        ctrl = ConditionalController()
        ctrl._execute_action("def broken(:", None)
        assert len(ctrl.warnings) == 1

    # ------------------------------------------------------------------
    # end-to-end
    # ------------------------------------------------------------------

    def test_transition_blocked_by_condition(self):
        ctrl = ConditionalController()
        ctrl.globals["g_ready"] = False
        ok = ctrl.fire_event("GO")
        assert ok is False
        assert ctrl.state == "Start"

    def test_transition_allowed_by_condition(self):
        ctrl = ConditionalController()
        ctrl.globals["g_ready"] = True
        ok = ctrl.fire_event("GO")
        assert ok is True
        assert ctrl.state == "Running"

    def test_action_runs_on_transition(self):
        ctrl = ConditionalController()
        ctrl.globals["g_ready"] = True
        ctrl.globals["g_count"] = 0
        ctrl.fire_event("GO")
        assert ctrl.state == "Running"
        ctrl.fire_event("STOP")
        assert ctrl.state == "Start"
        assert ctrl.globals["g_count"] == 1

    def test_reset_clears_warnings(self):
        ctrl = ConditionalController()
        ctrl._evaluate_condition("bad syntax !!!", None)
        assert len(ctrl.warnings) == 1
        ctrl.reset()
        assert ctrl.warnings == []

    def test_globals_shared_via_runtime(self):
        """runtime binding 後、self.globals は runtime.globals と同一オブジェクト。"""
        rt = StaTableRuntime([ConditionalController()], {"g_ready": True})
        ctrl = rt.layers_by_name["Conditional"]
        assert ctrl.globals is rt.globals
        assert ctrl.globals["g_ready"] is True
