"""Phase 2e v2: StaTableRuntime + External PID + ThermalMass closed loop."""

import sys
from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt

ROOT = Path(__file__).parent.parent.parent
sys.path.insert(0, str(ROOT / "generated"))
sys.path.insert(0, str(ROOT / "src"))

from CookingHeaterController_powersim import build_runtime  # noqa: E402

from python_runtime.components import ThermalMass  # noqa: E402


def try_trigger_oven(rt):
    """Try several APIs to start the oven state machine."""
    mw = rt.layers_by_name["MwOven"]
    # 1) fire_event on runtime
    for args in [("OV_START",), ("OV_START", "MwOven")]:
        try:
            r = rt.fire_event(*args)
            if mw.state != "OvIdle":
                return f"rt.fire_event({args}) -> {r!r}, state={mw.state}"
        except TypeError:
            pass
        except Exception:
            pass
    # 2) fire_event on the layer directly
    if hasattr(mw, "fire_event"):
        try:
            r = mw.fire_event("OV_START")
            if mw.state != "OvIdle":
                return f"mw.fire_event('OV_START') -> {r!r}, state={mw.state}"
        except Exception:
            pass
    # 3) manual state transition (fallback)
    mw.state = "PidHeating"
    return "manual state override -> PidHeating"


rt = build_runtime()
trigger_msg = try_trigger_oven(rt)
print(f"Oven trigger: {trigger_msg}")
print(f"MwOven state: {rt.layers_by_name['MwOven'].state}")


class ExternalPID:
    def __init__(self, kp=0.02, ki=0.001, kd=0.0, out_min=0.0, out_max=1.0):
        self.kp, self.ki, self.kd = kp, ki, kd
        self.out_min, self.out_max = out_min, out_max
        self.integral = 0.0
        self.prev_error = 0.0

    def update(self, target, measured, dt):
        err = target - measured
        self.integral += err * dt
        d = (err - self.prev_error) / dt if dt > 0 else 0.0
        self.prev_error = err
        u = self.kp * err + self.ki * self.integral + self.kd * d
        return max(self.out_min, min(self.out_max, u))


pid = ExternalPID(kp=0.02, ki=0.001, kd=0.0)

plant = ThermalMass("plant")
plant.parameters["thermal_resistance"] = 0.5
plant.parameters["thermal_capacity"] = 200.0
plant.parameters["ambient_temperature"] = 25.0
plant.parameters["initial_temperature"] = 25.0

dt = 0.05
t_end = 300.0
n_steps = int(round(t_end / dt))

times, targets, temps, duties, states = [], [], [], [], []

for i in range(n_steps + 1):
    t = i * dt
    target = 200.0
    current = plant.outputs.get("temperature", 25.0)

    rt.globals["oven_target_temp"] = target
    rt.globals["oven_current_temp"] = current

    duty = pid.update(target, current, dt)
    rt.globals["heater_top_duty"] = duty

    # tick() drives all layers' update()
    try:
        rt.tick()
    except Exception:
        pass

    plant.inputs["heat_flow"] = duty * 1000.0
    plant.update(t, dt, None)

    times.append(t)
    targets.append(target)
    temps.append(plant.outputs.get("temperature", 25.0))
    duties.append(duty)
    states.append(rt.layers_by_name["MwOven"].state)

fig, (ax1, ax2) = plt.subplots(2, 1, figsize=(12, 8), sharex=True)
ax1.plot(times, targets, "k--", label="target", linewidth=1.5)
ax1.plot(times, temps, "#FF5733", label="oven temperature", linewidth=2)
ax1.set_ylabel("temperature [degC]")
ax1.legend()
ax1.grid(True, alpha=0.3)
ax1.set_title("Phase 2e: StaTable state machine + External PID + ThermalMass")

ax2.plot(times, duties, "#33FF57", linewidth=2)
ax2.set_ylabel("heater_top_duty [-]")
ax2.set_xlabel("time [s]")
ax2.grid(True, alpha=0.3)

fig.tight_layout()
out = ROOT / "tmp" / "closed_loop_statable.png"
fig.savefig(out, dpi=120, facecolor="white", edgecolor="white")
print(f"saved: {out}")
print(f"final T = {temps[-1]:.2f} degC (target 200)")
print(f"final duty = {duties[-1]:.4f}")
print(f"MwOven final state: {states[-1]}")
print(f"unique states seen: {sorted(set(states))}")
