"""Tests for tools/i18n_check.py."""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parents[3] / "tools"
SPEC = importlib.util.spec_from_file_location("i18n_check", TOOLS_DIR / "i18n_check.py")
assert SPEC is not None and SPEC.loader is not None
i18n_check = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(i18n_check)


def _write(tmp_path: Path, name: str, content: str) -> Path:
    p = tmp_path / name
    p.write_text(content, encoding="utf-8")
    return p


def test_parse_valid(tmp_path):
    p = _write(tmp_path, "x.json", '{"a": "1"}\n')
    assert i18n_check.parse_error(p) is None


def test_parse_missing_comma(tmp_path):
    p = _write(tmp_path, "x.json", '{\n  "a": "1"\n  "b": "2"\n}\n')
    assert i18n_check.parse_error(p) is not None


def test_fix_missing_comma(tmp_path):
    p = _write(tmp_path, "x.json", '{\n  "a": "1"\n  "b": "2"\n}\n')
    n = i18n_check.fix_commas(p)
    assert n == 1
    assert i18n_check.parse_error(p) is None
    assert json.loads(p.read_text(encoding="utf-8")) == {"a": "1", "b": "2"}


def test_fix_trailing_comma(tmp_path):
    p = _write(tmp_path, "x.json", '{\n  "a": "1",\n}\n')
    n = i18n_check.fix_commas(p)
    assert n == 1
    assert i18n_check.parse_error(p) is None


def test_fix_no_change_when_valid(tmp_path):
    p = _write(tmp_path, "x.json", '{\n  "a": "1",\n  "b": "2"\n}\n')
    n = i18n_check.fix_commas(p)
    assert n == 0
    assert i18n_check.parse_error(p) is None


def test_fix_preserves_crlf(tmp_path):
    p = _write(tmp_path, "x.json", '{\r\n  "a": "1"\r\n  "b": "2"\r\n}\r\n')
    i18n_check.fix_commas(p)
    assert b"\r\n" in p.read_bytes()
    assert b"\n" not in p.read_bytes().replace(b"\r\n", b"")
