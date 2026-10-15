"""Numeric precision tests: compare against analytical solutions.

Verifies that runtime components match closed-form solutions within
tolerance. Uses short simulation runs to bound accumulated error.
"""

from __future__ import annotations

import math

from python_runtime.components import (
    FirstOrderLag,
    HeaterBlock,
    Integrator,
    LowPassFilter,
    MechanicalInertia,
    SineWave,
    ThermalMass,
    ThreePhaseSine,
)

# ---------------------------------------------------------------------------
# FirstOrderLag: y(t) = u * (1 - exp(-t/tau))
# ---------------------------------------------------------------------------


class TestFirstOrderLagAnalytical:
    def test_step_response_at_1tau(self):
        """At t = tau, output = 63.2% of input."""
        fol = FirstOrderLag("fol")
        fol.parameters["time_constant"] = 1.0
        fol.inputs["input"] = 1.0

        # Run for 1 second with small dt
        dt = 0.0001
        for k in range(10000):
            fol.update(k * dt, dt, None)

        expected = 1.0 - math.exp(-1.0)
        assert abs(fol.outputs["output"] - expected) < 1e-3

    def test_step_response_at_2tau(self):
        fol = FirstOrderLag("fol")
        fol.parameters["time_constant"] = 0.5
        fol.inputs["input"] = 2.0

        dt = 0.0001
        t_end = 1.0  # 2 tau
        for k in range(int(t_end / dt)):
            fol.update(k * dt, dt, None)

        expected = 2.0 * (1.0 - math.exp(-2.0))
        assert abs(fol.outputs["output"] - expected) < 1e-2

    def test_final_value_at_5tau(self):
        fol = FirstOrderLag("fol")
        fol.parameters["time_constant"] = 0.2
        fol.inputs["input"] = 5.0

        dt = 0.0001
        t_end = 1.0  # 5 tau
        for k in range(int(t_end / dt)):
            fol.update(k * dt, dt, None)

        # 1 - exp(-5) ~= 0.9933
        expected = 5.0 * (1.0 - math.exp(-5.0))
        assert abs(fol.outputs["output"] - expected) < 5e-2


# ---------------------------------------------------------------------------
# Integrator: y(t) = y0 + u * t
# ---------------------------------------------------------------------------


class TestIntegratorAnalytical:
    def test_linear_ramp(self):
        """Constant input 2.0 for 1 second -> output = 2.0."""
        integ = Integrator("i")
        integ.inputs["input"] = 2.0

        dt = 0.001
        for k in range(1000):
            integ.update(k * dt, dt, None)

        assert abs(integ.outputs["output"] - 2.0) < 1e-6

    def test_integrate_sine_quarter_period(self):
        """Integral of sin from 0 to pi/2 = 1.0."""
        integ = Integrator("i")
        src = SineWave("src")
        src.parameters["amplitude"] = 1.0
        src.parameters["frequency"] = 1.0

        dt = 0.0001
        t_end = 0.25  # quarter period
        for k in range(int(t_end / dt)):
            t = k * dt
            src.update(t, dt, None)
            integ.inputs["input"] = src.outputs["value"]
            integ.update(t, dt, None)

        # Integral of sin(2*pi*t) from 0 to 0.25 = 1/(2*pi) ~= 0.15915
        expected = 1.0 / (2.0 * math.pi)
        assert abs(integ.outputs["output"] - expected) < 1e-3


# ---------------------------------------------------------------------------
# MechanicalInertia: omega(t) = (T/B) * (1 - exp(-B*t/J))  (with zero load)
# ---------------------------------------------------------------------------


class TestMechanicalInertiaAnalytical:
    def test_first_order_speed_response(self):
        """Solution: omega(t) = (T/B)*(1 - exp(-B*t/J))."""
        mi = MechanicalInertia("mi")
        mi.parameters["inertia"] = 0.1
        mi.parameters["viscous_friction"] = 1.0
        mi.inputs["torque_drive"] = 1.0
        mi.inputs["torque_load"] = 0.0

        dt = 0.0001
        t_end = 1.0
        for k in range(int(t_end / dt)):
            mi.update(k * dt, dt, None)

        # Analytic: omega(1) = 1 * (1 - exp(-1*1/0.1)) = 1 * (1 - exp(-10))
        expected = 1.0 * (1.0 - math.exp(-10.0))
        assert abs(mi.outputs["speed"] - expected) < 1e-2

    def test_steady_state_speed(self):
        """omega_ss = T/B."""
        mi = MechanicalInertia("mi")
        mi.parameters["inertia"] = 0.01
        mi.parameters["viscous_friction"] = 0.5
        mi.inputs["torque_drive"] = 2.0

        dt = 0.001
        for k in range(10000):  # 10 s
            mi.update(k * dt, dt, None)

        # omega_ss = 2.0 / 0.5 = 4.0
        assert abs(mi.outputs["speed"] - 4.0) < 0.01


# ---------------------------------------------------------------------------
# ThermalMass: T(t) = T_amb + Q*R*(1 - exp(-t/(R*C)))
# ---------------------------------------------------------------------------


class TestThermalMassAnalytical:
    def test_thermal_time_constant(self):
        """At t = R*C, T = T_amb + Q*R*(1 - 1/e)."""
        tm = ThermalMass("tm")
        tm.parameters["thermal_resistance"] = 1.0
        tm.parameters["thermal_capacity"] = 1.0
        tm.parameters["ambient_temperature"] = 0.0
        tm.parameters["initial_temperature"] = 0.0
        tm.reset()
        tm.inputs["heat_flow"] = 10.0

        dt = 0.0001
        t_end = 1.0  # t = R*C = 1.0
        for k in range(int(t_end / dt)):
            tm.update(k * dt, dt, None)

        # T(1) = 0 + 10*1*(1 - 1/e) = 10 * 0.6321 = 6.321
        expected = 10.0 * (1.0 - math.exp(-1.0))
        assert abs(tm.outputs["temperature"] - expected) < 0.1

    def test_thermal_steady_state(self):
        """T_ss = T_amb + Q*R."""
        tm = ThermalMass("tm")
        tm.parameters["thermal_resistance"] = 2.0
        tm.parameters["thermal_capacity"] = 0.1
        tm.parameters["ambient_temperature"] = 25.0
        tm.parameters["initial_temperature"] = 25.0
        tm.reset()
        tm.inputs["heat_flow"] = 5.0

        dt = 0.001
        for k in range(10000):
            tm.update(k * dt, dt, None)

        # T_ss = 25 + 5*2 = 35
        assert abs(tm.outputs["temperature"] - 35.0) < 0.05


# ---------------------------------------------------------------------------
# HeaterBlock: steady-state temperature
# ---------------------------------------------------------------------------


class TestHeaterBlockAnalytical:
    def test_heater_steady_state_temperature(self):
        """T_ss = T_amb + P*R_th."""
        ht = HeaterBlock("ht")
        ht.parameters["rated_power"] = 1000.0
        ht.parameters["rated_voltage"] = 200.0
        ht.parameters["thermal_resistance"] = 0.5
        ht.parameters["thermal_capacity"] = 10.0
        ht.parameters["ambient_temperature"] = 25.0
        ht.parameters["initial_temperature"] = 25.0
        ht.reset()
        ht.inputs["voltage"] = 200.0  # full power

        dt = 0.001
        for k in range(50000):  # 50 s
            ht.update(k * dt, dt, None)

        # P = 1000 W, R_th = 0.5 -> delta T = 500 K, T = 525
        # Note: thermal_capacity=10, time_constant = 0.5*10 = 5s
        assert abs(ht.outputs["temperature"] - 525.0) < 1.0


# ---------------------------------------------------------------------------
# LowPassFilter: step response
# ---------------------------------------------------------------------------


class TestLowPassFilterAnalytical:
    def test_alpha_first_step(self):
        """First step output = alpha * input."""
        lpf = LowPassFilter("lpf")
        lpf.parameters["cutoff_frequency"] = 0.0
        lpf.parameters["alpha"] = 0.3
        lpf.inputs["input"] = 10.0

        lpf.update(0.0, 0.001, None)
        assert abs(lpf.outputs["output"] - 3.0) < 1e-9

    def test_steady_state_equals_input(self):
        """After many steps, output -> input."""
        lpf = LowPassFilter("lpf")
        lpf.parameters["cutoff_frequency"] = 0.0
        lpf.parameters["alpha"] = 0.1
        lpf.inputs["input"] = 5.0

        for k in range(500):
            lpf.update(k * 0.001, 0.001, None)

        assert abs(lpf.outputs["output"] - 5.0) < 1e-6


# ---------------------------------------------------------------------------
# SineWave: analytic values at known points
# ---------------------------------------------------------------------------


class TestSineWaveAnalytical:
    def test_at_quarter_period(self):
        """At t = T/4 (T=1/f), sin -> 1."""
        sw = SineWave("sw")
        sw.parameters["amplitude"] = 3.0
        sw.parameters["frequency"] = 2.0  # T = 0.5 s
        sw.update(0.125, 0.0001, None)  # T/4
        assert abs(sw.outputs["value"] - 3.0) < 1e-9

    def test_at_half_period(self):
        """At t = T/2, sin -> 0."""
        sw = SineWave("sw")
        sw.parameters["amplitude"] = 5.0
        sw.parameters["frequency"] = 1.0
        sw.update(0.5, 0.0001, None)
        assert abs(sw.outputs["value"]) < 1e-9

    def test_rms_over_full_period(self):
        """RMS of sine over full period = A / sqrt(2)."""
        sw = SineWave("sw")
        sw.parameters["amplitude"] = 10.0
        sw.parameters["frequency"] = 10.0  # T = 0.1 s

        dt = 1e-5
        t_end = 0.1  # exactly one period
        n = int(t_end / dt)
        samples = []
        for k in range(n):
            t = k * dt
            sw.update(t, dt, None)
            samples.append(sw.outputs["value"])

        rms = math.sqrt(sum(v * v for v in samples) / len(samples))
        expected = 10.0 / math.sqrt(2.0)  # ~7.071
        assert abs(rms - expected) < 0.05


# ---------------------------------------------------------------------------
# ThreePhaseSine: line-to-line voltage relationship
# ---------------------------------------------------------------------------


class TestThreePhaseSineAnalytical:
    def test_line_to_line_amplitude(self):
        """U_ab peak = sqrt(3) * phase peak."""
        tp = ThreePhaseSine("tp")
        tp.parameters["amplitude"] = 100.0
        tp.parameters["frequency"] = 50.0

        dt = 1e-5
        t_end = 0.02  # one period
        max_ua = 0.0
        max_uab = 0.0
        for k in range(int(t_end / dt)):
            t = k * dt
            tp.update(t, dt, None)
            ua = tp.outputs["ua"]
            ub = tp.outputs["ub"]
            uab = ua - ub
            if abs(ua) > max_ua:
                max_ua = abs(ua)
            if abs(uab) > max_uab:
                max_uab = abs(uab)

        # Peak phase = 100 V, line-to-line ~ sqrt(3) * 100 = 173.2
        assert abs(max_ua - 100.0) < 0.5
        assert abs(max_uab - math.sqrt(3) * 100.0) < 1.0

    def test_clarke_peak_magnitude(self):
        """Clarke vector magnitude equals phase peak amplitude."""
        tp = ThreePhaseSine("tp")
        tp.parameters["amplitude"] = 200.0
        tp.parameters["frequency"] = 50.0

        dt = 1e-5
        t_end = 0.02
        for k in range(int(t_end / dt)):
            t = k * dt
            tp.update(t, dt, None)
            mag = math.sqrt(tp.outputs["u_alpha"] ** 2 + tp.outputs["u_beta"] ** 2)
            assert abs(mag - 200.0) < 0.5
