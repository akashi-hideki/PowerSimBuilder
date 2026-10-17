# rectifier_motor

AC source rectified to DC drives an induction motor.

## Chain

    PowerSource.voltage -> DiodeBridge.ac_voltage
    DiodeBridge.dc_voltage -> InductionMotor.voltage

## Parameters

- ac.voltage = 200 V (RMS), frequency = 50 Hz, sine
- rect.type = full, diode_drop = 0.7 V
- motor.rated_voltage = 200 V, load_torque = 2 N*m, inertia = 0.01

## Test

tests/python/test_scenario_rectifier_motor.py

- Runs 1 s
- Asserts motor.speed > 100 rpm and slip < 0.9
