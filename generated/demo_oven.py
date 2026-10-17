"""Cooking Heater Demo (Task 3-C-4).

Two scenarios:
1. Overheat detection: heater on -> T > 250C -> error_code = 0xE3
2. Door safety: heater on -> door opens -> all actuators off
"""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "src"))

from python_runtime import SimulationRunner
from python_runtime.components import ControllerBox, HeaterBlock, Gain

GENERATED = ROOT / "generated" / "controller_box_powersim_demo.py"


def build_runner():
    """Build a fresh SimulationRunner for the demo."""
    runner = SimulationRunner(dt=0.01)

    ctrl = ControllerBox("ctrl", generated_path=str(GENERATED))
    ctrl.parameters["auto_events"] = "demo_oven"

    heater = HeaterBlock("heater1")
    # Override for faster demo: C_th=50 J/K => ~12.7s to reach 250C
    heater.parameters["thermal_capacity"] = 50.0
    heater.parameters["thermal_resistance"] = 1.0

    gain = Gain("gain1")
    gain.parameters["gain"] = 2.0   # 0-100% -> 0-200V
    gain.parameters["bias"] = 0.0

    runner.add_component(ctrl)
    runner.add_component(heater)
    runner.add_component(gain)

    runner.add_connection("ctrl.outputs.heater_top_duty", "gain1.inputs.input")
    runner.add_connection("gain1.outputs.output", "heater1.inputs.voltage")
    runner.add_connection("heater1.outputs.temperature", "ctrl.inputs.thermistor_top")

    return runner, ctrl, heater


def scenario_overheat():
    print("=" * 60)
    print("Scenario 1: Overheat detection")
    print("=" * 60)

    runner, ctrl, heater = build_runner()

    for i in range(2000):  # 20s max
        t = i * 0.01
        runner.step(t)

        if i % 100 == 0:
            print(
                f"  t={t:5.2f}s  T={heater.outputs['temperature']:6.1f}C  "
                f"duty={ctrl.outputs.get('heater_top_duty', 0):3.0f}  "
                f"err=0x{ctrl.outputs.get('error_code', 0):02X}"
            )

        if ctrl.outputs.get("error_code") == 0xE3:
            print(
                f"\n  [PASS] Overheat at t={t:.2f}s, "
                f"T={heater.outputs['temperature']:.1f}C"
            )
            return True

    print("\n  [FAIL] Overheat not detected in 20s")
    return False


def scenario_door_safety():
    print("\n" + "=" * 60)
    print("Scenario 2: Door safety")
    print("=" * 60)

    runner, ctrl, heater = build_runner()

    print("  Phase 1: run 1s (heaters on)")
    for i in range(100):
        runner.step(i * 0.01)

    duty_before = ctrl.outputs.get("heater_top_duty", 0)
    print(f"  After 1s: heater_top_duty={duty_before}")

    if duty_before == 0:
        print("  [FAIL] Heaters did not turn on")
        return False

    print("  Phase 2: open door, run 0.1s")
    ctrl.inputs["door_open"] = 1
    for i in range(10):
        runner.step(1.0 + i * 0.01)

    duty_after = ctrl.outputs.get("heater_top_duty", 0)
    fan_after = ctrl._globals.get("fan_on", 0)
    mag_after = ctrl._globals.get("mag_on", 0)

    if duty_after == 0 and fan_after == 0 and mag_after == 0:
        print(f"  [PASS] All actuators OFF (duty={duty_after})")
        return True

    print(
        f"  [FAIL] Actuators not off: duty={duty_after}, "
        f"fan={fan_after}, mag={mag_after}"
    )
    return False


def main():
    print(f"Cooking Heater Demo (Task 3-C-4)")
    print(f"Generated module: {GENERATED}")
    print(f"Exists: {GENERATED.is_file()}\n")

    if not GENERATED.is_file():
        print("[ERROR] Generated module not found.")
        return 1

    ok1 = scenario_overheat()
    ok2 = scenario_door_safety()

    print("\n" + "=" * 60)
    print("SUMMARY")
    print("=" * 60)
    print(f"  Overheat detection:  {'PASS' if ok1 else 'FAIL'}")
    print(f"  Door safety:         {'PASS' if ok2 else 'FAIL'}")

    return 0 if (ok1 and ok2) else 1


if __name__ == "__main__":
    sys.exit(main())
