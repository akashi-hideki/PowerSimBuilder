# fault_short_circuit

FaultDevice triggers a latched short-circuit fault while the power
chain (Engine + Generator) continues to run normally.

## Chain

    Engine.torque -> Generator.torque -> (V / I / P)
    FaultDevice (orthogonal, no connections)

## Fault timeline

- condition_expr = "t >= 0.3 and t < 0.4"
- duration = 0.05 s
- At t = 0.35 s: elapsed >= duration -> short_circuit latched True
- At t = 0.4 s: condition clears, but latch persists

## Test

tests/python/test_scenario_fault_short_circuit.py

- Runs 1 s
- Asserts fault.short_circuit is True (latched)
- Asserts engine.torque > 19 and 175 < gen.voltage < 185 (unaffected)
