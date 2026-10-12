# Python Runtime API Reference

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
