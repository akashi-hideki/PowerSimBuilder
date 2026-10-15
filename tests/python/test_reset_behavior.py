"""Reset behavior tests for all 23 components."""

from __future__ import annotations

from python_runtime.components import (
    AnalogInput,
    AnalogOutput,
    Clamp,
    Counter,
    DigitalInput,
    DigitalOutput,
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
    SineWaveSum,
    StateMachine,
    Sum,
    ThermalMass,
    ThreePhaseSine,
    Timer,
)


def _check_reset_idempotent(comp):
    comp.reset()
    after1 = dict(comp.outputs)
    comp.reset()
    after2 = dict(comp.outputs)
    assert after1 == after2


class TestSineWaveReset:
    def test_reset_clears_outputs(self):
        sw = SineWave("sw")
        sw.update(0.5, 0.001, None)
        sw.reset()
        assert sw.outputs["value"] == 0.0
        assert sw.outputs["phase_rad"] == 0.0

    def test_reset_idempotent(self):
        _check_reset_idempotent(SineWave("sw"))


class TestSineWaveSumReset:
    def test_reset_clears_outputs(self):
        sws = SineWaveSum("sws")
        sws.parameters["components"] = [
            {"amplitude": 1.0, "frequency": 1.0, "phase": 0.0, "offset": 0.0}
        ]
        sws.update(0.25, 0.001, None)
        sws.reset()
        assert sws.outputs["value"] == 0.0
        assert sws.outputs["channel_1"] == 0.0

    def test_reset_idempotent(self):
        _check_reset_idempotent(SineWaveSum("sws"))


class TestThreePhaseSineReset:
    def test_reset_clears_all_phases(self):
        tp = ThreePhaseSine("tp")
        tp.update(0.001, 0.0001, None)
        tp.reset()
        for key in ("ua", "ub", "uc", "u_alpha", "u_beta"):
            assert tp.outputs[key] == 0.0

    def test_reset_idempotent(self):
        _check_reset_idempotent(ThreePhaseSine("tp"))


class TestHeaterBlockReset:
    def test_reset_restores_initial_temperature(self):
        ht = HeaterBlock("ht")
        ht.parameters["initial_temperature"] = 30.0
        ht.inputs["voltage"] = 200.0
        for k in range(10):
            ht.update(k * 0.01, 0.01, None)
        ht.reset()
        assert ht.outputs["temperature"] == 30.0
        assert ht.outputs["power"] == 0.0

    def test_reset_idempotent(self):
        _check_reset_idempotent(HeaterBlock("ht"))


class TestThermalMassReset:
    def test_reset_restores_initial(self):
        tm = ThermalMass("tm")
        tm.parameters["initial_temperature"] = 20.0
        tm.inputs["heat_flow"] = 100.0
        tm.update(0.0, 1.0, None)
        tm.reset()
        assert tm.outputs["temperature"] == 20.0

    def test_reset_idempotent(self):
        _check_reset_idempotent(ThermalMass("tm"))


class TestFirstOrderLagReset:
    def test_reset_restores_initial(self):
        fol = FirstOrderLag("fol")
        fol.parameters["initial_output"] = 5.0
        fol.reset()
        assert fol.outputs["output"] == 5.0

    def test_reset_idempotent(self):
        fol = FirstOrderLag("fol")
        fol.parameters["initial_output"] = 3.0
        fol.reset()
        assert fol.outputs["output"] == 3.0
        fol.reset()
        assert fol.outputs["output"] == 3.0


class TestIntegratorReset:
    def test_reset_restores_initial(self):
        integ = Integrator("i")
        integ.parameters["initial_output"] = 10.0
        integ.inputs["input"] = 1.0
        for k in range(10):
            integ.update(k * 0.01, 0.01, None)
        integ.reset()
        assert integ.outputs["output"] == 10.0

    def test_reset_idempotent(self):
        _check_reset_idempotent(Integrator("i"))


class TestLowPassFilterReset:
    def test_reset_restores_initial(self):
        lpf = LowPassFilter("lpf")
        lpf.parameters["initial_output"] = 7.0
        lpf.reset()
        assert lpf.outputs["output"] == 7.0

    def test_reset_idempotent(self):
        _check_reset_idempotent(LowPassFilter("lpf"))


class TestGainReset:
    def test_reset_clears_output(self):
        g = Gain("g")
        g.inputs["input"] = 10.0
        g.update(0.0, 0.001, None)
        assert g.outputs["output"] == 10.0
        g.reset()
        assert g.outputs["output"] == 0.0

    def test_reset_idempotent(self):
        _check_reset_idempotent(Gain("g"))


class TestSumReset:
    def test_reset_clears_inputs_and_output(self):
        s = Sum("s")
        s.inputs["input_1"] = 3.0
        s.inputs["input_2"] = 4.0
        s.update(0.0, 0.001, None)
        s.reset()
        assert s.outputs["output"] == 0.0
        for v in s.inputs.values():
            assert v == 0.0

    def test_reset_idempotent(self):
        _check_reset_idempotent(Sum("s"))


class TestClampReset:
    def test_reset_clears_output(self):
        c = Clamp("c")
        c.inputs["input"] = 5.0
        c.update(0.0, 0.001, None)
        c.reset()
        assert c.outputs["output"] == 0.0
        assert c.outputs["active"] == 0.0

    def test_reset_idempotent(self):
        _check_reset_idempotent(Clamp("c"))


class TestRateLimiterReset:
    def test_reset_restores_initial(self):
        rl = RateLimiter("rl")
        rl.parameters["initial_output"] = 0.0
        rl.inputs["input"] = 100.0
        rl.update(0.0, 0.1, None)
        rl.reset()
        assert rl.outputs["output"] == 0.0
        assert rl.outputs["saturated"] == 0.0

    def test_reset_idempotent(self):
        _check_reset_idempotent(RateLimiter("rl"))


class TestMechanicalInertiaReset:
    def test_reset_restores_initial_speed(self):
        mi = MechanicalInertia("mi")
        mi.parameters["initial_speed"] = 5.0
        mi.reset()
        assert mi.outputs["speed"] == 5.0

    def test_reset_idempotent(self):
        mi = MechanicalInertia("mi")
        mi.parameters["initial_speed"] = 2.0
        mi.reset()
        assert mi.outputs["speed"] == 2.0
        mi.reset()
        assert mi.outputs["speed"] == 2.0


class TestPWM3PhaseReset:
    def test_reset_restores_default_duties(self):
        pwm = PWM3Phase("pwm")
        pwm.update(0.001, 0.0001, None)
        pwm.reset()
        assert pwm.outputs["da"] == 0.5
        assert pwm.outputs["db"] == 0.5
        assert pwm.outputs["dc"] == 0.5
        assert pwm.outputs["modulation_index"] == 0.0

    def test_reset_idempotent(self):
        _check_reset_idempotent(PWM3Phase("pwm"))


class TestOverCurrentDetectorReset:
    def test_reset_clears_trip(self):
        ocd = OverCurrentDetector("ocd")
        ocd.parameters["threshold"] = 10.0
        ocd.inputs["current"] = 15.0
        ocd.update(0.0, 0.001, None)
        assert ocd.outputs["tripped"] == 1.0
        ocd.reset()
        assert ocd.outputs["tripped"] == 0.0
        assert ocd.outputs["peak"] == 0.0

    def test_reset_idempotent(self):
        _check_reset_idempotent(OverCurrentDetector("ocd"))


class TestCounterReset:
    def test_reset_clears_count(self):
        c = Counter("c")
        for k in range(5):
            c.inputs["clock"] = 1.0 if k % 2 == 0 else 0.0
            c.update(k * 0.01, 0.01, None)
        c.reset()
        assert c.outputs["count"] == 0

    def test_reset_idempotent(self):
        _check_reset_idempotent(Counter("c"))


class TestStateMachineReset:
    def test_reset_returns_to_initial_state(self):
        sm = StateMachine("sm")
        sm.parameters["initial_state"] = "IDLE"
        sm.reset()
        assert sm.outputs["current_state"] == "IDLE"

    def test_reset_idempotent(self):
        _check_reset_idempotent(StateMachine("sm"))


class TestTimerReset:
    def test_reset_clears_elapsed(self):
        t = Timer("t")
        t.inputs["trigger"] = 1.0
        for k in range(10):
            t.update(k * 0.01, 0.01, None)
        t.reset()
        assert t.outputs["done"] is False

    def test_reset_idempotent(self):
        _check_reset_idempotent(Timer("t"))


class TestPIDControllerReset:
    def test_reset_clears_integral(self):
        pid = PIDController("pid")
        pid.inputs["setpoint"] = 10.0
        pid.inputs["measured"] = 0.0
        for k in range(10):
            pid.update(k * 0.01, 0.01, None)
        pid.reset()
        _check_reset_idempotent(pid)


class TestAnalogInputReset:
    def test_reset_idempotent(self):
        _check_reset_idempotent(AnalogInput("ai"))


class TestAnalogOutputReset:
    def test_reset_idempotent(self):
        _check_reset_idempotent(AnalogOutput("ao"))


class TestDigitalInputReset:
    def test_reset_idempotent(self):
        _check_reset_idempotent(DigitalInput("di"))


class TestDigitalOutputReset:
    def test_reset_idempotent(self):
        _check_reset_idempotent(DigitalOutput("do"))
