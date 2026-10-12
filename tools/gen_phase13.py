"""Phase 13 generator: documentation for the Python runtime."""
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
F = {}

F["docs/python_runtime.md"] = '''# Python Runtime

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
'''

F["docs/python_runtime_api.md"] = '''# Python Runtime API Reference

All components share the same interface:

    class Component:
        id: str
        type: str
        parameters: dict
        inputs: dict
        outputs: dict

        def update(self, t: float, dt: float, sim_data) -> None: ...
        def reset(self) -> None: ...

## DigitalInput

| Direction | Name           | Type | Description                          |
|-----------|----------------|------|--------------------------------------|
| param     | debounce_ms    | float| Debounce time [ms]; 0 disables       |
| param     | initial        | int  | Initial state (0 or 1)               |
| input     | state          | int  | Raw physical input (0 or 1)          |
| output    | state          | int  | Stabilized state after debounce      |
| output    | edge_rising    | bool | True for one step on rising edge     |
| output    | edge_falling   | bool | True for one step on falling edge    |

## DigitalOutput

| Direction | Name           | Type | Description                          |
|-----------|----------------|------|--------------------------------------|
| param     | active_low     | bool | Invert physical level if True        |
| param     | on_current_ma  | float| Load current when ON [mA]            |
| input     | command        | int  | Logical command (0 or 1)             |
| output    | state          | int  | Physical pin level                   |
| output    | current_draw   | float| Load current [mA]                    |

## AnalogInput

| Direction | Name           | Type | Description                          |
|-----------|----------------|------|--------------------------------------|
| param     | v_min, v_max   | float| Input voltage range [V]              |
| param     | filter_alpha   | float| 1st-order filter coefficient (0..1)  |
| input     | raw            | float| Raw input voltage [V]                |
| output    | scaled         | float| Normalized to 0..1 (clamped)         |
| output    | filtered       | float| Filtered scaled value                |

Filter: `y[n] = alpha * x[n] + (1 - alpha) * y[n-1]`

## AnalogOutput

| Direction | Name           | Type | Description                          |
|-----------|----------------|------|--------------------------------------|
| param     | v_ref          | float| Reference output voltage [V]         |
| input     | value          | float| Target value (0..1, clamped)         |
| output    | duty           | float| Duty cycle (0..1)                    |
| output    | voltage        | float| v_ref * duty [V]                     |
| output    | active         | bool | True when duty > 0                   |

## PIDController

| Direction | Name           | Type | Description                          |
|-----------|----------------|------|--------------------------------------|
| param     | kp, ki, kd     | float| Gains                                |
| param     | out_min, out_max | float| Output clamp range                 |
| param     | setpoint       | float| Default setpoint                     |
| param     | anti_windup    | bool | Prevent integral windup if True      |
| input     | measured       | float| Process value                        |
| input     | setpoint       | float| Optional per-step override           |
| output    | control        | float| Clamped output                       |
| output    | error          | float| setpoint - measured                  |
| output    | p_term, i_term, d_term | float| Individual contributions    |
| output    | saturated      | bool | True if output was clamped           |

## Timer

| Direction | Name           | Type | Description                          |
|-----------|----------------|------|--------------------------------------|
| param     | mode           | str  | "delay" | "oneshot" | "interval"     |
| param     | duration       | float| Duration in seconds                  |
| input     | trigger        | bool | Start / enable                       |
| input     | reset          | bool | Reset internal state                 |
| output    | done           | bool | Mode-dependent                       |
| output    | running        | bool | Timer currently active               |
| output    | elapsed        | float| Elapsed time [s]                     |

## Counter

| Direction | Name           | Type | Description                          |
|-----------|----------------|------|--------------------------------------|
| param     | min_count, max_count | int | Bounds                        |
| param     | initial        | int  | Initial count                        |
| param     | direction      | str  | "up" | "down"                       |
| param     | wrap           | bool | Wrap on limit if True                |
| input     | clock          | bool | Rising edge increments/decrements    |
| input     | reset          | bool | Reset to initial                     |
| output    | count          | int  | Current count                        |
| output    | overflow       | bool | One step when limit exceeded         |
| output    | underflow      | bool | One step when lower limit exceeded   |

## StateMachine

| Direction | Name           | Type | Description                          |
|-----------|----------------|------|--------------------------------------|
| param     | initial_state  | str  | Starting state                       |
| param     | transitions    | dict | "state:event" -> next_state          |
| input     | event          | str  | Event name                           |
| input     | reset          | bool | Return to initial state              |
| output    | current_state  | str  | Current state                        |
| output    | prev_state     | str  | Previous state                       |
| output    | transitioned   | bool | True for one step after transition   |

## SimulationRunner

    class SimulationRunner:
        def __init__(self, dt: float = 0.001) -> None
        def add_component(self, comp) -> None
        def add_connection(self, src: str, dst: str) -> None
        def step(self, t: float) -> None
        def run(self, t_end: float) -> SimulationData

`src` and `dst` use the form `"comp_id.section.port"`.

## SimulationData

    class SimulationData:
        time_step: float
        t: float
        logs: list
        stimulus: dict

        def set_stimulus(path, source) -> None
        def get_stimulus_value(path, t) -> Any
        def apply_stimulus(components, t) -> None
        def log(t, components) -> None

## Logger

    from python_runtime import dump_csv, plot

    dump_csv(sim_data, "out.csv")
    plot(sim_data, keys=["c1.outputs.y"], output_path="plot.png")
'''

F["docs/python_runtime_scenarios.md"] = '''# Python Runtime Scenarios

Each scenario is a small end-to-end example that can be verified by hand.

## Scenario A: button -> timer -> LED

File: `examples/scenario_a.py`

Timeline:

    t=0.00  button released
    t=0.10  button pressed
    t=0.60  timer.done becomes True
    t=0.62  LED turns ON
    t=1.00  button released

Why t=0.62 and not t=0.60:

- Connections propagate after `update()`, so the timer sees the button
  state one step later than the raw stimulus.
- The digital output also sees `timer.done` one step later.

Expected output:

    t=0.10 led=0 timer_done=False
    t=0.60 led=0 timer_done=False
    t=1.00 led=1 timer_done=True

## Scenario B: ramp -> analog filter -> PID -> analog output

File: `examples/scenario_b.py`

Input step: `raw = 5.0` starting at t=0.1.

    filtered[n] = 0.2 * scaled[n] + 0.8 * filtered[n-1]
    error       = setpoint - filtered
    control     = clamp(kp * error, 0.0, 1.0)

Hand calculation:

    t=0.10  filtered = 0.2
    t=0.20  filtered = 0.36
    t=0.30  filtered = 0.488
    ...

At t=0.5 the filtered value approaches 1.0, and the PID output
saturates to 0.0 because filtered > setpoint (0.5).

## Scenario C: clock -> counter -> state machine

File: `examples/scenario_c.py`

Clock stimulus toggles every 0.1s. Counter max_count = 5.

- 5 rising edges -> count = 5 -> overflow = True (one step).
- StateMachine sees `"IDLE:True"` (overflow is a boolean) and
  transitions to `"DONE"` one step later.

Expected output:

    t=0.50 count=3 state=IDLE
    t=1.00 count=5 state=IDLE
    t=1.20 count=5 state=DONE

## Running the scenarios

    python -m examples.scenario_a
    python -m examples.scenario_b
    python -m examples.scenario_c

Or run them all with pytest:

    pytest tests/python/test_scenarios.py -v
'''

F["docs/python_runtime_architecture.md"] = '''# Python Runtime Architecture

## High-level overview

    +------------------+       +---------------------+
    |  Component       |       |  SimulationRunner   |
    |  (base class)    |<------|  - components{}     |
    |  - id            |       |  - connections[]    |
    |  - parameters    |       |  - sim_data         |
    |  - inputs        |       |  - dt               |
    |  - outputs       |       |                     |
    |  - update(t,dt)  |       |  run(t_end)         |
    +------------------+       +---------------------+
            ^                            |
            |                            |
    +-------+--------+                   v
    |  Components    |          +---------------------+
    |  - Digital     |          |  SimulationData     |
    |  - Analog      |          |  - time_step        |
    |  - PID         |          |  - stimulus{}       |
    |  - Timer       |          |  - logs[]           |
    |  - Counter     |          +---------------------+
    |  - StateMachine|
    +----------------+

## Data flow per step

    apply_stimulus
        |
        v
    component.update()
        |
        v
    propagate connections (src.outputs -> dst.inputs)
        |
        v
    log snapshot

## Connection format

    runner.add_connection(
        "timer.outputs.done",     # source
        "led.inputs.command",     # destination
    )

Each path is split on `.`:

    comp_id . section . port

`section` must be `"outputs"` for the source and `"inputs"` for the
destination. Invalid paths are silently skipped.

## Sequence diagram

```mermaid
sequenceDiagram
    participant U as User
    participant R as SimulationRunner
    participant S as SimulationData
    participant C as Component

    U->>R: run(t_end)
    loop every dt
        R->>S: apply_stimulus(components, t)
        R->>C: update(t, dt, sim_data)
        C-->>R: (updates outputs)
        R->>R: _propagate()
        R->>S: log(t, components)
    end
    R-->>U: SimulationData (with logs)
```

## Design principles

1. **Single interface**: every component is just `update(t, dt, sim)`.
2. **No hidden state**: everything is in `inputs`, `outputs`,
   `parameters`, plus private underscore attributes.
3. **Composable**: connections are data, not code.
4. **Testable**: no global state, no side effects, deterministic.
5. **Portable**: pure Python, optional numpy/matplotlib only for I/O.

## Verification strategy

- Unit tests for every component.
- Integration tests for SimulationRunner.
- Scenario tests for 3 end-to-end examples.
- All expected values are hand-computable.
'''

F["examples/README.md"] = '''# Example Scenarios

Three small scenarios demonstrate the Python runtime.

| Scenario | File | Demonstrates |
|----------|------|--------------|
| A | scenario_a.py | Digital input -> delay timer -> digital output |
| B | scenario_b.py | Analog scaling + first-order filter + PID |
| C | scenario_c.py | Rising-edge counter + state transition |

## Run

    python -m examples.scenario_a
    python -m examples.scenario_b
    python -m examples.scenario_c

## Use as a library

Each scenario exports:

    build() -> SimulationRunner    # configured, not yet run
    run()   -> None                # builds, runs, prints summary

You can import and extend them:

    from examples import scenario_a
    runner = scenario_a.build()
    runner.run(t_end=2.0)
    for entry in runner.sim_data.logs:
        ...

## Dump to CSV or PNG

    from python_runtime import dump_csv, plot

    dump_csv(runner.sim_data, "scenario_a.csv")
    plot(runner.sim_data, keys=["led.outputs.state"], output_path="scenario_a.png")
'''

F["docs/README.md"] = '''# Documentation Index

- [Python Runtime overview](python_runtime.md)
- [API reference](python_runtime_api.md)
- [Scenarios and hand-verification](python_runtime_scenarios.md)
- [Architecture](python_runtime_architecture.md)
- [Handoff notes](HANDOFF.md)
- [Specifications](specs/)

## Python runtime at a glance

| Item | Value |
|------|-------|
| Components | 8 (4 I/O + 4 logic) |
| Tests | 83 |
| Coverage | 96% |
| Dependencies | numpy, matplotlib (optional for I/O) |
| Python | >= 3.10 |
'''


def main() -> None:
    for rel, content in F.items():
        p = ROOT / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(content, encoding="utf-8")
        print(f"  + {rel}")
    print()
    print(f"Done. {len(F)} files written to {ROOT}")


if __name__ == "__main__":
    main()