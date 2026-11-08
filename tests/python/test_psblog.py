"""Tests for psblog.py — psblog/v1 parser."""

from __future__ import annotations

from pathlib import Path

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
