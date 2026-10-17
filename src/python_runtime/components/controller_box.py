"""ControllerBox: adapter for StaTable-generated controller.

Wraps a ``StaTableRuntime`` (7 layers) as a single ``Component`` so it can
be placed on the Canvas and connected to other components.

The generated code (``generated/controller_box_powersim.py``) provides a
``build_runtime()`` factory. This adapter:

1. Loads that factory (via importlib from a configurable path).
2. Maps ``inputs``  -> shared ``globals`` dict (read by RoleFunctions).
3. Calls ``runtime.tick(t, dt, sim_data)`` (drives all 7 layers).
4. Maps shared ``globals`` -> ``outputs`` (for downstream components).
"""

from __future__ import annotations

import importlib.util
from pathlib import Path
from typing import Any

from ..component_base import Component

# Default global variable names that are exposed as ControllerBox ports.
DEFAULT_INPUT_GLOBALS = [
    "thermistor_top",
    "thermistor_bottom",
    "thermistor_middle",
    "ir_value",
    "steam_sensor",
    "door_open",
    "mw_anode_current",
    "rx_buf",
    "seq_total",
    "stage_duration",
    "steam_target_temp",
    "mw_target_power",
    "oven_target_temp",
]

DEFAULT_OUTPUT_GLOBALS = [
    "heater_top_duty",
    "heater_middle_duty",
    "heater_bottom_duty",
    "fan_duty",
    "pump_on",
    "mag_on",
    "mw_power",
    "error_code",
    "seq_step",
    "stage_mode",
    "boiler_temp",
]


class ControllerBox(Component):
    """StaTable controller wrapper as a single Canvas component.

    Parameters:
        generated_path (str | None): path to the generated Python module.
            If None, tries ``<cwd>/generated/controller_box_powersim.py``.
        input_globals (list[str] | None): global variable names to expose
            as ``inputs`` (defaults to ``DEFAULT_INPUT_GLOBALS``).
        output_globals (list[str] | None): global variable names to expose
            as ``outputs`` (defaults to ``DEFAULT_OUTPUT_GLOBALS``).
    """

    def __init__(
        self,
        comp_id: str,
        comp_type: str = "ControllerBox",
        *,
        generated_path: str | None = None,
        input_globals: list[str] | None = None,
        output_globals: list[str] | None = None,
    ) -> None:
        super().__init__(comp_id, comp_type)

        self.parameters = {
            "generated_path": str(generated_path or ""),
        }

        self._input_globals = list(input_globals) if input_globals else list(DEFAULT_INPUT_GLOBALS)
        self._output_globals = (
            list(output_globals) if output_globals else list(DEFAULT_OUTPUT_GLOBALS)
        )

        self._runtime = self._load_runtime(generated_path)
        self._globals = self._runtime.globals

        # External ports: initialize from current globals (defaults)
        self.inputs = {name: self._globals.get(name, 0) for name in self._input_globals}
        self.outputs = {name: self._globals.get(name, 0) for name in self._output_globals}

        # Metadata for debugging
        self._layer_names = [layer.layer_name for layer in self._runtime.layers]

    # ------------------------------------------------------------------
    # Public API (Component contract)
    # ------------------------------------------------------------------

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        # 1) Reflect inputs into shared globals
        for name in self._input_globals:
            if name in self.inputs:
                self._globals[name] = self.inputs[name]

        # 2) Derive g_system_tick from simulation time (ms)
        #    (Users can override via inputs["g_system_tick"] if desired.)
        if "g_system_tick" in self.inputs:
            self._globals["g_system_tick"] = self.inputs["g_system_tick"]
        else:
            self._globals["g_system_tick"] = int(round(t * 1000.0))

        # 3) Tick the StaTable runtime (drives all 7 layers)
        self._runtime.tick(t, dt, sim_data)

        # 4) Copy back outputs from globals
        for name in self._output_globals:
            self.outputs[name] = self._globals.get(name, 0)

    def reset(self) -> None:
        super().reset()
        # Re-initialize external ports from the (reset) globals
        self.inputs = {name: self._globals.get(name, 0) for name in self._input_globals}
        self.outputs = {name: self._globals.get(name, 0) for name in self._output_globals}
        if hasattr(self._runtime, "reset"):
            self._runtime.reset()

    # ------------------------------------------------------------------
    # Helpers
    # ------------------------------------------------------------------

    def get_layer_states(self) -> dict[str, str]:
        """Return current state of each of the 7 layers (for debugging)."""
        return self._runtime.get_layer_states()

    def _load_runtime(self, generated_path: str | None) -> Any:
        """Load the generated module and return ``build_runtime()``."""
        path = self._resolve_generated_path(generated_path)
        if not path.is_file():
            raise FileNotFoundError(
                f"Generated controller not found: {path}. "
                "Run PowerSimBuilder's --statable-import first."
            )

        spec = importlib.util.spec_from_file_location(
            f"_controller_box_generated_{id(self)}", str(path)
        )
        if spec is None or spec.loader is None:
            raise ImportError(f"Cannot load module from {path}")
        mod = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(mod)

        if not hasattr(mod, "build_runtime"):
            raise AttributeError(f"Generated module {path} has no build_runtime()")
        return mod.build_runtime()

    @staticmethod
    def _resolve_generated_path(generated_path: str | None) -> Path:
        if generated_path:
            return Path(generated_path).resolve()
        # Try a few likely locations
        candidates = [
            Path.cwd() / "generated" / "controller_box_powersim.py",
            Path(__file__).resolve().parents[3] / "generated" / "controller_box_powersim.py",
        ]
        for p in candidates:
            if p.is_file():
                return p
        return candidates[0]
