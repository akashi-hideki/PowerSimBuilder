# Python Runtime Scenarios

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
