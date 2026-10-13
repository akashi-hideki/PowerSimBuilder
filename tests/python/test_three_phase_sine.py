"""Tests for ThreePhaseSine."""

from __future__ import annotations

import math

from python_runtime.components import ThreePhaseSine


def test_at_t_zero_ua_zero():
    tp = ThreePhaseSine("tp")
    tp.parameters["amplitude"] = 100.0
    tp.update(0.0, 0.001, None)
    assert abs(tp.outputs["ua"]) < 1e-9


def test_balanced_sum_is_zero():
    tp = ThreePhaseSine("tp")
    tp.parameters["amplitude"] = 100.0
    tp.parameters["frequency"] = 50.0
    for k in range(10):
        t = k * 0.001
        tp.update(t, 0.001, None)
        total = tp.outputs["ua"] + tp.outputs["ub"] + tp.outputs["uc"]
        assert abs(total) < 1e-6


def test_clarke_amplitude_invariant():
    tp = ThreePhaseSine("tp")
    tp.parameters["amplitude"] = 100.0
    tp.parameters["frequency"] = 50.0
    tp.update(0.0, 0.001, None)
    # alpha should be peak amplitude when ua=peak
    # Here ua=0, so check magnitude is invariant across time
    for k in range(5):
        t = k * 0.0005
        tp.update(t, 0.0005, None)
        m = math.sqrt(tp.outputs["u_alpha"] ** 2 + tp.outputs["u_beta"] ** 2)
        assert abs(m - 100.0) < 1e-6


def test_negative_sequence_swaps():
    tp_pos = ThreePhaseSine("pos")
    tp_pos.parameters["sequence"] = "positive"
    tp_pos.parameters["frequency"] = 50.0
    tp_pos.update(0.001, 0.0001, None)

    tp_neg = ThreePhaseSine("neg")
    tp_neg.parameters["sequence"] = "negative"
    tp_neg.parameters["frequency"] = 50.0
    tp_neg.update(0.001, 0.0001, None)

    # ub and uc swap
    assert abs(tp_pos.outputs["ub"] - tp_neg.outputs["uc"]) < 1e-6
