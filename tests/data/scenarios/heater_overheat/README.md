# heater_overheat

AC voltage drives an electrical heater; over-current detector trips on the
resulting current peaks.

## Chain

    ThreePhaseSine.ua (amplitude=300 V, 50 Hz)
        -> HeaterBlock.voltage         -> HeaterBlock.current / power / temperature
        -> OverCurrentDetector.current -> tripped (threshold 2.0 A)

## Parameters

- tps.amplitude = 300 V, tps.frequency = 50 Hz.
- heater.rated_power = 1000 W, rated_voltage = 200 V  =>  R = 40 ohm.
  - Current peak = 300 / 40 = 7.5 A  =>  OCD trips immediately.
  - Power peak   = 300^2 / 40 = 2250 W; average ~1125 W.
- heater.thermal_capacity = 100 J/K (accelerated heating for the test).
- ocd.threshold = 2.0 A, hysteresis = 0.5 A, latch = true.

## Test

tests/python/test_scenario_heater_overheat.py

- Runs 5 s.
- Asserts heater.temperature > 50 degC (rose from ambient 25 degC).
- Asserts ocd.tripped == 1.0 and ocd.peak > 5 A.
