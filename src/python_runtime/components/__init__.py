"""I/O, logic, source, thermal, and control components.

This module re-exports all component classes available in the
PowerSimBuilder Python runtime.
"""

import importlib.util as _importlib_util
import sys as _sys
from pathlib import Path as _Path
from typing import Any as _Any

from .analog_input import AnalogInput
from .analog_output import AnalogOutput
from .clamp import Clamp
from .counter import Counter
from .digital_input import DigitalInput
from .digital_output import DigitalOutput
from .diode_bridge import DiodeBridge
from .engine import Engine
from .fault_device import FaultDevice
from .first_order_lag import FirstOrderLag
from .gain import Gain
from .generator import Generator
from .heater_block import HeaterBlock
from .induction_motor import InductionMotor
from .integrator import Integrator
from .low_pass_filter import LowPassFilter
from .mechanical_inertia import MechanicalInertia
from .oven_chamber import OvenChamber
from .over_current_detector import OverCurrentDetector
from .pid_controller import PIDController
from .power_source import PowerSource
from .pwm_3phase import PWM3Phase
from .rate_limiter import RateLimiter
from .sine_wave import SineWave
from .sine_wave_sum import SineWaveSum
from .state_machine import StateMachine
from .sum_block import Sum
from .thermal_mass import ThermalMass
from .three_phase_sine import ThreePhaseSine
from .timer import Timer

__all__ = [
    "AnalogInput",
    "AnalogOutput",
    "Clamp",
    "Counter",
    "DigitalInput",
    "DigitalOutput",
    "DiodeBridge",
    "FaultDevice",
    "Engine",
    "FirstOrderLag",
    "Gain",
    "Generator",
    "HeaterBlock",
    "InductionMotor",
    "Integrator",
    "LowPassFilter",
    "MechanicalInertia",
    "OverCurrentDetector",
    "OvenChamber",
    "PIDController",
    "PowerSource",
    "PWM3Phase",
    "RateLimiter",
    "SineWave",
    "SineWaveSum",
    "StateMachine",
    "Sum",
    "ThermalMass",
    "ThreePhaseSine",
    "Timer",
]
from .controller_box import ControllerBox  # noqa: E402,F401

__all__.append("ControllerBox")


# --- Override mechanism (user_components/) -------------------------------
# Bundled components are read-only. Project-specific changes must live in
# <cwd>/user_components/<Name>.py. This loader prefers the override.


def _user_components_dir() -> _Path | None:
    d = _Path.cwd() / "user_components"
    return d if d.is_dir() else None


def _load_user(name: str) -> _Any | None:
    d = _user_components_dir()
    if d is None:
        return None
    f = d / f"{name}.py"
    if not f.is_file():
        return None
    spec = _importlib_util.spec_from_file_location(f"_user_{name}", f)
    if spec is None or spec.loader is None:
        return None
    mod = _importlib_util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    if not hasattr(mod, name):
        raise AttributeError(f"user_components/{name}.py must define class '{name}'")
    return getattr(mod, name)


def load(name: str) -> _Any:
    """Return override from user_components/ if present, else bundled."""
    impl = _load_user(name)
    if impl is not None:
        return impl
    if name not in __all__:
        raise AttributeError(f"Unknown component: {name}")
    return globals()[name]


def scaffold_override(name: str) -> _Path:
    """Copy bundled <name>.py into <cwd>/user_components/ with every line commented out.

    The generated file preserves line numbers, so diffs against the bundled
    source stay readable. Refuses to overwrite an existing override.
    """
    if name not in __all__:
        raise AttributeError(f"Unknown component: {name}")
    cls = globals()[name]
    src = _Path(_sys.modules[cls.__module__].__file__ or "")
    dst_dir = _Path.cwd() / "user_components"
    dst_dir.mkdir(parents=True, exist_ok=True)
    dst = dst_dir / f"{name}.py"
    if dst.exists():
        raise FileExistsError(f"Override already exists: {dst}")

    lines = src.read_text(encoding="utf-8").splitlines()
    header = [
        f"# override: {src.name}",
        "# Bundled source is read-only. Only the lines you uncomment take effect.",
        "# Keep the rest commented so diffs against the bundled file remain minimal.",
        "",
    ]
    body = [f"# {line}" for line in lines]
    dst.write_text("\n".join(header + body) + "\n", encoding="utf-8")
    return dst


__all__ = list(__all__) + ["load", "scaffold_override"]
