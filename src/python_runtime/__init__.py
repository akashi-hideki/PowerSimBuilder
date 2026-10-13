"""PowerSimBuilder Python runtime.

Control-board I/O + control logic time-domain simulation runtime.
"""

from .component_base import Component
from .logger import configure_japanese_font, dump_csv, plot

# Apply Japanese font configuration once at import time (best-effort).
try:
    configure_japanese_font()
except Exception:
    # Never fail import due to font configuration issues.
    pass
from .simulation_data import SimulationData
from .simulation_runner import SimulationRunner

__all__ = [
    "Component",
    "SimulationData",
    "SimulationRunner",
    "configure_japanese_font",
    "dump_csv",
    "plot",
]
__version__ = "0.2.2"
