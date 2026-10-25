"""HANDOFF editor -- GUI tool for updating HANDOFF_MVP1_2.md.

Three modes:
  1. Section insert: add a new version section above the current top
     "## vX.Y.Z" section.
  2. Trap row: append a new row to the "学んだ罠（累積）" table with an
     auto-incremented trap ID.
  3. Free edit: edit the whole file in an internal textarea.

Traps baked into the design:
  #35  only match trap rows with line-anchored ^\\| \\d+ \\| AND only
       within the "学んだ罠（累積）" section. Other tables (file counts,
       test counts) also contain | N | rows.
  #20  read/write the whole file via Path I/O, never heredoc.
  #19  absolute path via Path(__file__).parent.parent.
  #45  always read_text(encoding='utf-8').

Usage:
    python tools/handoff_editor.py
    python tools/handoff_editor.py --path FILE
    python tools/handoff_editor.py --dry-run
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

SECTION_HEADER_RE = re.compile(r"^## v\d+\.\d+\.\d+", re.MULTILINE)
TRAP_ROW_RE = re.compile(r"^\| (\d+) \|")
TRAP_SECTION_TITLE = "## 学んだ罠（累積）"


class HandoffDocument:
    """Line-based editor for HANDOFF_MVP1_2.md."""

    def __init__(self, path):
        self.path = Path(path)
        self._load()

    def _load(self):
        text = self.path.read_text(encoding="utf-8")
        self.lines = text.splitlines(keepends=True)

    def save(self):
        self.path.write_text("".join(self.lines), encoding="utf-8")

    # ---- section insert ----

    def find_top_section_index(self):
        for i, line in enumerate(self.lines):
            if SECTION_HEADER_RE.match(line):
                return i
        return -1

    def insert_section(self, version, body):
        idx = self.find_top_section_index()
        if idx < 0:
            raise ValueError("No version section header found")
        header = f"## {version} 完了\n\n"
        block = header + body.rstrip("\n") + "\n\n"
        self.lines.insert(idx, block)
        return idx

    # ---- trap row ----

    def find_trap_section(self):
        start = -1
        for i, line in enumerate(self.lines):
            if line.rstrip("\r\n") == TRAP_SECTION_TITLE:
                start = i
                break
        if start < 0:
            raise ValueError(f"Trap section not found: {TRAP_SECTION_TITLE!r}")
        end = len(self.lines)
        for j in range(start + 1, len(self.lines)):
            if self.lines[j].startswith("## "):
                end = j
                break
        return start, end

    def find_max_trap_id(self):
        start, end = self.find_trap_section()
        max_id = 0
        for i in range(start, end):
            m = TRAP_ROW_RE.match(self.lines[i])
            if m:
                max_id = max(max_id, int(m.group(1)))
        return max_id

    def find_trap_table_end(self):
        start, end = self.find_trap_section()
        last = -1
        for i in range(start, end):
            if TRAP_ROW_RE.match(self.lines[i]):
                last = i
        return last

    def append_trap(self, description, mitigation, trap_id=None):
        # collapse newlines (markdown table rows must be single-line)
        description = " ".join(description.split())
        mitigation = " ".join(mitigation.split())
        last = self.find_trap_table_end()
        if last < 0:
            raise ValueError("No trap rows found")
        if trap_id is None:
            trap_id = self.find_max_trap_id() + 1
        row = f"| {trap_id} | {description} | {mitigation} |\n"
        self.lines.insert(last + 1, row)
        return trap_id

    # ---- util ----

    def backup(self, suffix=".bak"):
        bak = self.path.with_suffix(self.path.suffix + suffix)
        bak.write_text("".join(self.lines), encoding="utf-8")
        return bak


def run_gui(path):
    try:
        import tkinter as tk
        from tkinter import messagebox, ttk
    except ImportError:
        print("tkinter not available; use --dry-run", file=sys.stderr)
        return 2

    doc = HandoffDocument(path)

    root = tk.Tk()
    root.title("HANDOFF Editor - v1.7.0")
    root.geometry("900x680")

    mode_var = tk.StringVar(value="section")

    ttk.Label(root, text=f"File: {path}").pack(anchor="w", padx=8, pady=4)

    mode_frame = ttk.Frame(root)
    mode_frame.pack(anchor="w", padx=8, pady=4)
    ttk.Label(mode_frame, text="Mode:").pack(side="left")
    for val, label in [
        ("section", "Section insert"),
        ("trap", "Trap row"),
        ("free", "Free edit"),
    ]:
        ttk.Radiobutton(mode_frame, text=label, value=val, variable=mode_var).pack(
            side="left", padx=4
        )

    sec_frame = ttk.LabelFrame(root, text="Section insert")
    ttk.Label(sec_frame, text="Version:").grid(row=0, column=0, sticky="w", padx=4)
    sec_version = ttk.Entry(sec_frame, width=16)
    # intentionally empty: default leaked as wrong version in dogfooding
    # (v1.7.0 accidentally inserted as v1.8.0). Leave blank so user must type.
    sec_version.grid(row=0, column=1, sticky="w", padx=4)

    ttk.Label(sec_frame, text="Body:").grid(row=1, column=0, sticky="nw", padx=4)
    sec_body = tk.Text(sec_frame, height=14, wrap="word")
    sec_body.grid(row=1, column=1, sticky="nsew", padx=4, pady=4)
    sec_frame.columnconfigure(1, weight=1)
    sec_frame.rowconfigure(1, weight=1)

    trap_frame = ttk.LabelFrame(root, text="Trap row")
    ttk.Label(trap_frame, text="Next ID:").grid(row=0, column=0, sticky="w", padx=4)
    try:
        next_id = doc.find_max_trap_id() + 1
    except ValueError:
        next_id = 1
    trap_id_var = tk.StringVar(value=str(next_id))
    ttk.Entry(trap_frame, textvariable=trap_id_var, width=6).grid(
        row=0, column=1, sticky="w", padx=4
    )

    ttk.Label(trap_frame, text="Description:").grid(row=1, column=0, sticky="nw", padx=4)
    trap_desc = tk.Text(trap_frame, height=3, wrap="word")
    trap_desc.grid(row=1, column=1, sticky="nsew", padx=4, pady=4)

    ttk.Label(trap_frame, text="Mitigation:").grid(row=2, column=0, sticky="nw", padx=4)
    trap_mit = tk.Text(trap_frame, height=3, wrap="word")
    trap_mit.grid(row=2, column=1, sticky="nsew", padx=4, pady=4)
    trap_frame.columnconfigure(1, weight=1)
    trap_frame.rowconfigure(1, weight=1)
    trap_frame.rowconfigure(2, weight=1)

    free_frame = ttk.LabelFrame(root, text="Free edit")
    free_text = tk.Text(free_frame, height=14, wrap="none")
    free_text.pack(fill="both", expand=True, padx=4, pady=4)

    def refresh_free():
        free_text.delete("1.0", "end")
        free_text.insert("1.0", "".join(doc.lines))

    def update_mode(*_):
        for w in (sec_frame, trap_frame, free_frame):
            w.pack_forget()
        m = mode_var.get()
        if m == "section":
            sec_frame.pack(fill="both", expand=True, padx=8, pady=4)
        elif m == "trap":
            trap_frame.pack(fill="both", expand=True, padx=8, pady=4)
        else:
            free_frame.pack(fill="both", expand=True, padx=8, pady=4)
            refresh_free()

    mode_var.trace_add("write", update_mode)

    def apply_changes():
        try:
            m = mode_var.get()
            if m == "section":
                ver = sec_version.get().strip()
                if not ver:
                    messagebox.showerror("Error", "Version is empty")
                    return
                body = sec_body.get("1.0", "end")
                if not body.strip():
                    messagebox.showerror("Error", "Body is empty")
                    return
                idx = doc.insert_section(ver, body)
                doc.save()
                messagebox.showinfo(
                    "OK",
                    f"Inserted section {ver} at line {idx + 1}",
                )
            elif m == "trap":
                try:
                    tid = int(trap_id_var.get())
                except ValueError:
                    messagebox.showerror("Error", "Trap ID must be an integer")
                    return
                desc = trap_desc.get("1.0", "end").strip()
                mit = trap_mit.get("1.0", "end").strip()
                if not desc or not mit:
                    messagebox.showerror("Error", "Description and mitigation are required")
                    return
                used = doc.append_trap(desc, mit, trap_id=tid)
                doc.save()
                trap_id_var.set(str(used + 1))
                messagebox.showinfo("OK", f"Trap #{used} appended")
            else:
                new_text = free_text.get("1.0", "end")
                doc.lines = new_text.splitlines(keepends=True)
                doc.save()
                messagebox.showinfo("OK", "Free edit saved")
        except Exception as e:
            messagebox.showerror("Error", str(e))

    btn = ttk.Frame(root)
    btn.pack(fill="x", padx=8, pady=8)
    ttk.Button(btn, text="Apply", command=apply_changes).pack(side="right", padx=4)
    ttk.Button(btn, text="Reload", command=lambda: (doc._load(), refresh_free())).pack(
        side="right", padx=4
    )
    ttk.Button(
        btn, text="Backup", command=lambda: messagebox.showinfo("OK", str(doc.backup()))
    ).pack(side="left", padx=4)
    ttk.Button(btn, text="Quit", command=root.destroy).pack(side="left", padx=4)

    update_mode()
    root.mainloop()
    return 0


def main(argv=None):
    parser = argparse.ArgumentParser(description="HANDOFF editor GUI")
    parser.add_argument(
        "--path",
        type=Path,
        default=Path(__file__).parent.parent / "HANDOFF_MVP1_2.md",
        help="path to HANDOFF markdown",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="validate structure and exit (no GUI)",
    )
    args = parser.parse_args(argv)

    if not args.path.exists():
        print(f"File not found: {args.path}", file=sys.stderr)
        return 1

    if args.dry_run:
        doc = HandoffDocument(args.path)
        try:
            top = doc.find_top_section_index()
            print("Top section line: {}".format(top + 1 if top >= 0 else "?"))
            if top >= 0:
                print("Top header:       {}".format(doc.lines[top].rstrip("\r\n")))
            max_trap = doc.find_max_trap_id()
            print(f"Max trap id:      {max_trap}")
            print(f"Next trap id:     {max_trap + 1}")
        except ValueError as e:
            print(f"Error: {e}", file=sys.stderr)
            return 1
        return 0

    return run_gui(args.path)


if __name__ == "__main__":
    sys.exit(main())
