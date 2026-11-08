"""Tests for psblog.py — psblog/v1 parser."""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

import pytest

from python_runtime.psblog import parse_psblog


def _write(tmp_path: Path, content: str, name: str = "test.psblog") -> Path:
    p = tmp_path / name
    p.write_text(content, encoding="utf-8", newline="\n")
    return p


def test_parse_minimal_valid(tmp_path: Path) -> None:
    content = (
        "# minimal\n"
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        "@source uart:COM3\n"
        "\n"
        '@var oven_current_temp uint16_t degC Hardware "Current oven temperature"\n'
        "\n"
        "@0 oven_current_temp=25\n"
        "@10 oven_current_temp=26\n"
        "@20 oven_current_temp=27\n"
        "\n"
        "@end 2026-11-09T10:30:30+09:00\n"
        "@samples 3\n"
        "@dropped 0\n"
    )
    p = _write(tmp_path, content)
    trace = parse_psblog(p)
    assert trace.header["schema"] == "psblog/v1"
    assert trace.header["source"] == "uart:COM3"
    assert "oven_current_temp" in trace.vars
    m = trace.vars["oven_current_temp"]
    assert (m.type, m.unit, m.group) == ("uint16_t", "degC", "Hardware")
    assert m.description == "Current oven temperature"
    assert trace.rows == [
        (0, "oven_current_temp", 25),
        (10, "oven_current_temp", 26),
        (20, "oven_current_temp", 27),
    ]
    assert trace.footer["samples"] == "3"
    assert trace.warnings == 0


def test_var_set_only_one_variable(tmp_path: Path) -> None:
    """SPEC §1: variable set is per-log. 1-variable log is valid."""
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        "@var door_open uint8_t - Hardware\n"
        "@0 door_open=0\n"
        "@10 door_open=1\n"
    )
    p = _write(tmp_path, content)
    trace = parse_psblog(p)
    assert list(trace.vars.keys()) == ["door_open"]
    assert trace.vars["door_open"].unit == ""  # "-" は "" に正規化
    assert trace.rows == [(0, "door_open", 0), (10, "door_open", 1)]
    assert trace.warnings == 0


def test_utf8_no_bom_lf(tmp_path: Path) -> None:
    """SPEC §4: UTF-8 (BOM なし) + LF。日本語 description も受理。"""
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        '@var g_system_tick uint32_t 1ms Timer "システム ティック"\n'
        "@0 g_system_tick=0\n"
    )
    p = _write(tmp_path, content)
    trace = parse_psblog(p)
    assert "g_system_tick" in trace.vars
    assert "システム" in trace.vars["g_system_tick"].description
    assert trace.warnings == 0


def test_bom_tolerated(tmp_path: Path) -> None:
    """SPEC §4.1: Writer emits no BOM, but Reader is defensive (utf-8-sig)."""
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        "@var door_open uint8_t - Hardware\n"
        "@0 door_open=0\n"
    )
    p = tmp_path / "bom.psblog"
    p.write_text(content, encoding="utf-8-sig", newline="\n")  # BOM 付き
    trace = parse_psblog(p)
    assert trace.header["schema"] == "psblog/v1"
    assert "door_open" in trace.vars
    assert trace.rows == [(0, "door_open", 0)]
    assert trace.warnings == 0


def _sample_path(name: str) -> Path:
    return Path(__file__).resolve().parents[2] / "examples" / "logs" / name


# ---------------------------------------------------------------------------
# Tests #5-#24 (Phase 2A-2)
# ---------------------------------------------------------------------------


def test_parse_full_spec_example() -> None:
    """SPEC §11 full example (sample_cooking_heater.psblog)."""
    p = _sample_path("sample_cooking_heater.psblog")
    trace = parse_psblog(p)
    assert trace.header["schema"] == "psblog/v1"
    assert trace.header["source"] == "uart:COM3"
    assert len(trace.vars) == 9
    assert "oven_current_temp" in trace.vars
    assert trace.warnings == 0
    assert len(trace.rows) > 0


def test_header_fields(tmp_path: Path) -> None:
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00.000+09:00\n"
        "@source uart:COM3\n"
        "@firmware StaTable v1.9.0\n"
        "@protocol pstable-uart/v1\n"
        "@baud 115200\n"
        "@period_ms 10\n"
        "@var x uint8_t - A\n"
    )
    trace = parse_psblog(_write(tmp_path, content))
    for k in ("schema", "captured_at", "source", "firmware", "protocol", "baud", "period_ms"):
        assert k in trace.header, k
    assert trace.header["baud"] == "115200"


def test_vartable_parsing(tmp_path: Path) -> None:
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        '@var a uint16_t degC Hardware "Desc with spaces"\n'
        '@var b float32 % Hardware "Val" role=input\n'
        "@var c raw - System\n"
    )
    trace = parse_psblog(_write(tmp_path, content))
    assert trace.vars["a"].type == "uint16_t"
    assert trace.vars["a"].unit == "degC"
    assert trace.vars["a"].description == "Desc with spaces"
    assert trace.vars["b"].role == "input"
    assert trace.vars["c"].type == "raw"
    assert trace.vars["c"].unit == ""


def test_data_multiple_kv_per_line(tmp_path: Path) -> None:
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        "@var a uint8_t - A\n"
        "@var b uint8_t - B\n"
        "@var c uint8_t - C\n"
        "@100 a=1 b=2 c=3\n"
    )
    trace = parse_psblog(_write(tmp_path, content))
    assert trace.rows == [(100, "a", 1), (100, "b", 2), (100, "c", 3)]


def test_data_same_t_ms_multiple_lines(tmp_path: Path) -> None:
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        "@var a uint8_t - A\n"
        "@var b uint8_t - B\n"
        "@10 a=5\n"
        "@10 b=6\n"
    )
    trace = parse_psblog(_write(tmp_path, content))
    assert trace.rows == [(10, "a", 5), (10, "b", 6)]


def test_footer_parsing(tmp_path: Path) -> None:
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        "@var a uint8_t - A\n"
        "@0 a=1\n"
        "@end 2026-11-09T10:35:00+09:00\n"
        "@samples 30000\n"
        "@dropped 2\n"
    )
    trace = parse_psblog(_write(tmp_path, content))
    assert trace.footer["end"].startswith("2026-11-09T10:35")
    assert trace.footer["samples"] == "30000"
    assert trace.footer["dropped"] == "2"


def test_footer_absent(tmp_path: Path) -> None:
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        "@var a uint8_t - A\n"
        "@0 a=1\n"
    )
    trace = parse_psblog(_write(tmp_path, content))
    assert trace.footer == {}
    assert trace.warnings == 0


def test_empty_data(tmp_path: Path) -> None:
    content = (
        "@schema psblog/v1\n" "@captured_at 2026-11-09T10:30:00+09:00\n" "@var a uint8_t - A\n"
    )
    trace = parse_psblog(_write(tmp_path, content))
    assert trace.rows == []
    assert trace.warnings == 0


def test_unknown_at_line_ignored(tmp_path: Path) -> None:
    """Unknown @meta is ignored silently (SPEC §10.1)."""
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        "@future_meta something\n"
        "@var a uint8_t - A\n"
        "@0 a=1\n"
    )
    trace = parse_psblog(_write(tmp_path, content))
    assert trace.rows == [(0, "a", 1)]
    assert trace.warnings == 0


def test_unknown_var_token_skipped(tmp_path: Path) -> None:
    """Data line with undeclared var -> token skipped, warnings++."""
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        "@var a uint8_t - A\n"
        "@0 a=1 unknown_var=99\n"
        "@10 a=2\n"
    )
    trace = parse_psblog(_write(tmp_path, content))
    assert trace.rows == [(0, "a", 1), (10, "a", 2)]
    assert trace.warnings == 1


def test_numeric_parse_failure_skipped(tmp_path: Path) -> None:
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        "@var a uint8_t - A\n"
        "@0 a=1\n"
        "@10 a=abc\n"
        "@20 a=3\n"
    )
    trace = parse_psblog(_write(tmp_path, content))
    assert trace.rows == [(0, "a", 1), (20, "a", 3)]
    assert trace.warnings == 1


def test_corrupt_line_skipped_continues(tmp_path: Path) -> None:
    """A corrupt line does not stop subsequent parsing."""
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        "@var a uint8_t - A\n"
        "@0 a=1\n"
        "@@garbage line here\n"
        "@10 a=2\n"
    )
    trace = parse_psblog(_write(tmp_path, content))
    assert (0, "a", 1) in trace.rows
    assert (10, "a", 2) in trace.rows
    assert trace.warnings >= 1


def test_type_mapping_int(tmp_path: Path) -> None:
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        "@var a uint8_t - A\n"
        "@var b int16_t - B\n"
        "@0 a=255 b=-100\n"
    )
    trace = parse_psblog(_write(tmp_path, content))
    assert trace.rows == [(0, "a", 255), (0, "b", -100)]
    assert isinstance(trace.rows[0][2], int)


def test_type_mapping_float(tmp_path: Path) -> None:
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        "@var a float32 - A\n"
        "@0 a=1.5\n"
    )
    trace = parse_psblog(_write(tmp_path, content))
    assert trace.rows == [(0, "a", 1.5)]
    assert isinstance(trace.rows[0][2], float)


def test_type_mapping_bool(tmp_path: Path) -> None:
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        "@var flag bool - F\n"
        "@0 flag=0\n"
        "@10 flag=1\n"
    )
    trace = parse_psblog(_write(tmp_path, content))
    assert trace.rows == [(0, "flag", False), (10, "flag", True)]
    assert isinstance(trace.rows[0][2], bool)


def test_role_hint_parsed(tmp_path: Path) -> None:
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        '@var t uint16_t degC Hardware "Target" role=target\n'
        '@var p uint8_t % Hardware "PWM" role=pid_out\n'
    )
    trace = parse_psblog(_write(tmp_path, content))
    assert trace.vars["t"].role == "target"
    assert trace.vars["p"].role == "pid_out"


def test_series_preserves_arrival_order(tmp_path: Path) -> None:
    """Out-of-order t_ms is preserved as-is (SPEC §2.3)."""
    content = (
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:30:00+09:00\n"
        "@var a uint8_t - A\n"
        "@10 a=2\n"
        "@0 a=1\n"
        "@20 a=3\n"
    )
    trace = parse_psblog(_write(tmp_path, content))
    assert trace.series("a") == [(0.01, 2.0), (0.0, 1.0), (0.02, 3.0)]


def test_var_set_pid_tuning_subset() -> None:
    """SPEC §1: PID-tuning subset (4 vars) is valid."""
    p = _sample_path("sample_pid_tuning.psblog")
    trace = parse_psblog(p)
    assert set(trace.vars.keys()) == {
        "oven_target_temp",
        "oven_current_temp",
        "heater_top_duty",
        "error_code",
    }
    assert trace.warnings == 0


def test_var_set_diagnostic_subset() -> None:
    """SPEC §1: diagnostic subset (3 vars) is valid."""
    p = _sample_path("sample_diagnostic.psblog")
    trace = parse_psblog(p)
    assert set(trace.vars.keys()) == {"zc_count", "ir_value", "steam_sensor"}
    assert trace.warnings == 0


def test_different_var_sets_are_independent() -> None:
    """SPEC §1: three logs with different var sets parse independently."""
    minimal = parse_psblog(_sample_path("sample_minimal.psblog"))
    pid = parse_psblog(_sample_path("sample_pid_tuning.psblog"))
    diag = parse_psblog(_sample_path("sample_diagnostic.psblog"))
    assert set(minimal.vars.keys()) == {"door_open"}
    assert len(pid.vars) == 4
    assert len(diag.vars) == 3
    # No cross-contamination
    assert "door_open" not in pid.vars
    assert "zc_count" not in minimal.vars


# ---------------------------------------------------------------------------
# Phase 2B: glob + CLI tests (#25-#29)
# ---------------------------------------------------------------------------


def test_glob_concatenates_rotated_files() -> None:
    """SPEC §4.3: <base>.psblog + <base>.1.psblog are merged."""
    from python_runtime.psblog import parse_psblog_glob

    p = _sample_path("sample_rotated/log.psblog")
    trace = parse_psblog_glob(p)
    assert len(trace.source_files) == 2
    assert len(trace.rows) > 0
    t_values = [r[0] for r in trace.rows]
    assert t_values == sorted(t_values)


def test_glob_resorts_by_captured_at(tmp_path: Path) -> None:
    """Files are sorted by @captured_at even if names are reversed."""
    from python_runtime.psblog import parse_psblog_glob

    (tmp_path / "log.psblog").write_text(
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:00:10+09:00\n"
        "@var a uint8_t - A\n"
        "@0 a=2\n",
        encoding="utf-8",
        newline="\n",
    )
    (tmp_path / "log.1.psblog").write_text(
        "@schema psblog/v1\n"
        "@captured_at 2026-11-09T10:00:00+09:00\n"
        "@var a uint8_t - A\n"
        "@0 a=1\n",
        encoding="utf-8",
        newline="\n",
    )
    trace = parse_psblog_glob(tmp_path / "log.psblog")
    assert (0, "a", 1) in trace.rows
    assert (10000, "a", 2) in trace.rows


def test_schema_mismatch_raises(tmp_path: Path) -> None:
    content = (
        "@schema psblog/v2\n" "@captured_at 2026-11-09T10:30:00+09:00\n" "@var a uint8_t - A\n"
    )
    with pytest.raises(ValueError, match="unsupported schema"):
        parse_psblog(_write(tmp_path, content))


def test_missing_captured_at_raises(tmp_path: Path) -> None:
    content = "@schema psblog/v1\n" "@var a uint8_t - A\n" "@0 a=1\n"
    with pytest.raises(ValueError, match="missing @captured_at"):
        parse_psblog(_write(tmp_path, content))


def test_cli_json_out(tmp_path: Path) -> None:
    """CLI: python -m python_runtime.psblog <file> --json-out <out>."""
    sample = _sample_path("sample_minimal.psblog")
    out = tmp_path / "out.json"
    result = subprocess.run(
        [
            sys.executable,
            "-m",
            "python_runtime.psblog",
            str(sample),
            "--json-out",
            str(out),
        ],
        capture_output=True,
        text=True,
        cwd=str(Path(__file__).resolve().parents[2]),
    )
    assert result.returncode == 0, result.stderr
    assert out.is_file()
    data = json.loads(out.read_text(encoding="utf-8"))
    assert data["header"]["schema"] == "psblog/v1"
    assert "door_open" in data["vars"]
    assert data["warnings"] == 0
