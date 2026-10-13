"""Tests for Sum."""

from __future__ import annotations

from python_runtime.components import Sum


def test_two_inputs():
    s = Sum("s")
    s.parameters["input_count"] = 2
    s.parameters["gains"] = [1.0, 1.0]
    s.inputs["input_1"] = 3.0
    s.inputs["input_2"] = 4.0
    s.update(0.0, 0.001, None)
    assert s.outputs["output"] == 7.0


def test_weighted_sum():
    s = Sum("s")
    s.parameters["input_count"] = 2
    s.parameters["gains"] = [1.0, 2.0]
    s.inputs["input_1"] = 3.0
    s.inputs["input_2"] = 4.0
    s.update(0.0, 0.001, None)
    assert s.outputs["output"] == 11.0


def test_three_inputs():
    s = Sum("s")
    s.parameters["input_count"] = 3
    s.parameters["gains"] = [1.0, 1.0, 1.0]
    s.inputs["input_1"] = 1.0
    s.inputs["input_2"] = 2.0
    s.inputs["input_3"] = 3.0
    s.update(0.0, 0.001, None)
    assert s.outputs["output"] == 6.0
