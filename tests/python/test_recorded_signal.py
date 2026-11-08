"""Tests for RecordedSignal component."""

from __future__ import annotations

import pathlib

from python_runtime.components.recorded_signal import RecordedSignal


def _sample_path() -> pathlib.Path:
    return (
        pathlib.Path(__file__).resolve().parents[2] / "examples" / "logs" / "sample_minimal.psblog"
    )


def test_empty_path_returns_zero() -> None:
    c = RecordedSignal("t")
    c.update(1.0, 0.01, {})
    assert c.outputs["value"] == 0.0


def test_loads_sample_log() -> None:
    c = RecordedSignal("t")
    c.parameters["psblog_path"] = str(_sample_path())
    c.parameters["var_name"] = "door_open"
    c.update(0.0, 0.01, {})
    assert c.outputs["value"] == 0
    c.update(0.1, 0.01, {})
    assert c.outputs["value"] == 1
    c.update(0.15, 0.01, {})
    assert c.outputs["value"] == 0


def test_hold_last() -> None:
    c = RecordedSignal("t")
    c.parameters["psblog_path"] = str(_sample_path())
    c.parameters["var_name"] = "door_open"
    c.parameters["hold_last"] = True
    c.update(10.0, 0.01, {})
    assert c.outputs["value"] == 0


def test_loop_wraps_around() -> None:
    c = RecordedSignal("t")
    c.parameters["psblog_path"] = str(_sample_path())
    c.parameters["var_name"] = "door_open"
    c.parameters["loop"] = True
    c.update(0.3, 0.01, {})
    assert 0.4 < c.outputs["value"] < 0.6


def test_reset_clears_state() -> None:
    c = RecordedSignal("t")
    c.parameters["psblog_path"] = str(_sample_path())
    c.parameters["var_name"] = "door_open"
    c.update(0.1, 0.01, {})
    assert c.outputs["value"] == 1
    c.reset()
    assert c.outputs["value"] == 0.0
    assert c._series is None
