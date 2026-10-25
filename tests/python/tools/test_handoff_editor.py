"""Tests for tools/handoff_editor.py (document model only, no GUI)."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest

_repo_root = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(_repo_root / "tools"))
from handoff_editor import TRAP_SECTION_TITLE, HandoffDocument  # noqa: E402

SAMPLE = """\
# HANDOFF

## v1.7.0 完了

Some v1.7.0 body.

## v1.6.1 完了

Some v1.6.1 body.

## 学んだ罠（累積）

| # | 罠 | 対策 |
|---|-----|------|
| 1 | first trap | first fix |
| 2 | second trap | second fix |
| 44 | last trap | last fix |

## 次タスク候補

| # | タスク | 内容 |
|---|--------|------|
| 1 | do something | misc |
| 2 | do another | misc |
"""


@pytest.fixture
def doc(tmp_path):
    p = tmp_path / "HANDOFF.md"
    p.write_text(SAMPLE, encoding="utf-8")
    return HandoffDocument(p)


def test_find_top_section(doc):
    idx = doc.find_top_section_index()
    assert doc.lines[idx].strip() == "## v1.7.0 完了"


def test_insert_section_above_top(doc):
    doc.insert_section("v1.8.0", "New body\n")
    text = "".join(doc.lines)
    assert text.index("## v1.8.0 完了") < text.index("## v1.7.0 完了")
    assert "New body" in text


def test_find_max_trap_id(doc):
    assert doc.find_max_trap_id() == 44


def test_append_trap_auto_id(doc):
    used = doc.append_trap("trap 45 desc", "trap 45 fix")
    assert used == 45
    assert "| 45 | trap 45 desc | trap 45 fix |" in "".join(doc.lines)


def test_append_trap_explicit_id(doc):
    used = doc.append_trap("desc", "fix", trap_id=99)
    assert used == 99
    assert "| 99 | desc | fix |" in "".join(doc.lines)


def test_trap_table_isolation(doc):
    """Trap #35: | 1 | rows in other tables must NOT be counted."""
    assert doc.find_max_trap_id() == 44


def test_trap_row_inserted_in_trap_section(doc):
    doc.append_trap("new desc", "new fix")
    text = "".join(doc.lines)
    assert text.index("| 45 |") < text.index("## 次タスク候補")
    assert text.index("| 45 |") > text.index(TRAP_SECTION_TITLE)


def test_roundtrip_preserves_content(tmp_path):
    p = tmp_path / "HANDOFF.md"
    p.write_text(SAMPLE, encoding="utf-8")
    doc = HandoffDocument(p)
    doc.save()
    assert p.read_text(encoding="utf-8") == SAMPLE


def test_no_trap_section_raises(tmp_path):
    p = tmp_path / "HANDOFF.md"
    p.write_text("# empty\n", encoding="utf-8")
    doc = HandoffDocument(p)
    with pytest.raises(ValueError, match="Trap section not found"):
        doc.find_trap_section()


def test_backup_creates_bak(tmp_path):
    p = tmp_path / "HANDOFF.md"
    p.write_text(SAMPLE, encoding="utf-8")
    doc = HandoffDocument(p)
    bak = doc.backup()
    assert bak.exists()
    assert bak.read_text(encoding="utf-8") == SAMPLE


def test_newlines_in_trap_are_collapsed(doc):
    """Multi-line input must be collapsed (markdown table rows are 1 line)."""
    used = doc.append_trap("line1\nline2", "fix\nfix")
    assert used == 45
    text = "".join(doc.lines)
    assert "| 45 | line1 line2 | fix fix |" in text
    for line in text.splitlines():
        if line.startswith("| 45 |"):
            assert "\n" not in line
            break
