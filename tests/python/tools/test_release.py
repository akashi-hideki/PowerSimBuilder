"""Tests for tools/release.py."""

from __future__ import annotations

import importlib.util
from pathlib import Path
from unittest.mock import patch

import pytest

TOOLS_DIR = Path(__file__).resolve().parents[3] / "tools"
SPEC = importlib.util.spec_from_file_location("release", TOOLS_DIR / "release.py")
assert SPEC is not None and SPEC.loader is not None
release = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(release)


def test_parse_version() -> None:
    assert release.parse_version("v1.2.3") == (1, 2, 3)
    assert release.parse_version("v0.0.1") == (0, 0, 1)


def test_parse_version_invalid() -> None:
    with pytest.raises(ValueError):
        release.parse_version("1.2.3")
    with pytest.raises(ValueError):
        release.parse_version("v1.2")


def test_bump_version() -> None:
    assert release.bump_version((1, 2, 3), "patch") == (1, 2, 4)
    assert release.bump_version((1, 2, 3), "minor") == (1, 3, 0)
    assert release.bump_version((1, 2, 3), "major") == (2, 0, 0)


def test_format_version() -> None:
    assert release.format_version((1, 2, 3)) == "v1.2.3"


def test_categorize_conventional() -> None:
    assert release.categorize("feat(editor): add foo") == ("Features", "add foo")
    assert release.categorize("fix: bar") == ("Bug Fixes", "bar")
    assert release.categorize("docs: update") == ("Documentation", "update")
    assert release.categorize("test(ui): add tests") == ("Tests", "add tests")
    assert release.categorize("ci: bump") == ("CI", "bump")


def test_categorize_unknown() -> None:
    assert release.categorize("random message") == ("Other", "random message")
    assert release.categorize("wip: something") == ("Other", "something")


def test_categorize_breaking_change() -> None:
    assert release.categorize("feat!: breaking") == ("Features", "breaking")
    assert release.categorize("feat(api)!: breaking") == ("Features", "breaking")


def test_generate_sections() -> None:
    commits = [
        ("abc1234", "feat: first"),
        ("def5678", "fix: second"),
        ("ghi9012", "feat: third"),
    ]
    sections = release.generate_sections(commits)
    assert sections["Features"] == [("abc1234", "first"), ("ghi9012", "third")]
    assert sections["Bug Fixes"] == [("def5678", "second")]


def test_format_markdown_basic() -> None:
    sections = {
        "Features": [("abc1234", "add X")],
        "Bug Fixes": [("def5678", "fix Y")],
    }
    md = release.format_markdown("v1.2.3", sections)
    assert "## v1.2.3" in md
    assert "### Features" in md
    assert "- add X (abc1234)" in md
    assert "### Bug Fixes" in md
    assert "- fix Y (def5678)" in md
    assert "_Released on " in md


def test_format_markdown_extra_notes() -> None:
    md = release.format_markdown("v1.2.3", {"Features": [("a", "b")]}, extra_notes="Extra!")
    assert "Extra!" in md
    assert md.index("Extra!") < md.index("### Features")


def test_format_markdown_empty() -> None:
    md = release.format_markdown("v1.2.3", {})
    assert "## v1.2.3" in md
    for cat in ["Features", "Bug Fixes", "Documentation"]:
        assert f"### {cat}" not in md


def test_format_markdown_order() -> None:
    sections = {
        "Chores": [("c", "chore")],
        "Features": [("a", "feat")],
        "Bug Fixes": [("b", "fix")],
    }
    md = release.format_markdown("v1.0.0", sections)
    assert md.index("### Features") < md.index("### Bug Fixes")
    assert md.index("### Bug Fixes") < md.index("### Chores")


def test_tag_exists_missing_returns_false() -> None:
    """Regression: tag_exists must return False for a non-existent tag."""
    assert not release.tag_exists("v999.999.999-does-not-exist")


# ---------------------------------------------------------------------------
# Idempotency helpers (Trap #87)
# ---------------------------------------------------------------------------


def test_tag_exists_remote_detects_pushed_tag() -> None:
    # Pushed tag visible in `git ls-remote` output -> True
    with patch.object(release, "try_run", return_value=(True, "abc123\trefs/tags/v1.9.0")):
        assert release.tag_exists_remote("v1.9.0") is True

    # Remote responds but tag not present -> False
    with patch.object(release, "try_run", return_value=(True, "abc123\trefs/tags/v1.8.0")):
        assert release.tag_exists_remote("v1.9.0") is False

    # Remote unreachable -> False (safe default, tag will be re-pushed)
    with patch.object(release, "try_run", return_value=(False, "fatal: could not read")):
        assert release.tag_exists_remote("v1.9.0") is False


def test_release_exists_detects_existing_release() -> None:
    # gh release view succeeded -> True
    with patch.object(release, "try_run", return_value=(True, "title: v1.9.0")):
        assert release.release_exists("v1.9.0") is True

    # gh release view failed (404 / no auth) -> False
    with patch.object(release, "try_run", return_value=(False, "release not found")):
        assert release.release_exists("v999.0.0") is False


def test_main_idempotent_when_all_exist(tmp_path: Path, capsys) -> None:
    """All three states true -> main() returns 0 with [DONE] message.

    This is the Trap #87 regression guard: the previous version errored out
    with "Tag ... already exists" and returned 1, which made the release
    unrecoverable after a partial failure (tag pushed, gh auth expired).
    """
    notes = tmp_path / "notes.md"
    notes.write_text("# test notes\n", encoding="utf-8")
    argv = ["release.py", "v1.9.0", "--notes", str(notes), "--dry-run"]

    with (
        patch("sys.argv", argv),
        patch.object(release, "tag_exists", return_value=True),
        patch.object(release, "tag_exists_remote", return_value=True),
        patch.object(release, "release_exists", return_value=True),
    ):
        rc = release.main()

    captured = capsys.readouterr().out
    assert rc == 0
    assert "[DONE]" in captured
    assert "Nothing to do" in captured
    assert "[ERR]" not in captured
