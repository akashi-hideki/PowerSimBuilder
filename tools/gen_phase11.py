"""Phase 11 generator: PID / Timer / Counter / StateMachine."""

from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
F = {}

F["src/python_runtime/components/pid_controller.py"] = '''"""PIDController component."""
from __future__ import annotations

from typing import Any

from ..component_base import Component


def _clamp(v: float, lo: float, hi: float) -> float:
    return max(lo, min(hi, v))


class PIDController(Component):
    """Discrete-time PID controller.

    Parameters:
        kp, ki, kd: gains.
        out_min, out_max: output clamp range.
        setpoint: target value (can be overridden via inputs).
        anti_windup: if True, prevent integral windup when saturated.

    Inputs:
        measured: current process value.
        setpoint: optional per-step override.

    Outputs:
        control: clamped controller output.
        error: setpoint - measured.
        p_term, i_term, d_term: individual contributions.
        saturated: True when output was clamped.
    """

    def __init__(self, comp_id: str, comp_type: str = "PIDController") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "kp": 1.0,
            "ki": 0.0,
            "kd": 0.0,
            "out_min": -1e9,
            "out_max": 1e9,
            "setpoint": 0.0,
            "anti_windup": False,
        }
        self.inputs = {"measured": 0.0, "setpoint": None}
        self.outputs = {
            "control": 0.0,
            "error": 0.0,
            "p_term": 0.0,
            "i_term": 0.0,
            "d_term": 0.0,
            "saturated": False,
        }
        self._integral: float = 0.0
        self._prev_error: float = 0.0
        self._initialized: bool = False

    def reset(self) -> None:
        self._integral = 0.0
        self._prev_error = 0.0
        self._initialized = False

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        sp_in = self.inputs.get("setpoint")
        setpoint = float(sp_in) if sp_in is not None else float(
            self.parameters.get("setpoint", 0.0)
        )
        measured = float(self.inputs.get("measured", 0.0))
        kp = float(self.parameters.get("kp", 1.0))
        ki = float(self.parameters.get("ki", 0.0))
        kd = float(self.parameters.get("kd", 0.0))
        out_min = float(self.parameters.get("out_min", -1e9))
        out_max = float(self.parameters.get("out_max", 1e9))
        anti_windup = bool(self.parameters.get("anti_windup", False))

        error = setpoint - measured

        if not self._initialized:
            self._prev_error = error
            self._initialized = True

        p_term = kp * error
        i_term = ki * self._integral
        d_term = kd * (error - self._prev_error) / dt if dt > 0 else 0.0

        raw = p_term + i_term + d_term
        control = _clamp(raw, out_min, out_max)
        saturated = (control != raw)

        if not (anti_windup and saturated):
            self._integral += error * dt

        self._prev_error = error
        self.outputs["control"] = control
        self.outputs["error"] = error
        self.outputs["p_term"] = p_term
        self.outputs["i_term"] = i_term
        self.outputs["d_term"] = d_term
        self.outputs["saturated"] = saturated
'''

F["src/python_runtime/components/timer.py"] = '''"""Timer component: delay / oneshot / interval."""
from __future__ import annotations

from typing import Any

from ..component_base import Component


class Timer(Component):
    """Timer with three modes.

    Parameters:
        mode: "delay" | "oneshot" | "interval".
        duration: seconds.

    Inputs:
        trigger: bool
        reset: bool

    Outputs:
        done: bool
        running: bool
        elapsed: float (seconds)

    Modes:
        delay: done becomes True after trigger stays high >= duration.
        oneshot: on rising edge, done stays True for exactly duration.
        interval: while trigger is high, done toggles every duration.
    """

    def __init__(self, comp_id: str, comp_type: str = "Timer") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {"mode": "delay", "duration": 1.0}
        self.inputs = {"trigger": False, "reset": False}
        self.outputs = {"done": False, "running": False, "elapsed": 0.0}
        self._start_t: float | None = None
        self._prev_trigger: bool = False
        self._interval_state: bool = False
        self._last_toggle_t: float | None = None

    def reset(self) -> None:
        self._start_t = None
        self._prev_trigger = False
        self._interval_state = False
        self._last_toggle_t = None
        self.outputs = {"done": False, "running": False, "elapsed": 0.0}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        trigger = bool(self.inputs.get("trigger", False))
        do_reset = bool(self.inputs.get("reset", False))
        mode = str(self.parameters.get("mode", "delay"))
        duration = float(self.parameters.get("duration", 1.0))

        if do_reset:
            self.reset()
            self._prev_trigger = trigger
            return

        if mode == "delay":
            if trigger:
                if self._start_t is None:
                    self._start_t = t
                elapsed = t - self._start_t
                done = elapsed >= duration
                self.outputs["elapsed"] = elapsed
                self.outputs["running"] = not done
                self.outputs["done"] = done
            else:
                self._start_t = None
                self.outputs["elapsed"] = 0.0
                self.outputs["running"] = False
                self.outputs["done"] = False

        elif mode == "oneshot":
            rising = trigger and not self._prev_trigger
            if rising:
                self._start_t = t
            if self._start_t is not None:
                elapsed = t - self._start_t
                if elapsed < duration:
                    self.outputs["done"] = True
                    self.outputs["running"] = True
                    self.outputs["elapsed"] = elapsed
                else:
                    self.outputs["done"] = False
                    self.outputs["running"] = False
                    self.outputs["elapsed"] = duration
                    self._start_t = None
            else:
                self.outputs["done"] = False
                self.outputs["running"] = False
                self.outputs["elapsed"] = 0.0

        elif mode == "interval":
            if trigger:
                if self._last_toggle_t is None:
                    self._last_toggle_t = t
                    self._interval_state = True
                elif (t - self._last_toggle_t) >= duration:
                    self._last_toggle_t = t
                    self._interval_state = not self._interval_state
                self.outputs["done"] = self._interval_state
                self.outputs["running"] = True
                self.outputs["elapsed"] = t - self._last_toggle_t
            else:
                self._last_toggle_t = None
                self._interval_state = False
                self.outputs["done"] = False
                self.outputs["running"] = False
                self.outputs["elapsed"] = 0.0

        self._prev_trigger = trigger
'''

F["src/python_runtime/components/counter.py"] = '''"""Counter component."""
from __future__ import annotations

from typing import Any

from ..component_base import Component


class Counter(Component):
    """Up/down counter with overflow and underflow.

    Parameters:
        min_count, max_count: bounds.
        initial: starting count.
        direction: "up" | "down" | "updown".
        wrap: if True, wrap around on limit; else saturate.

    Inputs:
        clock: bool (rising edge increments/decrements).
        reset: bool.

    Outputs:
        count: int
        overflow: bool
        underflow: bool
    """

    def __init__(self, comp_id: str, comp_type: str = "Counter") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "min_count": 0,
            "max_count": 10,
            "initial": 0,
            "direction": "up",
            "wrap": False,
        }
        self.inputs = {"clock": False, "reset": False}
        self.outputs = {"count": 0, "overflow": False, "underflow": False}
        self._prev_clock: bool = False

    def reset(self) -> None:
        init = int(self.parameters.get("initial", 0))
        self.outputs["count"] = init
        self.outputs["overflow"] = False
        self.outputs["underflow"] = False
        self._prev_clock = False

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        clock = bool(self.inputs.get("clock", False))
        do_reset = bool(self.inputs.get("reset", False))

        if do_reset:
            self.reset()
            return

        rising = clock and not self._prev_clock
        self._prev_clock = clock

        if not rising:
            return

        self.outputs["overflow"] = False
        self.outputs["underflow"] = False

        count = int(self.outputs.get("count", 0))
        mn = int(self.parameters.get("min_count", 0))
        mx = int(self.parameters.get("max_count", 10))
        direction = str(self.parameters.get("direction", "up"))
        wrap = bool(self.parameters.get("wrap", False))

        if direction == "down":
            count -= 1
            if count < mn:
                if wrap:
                    count = mx
                else:
                    count = mn
                    self.outputs["underflow"] = True
        else:
            count += 1
            if count > mx:
                if wrap:
                    count = mn
                else:
                    count = mx
                    self.outputs["overflow"] = True

        self.outputs["count"] = count
'''

F["src/python_runtime/components/state_machine.py"] = '''"""StateMachine component."""
from __future__ import annotations

from typing import Any

from ..component_base import Component


class StateMachine(Component):
    """Finite state machine driven by events.

    Parameters:
        initial_state: str
        transitions: dict mapping "state:event" -> next_state.
        states: optional list of known states.

    Inputs:
        event: str or None
        reset: bool

    Outputs:
        current_state: str
        prev_state: str
        transitioned: bool
    """

    def __init__(self, comp_id: str, comp_type: str = "StateMachine") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "initial_state": "IDLE",
            "transitions": {},
            "states": ["IDLE"],
        }
        self.inputs = {"event": None, "reset": False}
        self.outputs = {
            "current_state": "IDLE",
            "prev_state": "IDLE",
            "transitioned": False,
        }
        self._initialized: bool = False

    def reset(self) -> None:
        init = str(self.parameters.get("initial_state", "IDLE"))
        self.outputs["current_state"] = init
        self.outputs["prev_state"] = init
        self.outputs["transitioned"] = False
        self._initialized = True

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        do_reset = bool(self.inputs.get("reset", False))
        if do_reset:
            self.reset()
            return

        if not self._initialized:
            init = str(self.parameters.get("initial_state", "IDLE"))
            self.outputs["current_state"] = init
            self.outputs["prev_state"] = init
            self._initialized = True

        current = str(self.outputs.get("current_state", "IDLE"))
        event = self.inputs.get("event")

        self.outputs["transitioned"] = False
        self.outputs["prev_state"] = current

        if event is None or event == "":
            return

        transitions = self.parameters.get("transitions", {}) or {}
        key = f"{current}:{event}"
        if key in transitions:
            nxt = str(transitions[key])
            self.outputs["prev_state"] = current
            self.outputs["current_state"] = nxt
            self.outputs["transitioned"] = True
'''

F["tests/python/test_pid_controller.py"] = '''"""Unit tests for PIDController."""
import pytest

from python_runtime.components import PIDController


def _new(**params):
    c = PIDController("pid1")
    c.parameters.update(params)
    return c


def test_proportional_only():
    c = _new(kp=2.0, ki=0.0, kd=0.0, setpoint=1.0)
    c.inputs["measured"] = 0.0
    c.update(0.0, 0.1, None)
    assert c.outputs["error"] == pytest.approx(1.0)
    assert c.outputs["control"] == pytest.approx(2.0)


def test_integral_accumulates():
    c = _new(kp=0.0, ki=1.0, kd=0.0, setpoint=1.0)
    c.inputs["measured"] = 0.0

    c.update(0.0, 0.1, None)
    assert c.outputs["i_term"] == pytest.approx(0.0)

    c.update(0.1, 0.1, None)
    assert c.outputs["i_term"] == pytest.approx(0.1)

    c.update(0.2, 0.1, None)
    assert c.outputs["i_term"] == pytest.approx(0.2)


def test_derivative_step_response():
    c = _new(kp=0.0, ki=0.0, kd=1.0, setpoint=1.0)
    c.inputs["measured"] = 0.0

    c.update(0.0, 0.1, None)
    assert c.outputs["d_term"] == pytest.approx(0.0)

    c.inputs["measured"] = 1.0
    c.update(0.1, 0.1, None)
    # error goes 1 -> 0, derivative = (0-1)/0.1 = -10
    assert c.outputs["d_term"] == pytest.approx(-10.0)


def test_output_clamping():
    c = _new(kp=10.0, out_min=-1.0, out_max=1.0, setpoint=1.0)
    c.inputs["measured"] = 0.0
    c.update(0.0, 0.1, None)
    assert c.outputs["control"] == 1.0
    assert c.outputs["saturated"] is True
    assert c.outputs["p_term"] == pytest.approx(10.0)


def test_no_saturation():
    c = _new(kp=0.5, out_min=-1.0, out_max=1.0, setpoint=1.0)
    c.inputs["measured"] = 0.0
    c.update(0.0, 0.1, None)
    assert c.outputs["control"] == pytest.approx(0.5)
    assert c.outputs["saturated"] is False


def test_anti_windup_blocks_integral():
    c = _new(kp=10.0, ki=1.0, out_min=-1.0, out_max=1.0,
             anti_windup=True, setpoint=1.0)
    c.inputs["measured"] = 0.0
    c.update(0.0, 0.1, None)
    # saturated, so integral not accumulated
    assert c.outputs["saturated"] is True
    c.update(0.1, 0.1, None)
    assert c.outputs["i_term"] == pytest.approx(0.0)


def test_setpoint_input_override():
    c = _new(kp=1.0, setpoint=0.0)
    c.inputs["setpoint"] = 2.0
    c.inputs["measured"] = 0.0
    c.update(0.0, 0.1, None)
    assert c.outputs["error"] == pytest.approx(2.0)
'''

F["tests/python/test_timer.py"] = '''"""Unit tests for Timer."""
from python_runtime.components import Timer


def _new(**params):
    c = Timer("tmr1")
    c.parameters.update(params)
    return c


def test_delay_mode_fires_after_duration():
    c = _new(mode="delay", duration=0.5)
    c.inputs["trigger"] = True
    c.update(0.0, 0.1, None)
    assert c.outputs["done"] is False
    c.update(0.3, 0.1, None)
    assert c.outputs["done"] is False
    c.update(0.5, 0.1, None)
    assert c.outputs["done"] is True


def test_delay_mode_resets_when_trigger_low():
    c = _new(mode="delay", duration=0.5)
    c.inputs["trigger"] = True
    c.update(0.0, 0.1, None)
    c.update(0.4, 0.1, None)
    assert c.outputs["done"] is False

    c.inputs["trigger"] = False
    c.update(0.5, 0.1, None)
    assert c.outputs["done"] is False
    assert c.outputs["elapsed"] == 0.0


def test_oneshot_stays_on_for_duration():
    c = _new(mode="oneshot", duration=0.5)
    # rising edge
    c.inputs["trigger"] = True
    c.update(0.0, 0.1, None)
    assert c.outputs["done"] is True

    c.update(0.4, 0.1, None)
    assert c.outputs["done"] is True

    c.update(0.5, 0.1, None)
    assert c.outputs["done"] is False


def test_oneshot_ignores_long_trigger():
    c = _new(mode="oneshot", duration=0.3)
    c.inputs["trigger"] = True
    c.update(0.0, 0.1, None)
    c.update(0.4, 0.1, None)
    assert c.outputs["done"] is False
    # still True on trigger, but oneshot already finished
    c.update(0.5, 0.1, None)
    assert c.outputs["done"] is False


def test_interval_toggles():
    c = _new(mode="interval", duration=0.2)
    c.inputs["trigger"] = True
    c.update(0.0, 0.1, None)
    assert c.outputs["done"] is True
    c.update(0.1, 0.1, None)
    assert c.outputs["done"] is True
    c.update(0.2, 0.1, None)
    assert c.outputs["done"] is False
    c.update(0.4, 0.1, None)
    assert c.outputs["done"] is True


def test_reset_input_clears_state():
    c = _new(mode="delay", duration=0.5)
    c.inputs["trigger"] = True
    c.update(0.0, 0.1, None)
    c.inputs["reset"] = True
    c.update(0.1, 0.1, None)
    assert c.outputs["elapsed"] == 0.0
    assert c.outputs["done"] is False
'''

F["tests/python/test_counter.py"] = '''"""Unit tests for Counter."""
from python_runtime.components import Counter


def _new(**params):
    c = Counter("cnt1")
    c.parameters.update(params)
    return c


def _tick(c, t):
    c.inputs["clock"] = True
    c.update(t, 0.1, None)
    c.inputs["clock"] = False
    c.update(t + 0.01, 0.1, None)


def test_count_up():
    c = _new(max_count=5)
    c.reset()
    _tick(c, 0.0)
    assert c.outputs["count"] == 1
    _tick(c, 0.1)
    assert c.outputs["count"] == 2


def test_overflow_at_max():
    c = _new(max_count=2)
    c.reset()
    _tick(c, 0.0)
    _tick(c, 0.1)
    _tick(c, 0.2)
    assert c.outputs["count"] == 2
    assert c.outputs["overflow"] is True


def test_overflow_with_wrap():
    c = _new(max_count=2, min_count=0, wrap=True)
    c.reset()
    _tick(c, 0.0)
    _tick(c, 0.1)
    _tick(c, 0.2)
    assert c.outputs["count"] == 0
    assert c.outputs["overflow"] is False


def test_count_down():
    c = _new(direction="down", min_count=0, initial=2)
    c.reset()
    _tick(c, 0.0)
    assert c.outputs["count"] == 1
    _tick(c, 0.1)
    assert c.outputs["count"] == 0


def test_underflow():
    c = _new(direction="down", min_count=0, initial=0)
    c.reset()
    _tick(c, 0.0)
    assert c.outputs["count"] == 0
    assert c.outputs["underflow"] is True


def test_reset_input():
    c = _new(max_count=5, initial=0)
    c.reset()
    _tick(c, 0.0)
    _tick(c, 0.1)
    assert c.outputs["count"] == 2
    c.inputs["reset"] = True
    c.update(0.2, 0.1, None)
    c.inputs["reset"] = False
    assert c.outputs["count"] == 0


def test_no_count_without_rising_edge():
    c = _new()
    c.reset()
    c.inputs["clock"] = True
    c.update(0.0, 0.1, None)
    c.update(0.1, 0.1, None)  # still True, no rising edge
    assert c.outputs["count"] == 1
'''

F["tests/python/test_state_machine.py"] = '''"""Unit tests for StateMachine."""
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
