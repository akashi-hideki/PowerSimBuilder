"""Boundary and edge-case tests for Python runtime components.

Covers:
- zero/negative/NaN/Inf parameters
- zero dt, negative time
- parameter out-of-range
- empty/missing inputs
"""

from __future__ import annotations

import math

from python_runtime.components import (
    Clamp,
    FirstOrderLag,
    Gain,
    HeaterBlock,
    Integrator,
    LowPassFilter,
    MechanicalInertia,
    OverCurrentDetector,
    PWM3Phase,
    RateLimiter,
    SineWave,
    SineWaveSum,
    Sum,
    ThermalMass,
    ThreePhaseSine,
)

# ---------------------------------------------------------------------------
# SineWave
# ---------------------------------------------------------------------------


class TestSineWaveBoundary:
    def test_zero_amplitude(self):
        sw = SineWave("sw")
        sw.parameters["amplitude"] = 0.0
        sw.update(0.25, 0.001, None)
        assert sw.outputs["value"] == 0.0

    def test_zero_frequency(self):
        sw = SineWave("sw")
        sw.parameters["frequency"] = 0.0
        sw.parameters["amplitude"] = 5.0
        sw.update(0.5, 0.001, None)
        assert abs(sw.outputs["value"]) < 1e-9  # sin(0)=0

    def test_negative_start_time(self):
        sw = SineWave("sw")
        sw.parameters["start_time"] = -1.0
        sw.parameters["amplitude"] = 1.0
        sw.parameters["frequency"] = 1.0
        sw.update(0.25, 0.001, None)
        assert abs(sw.outputs["value"]) > 0.0


# ---------------------------------------------------------------------------
# HeaterBlock: rated_power <= 0 分岐
# ---------------------------------------------------------------------------


class TestHeaterBlockBoundary:
    def test_zero_rated_power(self):
        ht = HeaterBlock("ht")
        ht.parameters["rated_power"] = 0.0
        ht.inputs["voltage"] = 100.0
        ht.update(0.0, 0.01, None)
        # R -> 1e9, P -> ~1e-5
        assert ht.outputs["power"] < 1e-3

    def test_negative_rated_power(self):
        ht = HeaterBlock("ht")
        ht.parameters["rated_power"] = -100.0
        ht.inputs["voltage"] = 100.0
        ht.update(0.0, 0.01, None)
        assert ht.outputs["power"] >= 0.0  # no crash

    def test_zero_voltage_no_power(self):
        ht = HeaterBlock("ht")
        ht.inputs["voltage"] = 0.0
        ht.update(0.0, 0.01, None)
        assert ht.outputs["power"] == 0.0
        assert ht.outputs["current"] == 0.0


# ---------------------------------------------------------------------------
# Integrator: 飽和ブランチ + 異常 dt
# ---------------------------------------------------------------------------


class TestIntegratorBoundary:
    def test_upper_saturation_branch(self):
        i = Integrator("i")
        i.parameters["upper_limit"] = 5.0
        i.parameters["lower_limit"] = -5.0
        i.inputs["input"] = 100.0
        for k in range(100):
            i.update(k * 0.01, 0.01, None)
        assert i.outputs["output"] == 5.0

    def test_lower_saturation_branch(self):
        i = Integrator("i")
        i.parameters["upper_limit"] = 5.0
        i.parameters["lower_limit"] = -5.0
        i.inputs["input"] = -100.0
        for k in range(100):
            i.update(k * 0.01, 0.01, None)
        assert i.outputs["output"] == -5.0

    def test_zero_dt(self):
        i = Integrator("i")
        i.inputs["input"] = 1.0
        i.update(0.0, 0.0, None)
        assert i.outputs["output"] == 0.0

    def test_negative_dt(self):
        i = Integrator("i")
        i.inputs["input"] = 1.0
        i.update(0.0, -0.01, None)
        # negative dt decreases output
        assert i.outputs["output"] < 0.0


# ---------------------------------------------------------------------------
# MechanicalInertia: J <= 0 分岐
# ---------------------------------------------------------------------------


class TestMechanicalInertiaBoundary:
    def test_zero_inertia(self):
        mi = MechanicalInertia("mi")
        mi.parameters["inertia"] = 0.0
        mi.inputs["torque_drive"] = 1.0
        mi.update(0.0, 0.01, None)
        # a = 0 -> speed unchanged
        assert mi.outputs["speed"] == 0.0
        assert mi.outputs["accel"] == 0.0

    def test_negative_inertia(self):
        mi = MechanicalInertia("mi")
        mi.parameters["inertia"] = -0.01
        mi.inputs["torque_drive"] = 1.0
        mi.update(0.0, 0.01, None)
        # fallback: a = 0
        assert mi.outputs["accel"] == 0.0


# ---------------------------------------------------------------------------
# Sum: 空 inputs, input_count=0
# ---------------------------------------------------------------------------


class TestSumBoundary:
    def test_zero_input_count(self):
        s = Sum("s")
        s.parameters["input_count"] = 0
        s.parameters["gains"] = []
        s._sync_ports()
        s.update(0.0, 0.001, None)
        assert s.outputs["output"] == 0.0

    def test_input_count_exceeds_gains(self):
        s = Sum("s")
        s.parameters["input_count"] = 5
        s.parameters["gains"] = [1.0, 2.0]  # only 2 gains
        s._sync_ports()
        s.inputs["input_1"] = 1.0
        s.inputs["input_2"] = 1.0
        s.inputs["input_3"] = 1.0  # gain default 1.0
        s.update(0.0, 0.001, None)
        # 1*1 + 2*1 + 1*1 = 4
        assert s.outputs["output"] == 4.0


# ---------------------------------------------------------------------------
# PWM3Phase: コモンモード分岐
# ---------------------------------------------------------------------------


class TestPWM3PhaseBoundary:
    def test_no_common_mode(self):
        pwm = PWM3Phase("pwm")
        pwm.parameters["common_mode"] = False
        pwm.parameters["amplitude"] = 0.5
        pwm.update(0.001, 0.0001, None)
        assert 0.0 <= pwm.outputs["da"] <= 1.0
        assert 0.0 <= pwm.outputs["db"] <= 1.0

    def test_zero_amplitude(self):
        pwm = PWM3Phase("pwm")
        pwm.parameters["amplitude"] = 0.0
        pwm.update(0.001, 0.0001, None)
        assert abs(pwm.outputs["da"] - 0.5) < 1e-9


# ---------------------------------------------------------------------------
# OverCurrentDetector: non-latch branch
# ---------------------------------------------------------------------------


class TestOverCurrentDetectorBoundary:
    def test_non_latching_returns_below_hysteresis(self):
        ocd = OverCurrentDetector("ocd")
        ocd.parameters["threshold"] = 10.0
        ocd.parameters["hysteresis"] = 2.0
        ocd.parameters["latch"] = False
        ocd.inputs["current"] = 15.0
        ocd.update(0.0, 0.001, None)
        assert ocd.outputs["tripped"] == 1.0
        # below (threshold - hysteresis) = 8
        ocd.inputs["current"] = 5.0
        ocd.update(0.001, 0.001, None)
        assert ocd.outputs["tripped"] == 0.0

    def test_zero_threshold(self):
        ocd = OverCurrentDetector("ocd")
        ocd.parameters["threshold"] = 0.0
        ocd.inputs["current"] = 0.1
        ocd.update(0.0, 0.001, None)
        assert ocd.outputs["tripped"] == 1.0


# ---------------------------------------------------------------------------
# SineWaveSum: 余剰チャネル 0埋め
# ---------------------------------------------------------------------------


class TestSineWaveSumBoundary:
    def test_fewer_components_than_channels(self):
        sws = SineWaveSum("sws")
        sws.parameters["max_channels"] = 5
        sws.parameters["components"] = [
            {"amplitude": 1.0, "frequency": 1.0, "phase": 0.0, "offset": 0.0}
        ]
        sws._sync_outputs()
        sws.update(0.0, 0.001, None)
        # channels 2..5 should be 0
        for i in range(2, 6):
            assert sws.outputs[f"channel_{i}"] == 0.0


# ---------------------------------------------------------------------------
# FirstOrderLag: tau=0 分岐（既存テストに追加）
# ---------------------------------------------------------------------------


class TestFirstOrderLagBoundary:
    def test_negative_tau(self):
        fol = FirstOrderLag("fol")
        fol.parameters["time_constant"] = -1.0
        fol.inputs["input"] = 5.0
        fol.update(0.0, 0.001, None)
        assert fol.outputs["output"] == 5.0  # passthrough


# ---------------------------------------------------------------------------
# LowPassFilter
# ---------------------------------------------------------------------------


class TestLowPassFilterBoundary:
    def test_alpha_zero(self):
        lpf = LowPassFilter("lpf")
        lpf.parameters["cutoff_frequency"] = 0.0
        lpf.parameters["alpha"] = 0.0
        lpf.inputs["input"] = 10.0
        lpf.update(0.0, 0.001, None)
        assert lpf.outputs["output"] == 0.0  # no update

    def test_alpha_one(self):
        lpf = LowPassFilter("lpf")
        lpf.parameters["cutoff_frequency"] = 0.0
        lpf.parameters["alpha"] = 1.0
        lpf.inputs["input"] = 10.0
        lpf.update(0.0, 0.001, None)
        assert lpf.outputs["output"] == 10.0  # passthrough


# ---------------------------------------------------------------------------
# Clamp: reversed bounds
# ---------------------------------------------------------------------------


class TestClampBoundary:
    def test_reversed_bounds_input_below_lower(self):
        """Reversed bounds (lo=10, hi=0):
        - input below lo -> clamped to lo."""
        c = Clamp("c")
        c.parameters["lower"] = 10.0
        c.parameters["upper"] = 0.0
        c.inputs["input"] = 5.0
        c.update(0.0, 0.001, None)
        # 5 < 10 -> lower branch -> 10
        assert c.outputs["output"] == 10.0

    def test_reversed_bounds_input_above_upper(self):
        """Reversed bounds (lo=10, hi=0):
        - input above hi -> clamped to hi."""
        c = Clamp("c")
        c.parameters["lower"] = 10.0
        c.parameters["upper"] = 0.0
        c.inputs["input"] = 100.0
        c.update(0.0, 0.001, None)
        # 100 > 0 -> upper branch -> 0
        assert c.outputs["output"] == 0.0

    def test_input_exactly_at_bound(self):
        c = Clamp("c")
        c.parameters["lower"] = 0.0
        c.parameters["upper"] = 10.0
        c.inputs["input"] = 10.0
        c.update(0.0, 0.001, None)
        assert c.outputs["output"] == 10.0
        assert c.outputs["active"] == 0.0


# ---------------------------------------------------------------------------
# ThermalMass: R_th=0 分岐
# ---------------------------------------------------------------------------


class TestThermalMassBoundary:
    def test_zero_thermal_resistance(self):
        tm = ThermalMass("tm")
        tm.parameters["thermal_resistance"] = 0.0
        tm.inputs["heat_flow"] = 100.0
        tm.update(0.0, 1.0, None)
        # Q_out = 0 (avoid div by zero)
        assert tm.outputs["heat_loss"] == 0.0


# ---------------------------------------------------------------------------
# RateLimiter: rising=0, falling=0
# ---------------------------------------------------------------------------


class TestRateLimiterBoundary:
    def test_zero_rising_rate(self):
        rl = RateLimiter("rl")
        rl.parameters["rising_rate"] = 0.0
        rl.parameters["falling_rate"] = 0.0
        rl.inputs["input"] = 100.0
        rl.update(0.0, 0.1, None)
        assert rl.outputs["output"] == 0.0
        assert rl.outputs["saturated"] == 1.0


# ---------------------------------------------------------------------------
# ThreePhaseSine: sequence variants
# ---------------------------------------------------------------------------


class TestThreePhaseSineBoundary:
    def test_unknown_sequence_defaults_positive(self):
        tp = ThreePhaseSine("tp")
        tp.parameters["sequence"] = "unknown"
        tp.update(0.0, 0.0001, None)
        # same as positive sequence at t=0
        assert abs(tp.outputs["ua"]) < 1e-9

    def test_zero_sequence(self):
        tp = ThreePhaseSine("tp")
        tp.parameters["sequence"] = "zero"
        tp.parameters["amplitude"] = 100.0
        tp.update(0.001, 0.0001, None)
        # zero sequence: ua == ub == uc
        assert abs(tp.outputs["ua"] - tp.outputs["ub"]) < 1e-6
        assert abs(tp.outputs["ub"] - tp.outputs["uc"]) < 1e-6


# ---------------------------------------------------------------------------
# Gain: NaN input propagation
# ---------------------------------------------------------------------------


class TestGainBoundary:
    def test_nan_input_propagates(self):
        g = Gain("g")
        g.parameters["gain"] = 2.0
        g.inputs["input"] = float("nan")
        g.update(0.0, 0.001, None)
        assert math.isnan(g.outputs["output"])

    def test_inf_input(self):
        g = Gain("g")
        g.parameters["gain"] = 1.0
        g.inputs["input"] = float("inf")
        g.update(0.0, 0.001, None)
        assert math.isinf(g.outputs["output"])
