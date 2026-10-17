# generator_load

Engine torque drives a Generator; verifies torque -> voltage conversion.

## Chain

    Engine.torque -> Generator.torque -> (voltage / current / power)

## Parameters

- engine.torque_max = 20 N*m, ramp_time = 0.1 s, idle_ratio = 0
- gen.efficiency = 0.9, voltage_per_torque = 10 V/(N*m)
- gen.rated_voltage = 200 V, load_resistance = 10 ohm

## Expected steady-state (t >= 0.1 s)

- torque = 20
- V = 0.9 * 20 * 10 = 180 V (below 200 cap)
- I = 180 / 10 = 18 A
- P = 180 * 18 = 3240 W

## Test

tests/python/test_scenario_generator_load.py
