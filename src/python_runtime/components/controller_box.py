"""ControllerBox: adapter for StaTable-generated controller.

Wraps a ``StaTableRuntime`` (7 layers) as a single ``Component`` so it can
be placed on the Canvas and connected to other components.

The generated code (``generated/controller_box_powersim.py``) provides a
``build_runtime()`` factory. This adapter:

1. Lazily loads that factory on first update() (via importlib).
2. Maps ``inputs``  -> shared ``globals`` dict (read by RoleFunctions).
3. Calls ``runtime.tick(t, dt, sim_data)`` (drives all 7 layers).
4. Maps shared ``globals`` -> ``outputs`` (for downstream components).

Lazy loading is required because the generated main.py sets
``components["id"].parameters["generated_path"]`` AFTER construction.
"""

from __future__ import annotations

import importlib.util
from pathlib import Path
from typing import Any

from ..component_base import Component

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

    Parameters (constructor kwargs):
        generated_path: path to the generated Python module.
            If empty/None, auto-discovery is attempted on first update().
        input_globals / output_globals: override the default port lists.

    Runtime parameters (set via ``self.parameters`` after construction):
        generated_path: same as above; read lazily on first update().
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
            "auto_events": "",  # "" = disabled; "demo_oven" = enable demo driver
        }
        self._demo_state: dict[str, bool] = {
            "startup_fired": False,
            "overheat_fired": False,
            "door_fired": False,
        }

        self._input_globals = list(input_globals) if input_globals else list(DEFAULT_INPUT_GLOBALS)
        self._output_globals = (
            list(output_globals) if output_globals else list(DEFAULT_OUTPUT_GLOBALS)
        )

        # Lazy loading: runtime is None until _ensure_runtime() is called
        self._runtime: Any = None
        self._globals: dict[str, Any] = {}
        self._layer_names: list[str] = []

        # Initialize ports with default 0; refreshed after runtime load
        self.inputs = {name: 0 for name in self._input_globals}
        self.outputs = {name: 0 for name in self._output_globals}

    # ------------------------------------------------------------------
    # Lazy runtime loading
    # ------------------------------------------------------------------

    def _ensure_runtime(self) -> None:
        """Load runtime on first access (lazy)."""
        if self._runtime is not None:
            return
        path_str = self.parameters.get("generated_path", "")
        path = path_str.strip() if path_str else None
        self._runtime = self._load_runtime(path)
        assert self._runtime is not None  # for mypy
        self._globals = self._runtime.globals
        self._layer_names = [layer.layer_name for layer in self._runtime.layers]
        # Refresh ports from actual globals
        self.inputs = {name: self._globals.get(name, 0) for name in self._input_globals}
        self.outputs = {name: self._globals.get(name, 0) for name in self._output_globals}

    # ------------------------------------------------------------------
    # Public API (Component contract)
    # ------------------------------------------------------------------

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        self._ensure_runtime()
        assert self._runtime is not None  # for mypy

        # 1) Reflect inputs into shared globals
        for name in self._input_globals:
            if name in self.inputs:
                self._globals[name] = self.inputs[name]

        # 2) Derive g_system_tick from simulation time (ms)
        if "g_system_tick" in self.inputs:
            self._globals["g_system_tick"] = self.inputs["g_system_tick"]
        else:
            self._globals["g_system_tick"] = int(round(t * 1000.0))

        # 2.5) Auto-fire events (demo driver) BEFORE tick, so transitions run this tick
        self._fire_auto_events()

        # 3) Tick the StaTable runtime (drives all 7 layers)
        self._runtime.tick(t, dt, sim_data)

        # 4) Copy back outputs from globals
        for name in self._output_globals:
            self.outputs[name] = self._globals.get(name, 0)

    def reset(self) -> None:
        super().reset()
        if self._runtime is not None and hasattr(self._runtime, "reset"):
            self._runtime.reset()
        self.inputs = {name: 0 for name in self._input_globals}
        self.outputs = {name: 0 for name in self._output_globals}
        self._demo_state = {
            "startup_fired": False,
            "overheat_fired": False,
            "door_fired": False,
        }

    # ------------------------------------------------------------------
    # Debug helpers
    # ------------------------------------------------------------------

    def get_layer_states(self) -> dict[str, str]:
        """Return current state of each of the 7 layers (for debugging)."""
        self._ensure_runtime()
        assert self._runtime is not None  # for mypy
        return self._runtime.get_layer_states()

    def fire_event(self, layer_name: str, event_name: str, data: Any = None) -> bool:
        """Fire a direct event to a specific layer (passthrough to runtime)."""
        self._ensure_runtime()
        assert self._runtime is not None  # for mypy
        return self._runtime.fire_event(layer_name, event_name, data)

    # ------------------------------------------------------------------
    # Auto-events driver (demo)
    # ------------------------------------------------------------------

    def _fire_auto_events(self) -> None:
        """Fire events based on ``auto_events`` parameter."""
        mode = self.parameters.get("auto_events", "").strip()
        if mode == "demo_oven":
            self._demo_oven_events()

    def _demo_oven_events(self) -> None:
        """Cooking heater demo driver.

        Triggers:
        - Startup: HEAT_START (DriverOutput) + OV_START (MwOven)
        - Overheat: thermistor_top > 250 -> OV_OVERHEAT (MwOven)
        - Door safety: door_open == 1 -> DOOR_OPEN (DriverInput)
        """
        s = self._demo_state
        g = self._globals

        if not s["startup_fired"]:
            self.fire_event("DriverOutput", "HEAT_START")
            self.fire_event("MwOven", "OV_START")
            s["startup_fired"] = True

        if not s["overheat_fired"] and g.get("thermistor_top", 0) > 250:
            self.fire_event("MwOven", "OV_OVERHEAT")
            s["overheat_fired"] = True

        if not s["door_fired"] and g.get("door_open", 0) == 1:
            self.fire_event("DriverInput", "DOOR_OPEN")
            s["door_fired"] = True

    # ------------------------------------------------------------------
    # Helpers
    # ------------------------------------------------------------------

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
        candidates = [
            Path.cwd() / "generated" / "controller_box_powersim.py",
            Path(__file__).resolve().parents[3] / "generated" / "controller_box_powersim.py",
        ]
        for p in candidates:
            if p.is_file():
                return p
        return candidates[0]
