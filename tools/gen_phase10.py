"""Phase 10 generator: write I/O component files with UTF-8 (no BOM)."""
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

FILES = {}

FILES["src/python_runtime/components/__init__.py"] = '''"""I/O components for PowerSimBuilder runtime.

Models digital/analog I/O for control-board simulation.
"""

from .analog_input import AnalogInput
from .analog_output import AnalogOutput
from .digital_input import DigitalInput
from .digital_output import DigitalOutput

__all__ = [
    "AnalogInput",
    "AnalogOutput",
    "DigitalInput",
    "DigitalOutput",
]
'''

FILES["src/python_runtime/components/digital_input.py"] = '''"""DigitalInput component: debounce + edge detection."""
from __future__ import annotations

from typing import Any

from ..component_base import Component


class DigitalInput(Component):
    """Digital input with debounce and edge detection.

    Parameters:
        debounce_ms: debounce time [ms]; 0 disables debounce.
        initial: initial state (0 or 1).

    Inputs:
        state: raw physical input (0 or 1).

    Outputs:
        state: stabilized state after debounce.
        edge_rising: True on rising edge (one step only).
        edge_falling: True on falling edge (one step only).
    """

    def __init__(self, comp_id: str, comp_type: str = "DigitalInput") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {"debounce_ms": 0.0, "initial": 0}
        self.inputs = {"state": 0}
        self.outputs = {"state": 0, "edge_rising": False, "edge_falling": False}
        self._prev_stable: int = 0
        self._pending: int = 0
        self._last_change_t: float = 0.0
        self._initialized: bool = False

    def reset(self) -> None:
        init = int(self.parameters.get("initial", 0))
        self.inputs = {"state": init}
        self.outputs = {"state": init, "edge_rising": False, "edge_falling": False}
        self._prev_stable = init
        self._pending = init
        self._last_change_t = 0.0
        self._initialized = True

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        raw = 1 if self.inputs.get("state", 0) else 0
        debounce_s = float(self.parameters.get("debounce_ms", 0.0)) / 1000.0

        if not self._initialized:
            init = int(self.parameters.get("initial", 0))
            self._prev_stable = init
            self._pending = init
            self._last_change_t = t
            self._initialized = True

        if raw != self._pending:
            self._pending = raw
            self._last_change_t = t

        if debounce_s <= 0.0 or (t - self._last_change_t) >= debounce_s:
            new_stable = self._pending
        else:
            new_stable = self._prev_stable

        self.outputs["edge_rising"] = (self._prev_stable == 0 and new_stable == 1)
        self.outputs["edge_falling"] = (self._prev_stable == 1 and new_stable == 0)
        self._prev_stable = new_stable
        self.outputs["state"] = new_stable
'''

FILES["src/python_runtime/components/digital_output.py"] = '''"""DigitalOutput component: active_low aware."""
from __future__ import annotations

from typing import Any

from ..component_base import Component


class DigitalOutput(Component):
    """Digital output with active_low support.

    Parameters:
        active_low: if True, physical level is inverted.
        initial: initial command (0 or 1).
        on_current_ma: load current when ON [mA].

    Inputs:
        command: logical command (0 or 1).

    Outputs:
        state: physical pin level (inverted if active_low).
        current_draw: load current [mA] (non-zero only when command is 1).
    """

    def __init__(self, comp_id: str, comp_type: str = "DigitalOutput") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {"active_low": False, "initial": 0, "on_current_ma": 20.0}
        self.inputs = {"command": 0}
        self.outputs = {"state": 0, "current_draw": 0.0}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        cmd = 1 if self.inputs.get("command", 0) else 0
        active_low = bool(self.parameters.get("active_low", False))
        on_current = float(self.parameters.get("on_current_ma", 20.0))

        physical = (1 - cmd) if active_low else cmd

        self.outputs["state"] = physical
        self.outputs["current_draw"] = on_current if cmd == 1 else 0.0
'''

FILES["src/python_runtime/components/analog_input.py"] = '''"""AnalogInput component: scaling + first-order filter."""
from __future__ import annotations

from typing import Any

from ..component_base import Component


class AnalogInput(Component):
    """Analog input with scaling and first-order low-pass filter.

    Parameters:
        v_min: input voltage lower bound [V].
        v_max: input voltage upper bound [V].
        resolution: ADC resolution [bit] (informational).
        filter_alpha: first-order filter coefficient (0..1); 0 disables.

    Inputs:
        raw: raw input voltage [V].

    Outputs:
        raw: input passthrough.
        scaled: normalized to 0..1 (clamped).
        filtered: filtered scaled value.
    """

    def __init__(self, comp_id: str, comp_type: str = "AnalogInput") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "v_min": 0.0,
            "v_max": 5.0,
            "resolution": 12,
            "filter_alpha": 0.0,
        }
        self.inputs = {"raw": 0.0}
        self.outputs = {"raw": 0.0, "scaled": 0.0, "filtered": 0.0}
        self._filtered: float | None = None

    def reset(self) -> None:
        self.inputs = {"raw": 0.0}
        self.outputs = {"raw": 0.0, "scaled": 0.0, "filtered": 0.0}
        self._filtered = None

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        raw = float(self.inputs.get("raw", 0.0))
        v_min = float(self.parameters.get("v_min", 0.0))
        v_max = float(self.parameters.get("v_max", 5.0))
        alpha = float(self.parameters.get("filter_alpha", 0.0))

        if v_max == v_min:
            scaled = 0.0
        else:
            scaled = (raw - v_min) / (v_max - v_min)
        scaled = max(0.0, min(1.0, scaled))

        if self._filtered is None:
            self._filtered = scaled
        elif alpha <= 0.0 or alpha >= 1.0:
            self._filtered = scaled
        else:
            self._filtered = alpha * scaled + (1.0 - alpha) * self._filtered

        self.outputs["raw"] = raw
        self.outputs["scaled"] = scaled
        self.outputs["filtered"] = self._filtered
'''

FILES["src/python_runtime/components/analog_output.py"] = '''"""AnalogOutput component: PWM duty conversion."""
from __future__ import annotations

from typing import Any

from ..component_base import Component


class AnalogOutput(Component):
    """Analog output modeled as PWM.

    Parameters:
        v_ref: reference output voltage [V].
        pwm_freq: PWM frequency [Hz] (informational).
        resolution: PWM resolution [bit] (informational).

    Inputs:
        value: target value (0..1, clamped).

    Outputs:
        duty: duty cycle (0..1).
        voltage: v_ref * duty [V].
        active: True when duty > 0.
    """

    def __init__(self, comp_id: str, comp_type: str = "AnalogOutput") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {"v_ref": 5.0, "pwm_freq": 1000.0, "resolution": 10}
        self.inputs = {"value": 0.0}
        self.outputs = {"duty": 0.0, "voltage": 0.0, "active": False}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        v = float(self.inputs.get("value", 0.0))
        duty = max(0.0, min(1.0, v))
        v_ref = float(self.parameters.get("v_ref", 5.0))

        self.outputs["duty"] = duty
        self.outputs["voltage"] = v_ref * duty
        self.outputs["active"] = duty > 0.0
'''

FILES["tests/python/test_digital_input.py"] = '''"""Unit tests for DigitalInput."""
from python_runtime.components import DigitalInput


def _new(**params):
    c = DigitalInput("din1")
    c.parameters.update(params)
    return c


def test_initial_state_zero():
    c = _new()
    c.update(0.0, 0.001, None)
    assert c.outputs["state"] == 0
    assert c.outputs["edge_rising"] is False
    assert c.outputs["edge_falling"] is False


def test_rising_edge_no_debounce():
    c = _new()
    c.inputs["state"] = 0
    c.update(0.0, 0.001, None)
    assert c.outputs["state"] == 0

    c.inputs["state"] = 1
    c.update(0.1, 0.001, None)
    assert c.outputs["state"] == 1
    assert c.outputs["edge_rising"] is True
    assert c.outputs["edge_falling"] is False


def test_rising_edge_is_one_shot():
    c = _new()
    c.inputs["state"] = 1
    c.update(0.1, 0.001, None)
    assert c.outputs["edge_rising"] is True

    c.update(0.2, 0.001, None)
    assert c.outputs["edge_rising"] is False
    assert c.outputs["state"] == 1


def test_falling_edge_no_debounce():
    c = _new(initial=1)
    c.reset()
    c.inputs["state"] = 1
    c.update(0.0, 0.001, None)

    c.inputs["state"] = 0
    c.update(0.1, 0.001, None)
    assert c.outputs["state"] == 0
    assert c.outputs["edge_falling"] is True


def test_debounce_filters_short_pulse():
    c = _new(debounce_ms=10.0)
    c.inputs["state"] = 0
    c.update(0.0, 0.001, None)

    c.inputs["state"] = 1
    c.update(0.001, 0.001, None)
    c.inputs["state"] = 0
    c.update(0.002, 0.001, None)

    assert c.outputs["state"] == 0


def test_debounce_accepts_long_pulse():
    c = _new(debounce_ms=10.0)
    c.inputs["state"] = 0
    c.update(0.0, 0.001, None)

    c.inputs["state"] = 1
    c.update(0.001, 0.001, None)
    c.update(0.006, 0.001, None)
    assert c.outputs["state"] == 0

    c.update(0.012, 0.001, None)
    assert c.outputs["state"] == 1
    assert c.outputs["edge_rising"] is True
'''

FILES["tests/python/test_digital_output.py"] = '''"""Unit tests for DigitalOutput."""
from python_runtime.components import DigitalOutput


def _new(**params):
    c = DigitalOutput("dout1")
    c.parameters.update(params)
    return c


def test_command_zero_gives_state_zero():
    c = _new()
    c.inputs["command"] = 0
    c.update(0.0, 0.001, None)
    assert c.outputs["state"] == 0
    assert c.outputs["current_draw"] == 0.0


def test_command_one_gives_state_one_active_high():
    c = _new(active_low=False)
    c.inputs["command"] = 1
    c.update(0.0, 0.001, None)
    assert c.outputs["state"] == 1
    assert c.outputs["current_draw"] == 20.0


def test_active_low_inverts_physical_state():
    c = _new(active_low=True)
    c.inputs["command"] = 1
    c.update(0.0, 0.001, None)
    assert c.outputs["state"] == 0
    assert c.outputs["current_draw"] == 20.0


def test_active_low_command_zero():
    c = _new(active_low=True)
    c.inputs["command"] = 0
    c.update(0.0, 0.001, None)
    assert c.outputs["state"] == 1
    assert c.outputs["current_draw"] == 0.0


def test_custom_on_current():
    c = _new(on_current_ma=50.0)
    c.inputs["command"] = 1
    c.update(0.0, 0.001, None)
    assert c.outputs["current_draw"] == 50.0
'''

FILES["tests/python/test_analog_input.py"] = '''"""Unit tests for AnalogInput."""
import pytest

from python_runtime.components import AnalogInput


def _new(**params):
    c = AnalogInput("ain1")
    c.parameters.update(params)
    return c


def test_scaling_midpoint():
    c = _new(v_min=0.0, v_max=5.0)
    c.inputs["raw"] = 2.5
    c.update(0.0, 0.001, None)
    assert c.outputs["scaled"] == pytest.approx(0.5)


def test_scaling_vmin_vmax():
    c = _new(v_min=0.0, v_max=5.0)
    c.inputs["raw"] = 0.0
    c.update(0.0, 0.001, None)
    assert c.outputs["scaled"] == pytest.approx(0.0)

    c.inputs["raw"] = 5.0
    c.update(0.001, 0.001, None)
    assert c.outputs["scaled"] == pytest.approx(1.0)


def test_scaling_clamps_out_of_range():
    c = _new(v_min=0.0, v_max=5.0)
    c.inputs["raw"] = -1.0
    c.update(0.0, 0.001, None)
    assert c.outputs["scaled"] == 0.0

    c.inputs["raw"] = 10.0
    c.update(0.001, 0.001, None)
    assert c.outputs["scaled"] == 1.0


def test_custom_range():
    c = _new(v_min=1.0, v_max=3.0)
    c.inputs["raw"] = 2.0
    c.update(0.0, 0.001, None)
    assert c.outputs["scaled"] == pytest.approx(0.5)


def test_filter_passthrough_alpha_1():
    c = _new(v_min=0.0, v_max=5.0, filter_alpha=1.0)
    c.inputs["raw"] = 0.0
    c.update(0.0, 0.001, None)
    c.inputs["raw"] = 5.0
    c.update(0.001, 0.001, None)
    assert c.outputs["filtered"] == pytest.approx(1.0)


def test_filter_first_order_step_response():
    c = _new(v_min=0.0, v_max=5.0, filter_alpha=0.5)
    c.inputs["raw"] = 0.0
    c.update(0.0, 0.001, None)
    assert c.outputs["filtered"] == pytest.approx(0.0)

    c.inputs["raw"] = 5.0
    c.update(0.001, 0.001, None)
    assert c.outputs["filtered"] == pytest.approx(0.5)

    c.update(0.002, 0.001, None)
    assert c.outputs["filtered"] == pytest.approx(0.75)

    c.update(0.003, 0.001, None)
    assert c.outputs["filtered"] == pytest.approx(0.875)
'''

FILES["tests/python/test_analog_output.py"] = '''"""Unit tests for AnalogOutput."""
import pytest

from python_runtime.components import AnalogOutput


def _new(**params):
    c = AnalogOutput("aout1")
    c.parameters.update(params)
    return c


def test_zero_value():
    c = _new()
    c.inputs["value"] = 0.0
    c.update(0.0, 0.001, None)
    assert c.outputs["duty"] == 0.0
    assert c.outputs["voltage"] == 0.0
    assert c.outputs["active"] is False


def test_half_value():
    c = _new(v_ref=5.0)
    c.inputs["value"] = 0.5
    c.update(0.0, 0.001, None)
    assert c.outputs["duty"] == pytest.approx(0.5)
    assert c.outputs["voltage"] == pytest.approx(2.5)
    assert c.outputs["active"] is True


def test_full_value():
    c = _new(v_ref=3.3)
    c.inputs["value"] = 1.0
    c.update(0.0, 0.001, None)
    assert c.outputs["duty"] == pytest.approx(1.0)
    assert c.outputs["voltage"] == pytest.approx(3.3)


def test_clamps_negative():
    c = _new(v_ref=5.0)
    c.inputs["value"] = -0.5
    c.update(0.0, 0.001, None)
    assert c.outputs["duty"] == 0.0
    assert c.outputs["active"] is False


def test_clamps_above_one():
    c = _new(v_ref=5.0)
    c.inputs["value"] = 2.0
    c.update(0.0, 0.001, None)
    assert c.outputs["duty"] == 1.0
    assert c.outputs["voltage"] == pytest.approx(5.0)
'''


def main() -> None:
    for rel, content in FILES.items():
        p = ROOT / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(content, encoding="utf-8")
        print(f"  + {rel}")
    print()
    print(f"Done. {len(FILES)} files written to {ROOT}")


if __name__ == "__main__":
    main()