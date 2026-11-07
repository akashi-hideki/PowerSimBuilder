"""Capture environment snapshot (git / Python / .NET / OS) as JSON.

Usage:
    python tools/capture_env.py <out.json>
"""
import json
import platform
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path


def _optional(cmd):
    """Return stripped stdout, or "" if the command fails (e.g. no matching tag)."""
    try:
        return subprocess.check_output(cmd, text=True, stderr=subprocess.DEVNULL).strip()
    except Exception:
        return ""


def sh(cmd):
    try:
        return subprocess.check_output(cmd, text=True, stderr=subprocess.DEVNULL).strip()
    except Exception as e:
        return f"ERROR: {e}"


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: capture_env.py <out.json>", file=sys.stderr)
        return 2
    out = Path(sys.argv[1])
    out.parent.mkdir(parents=True, exist_ok=True)
    env = {
        "measured_at_utc": datetime.now(timezone.utc).isoformat(),
        "git_commit": sh(["git", "rev-parse", "HEAD"]),
        "git_tag": _optional(["git", "describe", "--tags", "--exact-match", "HEAD"]),
        "git_branch": sh(["git", "rev-parse", "--abbrev-ref", "HEAD"]),
        "git_dirty": bool(sh(["git", "status", "--porcelain"])),
        "python_version": sys.version.split()[0],
        "dotnet_version": sh(["dotnet", "--version"]),
        "os": f"{platform.system()} {platform.release()}",
        "hostname": platform.node(),
    }
    out.write_text(json.dumps(env, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(env, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    sys.exit(main())
