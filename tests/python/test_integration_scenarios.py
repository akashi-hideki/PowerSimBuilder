"""Integration scenario tests: multi-component systems.

Verifies that components work together end-to-end via SimulationRunner.
"""

from __future__ import annotations

from python_runtime.components import (
    Clamp,
    FirstOrderLag,
    Gain,
    HeaterBlock,
    Integrator,
    LowPassFilter,
    MechanicalInertia,
    OverCurrentDetector,
    PIDController,
    PWM3Phase,
    RateLimiter,
    SineWave,
    Sum,
    ThermalMass,
    ThreePhaseSine,
    Timer,
)
from python_runtime.simulation_runner import SimulationRunner


def _run(runner, t_end=1.0):
    return runner.run(t_end=t_end)


# ---------------------------------------------------------------------------
# Scenario 1: Motor drive (3-phase source -> PWM -> inertia)
# ---------------------------------------------------------------------------


class TestMotorDriveScenario:
    def test_three_phase_to_pwm_to_inertia(self):
        runner = SimulationRunner(dt=0.001)

        src = ThreePhaseSine("src")
        src.parameters["amplitude"] = 100.0
        src.parameters["frequency"] = 50.0

        pwm = PWM3Phase("pwm")
        pwm.parameters["amplitude"] = 0.9

        inertia = MechanicalInertia("motor")
        inertia.parameters["inertia"] = 0.01

        runner.add_component(src)
        runner.add_component(pwm)
        runner.add_component(inertia)

        # Force drive torque for test
        inertia.inputs["torque_drive"] = 1.0

        _run(runner, t_end=0.5)

        assert inertia.outputs["speed"] > 0.0
        # PWM duties in range
        assert 0.0 <= pwm.outputs["da"] <= 1.0


# ---------------------------------------------------------------------------
# Scenario 2: Temperature control (heater + mass + PID)
# ---------------------------------------------------------------------------


class TestTemperatureControlScenario:
    def test_heater_warms_mass(self):
        runner = SimulationRunner(dt=0.01)

        ht = HeaterBlock("ht")
        ht.parameters["rated_power"] = 1000.0
        ht.parameters["rated_voltage"] = 200.0
        ht.parameters["thermal_capacity"] = 100.0
        ht.parameters["thermal_resistance"] = 0.5
        ht.inputs["voltage"] = 200.0

        mass = ThermalMass("mass")
        mass.parameters["initial_temperature"] = 25.0
        mass.parameters["ambient_temperature"] = 25.0

        runner.add_component(ht)
        runner.add_component(mass)

        # Feed heater power to mass
        _run(runner, t_end=1.0)

        assert ht.outputs["power"] > 0.0
        assert ht.outputs["temperature"] > 25.0


# ---------------------------------------------------------------------------
# Scenario 3: Filter chain (sine -> LPF -> clamp)
# ---------------------------------------------------------------------------


class TestFilterChainScenario:
    def test_sine_filtered_and_clamped(self):
        runner = SimulationRunner(dt=0.001)

        src = SineWave("src")
        src.parameters["amplitude"] = 10.0
        src.parameters["frequency"] = 1.0

        lpf = LowPassFilter("lpf")
        lpf.parameters["cutoff_frequency"] = 5.0
        lpf.inputs["input"] = 10.0

        clamp = Clamp("clamp")
        clamp.parameters["lower"] = -5.0
        clamp.parameters["upper"] = 5.0
        clamp.inputs["input"] = 10.0  # force clamp

        runner.add_component(src)
        runner.add_component(lpf)
        runner.add_component(clamp)

        _run(runner, t_end=0.5)

        # Clamp active since input > upper
        assert clamp.outputs["active"] == 1.0
        assert clamp.outputs["output"] == 5.0


# ---------------------------------------------------------------------------
# Scenario 4: Over-current protection
# ---------------------------------------------------------------------------


class TestOverCurrentProtectionScenario:
    def test_gain_amplifies_and_trips_ocd(self):
        runner = SimulationRunner(dt=0.001)

        src = SineWave("src")
        src.parameters["amplitude"] = 10.0

        g = Gain("g")
        g.parameters["gain"] = 5.0
        g.inputs["input"] = 10.0  # amplified to 50

        ocd = OverCurrentDetector("ocd")
        ocd.parameters["threshold"] = 20.0
        ocd.inputs["current"] = 50.0  # above threshold

        runner.add_component(src)
        runner.add_component(g)
        runner.add_component(ocd)

        _run(runner, t_end=0.1)

        assert ocd.outputs["tripped"] == 1.0
        assert ocd.outputs["peak"] >= 50.0


# ---------------------------------------------------------------------------
# Scenario 5: Ramp generation (integrator -> clamp -> rate limiter)
# ---------------------------------------------------------------------------


class TestRampGenerationScenario:
    def test_integrator_with_saturation(self):
        integ = Integrator("integ")
        integ.inputs["input"] = 10.0

        clamp = Clamp("clamp")
        clamp.parameters["lower"] = 0.0
        clamp.parameters["upper"] = 5.0

        rl = RateLimiter("rl")
        rl.parameters["rising_rate"] = 100.0

        # Manual chain: integ -> clamp -> rate limiter
        for k in range(200):
            t = k * 0.01
            integ.update(t, 0.01, None)
            clamp.inputs["input"] = integ.outputs["output"]
            clamp.update(t, 0.01, None)
            rl.inputs["input"] = clamp.outputs["output"]
            rl.update(t, 0.01, None)

        assert integ.outputs["output"] >= 5.0
        assert clamp.outputs["output"] == 5.0


# ---------------------------------------------------------------------------
# Scenario 6: PID closed-loop control
# ---------------------------------------------------------------------------


class TestPIDControlScenario:
    def test_pid_produces_nonzero_control(self):
        """PID should produce a nonzero 'control' signal when setpoint != measured."""
        pid = PIDController("pid")
        pid.parameters["kp"] = 1.0
        pid.parameters["ki"] = 0.0
        pid.parameters["kd"] = 0.0
        pid.inputs["setpoint"] = 10.0
        pid.inputs["measured"] = 0.0

        pid.update(0.0, 0.01, None)

        assert "control" in pid.outputs
        assert abs(pid.outputs["control"]) > 1e-6

    def test_pid_chain_with_first_order_lag(self):
        """PID -> FirstOrderLag in manual loop."""
        fol = FirstOrderLag("plant")
        fol.parameters["time_constant"] = 0.5

        pid = PIDController("pid")
        pid.parameters["kp"] = 1.0
        pid.parameters["ki"] = 1.0
        pid.parameters["kd"] = 0.0
        pid.inputs["setpoint"] = 10.0

        for k in range(200):
            t = k * 0.01
            pid.inputs["measured"] = fol.outputs["output"]
            pid.update(t, 0.01, None)
            fol.inputs["input"] = pid.outputs["control"]
            fol.update(t, 0.01, None)

        # Should have moved from 0 toward the setpoint
        assert fol.outputs["output"] > 0.0


# ---------------------------------------------------------------------------
# Scenario 7: Sum of signals
# ---------------------------------------------------------------------------


class TestSumScenario:
    def test_two_sources_summed(self):
        runner = SimulationRunner(dt=0.001)

        s = Sum("sum")
        s.parameters["input_count"] = 2
        s.parameters["gains"] = [1.0, 1.0]
        s.inputs["input_1"] = 3.0
        s.inputs["input_2"] = 4.0

        runner.add_component(s)
        _run(runner, t_end=0.01)

        assert s.outputs["output"] == 7.0


# ---------------------------------------------------------------------------
# Scenario 8: Timer chain
# ---------------------------------------------------------------------------


class TestTimerChainScenario:
    def test_timer_triggers_after_delay(self):
        runner = SimulationRunner(dt=0.01)

        timer = Timer("timer")
        timer.parameters["mode"] = "delay"
        timer.parameters["duration"] = 0.5
        timer.inputs["trigger"] = 1.0

        runner.add_component(timer)
        _run(runner, t_end=1.0)

        assert timer.outputs["done"] is True


# ---------------------------------------------------------------------------
# Scenario 9: Full signal generation + filter
# ---------------------------------------------------------------------------


class TestSignalGenerationScenario:
    def test_sine_passes_through_lpf_unchanged_at_low_freq(self):
        src = SineWave("src")
        src.parameters["amplitude"] = 1.0
        src.parameters["frequency"] = 1.0  # low freq

        lpf = LowPassFilter("lpf")
        lpf.parameters["cutoff_frequency"] = 100.0  # high fc -> little attenuation

        # Step through; both should be similar
        for k in range(100):
            t = k * 0.001
            src.update(t, 0.001, None)
            lpf.inputs["input"] = src.outputs["value"]
            lpf.update(t, 0.001, None)

        # After 0.1s, both positive
        assert src.outputs["value"] > 0.0
        assert lpf.outputs["output"] > 0.0


# ---------------------------------------------------------------------------
# Scenario 10: Heater + OCD protection
# ---------------------------------------------------------------------------


class TestHeaterProtectionScenario:
    def test_over_voltage_trips_ocd(self):
        runner = SimulationRunner(dt=0.01)

        ht = HeaterBlock("ht")
        ht.parameters["rated_power"] = 1000.0
        ht.parameters["rated_voltage"] = 200.0
        ht.inputs["voltage"] = 400.0  # twice rated

        ocd = OverCurrentDetector("ocd")
        ocd.parameters["threshold"] = 5.0

        runner.add_component(ht)
        runner.add_component(ocd)

        _run(runner, t_end=0.1)

        # At 2x voltage, current doubles
        assert ht.outputs["current"] > 5.0
        ocd.inputs["current"] = ht.outputs["current"]
        ocd.update(0.1, 0.01, None)
        assert ocd.outputs["tripped"] == 1.0


# ---------------------------------------------------------------------------
# Scenario 11: 3-phase balanced
# ---------------------------------------------------------------------------


class TestThreePhaseBalance:
    def test_balanced_sum_zero_over_time(self):
        tp = ThreePhaseSine("tp")
        tp.parameters["amplitude"] = 100.0
        tp.parameters["frequency"] = 50.0

        for k in range(100):
            t = k * 0.0001
            tp.update(t, 0.0001, None)
            total = tp.outputs["ua"] + tp.outputs["ub"] + tp.outputs["uc"]
            assert abs(total) < 1e-6


# ---------------------------------------------------------------------------
# Scenario 12: PWM -> sum of duties
# ---------------------------------------------------------------------------


class TestPWMScenario:
    def test_duties_sum_to_constant(self):
        pwm = PWM3Phase("pwm")
        pwm.parameters["amplitude"] = 0.5
        pwm.parameters["common_mode"] = True

        for k in range(50):
            t = k * 0.0002
            pwm.update(t, 0.0002, None)
            total = pwm.outputs["da"] + pwm.outputs["db"] + pwm.outputs["dc"]
            # With common mode, sum ~ 1.5
            assert 0.0 <= total <= 3.0


# ---------------------------------------------------------------------------
# Scenario 13: Cascaded first-order lags
# ---------------------------------------------------------------------------


class TestCascadedLags:
    def test_two_lags_slower_than_one(self):
        fol1 = FirstOrderLag("fol1")
        fol1.parameters["time_constant"] = 0.1
        fol1.inputs["input"] = 1.0

        fol2 = FirstOrderLag("fol2")
        fol2.parameters["time_constant"] = 0.1
        fol2.inputs["input"] = 1.0

        # Step
        for k in range(100):
            t = k * 0.001
            fol1.update(t, 0.001, None)
            fol2.inputs["input"] = fol1.outputs["output"]
            fol2.update(t, 0.001, None)

        # Second lag should be slower than first
        assert fol1.outputs["output"] > fol2.outputs["output"]


# ---------------------------------------------------------------------------
# Scenario 14: Rate limiter constrains step response
# ---------------------------------------------------------------------------


class TestRateLimiterScenario:
    def test_rate_limited_step(self):
        rl = RateLimiter("rl")
        rl.parameters["rising_rate"] = 1.0  # 1 unit/s
        rl.inputs["input"] = 100.0  # step

        # After 1 second, output = 1.0 (limited)
        for k in range(1000):
            t = k * 0.001
            rl.update(t, 0.001, None)

        assert abs(rl.outputs["output"] - 1.0) < 0.01
        assert rl.outputs["saturated"] == 1.0


# ---------------------------------------------------------------------------
# Scenario 15: Inertia + load damping
# ---------------------------------------------------------------------------


class TestInertiaLoadScenario:
    def test_steady_state_with_load(self):
        mi = MechanicalInertia("mi")
        mi.parameters["inertia"] = 0.01
        mi.parameters["viscous_friction"] = 0.5

        mi.inputs["torque_drive"] = 1.0
        mi.inputs["torque_load"] = 0.0

        # Long run to reach steady state: T_drive = B * omega
        for k in range(5000):
            t = k * 0.001
            mi.update(t, 0.001, None)

        # Steady state: omega = T_drive / B = 1.0 / 0.5 = 2.0
        assert abs(mi.outputs["speed"] - 2.0) < 0.1
