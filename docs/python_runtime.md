# Python Runtime

The PowerSimBuilder Python runtime is a lightweight, dependency-free
time-domain simulator for control-board I/O and control logic.

## Goals

- Model digital/analog I/O and control logic as reusable components.
- Support hand-verifiable (paper-and-pencil) tests.
- Connect components into a network and propagate signals over time.
- Run entirely on pure Python (numpy/matplotlib optional for plotting).

## Package layout

    src/python_runtime/
        __init__.py              Public API re-exports
        component_base.py        Component base class
        simulation_data.py       SimulationData + stimulus injection
        simulation_runner.py     Main loop + connection propagation
        logger.py                CSV / matplotlib output
        components/
            digital_input.py     Debounce + edge detection
            digital_output.py    active_low aware output
            analog_input.py      Scaling + first-order filter
            analog_output.py     PWM duty conversion
            pid_controller.py    Discrete-time PID
            timer.py             delay / oneshot / interval
            counter.py           Up/down counter with overflow
            state_machine.py     Transition-table state machine

## Quick start

    from python_runtime import SimulationRunner
    from python_runtime.components import DigitalInput, Timer, DigitalOutput

    runner = SimulationRunner(dt=0.01)

    button = DigitalInput("button")
    timer = Timer("timer")
    timer.parameters["mode"] = "delay"
    timer.parameters["duration"] = 0.5
    led = DigitalOutput("led")

    runner.add_component(button)
    runner.add_component(timer)
    runner.add_component(led)
    runner.add_connection("button.outputs.state", "timer.inputs.trigger")
    runner.add_connection("timer.outputs.done", "led.inputs.command")

    runner.sim_data.set_stimulus(
        "button.inputs.state",
        lambda t: 1 if 0.1 <= t < 1.0 else 0,
    )
    runner.run(t_end=1.2)

## Design principles

1. Every component inherits from `Component` and implements
   `update(t, dt, sim_data)`.
2. Ports are plain dictionaries (`inputs`, `outputs`) so they can be
   serialized to JSON.
3. Stimulus is injected via `SimulationData.set_stimulus()` using
   `f(t) -> value` or a list of `(t, value)` pairs.
4. Connections are declared as `"src.outputs.port" -> "dst.inputs.port"`.
5. All state is observable through `SimulationData.logs`.

## Verification

- 83 unit tests, coverage 96%.
- 3 end-to-end scenarios under `examples/`.
- All tests are hand-verifiable (truth tables, step responses).
