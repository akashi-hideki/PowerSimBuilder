"""Scenario A: button -> timer -> LED.

Timeline:
    t=0.00: button released
    t=0.10: button pressed
    t=0.60: LED turns ON (delay timer = 0.5s)
    t=1.00: button released
"""
from __future__ import annotations

from python_runtime.components import DigitalInput, DigitalOutput, Timer
from python_runtime.simulation_runner import SimulationRunner


def build() -> SimulationRunner:
    runner = SimulationRunner(dt=0.01)

    button = DigitalInput("button")
    timer = Timer("timer")
    timer.parameters["mode"] = "delay"
    timer.parameters["duration"] = 0.5

    led = DigitalOutput("led")
    led.parameters["active_low"] = False

    runner.add_component(button)
    runner.add_component(timer)
    runner.add_component(led)

    runner.add_connection("button.outputs.state", "timer.inputs.trigger")
    runner.add_connection("timer.outputs.done", "led.inputs.command")

    runner.sim_data.set_stimulus(
        "button.inputs.state",
        lambda t: 1 if 0.10 <= t < 1.00 else 0,
    )
    return runner


def run() -> None:
    runner = build()
    runner.run(t_end=1.2)
    for entry in runner.sim_data.logs:
        t = entry["time"]
        if abs(t - 0.10) < 1e-9 or abs(t - 0.60) < 1e-9 or abs(t - 1.00) < 1e-9:
            led_state = entry["components"]["led"]["outputs"]["state"]
            timer_done = entry["components"]["timer"]["outputs"]["done"]
            print(f"t={t:.2f} led={led_state} timer_done={timer_done}")


if __name__ == "__main__":
    run()
