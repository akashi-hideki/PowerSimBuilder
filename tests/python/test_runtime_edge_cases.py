"""Edge case tests: error paths + boundary conditions.

Complement the 98% line coverage with quality-focused tests:
- ControllerBox error branches (_load_runtime: FileNotFoundError / ImportError / AttributeError)
- InductionMotor boundary conditions (zero freq / zero voltage / zero inertia)
- ControllerBox demo_oven event firing (overheat / door open)
"""

from __future__ import annotations

from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
_GENERATED_CANDIDATES = [
    ROOT / "generated" / "controller_box_powersim.py",
    ROOT / "tests" / "data" / "scenarios" / "cooking_heater" / "controller_box.py",
]
GENERATED = next(
    (p for p in _GENERATED_CANDIDATES if p.is_file()),
    _GENERATED_CANDIDATES[0],
)


# ============================================================================
# ControllerBox: error paths in _load_runtime (L221, L230, L235)
# ============================================================================


class TestControllerBoxErrorPaths:
    def test_missing_generated_file_raises_filenotfound(self):
        """L221: nonexistent generated_path -> FileNotFoundError on first update."""
        from python_runtime import SimulationData
        from python_runtime.components import ControllerBox

        box = ControllerBox("cb_missing", generated_path="nonexistent_xyz_12345.py")
        with pytest.raises(FileNotFoundError):
            box.update(0.0, 0.001, SimulationData(time_step=0.001))

    def test_invalid_python_module_raises(self, tmp_path):
        """L230/L232: bad module -> ImportError/SyntaxError from exec_module."""
        from python_runtime import SimulationData
        from python_runtime.components import ControllerBox

        bad = tmp_path / "bad_syntax.py"
        # Actual newline inside the source (not literal backslash-n)
        bad.write_text("def broken(\n    pass\n", encoding="utf-8")
        box = ControllerBox("cb_bad", generated_path=str(bad))
        with pytest.raises((ImportError, SyntaxError)):
            box.update(0.0, 0.001, SimulationData(time_step=0.001))

    def test_missing_build_runtime_raises_attributeerror(self, tmp_path):
        """L235: module without build_runtime() -> AttributeError."""
        from python_runtime import SimulationData
        from python_runtime.components import ControllerBox

        no_build = tmp_path / "no_build.py"
        # Actual newline (not literal backslash-n)
        no_build.write_text("x = 1\n", encoding="utf-8")
        box = ControllerBox("cb_nobuild", generated_path=str(no_build))
        with pytest.raises(AttributeError):
            box.update(0.0, 0.001, SimulationData(time_step=0.001))


# ============================================================================
# InductionMotor: boundary conditions (L79, L95, L101)
# ============================================================================


class TestInductionMotorBoundaries:
    def _make_motor(self):
        from python_runtime.components import InductionMotor

        return InductionMotor("im_edge")

    def test_zero_frequency_sets_slip_to_one(self):
        """L79: omega_sync == 0 -> slip = 1.0."""
        from python_runtime import SimulationData

        motor = self._make_motor()
        motor.parameters["frequency"] = 0.0
        motor.inputs["voltage"] = 200.0
        motor.update(0.0, 0.001, SimulationData(time_step=0.001))
        assert motor.outputs["slip"] == pytest.approx(1.0)

    def test_standstill_torque_is_positive_and_finite(self):
        """v0.5.0 regression: slip=1.0 must produce finite nonzero torque."""
        from python_runtime import SimulationData

        motor = self._make_motor()
        motor.parameters["initial_speed"] = 0.0
        motor.parameters["frequency"] = 50.0
        motor.inputs["voltage"] = float(motor.parameters.get("rated_voltage", 200.0))
        motor.update(0.0, 0.001, SimulationData(time_step=0.001))

        torque = motor.outputs["torque"]
        assert torque > 0.0, f"standstill torque should be > 0, got {torque}"
        assert torque == torque, "torque must not be NaN"

    def test_zero_voltage_produces_zero_torque(self):
        """L95: v_ratio = 0 -> torque = 0."""
        from python_runtime import SimulationData

        motor = self._make_motor()
        motor.inputs["voltage"] = 0.0
        motor.update(0.0, 0.001, SimulationData(time_step=0.001))
        assert motor.outputs["torque"] == pytest.approx(0.0)

    def test_zero_inertia_does_not_advance_speed(self):
        """L101: alpha = 0 when inertia == 0 -> speed stays at initial."""
        from python_runtime import SimulationData

        motor = self._make_motor()
        motor.parameters["inertia"] = 0.0
        motor.parameters["initial_speed"] = 0.0
        motor.inputs["voltage"] = 200.0
        motor.update(0.0, 0.001, SimulationData(time_step=0.001))
        assert motor.outputs["speed"] == pytest.approx(0.0)


# ============================================================================
# Generator: zero torque -> zero voltage (L62)
# ============================================================================


class TestGeneratorBoundaries:
    def test_zero_torque_produces_zero_voltage(self):
        """L62: torque <= 0 -> v = 0.0."""
        from python_runtime import SimulationData
        from python_runtime.components import Generator

        gen = Generator("gen_edge")
        gen.inputs["torque"] = 0.0
        gen.update(0.0, 0.001, SimulationData(time_step=0.001))
        assert gen.outputs["voltage"] == pytest.approx(0.0)


# ============================================================================
# ControllerBox: demo_oven event paths (L205-211)
# ============================================================================


@pytest.mark.skipif(
    not GENERATED.is_file(),
    reason="generated/controller_box_powersim.py not available",
)
class TestControllerBoxDemoEvents:
    def _make_ready_box(self):
        """Return a ControllerBox warmed up once so _globals is initialized."""
        from python_runtime import SimulationData
        from python_runtime.components import ControllerBox

        box = ControllerBox("cb_demo", generated_path=str(GENERATED))
        box.parameters["auto_events"] = "demo_oven"
        box.update(0.0, 0.001, SimulationData(time_step=0.001))
        return box

    def test_overheat_event_fires_when_thermistor_exceeds_threshold(self):
        """L205-207: thermistor_top > 250 -> OV_OVERHEAT fires once."""
        from python_runtime import SimulationData

        box = self._make_ready_box()
        # Set both input and _globals to bypass mapping-order ambiguity
        box.inputs["thermistor_top"] = 260.0
        box._globals["thermistor_top"] = 260.0
        box.update(0.001, 0.001, SimulationData(time_step=0.001))
        assert box._demo_state["overheat_fired"] is True

    def test_overheat_event_does_not_fire_below_threshold(self):
        """L205: thermistor_top <= 250 -> no OV_OVERHEAT."""
        from python_runtime import SimulationData

        box = self._make_ready_box()
        box.inputs["thermistor_top"] = 240.0
        box._globals["thermistor_top"] = 240.0
        box.update(0.001, 0.001, SimulationData(time_step=0.001))
        assert box._demo_state["overheat_fired"] is False

    def test_door_open_event_fires_when_door_is_open(self):
        """L209-211: door_open == 1 -> DOOR_OPEN fires once."""
        from python_runtime import SimulationData

        box = self._make_ready_box()
        box.inputs["door_open"] = 1
        box._globals["door_open"] = 1
        box.update(0.001, 0.001, SimulationData(time_step=0.001))
        assert box._demo_state["door_fired"] is True
