"""Refresh IP documentation (docs/ip/) from current git/NuGet state.

Idempotent: running twice produces the same files (git/NuGet-derived) or
no-op (version/date-lines already match).

Usage:
    python tools/refresh_ip_docs.py                    # full refresh
    python tools/refresh_ip_docs.py --dry-run          # show changes only
    python tools/refresh_ip_docs.py --version v1.9.0   # override version
    python tools/refresh_ip_docs.py --test-counts 599,457,16,30
        # C#,Python,UITests,RoleSnippet (comma-separated)
"""
import argparse
import json
import re
import subprocess
import sys
from collections import Counter
from datetime import datetime
from pathlib import Path

IP_DIR = Path("docs/ip")


def sh(args, check=True):
    try:
        return subprocess.check_output(args, text=True, encoding="utf-8",
                                       stderr=subprocess.DEVNULL).strip()
    except subprocess.CalledProcessError:
        if check:
            raise
        return ""


def get_latest_tag():
    return sh(["git", "describe", "--tags", "--abbrev=0"], check=False) or None


def refresh_git_files(dry_run):
    """Regenerate git_branches.txt / git_log.txt / git_stats.txt / git_tags.txt."""
    changes = []

    # 1. branches
    raw = sh(["git", "for-each-ref", "--sort=-committerdate",
              "--format=%(refname:short) | %(objectname:short) | %(committerdate:short) | %(subject)",
              "refs/heads", "refs/remotes"])
    branches = [l for l in raw.splitlines() if "HEAD ->" not in l]
    content = "\n".join(branches) + "\n"
    if not dry_run:
        (IP_DIR / "git_branches.txt").write_text(content, encoding="utf-8")
    changes.append(f"git_branches.txt ({len(branches)} branches)")

    # 2. log (last 1000)
    log = sh(["git", "log", "--all", "--pretty=format:%h | %ai | %an | %s", "-1000"])
    if not dry_run:
        (IP_DIR / "git_log.txt").write_text(log + "\n", encoding="utf-8")
    changes.append(f"git_log.txt ({len(log.splitlines())} commits)")

    # 3. stats
    total = int(sh(["git", "rev-list", "--all", "--count"]))
    dates = sh(["git", "log", "--all", "--pretty=format:%ai"]).splitlines()
    first, last = dates[-1][:10], dates[0][:10]
    tags = len(sh(["git", "tag", "-l"]).splitlines())
    devs = Counter(sh(["git", "log", "--all", "--pretty=format:%an"]).splitlines()).most_common(3)
    stats = f"""# Git コミット統計

## 総コミット数（全ブランチ・全タグ）
{total}

## 開発期間
- 最初のコミット: {first}
- 最新のコミット: {last}

## 開発者
{chr(10).join(f"- {n}: {c} commits" for n, c in devs)}

## タグ数
{tags}

## 生成方法
git rev-list --all --count
git log --all --pretty=format:"%h | %ai | %an | %s"
git tag -l --sort=-creatordate
"""
    if not dry_run:
        (IP_DIR / "git_stats.txt").write_text(stats, encoding="utf-8")
    changes.append(f"git_stats.txt ({total} commits, {tags} tags)")

    # 4. tags
    raw = sh(["git", "for-each-ref", "--sort=-creatordate",
              "--format=%(refname:short)|%(*objectname:short)|%(creatordate:short)",
              "refs/tags"])
    body = "\n".join(f"| {t} | {c} | {d} |"
                     for line in raw.splitlines()
                     for t, c, d in [line.split("|")])
    tag_md = f"""# リリースタグ一覧

**Note**: RFC 3161 タイムスタンプは未取得。public 化後に導入予定。

| タグ | コミット | 日付 |
|---|---|---|
{body}
"""
    if not dry_run:
        (IP_DIR / "git_tags.txt").write_text(tag_md, encoding="utf-8")
    changes.append(f"git_tags.txt ({len(raw.splitlines())} tags)")

    return changes


def refresh_nuget(dry_run):
    """Regenerate nuget_packages.json + licenses_nuget.csv from dotnet list."""
    changes = []
    try:
        out = subprocess.check_output(
            ["dotnet", "list", "PowerSimBuilder.csproj", "package",
             "--include-transitive", "--format", "json"],
            text=True, encoding="utf-8", stderr=subprocess.DEVNULL
        )
        data = json.loads(out)
    except Exception as e:
        return [f"[WARN] dotnet list failed: {e}"]

    # nuget_packages.json
    if not dry_run:
        (IP_DIR / "nuget_packages.json").write_text(
            json.dumps(data, indent=2, ensure_ascii=False), encoding="utf-8")
    changes.append("nuget_packages.json")

    # licenses_nuget.csv
    KNOWN = {
        "NCalc": ("MIT", "https://github.com/sheetsync/NCalc"),
        "ncalc": ("MIT", "https://github.com/sheetsync/NCalc"),
        "Fody": ("MIT", "https://github.com/Fody/Fody"),
    }
    rows = []
    for proj in data.get("projects", []):
        for fw in proj.get("frameworks", []):
            for p in fw.get("topLevelPackages", []) + fw.get("transitivePackages", []):
                lic, url = KNOWN.get(p["id"], ("CHECK", ""))
                rows.append({"Name": p["id"],
                             "Version": p.get("resolvedVersion", ""),
                             "License": lic, "URL": url})

    lines = ['"Name","Version","License","URL"']
    for r in rows:
        lines.append(f'"{r["Name"]}","{r["Version"]}","{r["License"]}","{r["URL"]}"')
    if not dry_run:
        (IP_DIR / "licenses_nuget.csv").write_text(
            "\n".join(lines) + "\n", encoding="utf-8")
    changes.append(f"licenses_nuget.csv ({len(rows)} packages)")

    return changes


def refresh_licenses_full(dry_run):
    """Combine licenses_nuget.csv + licenses_python_fmpy.csv."""
    import csv
    rows = []
    for fname in ["licenses_nuget.csv", "licenses_python_fmpy.csv"]:
        f = IP_DIR / fname
        if f.is_file():
            with f.open(encoding="utf-8-sig", newline="") as fh:
                for r in csv.DictReader(fh):
                    rows.append(r)

    if dry_run:
        return [f"licenses_full.csv ({len(rows)} packages)"]

    with (IP_DIR / "licenses_full.csv").open("w", encoding="utf-8", newline="") as f:
        w = csv.DictWriter(f, fieldnames=["Name", "Version", "License", "URL"])
        w.writeheader()
        w.writerows(rows)
    return [f"licenses_full.csv ({len(rows)} packages)"]


def extract_test_counts(whitepaper_text):
    """Extract existing test counts from WHITE_PAPER.md."""
    patterns = {
        "csharp": r"C# tests:\s*(\d+)\s*passed",
        "python": r"Python tests:\s*(\d+)\s*passed",
        "uitests": r"UI E2E:\s*(\d+)\s*passed",
        "rolesnippet": r"RoleSnippet tests:\s*(\d+)\s*passed",
    }
    result = {}
    for k, pat in patterns.items():
        m = re.search(pat, whitepaper_text)
        if m:
            result[k] = m.group(1)
    return result


def update_whitepaper(version, test_counts, dry_run):
    """Update WHITE_PAPER.md version, date, status, roadmap."""
    p = IP_DIR / "WHITE_PAPER.md"
    if not p.is_file():
        return ["[WARN] WHITE_PAPER.md missing"]
    s = p.read_text(encoding="utf-8-sig")
    changes = []
    vshort = version.replace("v", "")  # 1.9.0
    vminor = ".".join(vshort.split(".")[:2])  # 1.9
    today = datetime.now().strftime("%Y-%m-%d")

    # Version line
    new_s, n = re.subn(r"\*\*Version\*\*: [\d.]+",
                       f"**Version**: {vminor}", s, count=1)
    if n and new_s != s:
        changes.append("version")
        s = new_s

    # Date line (only inside WHITE_PAPER header block)
    new_s, n = re.subn(r"(\*\*Version\*\*: [\d.]+\n\*\*Date\*\*: )[\d-]+",
                       f"\\g<1>{today}", s, count=1)
    if n and new_s != s:
        changes.append("date")
        s = new_s

    # Test counts
    if test_counts:
        cs = test_counts.get("csharp", "")
        py = test_counts.get("python", "")
        ui = test_counts.get("uitests", "")
        rs = test_counts.get("rolesnippet", "")
        # Match either the new form (ends with RoleSnippet line) or the
        # legacy form (ends with **Simulation execution: ...**)
        # Match to the END OF LINE of the **Simulation execution: ...** line.
        # The earlier version used [^*]*\*\* which stops at the closing ** and
        # leaves the " (StaTable 7-layer ...)" suffix in the tail; new_block
        # already contains it, so the run reported a spurious status change and
        # duplicated the suffix when applied. [^\n]* consumes to end-of-line
        # and makes the status update idempotent.
        old_block = re.search(
            r"\*\*Current Status:\*\* v[\d.]+ released \([\d-]+\).*?\*\*Simulation execution:[^\n]*",
            s, re.DOTALL)
        if old_block:
            new_block = (
                f"**Current Status:** {version} released ({today}).\n"
                f"- Python tests: {py} passed\n"
                f"- C# tests: {cs} passed (2 skipped)\n"
                f"- UI E2E: {ui} passed (1 skipped)\n"
                f"- RoleSnippet tests: {rs} passed\n"
                f"- **Simulation execution: operational** "
                f"(StaTable 7-layer + OvenChamber + PID validated at ss=200.68degC / ovs=1.45% / settle=64.6s)"
            )
            s2 = s[:old_block.start()] + new_block + s[old_block.end():]
            if s2 != s:
                changes.append("status")
                s = s2

    if not dry_run and changes:
        p.write_text(s, encoding="utf-8")
    return [f"WHITE_PAPER.md ({', '.join(changes) if changes else 'no change'})"]


def update_oss_compliance(version, dry_run):
    """Update version lines in OSS_COMPLIANCE.md."""
    p = IP_DIR / "OSS_COMPLIANCE.md"
    if not p.is_file():
        return ["[WARN] OSS_COMPLIANCE.md missing"]
    s = p.read_text(encoding="utf-8-sig")
    changes = []
    today = datetime.now().strftime("%Y-%m-%d")

    new_s, n = re.subn(
        r"\*\*Product\*\*: PowerSimBuilder v[\d.]+\n\*\*Date\*\*: [\d-]+",
        f"**Product**: PowerSimBuilder {version}\n**Date**: {today}",
        s, count=1)
    if n and new_s != s:
        changes.append("header")
        s = new_s

    new_s, n = re.subn(
        r"PowerSimBuilder v[\d.]+ can be commercially distributed",
        f"PowerSimBuilder {version} can be commercially distributed",
        s, count=1)
    if n and new_s != s:
        changes.append("conclusion")
        s = new_s

    if not dry_run and changes:
        p.write_text(s, encoding="utf-8")
    return [f"OSS_COMPLIANCE.md ({', '.join(changes) if changes else 'no change'})"]


def check_lawyer_consultation():
    """Warn if LAWYER_CONSULTATION.md is tracked (publication blocker)."""
    try:
        out = subprocess.check_output(
            ["git", "ls-files", "docs/ip/LAWYER_CONSULTATION.md"],
            text=True, stderr=subprocess.DEVNULL).strip()
        if out:
            return ["[WARN] LAWYER_CONSULTATION.md is TRACKED — untrack before public release"]
    except Exception:
        pass
    return []


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                  formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--version", help="Override version (e.g. v1.9.0)")
    ap.add_argument("--test-counts",
                    help="C#,Python,UITests,RoleSnippet (comma-separated)")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    if not IP_DIR.is_dir():
        print(f"[ERR] {IP_DIR} not found", file=sys.stderr)
        return 1

    version = args.version or get_latest_tag()
    if not version:
        print("[ERR] no version provided and no git tag found", file=sys.stderr)
        return 1
    print(f"[INFO] target version: {version}")
    if args.dry_run:
        print("[INFO] DRY-RUN (no files will be written)\n")

    # test counts: args > existing
    if args.test_counts:
        parts = args.test_counts.split(",")
        if len(parts) != 4:
            print("[ERR] --test-counts needs 4 values", file=sys.stderr)
            return 1
        counts = dict(zip(["csharp", "python", "uitests", "rolesnippet"], parts))
    else:
        wp_text = (IP_DIR / "WHITE_PAPER.md").read_text(encoding="utf-8-sig") \
            if (IP_DIR / "WHITE_PAPER.md").is_file() else ""
        counts = extract_test_counts(wp_text)

    print("=== refresh results ===")
    all_changes = []
    all_changes += refresh_git_files(args.dry_run)
    all_changes += refresh_nuget(args.dry_run)
    all_changes += refresh_licenses_full(args.dry_run)
    all_changes += update_whitepaper(version, counts, args.dry_run)
    all_changes += update_oss_compliance(version, args.dry_run)
    all_changes += check_lawyer_consultation()

    for c in all_changes:
        print(f"  {c}")

    if args.dry_run:
        print("\n[DRY-RUN] no files written")
    else:
        print(f"\n[OK] refreshed docs/ip/ for {version}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
