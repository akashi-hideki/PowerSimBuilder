"""Append Phase-14 cleanup patterns to .gitignore safely.

BOM なし UTF-8 で追記する。既に同じ行があればスキップ（冪等）。
"""
from pathlib import Path

GITIGNORE = Path(".gitignore")

ADDITIONS = """
# --- Business spec docs (not for public) ---
MD/
pythonlib/
previou_files/

# --- VS Copilot index / IDE caches ---
*.vsidx
*.db
*.db-shm
*.db-wal

# --- Downloads / partial ---
*.crdownload

# --- AI-generated scratch files ---
deepseek_*
combined_*

# --- Costura compression artifacts ---
*.costura.*
*.resources

# --- Security scan outputs ---
gitleaks-report*.json
.secrets.baseline
all-files-ever.txt
""".lstrip("\n")


def main() -> None:
    if not GITIGNORE.exists():
        print("ERROR: .gitignore not found in current directory")
        raise SystemExit(1)

    text = GITIGNORE.read_text(encoding="utf-8")

    marker = "# --- Business spec docs (not for public) ---"
    if marker in text:
        print("SKIP: Phase-14 cleanup patterns already present")
        return

    if not text.endswith("\n"):
        text += "\n"

    text += "\n" + ADDITIONS

    GITIGNORE.write_text(text, encoding="utf-8")
    print(f"OK: appended {len(ADDITIONS.splitlines())} lines to .gitignore")


if __name__ == "__main__":
    main()