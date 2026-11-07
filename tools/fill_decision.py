"""Fill the decision fields of a GO/NO-GO DECISION.md.

Idempotent: re-running with the same arguments is a no-op.
Refuses to overwrite an already-recorded decision unless --force.

Usage:
    # GO (default, interactive-free)
    python tools/fill_decision.py docs/qa/2026-11-08-gonogo \\
        --decision GO --by "Hideki Akashi" --date 2026-11-08

    # NO-GO with reason
    python tools/fill_decision.py docs/qa/2026-11-08-gonogo \\
        --decision NO-GO --by "Hideki Akashi" --date 2026-11-08 \\
        --reason "Legal review pending"

    # Conditional GO
    python tools/fill_decision.py docs/qa/2026-11-08-gonogo \\
        --decision CONDITIONAL --by "Hideki Akashi" --date 2026-11-08 \\
        --conditions "CI restored|Legal sign-off"

    # Preview without writing
    python tools/fill_decision.py docs/qa/2026-11-08-gonogo \\
        --decision GO --by "Hideki Akashi" --date 2026-11-08 --dry-run
"""
import argparse
import re
import sys
from datetime import datetime
from pathlib import Path


GO_LINE = "- [ ] **GO** — proceed with 11/22 public release"
NOGO_LINE = "- [ ] **NO-GO** — postpone"
COND_LINE = "- [ ] **Conditional GO** — conditions in section 6"
BY_LINE_TPL = "**Decision by**: ________________  **Date**: ____________"
COND_BLOCK_TPL = """## 6. Conditions (if Conditional GO)

（条件付き GO の場合、ここに条件を記入）

1. _______________________________
2. _______________________________
"""


def mark_checkbox(text: str, chosen: str) -> tuple[str, bool]:
    """Check the chosen line, uncheck others. Returns (new_text, changed)."""
    mapping = {
        "GO": GO_LINE,
        "NO-GO": NOGO_LINE,
        "CONDITIONAL": COND_LINE,
    }
    target = mapping[chosen]
    checked = target.replace("- [ ]", "- [x]")

    changed = False
    for line in mapping.values():
        # Uncheck all (except already target checked)
        unchecked_line = line.replace("- [ ]", "- [x]")
        if unchecked_line in text and line != target:
            text = text.replace(unchecked_line, line)
            changed = True
        # Uncheck target if previously checked (idempotent rewrite)
        if unchecked_line in text and line == target:
            pass  # will be re-checked below

    if target in text:
        text = text.replace(target, checked, 1)
        changed = True
    elif checked not in text:
        raise SystemExit(f"[ERROR] target line not found in DECISION.md: {target!r}")

    return text, changed


def fill_by_date(text: str, by: str, date: str) -> tuple[str, bool]:
    if BY_LINE_TPL not in text:
        if f"**Decision by**: {by}" in text and f"**Date**: {date}" in text:
            return text, False
        # Try generic pattern
        pat = re.compile(r"\*\*Decision by\*\*:[^\n]*?\*\*Date\*\*:[^\n]*")
        if pat.search(text):
            new_line = f"**Decision by**: {by}  **Date**: {date}"
            text = pat.sub(new_line, text, count=1)
            return text, True
        raise SystemExit("[ERROR] 'Decision by / Date' line not found")
    new_line = f"**Decision by**: {by}  **Date**: {date}"
    text = text.replace(BY_LINE_TPL, new_line, 1)
    return text, True


def fill_conditions(text: str, conditions: list[str]) -> tuple[str, bool]:
    if COND_BLOCK_TPL not in text:
        # Already filled? (heuristic: numeric list items exist)
        m = re.search(r"## 6\. Conditions.*?(?=\n##|\Z)", text, re.DOTALL)
        if m and re.search(r"^\d+\.\s+\S", m.group(0), re.MULTILINE):
            return text, False
        raise SystemExit("[ERROR] §6 Conditions block not found")
    numbered = "\n".join(f"{i+1}. {c}" for i, c in enumerate(conditions))
    new_block = f"""## 6. Conditions (if Conditional GO)

1. {conditions[0] if conditions else ""}
{chr(10).join(f"{i+2}. {c}" for i, c in enumerate(conditions[1:])) if len(conditions) > 1 else ""}
"""
    text = text.replace(COND_BLOCK_TPL, new_block.rstrip() + "\n", 1)
    return text, True


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                  formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("qa_dir", help="docs/qa/<label>")
    ap.add_argument("--decision", choices=["GO", "NO-GO", "CONDITIONAL"], default="GO")
    ap.add_argument("--by", default="Hideki Akashi")
    ap.add_argument("--date", default=datetime.now().strftime("%Y-%m-%d"))
    ap.add_argument("--reason", help="NO-GO reason (free text, recorded in §6)")
    ap.add_argument("--conditions", help="CONDITIONAL: pipe-separated conditions")
    ap.add_argument("--force", action="store_true",
                    help="Overwrite even if a decision is already recorded")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    path = Path(args.qa_dir) / "DECISION.md"
    if not path.is_file():
        print(f"[ERR] {path} not found", file=sys.stderr)
        return 1

    text = path.read_text(encoding="utf-8")

    # Guard: already recorded?
    if not args.force and re.search(r"- \[x\] \*\*(GO|NO-GO|Conditional GO)\*\*", text):
        print("[SKIP] decision already recorded (use --force to overwrite)")
        return 0

    text, c1 = mark_checkbox(text, args.decision)
    text, c2 = fill_by_date(text, args.by, args.date)

    changes = ["checkbox", "by/date"]
    if args.decision == "NO-GO" and args.reason:
        # Record reason in §6
        m = re.search(r"(## 6\. Conditions \(if Conditional GO\)\n)(.*?)(?=\n## |\Z)",
                      text, re.DOTALL)
        if m:
            new_section = (
                "## 6. Conditions / NO-GO reason\n\n"
                f"**NO-GO reason**: {args.reason}\n\n"
            )
            text = text[:m.start()] + new_section + text[m.end():]
            changes.append("no-go-reason")

    if args.decision == "CONDITIONAL" and args.conditions:
        conds = [c.strip() for c in args.conditions.split("|") if c.strip()]
        text, c3 = fill_conditions(text, conds)
        if c3:
            changes.append("conditions")

    print(f"[INFO] decision = {args.decision}")
    print(f"[INFO] by       = {args.by}")
    print(f"[INFO] date     = {args.date}")
    if args.reason:
        print(f"[INFO] reason   = {args.reason}")
    if args.conditions:
        print(f"[INFO] conds    = {args.conditions}")
    print(f"[INFO] changes  = {', '.join(changes)}")

    if args.dry_run:
        print("\n----- Preview (first 30 lines) -----")
        print("\n".join(text.splitlines()[:30]))
        print("...")
        print("\n[DRY-RUN] no files written")
        return 0

    path.write_text(text, encoding="utf-8")
    print(f"[OK] wrote {path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
