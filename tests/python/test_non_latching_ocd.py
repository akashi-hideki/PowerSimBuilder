"""Tests for OverCurrentDetector non-latching mode."""

from __future__ import annotations

from python_runtime.components import OverCurrentDetector


def test_non_latch_resets():
    ocd = OverCurrentDetector("ocd")
    ocd.parameters["threshold"] = 10.0
    ocd.parameters["hysteresis"] = 1.0
    ocd.parameters["latch"] = False

    ocd.inputs["current"] = 15.0
    ocd.update(0.0, 0.001, None)
    assert ocd.outputs["tripped"] == 1.0

    # Drop below (threshold - hysteresis) = 9.0
    ocd.inputs["current"] = 8.0
    ocd.update(0.001, 0.001, None)
    assert ocd.outputs["tripped"] == 0.0


def test_hysteresis_zone_keeps_tripped():
    ocd = OverCurrentDetector("ocd")
    ocd.parameters["threshold"] = 10.0
    ocd.parameters["hysteresis"] = 2.0
    ocd.parameters["latch"] = False

    ocd.inputs["current"] = 15.0
    ocd.update(0.0, 0.001, None)
    assert ocd.outputs["tripped"] == 1.0

    # 9.5 is below threshold but above (10-2)=8, stays tripped
    ocd.inputs["current"] = 9.5
    ocd.update(0.001, 0.001, None)
    assert ocd.outputs["tripped"] == 1.0
