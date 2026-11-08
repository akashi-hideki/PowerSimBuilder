"""ASCII trajectory viewer for PID closed loop."""
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "src"))
from python_runtime.components import OvenChamber, PIDController

def run(kp, ki, kd, aw, t_end=300.0, dt=0.1, target=200.0):
    oven = OvenChamber("oven")
    oven.parameters.update(thermal_resistance=0.5, thermal_capacity=200.0,
                            ambient_temperature=25.0, initial_temperature=25.0,
                            heater_power_max=1000.0)
    pid = PIDController("pid")
    pid.parameters.update(kp=kp, ki=ki, kd=kd, out_min=0.0, out_max=1.0,
                           setpoint=target, anti_windup=aw)
    n = int(round(t_end / dt))
    T, U = [], []
    for i in range(n + 1):
        t = i * dt
        pid.inputs["measured"] = oven.outputs["oven_current_temp"]
        pid.update(t, dt, None)
        oven.inputs["heater_top_duty"] = pid.outputs["control"]
        oven.update(t, dt, None)
        T.append(oven.outputs["oven_current_temp"])
        U.append(pid.outputs["control"])
    return T, U

def ascii_plot(T, U, target, width=60, height=12, t_end=300.0):
    n = len(T)
    step = max(1, n // width)
    Td = [T[i] for i in range(0, n, step)][:width]
    Ud = [U[i] for i in range(0, n, step)][:width]
    tmax = max(T)
    tmin = min(T)
    print(f"\n=== Temperature (target={target}, max={tmax:.1f}, min={tmin:.1f}) ===")
    print(f"t_axis: 0{'':>8}{t_end/2:>8.0f}{t_end:>8.0f} s")
    for h in range(height, -1, -1):
        level = tmin + (tmax - tmin) * h / height
        row = ""
        for v in Td:
            d = abs(v - level) / (tmax - tmin) * height
            row += "#" if d < 0.5 else " "
        marker = " <-target" if abs(level - target) < (tmax-tmin)/height else ""
        print(f"{level:>6.1f} |{row}{marker}")
    print(f"\n=== Control (duty, min={min(Ud):.2f}, max={max(Ud):.2f}) ===")
    for h in range(4, -1, -1):
        level = h / 4
        row = "".join("=" if abs(v - level) < 0.125 else " " for v in Ud)
        print(f"{level:>6.2f} |{row}")

if __name__ == "__main__":
    print("Best: no_aw (kp=0.01, ki=0.0001, anti_windup=False)")
    T, U = run(0.01, 0.0001, 0.0, False)
    ascii_plot(T, U, 200.0)