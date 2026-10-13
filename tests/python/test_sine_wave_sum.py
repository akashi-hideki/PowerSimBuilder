"""Tests for SineWaveSum."""

from __future__ import annotations

from python_runtime.components import SineWaveSum


def test_empty_components_produces_zero():
    sws = SineWaveSum("sws")
    sws.parameters["components"] = []
    sws.update(0.0, 0.001, None)
    assert sws.outputs["value"] == 0.0


def test_single_component_matches_sine():
    sws = SineWaveSum("sws")
    sws.parameters["components"] = [
        {"amplitude": 2.0, "frequency": 1.0, "phase": 0.0, "offset": 0.0}
    ]
    sws.update(0.25, 0.001, None)
    assert abs(sws.outputs["value"] - 2.0) < 1e-9
    assert abs(sws.outputs["channel_1"] - 2.0) < 1e-9


def test_multi_component_sum():
    sws = SineWaveSum("sws")
    sws.parameters["components"] = [
        {"amplitude": 1.0, "frequency": 1.0, "phase": 0.0, "offset": 0.0},
        {"amplitude": 1.0, "frequency": 1.0, "phase": 0.0, "offset": 0.0},
    ]
    sws.update(0.25, 0.001, None)
    assert abs(sws.outputs["value"] - 2.0) < 1e-9


def test_channels_exposed():
    sws = SineWaveSum("sws")
    sws.parameters["max_channels"] = 3
    sws.parameters["components"] = [
        {"amplitude": 1.0, "frequency": 1.0, "phase": 0.0, "offset": 0.0}
    ]
    sws.update(0.0, 0.001, None)
    assert "channel_1" in sws.outputs
    assert "channel_2" in sws.outputs
    assert "channel_3" in sws.outputs
    assert sws.outputs["channel_2"] == 0.0
