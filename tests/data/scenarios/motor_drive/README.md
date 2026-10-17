# motor_drive

PWM-derived duty ratios drive a rotational inertia through a torque gain.

## Chain

    PWM3Phase.da (duty 0..1)
        -> Gain (gain=10, bias=0)
            -> MechanicalInertia.torque_drive

## Parameters

- pwm.amplitude = 0.9, pwm.frequency = 50 Hz, common-mode on.
- gain.gain = 10  -> torque drive in [0, 10] N m (mean ~5 N m).
- motor.inertia = 0.01 kg m^2, viscous_friction = 0.1 N m s/rad.

Steady-state speed ~ (mean torque) / friction = 5 / 0.1 = 50 rad/s.

## Test

tests/python/test_scenario_motor_drive.py

- Loads system.json, runs 1 s.
- Asserts final speed > 30 rad/s and speed_rpm > 200.
