#!/usr/bin/env python3
"""Setup GitHub token via GUI dialog and store in user config file.

Usage:
    python tools/setup_github_token.py           # open dialog
    python tools/setup_github_token.py --show    # mask status only
    python tools/setup_github_token.py --delete  # remove saved token

Token file: %USERPROFILE%/.powersimbuilder/github_token
Permissions: user-only (Windows: icacls, POSIX: chmod 600)
Never printed in full. Never written to repo.
"""
from __future__ import annotations

import argparse
import os
import stat
import subprocess
import sys
from pathlib import Path

CONFIG_DIR = Path.home() / ".powersimbuilder"
TOKEN_FILE = CONFIG_DIR / "github_token"


def save_token(token: str) -> Path:
    CONFIG_DIR.mkdir(parents=True, exist_ok=True)
    TOKEN_FILE.write_text(token.strip() + "\n", encoding="utf-8")
    _restrict_permissions(TOKEN_FILE)
    return TOKEN_FILE


def load_token() -> str | None:
    if not TOKEN_FILE.exists():
        return None
    try:
        t = TOKEN_FILE.read_text(encoding="utf-8").strip()
        return t or None
    except OSError:
        return None


def _restrict_permissions(path: Path) -> None:
    if sys.platform == "win32":
        user = os.environ.get("USERNAME")
        if not user:
            return
        subprocess.run(
            ["icacls", str(path), "/inheritance:r", "/grant:r", f"{user}:F"],
            check=False,
            capture_output=True,
        )
    else:
        os.chmod(path, stat.S_IRUSR | stat.S_IWUSR)


def _mask(token: str) -> str:
    if len(token) < 8:
        return "***"
    return f"{token[:4]}...{token[-4:]} ({len(token)} chars)"


def _show_dialog() -> str | None:
    import tkinter as tk
    from tkinter import messagebox, ttk

    root = tk.Tk()
    root.title("GitHub Token Setup")
    root.resizable(False, False)

    frame = ttk.Frame(root, padding=16)
    frame.grid(row=0, column=0, sticky="nsew")

    ttk.Label(
        frame,
        text=(
            "Paste a GitHub token (ghp_... or github_pat_...).\n"
            "Saved to:\n"
            f"  {TOKEN_FILE}\n"
            "with user-only permissions."
        ),
        justify="left",
    ).grid(row=0, column=0, columnspan=2, pady=(0, 12), sticky="w")

    ttk.Label(frame, text="Token:").grid(row=1, column=0, sticky="w")
    token_var = tk.StringVar()
    entry = ttk.Entry(frame, textvariable=token_var, show="*", width=48)
    entry.grid(row=1, column=1, sticky="ew", padx=(6, 0))
    entry.focus_set()

    show_var = tk.BooleanVar(value=False)

    def toggle_show() -> None:
        entry.config(show="" if show_var.get() else "*")

    ttk.Checkbutton(
        frame, text="Show", variable=show_var, command=toggle_show
    ).grid(row=2, column=1, sticky="w", pady=(4, 0))

    status_var = tk.StringVar(value="")
    ttk.Label(frame, textvariable=status_var, foreground="#a00").grid(
        row=3, column=0, columnspan=2, sticky="w", pady=(8, 0)
    )

    result = {"value": None}

    def on_save() -> None:
        v = token_var.get().strip()
        if not v:
            status_var.set("Token is empty.")
            return
        if not (v.startswith("ghp_") or v.startswith("github_pat_") or v.startswith("gho_")):
            if not messagebox.askyesno(
                "Unusual token format",
                "Token does not start with 'ghp_' or 'github_pat_'.\nSave anyway?",
            ):
                return
        try:
            save_token(v)
        except OSError as exc:
            status_var.set(f"Failed to save: {exc}")
            return
        result["value"] = v
        messagebox.showinfo("Saved", f"Token saved to:\n{TOKEN_FILE}")
        root.destroy()

    def on_cancel() -> None:
        root.destroy()

    btns = ttk.Frame(frame)
    btns.grid(row=4, column=0, columnspan=2, pady=(12, 0), sticky="e")
    ttk.Button(btns, text="Cancel", command=on_cancel).grid(row=0, column=0, padx=(0, 6))
    ttk.Button(btns, text="Save", command=on_save).grid(row=0, column=1)

    root.bind("<Return>", lambda e: on_save())
    root.bind("<Escape>", lambda e: on_cancel())
    root.mainloop()
    return result["value"]


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description="Setup GitHub token via GUI.")
    ap.add_argument("--show", action="store_true", help="show masked status only")
    ap.add_argument("--delete", action="store_true", help="delete saved token")
    args = ap.parse_args(argv)

    if args.delete:
        if TOKEN_FILE.exists():
            TOKEN_FILE.unlink()
            print(f"[OK] deleted {TOKEN_FILE}")
        else:
            print(f"[SKIP] no token at {TOKEN_FILE}")
        return 0

    if args.show:
        t = load_token()
        if t:
            print(f"[OK] token present: {_mask(t)}")
        else:
            print(f"[SKIP] no token at {TOKEN_FILE}")
        return 0

    token = _show_dialog()
    if token:
        print(f"[OK] token saved: {_mask(token)}")
        return 0
    print("[SKIP] cancelled")
    return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))