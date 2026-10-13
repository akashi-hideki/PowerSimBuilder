"""Tests for OverCurrentDetector."""

from __future__ import annotations

import pytest

from python_runtime.components import OverCurrentDetector


def test_not_tripped_below_threshold():
    ocd = OverCurrentDetector("ocd")
    ocd.parameters["threshold"] = 10.0
    ocd.inputs["current"] = 5.0
    ocd.update(0.0, 0.001, None)
    assert ocd.outputs["tripped"] == 0.0


def test_trips_above_threshold():
    ocd = OverCurrentDetector("ocd")
    ocd.parameters["threshold"] = 10.0
    ocd.inputs["current"] = 15.0
    ocd.update(0.0, 0.001, None)
    assert ocd.outputs["tripped"] == 1.0


def test_latch_stays_tripped():
    ocd = OverCurrentDetector("ocd")
    ocd.parameters["threshold"] = 10.0
    ocd.parameters["latch"] = True
    ocd.inputs["current"] = 15.0
    ocd.update(0.0, 0.001, None)
    # current drops but latch keeps it tripped
    ocd.inputs["current"] = 1.0
    ocd.update(0.001, 0.001, None)
    assert ocd.outputs["tripped"] == 1.0


def test_reset_clears_latch():
    ocd = OverCurrentDetector("ocd")
    ocd.parameters["threshold"] = 10.0
    ocd.parameters["latch"] = True
    ocd.inputs["current"] = 15.0
    ocd.update(0.0, 0.001, None)
    ocd.inputs["current"] = 1.0
    ocd.inputs["reset"] = 1.0
    ocd.update(0.001, 0.001, None)
    assert ocd.outputs["tripped"] == 0.0


def test_peak_tracking():
    ocd = OverCurrentDetector("ocd")
    ocd.parameters["threshold"] = 100.0
    for i, cur in enumerate([5.0, 8.0, 12.0, 3.0]):
        ocd.inputs["current"] = cur
        ocd.update(i * 0.001, 0.001, None)
    assert ocd.outputs["peak"] == 12.0
