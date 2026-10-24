"""One-command release: tag + release notes + GitHub Release.

Usage:
    python tools/release.py v1.2.6
    python tools/release.py --auto minor
    python tools/release.py v1.2.6 --notes RELEASE_NOTES.md
    python tools/release.py v1.2.6 --dry-run
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent

CATEGORY_MAP: dict[str, str] = {
    "feat": "Features",
    "fix": "Bug Fixes",
    "docs": "Documentation",
    "test": "Tests",
    "ci": "CI",
    "refactor": "Refactoring",
    "perf": "Performance",
    "style": "Style",
    "build": "Build",
    "chore": "Chores",
}

ORDER = [
    "Features",
    "Bug Fixes",
    "Documentation",
    "Tests",
    "CI",
    "Refactoring",
    "Performance",
    "Style",
    "Build",
    "Chores",
    "Other",
]


def run(cmd: list[str]) -> str:
    """Run a command in REPO_ROOT and return stripped stdout."""
    return subprocess.check_output(
        cmd, cwd=REPO_ROOT, text=True, encoding="utf-8"
    ).strip()


def try_run(cmd: list[str]) -> tuple[bool, str]:
    """Run a command; return (success, stdout). Suppresses stderr noise."""
    result = subprocess.run(
        cmd,
        cwd=REPO_ROOT,
        text=True,
        encoding="utf-8",
        capture_output=True,
        check=False,
    )
    return result.returncode == 0, result.stdout.strip()


def get_latest_tag() -> str | None:
    ok, out = try_run(["git", "describe", "--tags", "--abbrev=0"])
    return out if ok and out else None


def parse_version(tag: str) -> tuple[int, int, int]:
    m = re.match(r"^v(\d+)\.(\d+)\.(\d+)$", tag)
    if not m:
        raise ValueError(f"Unrecognized tag format: {tag}")
    return int(m.group(1)), int(m.group(2)), int(m.group(3))


def bump_version(version: tuple[int, int, int], kind: str) -> tuple[int, int, int]:
    major, minor, patch = version
    if kind == "major":
        return major + 1, 0, 0
    if kind == "minor":
        return major, minor + 1, 0
    return major, minor, patch + 1


def format_version(v: tuple[int, int, int]) -> str:
    return f"v{v[0]}.{v[1]}.{v[2]}"


def get_commits_since(tag: str | None) -> list[tuple[str, str]]:
    if tag:
        log = run(["git", "log", f"{tag}..HEAD", "--pretty=format:%h%x09%s", "--reverse"])
    else:
        log = run(["git", "log", "--pretty=format:%h%x09%s", "--reverse"])
    out: list[tuple[str, str]] = []
    for line in log.splitlines():
        if not line.strip():
            continue
        sha, _, subject = line.partition("\t")
        out.append((sha, subject.strip()))
    return out


def categorize(subject: str) -> tuple[str, str]:
    m = re.match(r"^(\w+)(\([^)]+\))?!?:\s*(.+)$", subject)
    if not m:
        return "Other", subject
    kind = m.group(1).lower()
    rest = m.group(3).strip()
    return CATEGORY_MAP.get(kind, "Other"), rest


def generate_sections(commits: list[tuple[str, str]]) -> dict[str, list[tuple[str, str]]]:
    sections: dict[str, list[tuple[str, str]]] = {}
    for sha, subject in commits:
        cat, msg = categorize(subject)
        sections.setdefault(cat, []).append((sha, msg))
    return sections


def format_markdown(
    version: str,
    sections: dict[str, list[tuple[str, str]]],
    extra_notes: str | None = None,
) -> str:
    lines: list[str] = [f"## {version}", ""]
    if extra_notes and extra_notes.strip():
        lines.append(extra_notes.strip())
        lines.append("")
    for cat in ORDER:
        if cat not in sections:
            continue
        lines.append(f"### {cat}")
        for sha, msg in sections[cat]:
            lines.append(f"- {msg} ({sha})")
        lines.append("")
    today = datetime.now(timezone.utc).strftime("%Y-%m-%d")
    lines.append(f"_Released on {today}_")
    return "\n".join(lines)


def check_preconditions() -> list[str]:
    errs: list[str] = []
    if run(["git", "status", "--porcelain"]):
        errs.append("Uncommitted changes; commit or stash first.")
    branch = run(["git", "rev-parse", "--abbrev-ref", "HEAD"])
    if branch != "main":
        errs.append(f"Not on main branch (current: {branch}).")
    ok, _ = try_run(["git", "fetch", "origin"])
    if not ok:
        errs.append("Failed to fetch origin.")
    local = run(["git", "rev-parse", "HEAD"])
    ok, remote = try_run(["git", "rev-parse", "origin/main"])
    if ok and remote and local != remote:
        errs.append(f"HEAD ({local[:7]}) != origin/main ({remote[:7]}). Push first.")
    return errs


def tag_exists(tag: str) -> bool:
    ok, _ = try_run(["git", "rev-parse", tag])
    return ok


def create_tag(version: str, message: str, dry_run: bool) -> None:
    if dry_run:
        print(f"[DRY] git tag -a {version} -m {message!r}")
        return
    run(["git", "tag", "-a", version, "-m", message])
    print(f"[OK] Tagged {version}")


def push_tag(version: str, dry_run: bool) -> None:
    if dry_run:
        print(f"[DRY] git push origin {version}")
        return
    run(["git", "push", "origin", version])
    print(f"[OK] Pushed {version}")


def create_release(version: str, markdown: str, title: str | None, dry_run: bool) -> None:
    actual_title = title or version
    if dry_run:
        print(f"[DRY] gh release create {version} --title {actual_title!r} --notes-file <temp>")
        print(f"[DRY] notes:\n{markdown}")
        return
    with tempfile.NamedTemporaryFile("w", suffix=".md", delete=False, encoding="utf-8") as f:
        f.write(markdown)
        notes_path = f.name
    try:
        run(
            [
                "gh",
                "release",
                "create",
                version,
                "--title",
                actual_title,
                "--notes-file",
                notes_path,
            ]
        )
        print(f"[OK] Released {version}")
    finally:
        Path(notes_path).unlink(missing_ok=True)


def resolve_version(args: argparse.Namespace) -> str:
    if args.version:
        return args.version
    latest = get_latest_tag()
    if not latest:
        print("[ERR] No tags in repo; specify version explicitly.", file=sys.stderr)
        sys.exit(1)
    return format_version(bump_version(parse_version(latest), args.auto))


def main() -> int:
    parser = argparse.ArgumentParser(
        description="One-command release: tag + notes + GitHub Release."
    )
    parser.add_argument("version", nargs="?", help="Tag (e.g. v1.2.6).")
    parser.add_argument(
        "--auto",
        choices=["major", "minor", "patch"],
        default="patch",
        help="Auto-bump kind when version is omitted.",
    )
    parser.add_argument("--notes", help="Path to a Markdown file with extra notes.")
    parser.add_argument("--title", help="Release title (default: version).")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--skip-checks", action="store_true")
    parser.add_argument("--with-catalog", action="store_true", help="Build and attach FMU catalog to release.")
    args = parser.parse_args()

    catalog_assets = None
    if args.with_catalog:
        print("[INFO] Building FMU catalog...")
        rc = subprocess.run(
            [sys.executable, "tools/build_cross_check_catalog.py", "--output", "Resources/generated_catalog.json"]
        ).returncode
        if rc != 0:
            print("[ERR] Catalog build failed", file=sys.stderr)
            return 1
        catalog_assets = [Path("Resources/generated_catalog.json")]

    version = resolve_version(args)
    print(f"[INFO] Target version: {version}")

    if not args.skip_checks:
        errs = check_preconditions()
        if errs:
            print("[ERR] Pre-flight failed:", file=sys.stderr)
            for e in errs:
                print(f"  - {e}", file=sys.stderr)
            return 1

    if tag_exists(version):
        print(f"[ERR] Tag {version} already exists.", file=sys.stderr)
        return 1

    latest = get_latest_tag()
    commits = get_commits_since(latest)
    if not commits:
        print("[ERR] No commits since last tag.", file=sys.stderr)
        return 1

    extra = None
    if args.notes:
        extra_path = Path(args.notes).resolve()
        if extra_path.is_file():
            extra = extra_path.read_text(encoding="utf-8")
        else:
            print(f"[WARN] Notes file not found: {extra_path}")

    sections = generate_sections(commits)
    md = format_markdown(version, sections, extra_notes=extra)

    print("\n----- Release Notes -----")
    print(md)
    print("-------------------------\n")

    tag_message = args.title or f"{version} - release"
    create_tag(version, tag_message, args.dry_run)
    push_tag(version, args.dry_run)
    create_release(version, md, args.title, args.dry_run)

    if args.dry_run:
        print("[DRY] No changes made.")
    # (create_release already printed "[OK] Released ...")

    if catalog_assets:
        if args.dry_run:
            print(f"[DRY] gh release upload {version} {" ".join(str(a) for a in catalog_assets)}")
        else:
            run(["gh", "release", "upload", version, *[str(a) for a in catalog_assets]])
            print(f"[OK] Catalog uploaded: {", ".join(str(a) for a in catalog_assets)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
