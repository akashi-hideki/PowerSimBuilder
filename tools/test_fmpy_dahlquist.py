"""Minimal FMPy check: simulate Dahlquist.fmu via simulate_fmu (correct API)."""
from pathlib import Path

from fmpy import simulate_fmu

fmu = Path(__file__).parent.parent / "user_physics_models" / "Dahlquist" / "source" / "Dahlquist.fmu"
print("FMU path:", fmu)
print("Exists:", fmu.exists())

result = simulate_fmu(
    str(fmu),
    start_time=0.0,
    stop_time=2.0,
    output=["x"],
    step_size=0.01,
)

print("dtype names:", result.dtype.names)
print("time samples:", len(result["time"]))
print("x min/max:", result["x"].min(), "/", result["x"].max())