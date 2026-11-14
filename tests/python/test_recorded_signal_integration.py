"""Integration: .psblog -> RecordedSignal -> non-zero output.

Guards against the scenario where psblog_path is set but update()
silently returns 0 (e.g. wrong API, missing file, or empty series).
"""

from __future__ import annotations

import pathlib

from python_runtime.components import RecordedSignal


def _sample_log() -> pathlib.Path:
    return (
        pathlib.Path(__file__).resolve().parents[2] / "examples" / "logs" / "sample_minimal.psblog"
    )


def test_recorded_signal_replays_psblog() -> None:
    """door_open sample: 0 -> 1 -> 0 -> 1 -> 0. Verify non-zero at t>=0.1."""
    c = RecordedSignal("t")
    c.parameters["psblog_path"] = str(_sample_log()).replace("\\", "/")
    c.parameters["var_name"] = "door_open"

    c.update(0.0, 0.01, {})
    assert c.outputs["value"] == 0

    c.update(0.1, 0.01, {})
    assert c.outputs["value"] == 1, f"expected 1 at t=0.1, got {c.outputs['value']}"

    c.update(0.15, 0.01, {})
    assert c.outputs["value"] == 0


def test_recorded_signal_missing_file_returns_zero() -> None:
    """Missing file should not raise; returns 0 silently."""
    c = RecordedSignal("t")
    c.parameters["psblog_path"] = "/nonexistent/file.psblog"
    c.parameters["var_name"] = "anything"
    c.update(0.0, 0.01, {})
    assert c.outputs["value"] == 0.0


def test_recorded_signal_missing_var_returns_zero() -> None:
    """Var not in VarTable -> empty series -> 0."""
    c = RecordedSignal("t")
    c.parameters["psblog_path"] = str(_sample_log()).replace("\\", "/")
    c.parameters["var_name"] = "not_a_real_var"
    c.update(0.0, 0.01, {})
    assert c.outputs["value"] == 0.0
