#!/usr/bin/env python3
"""Build a PowerSimBuilder-compatible catalog JSON from real FMU repositories.

Modes:
  --source fmi-cross-check   Scan modelica/fmi-cross-check for .fmu files.
  --repo OWNER/REPO          Scan an arbitrary GitHub repository.

Usage:
  python tools/fmu_catalog_builder.py --source fmi-cross-check --output Resources/generated_catalog.json
  python tools/fmu_catalog_builder.py --repo altairengineering/fmus --output Resources/altair_catalog.json
  python tools/fmu_catalog_builder.py --source fmi-cross-check --platform win64 --max-models 50

Requires GITHUB_TOKEN env var (or --token) for authenticated rate limits (5000/h).
Unauthenticated requests are limited to 60/h.
"""
from __future__ import annotations

import argparse
import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from datetime import datetime, timezone
from typing import Any

GITHUB_API = "https://api.github.com"
RAW_BASE = "https://raw.githubusercontent.com"
USER_AGENT = "PowerSimBuilder-FmuCatalogBuilder/1.0"

# FMI Cross Check specific
CROSS_CHECK_REPO = "modelica/fmi-cross-check"
CROSS_CHECK_BRANCH = "master"
CROSS_CHECK_PREFIX = "fmus"
DEFAULT_PLATFORMS = ["win64", "c-code"]  # c-code = platform-independent


# ---------------------------------------------------------------- token helpers

def _load_token_from_config() -> str | None:
    """Read token from ~/.powersimbuilder/github_token if present."""
    from pathlib import Path
    p = Path.home() / ".powersimbuilder" / "github_token"
    if not p.exists():
        return None
    try:
        t = p.read_text(encoding="utf-8").strip()
        return t or None
    except OSError:
        return None


def _resolve_token(cli_token: str | None) -> str | None:
    """Precedence: --token > $GITHUB_TOKEN > ~/.powersimbuilder/github_token."""
    if cli_token:
        return cli_token
    env = os.environ.get("GITHUB_TOKEN")
    if env:
        return env
    return _load_token_from_config()


# ---------------------------------------------------------------- HTTP helpers

def _headers(token: str | None) -> dict[str, str]:
    h = {"User-Agent": USER_AGENT, "Accept": "application/vnd.github+json"}
    if token:
        h["Authorization"] = f"Bearer {token}"
    return h


def _get_json(url: str, token: str | None, timeout: float = 30.0) -> Any:
    req = urllib.request.Request(url, headers=_headers(token))
    with urllib.request.urlopen(req, timeout=timeout) as resp:
        return json.loads(resp.read().decode("utf-8"))


def _get_default_branch(owner: str, repo: str, token: str | None) -> str:
    data = _get_json(f"{GITHUB_API}/repos/{owner}/{repo}", token)
    return data.get("default_branch", "main")


# ---------------------------------------------------------------- GitHub tree listing

def _list_tree_recursive(owner: str, repo: str, branch: str, token: str | None) -> list[dict[str, Any]]:
    """Return all blobs in the repo tree. Falls back to Contents API if truncated."""
    url = f"{GITHUB_API}/repos/{owner}/{repo}/git/trees/{branch}?recursive=1"
    data = _get_json(url, token)
    if not data.get("truncated"):
        return data.get("tree", [])
    # Fallback: walk root-level entries via Contents API
    sys.stderr.write(f"[WARN] tree for {owner}/{repo} is truncated; falling back to Contents API\n")
    return _walk_contents(owner, repo, branch, "", token)


def _walk_contents(owner: str, repo: str, branch: str, path: str, token: str | None) -> list[dict[str, Any]]:
    """Recursively list files via the Contents API (1 call per directory)."""
    api_path = urllib.parse.quote(path, safe="/")
    url = f"{GITHUB_API}/repos/{owner}/{repo}/contents/{api_path}?ref={branch}"
    data = _get_json(url, token)
    out: list[dict[str, Any]] = []
    for entry in data:
        if entry["type"] == "dir":
            out.extend(_walk_contents(owner, repo, branch, entry["path"], token))
            time.sleep(0.05)  # gentle pacing
        elif entry["type"] == "file":
            out.append({"path": entry["path"], "type": "blob", "size": entry.get("size")})
    return out


def _raw_url(owner: str, repo: str, branch: str, path: str) -> str:
    return f"{RAW_BASE}/{owner}/{repo}/{branch}/{path}"


# ---------------------------------------------------------------- FMI Cross Check

def _parse_cross_check_path(path: str) -> dict[str, str] | None:
    """Parse 'fmus/2.0/cs/win64/Dymola/2019FD01/Rectifier/Rectifier.fmu'.

    Returns dict with fmi_version, fmi_type, platform, tool_id, tool_version, model_name.
    """
    parts = path.split("/")
    if len(parts) < 8 or parts[0] != CROSS_CHECK_PREFIX:
        return None
    if not parts[-1].lower().endswith(".fmu"):
        return None
    # fmus / {version} / {type} / {platform} / {tool_id} / {tool_version} / {model} / {file}
    try:
        return {
            "fmi_version": parts[1],
            "fmi_type": parts[2],
            "platform": parts[3],
            "tool_id": parts[4],
            "tool_version": parts[5],
            "model_name": parts[6],
        }
    except IndexError:
        return None


def build_from_cross_check(token: str | None, platforms: list[str], max_models: int) -> list[dict[str, Any]]:
    owner, repo = CROSS_CHECK_REPO.split("/")
    tree = _list_tree_recursive(owner, repo, CROSS_CHECK_BRANCH, token)
    models: list[dict[str, Any]] = []
    seen: set[tuple[str, str, str]] = set()
    for entry in tree:
        if entry.get("type") != "blob":
            continue
        meta = _parse_cross_check_path(entry["path"])
        if meta is None:
            continue
        if platforms and meta["platform"] not in platforms:
            continue
        # Prefer win64 over c-code if both exist for same (model, tool, version)
        key = (meta["model_name"], meta["tool_id"], meta["tool_version"])
        if key in seen:
            continue
        seen.add(key)
        models.append({
            "name": meta["model_name"],
            "url": _raw_url(owner, repo, CROSS_CHECK_BRANCH, entry["path"]),
            "sizeBytes": entry.get("size") or 0,
            "license": "Modelica Association (FMI Cross Check)",
            "description": (
                f"FMI {meta['fmi_version']} / {meta['fmi_type']} / {meta['platform']} "
                f"/ {meta['tool_id']} {meta['tool_version']}"
            ),
            "_meta": meta,  # stripped before output
        })
        if max_models and len(models) >= max_models:
            break
    return models


# ---------------------------------------------------------------- Generic GitHub repo

def build_from_repo(repo_full: str, branch: str | None, token: str | None, max_models: int) -> list[dict[str, Any]]:
    if "/" not in repo_full:
        raise ValueError(f"repo must be OWNER/REPO: {repo_full}")
    owner, repo = repo_full.split("/", 1)
    if not branch:
        branch = _get_default_branch(owner, repo, token)
    tree = _list_tree_recursive(owner, repo, branch, token)
    models: list[dict[str, Any]] = []
    for entry in tree:
        if entry.get("type") != "blob":
            continue
        path = entry["path"]
        if not path.lower().endswith(".fmu"):
            continue
        name = path.rsplit("/", 1)[-1][:-4]  # strip .fmu
        models.append({
            "name": name,
            "url": _raw_url(owner, repo, branch, path),
            "sizeBytes": entry.get("size") or 0,
            "license": "Unknown",
            "description": f"{repo_full} @ {branch}",
        })
        if max_models and len(models) >= max_models:
            break
    return models


# ---------------------------------------------------------------- output

def _strip_meta(models: list[dict[str, Any]]) -> list[dict[str, Any]]:
    return [{k: v for k, v in m.items() if not k.startswith("_")} for m in models]


def write_catalog(path: str, source: str, models: list[dict[str, Any]]) -> None:
    payload = {
        "version": 1,
        "generated_at": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "source": source,
        "models": _strip_meta(models),
    }
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(payload, f, indent=2, ensure_ascii=False)
        f.write("\n")


# ---------------------------------------------------------------- main

def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description="Build FMU catalog JSON from real repositories.")
    src = ap.add_mutually_exclusive_group(required=True)
    src.add_argument("--source", choices=["fmi-cross-check"], help="built-in source")
    src.add_argument("--repo", help="OWNER/REPO for a generic GitHub repository")
    ap.add_argument("--branch", help="branch name (default: repo default branch)")
    ap.add_argument("--platform", action="append", help="FMI Cross Check platform filter (repeatable). Default: win64,c-code")
    ap.add_argument("--max-models", type=int, default=0, help="0 = no limit")
    ap.add_argument("--output", required=True, help="output catalog JSON path")
    ap.add_argument("--token", default=None, help="GitHub token (default: $GITHUB_TOKEN or ~/.powersimbuilder/github_token)")
    args = ap.parse_args(argv)

    token = _resolve_token(args.token)
    if not token:
        sys.stderr.write(
            "[WARN] no token found; unauthenticated rate limit is 60 req/h\n"
            "[WARN] run: python tools/setup_github_token.py\n"
        )

    try:
        if args.source == "fmi-cross-check":
            platforms = args.platform or DEFAULT_PLATFORMS
            models = build_from_cross_check(token, platforms, args.max_models)
            source_label = f"{CROSS_CHECK_REPO} ({', '.join(platforms)})"
        else:
            models = build_from_repo(args.repo, args.branch, token, args.max_models)
            source_label = args.repo
    except urllib.error.HTTPError as exc:
        sys.stderr.write(f"[NG] HTTP {exc.code}: {exc.reason}\n")
        if exc.code in (403, 429):
            sys.stderr.write("[NG] rate limit exceeded; set GITHUB_TOKEN or wait an hour\n")
        return 2
    except urllib.error.URLError as exc:
        sys.stderr.write(f"[NG] network error: {exc}\n")
        return 2

    if not models:
        sys.stderr.write("[NG] no .fmu files found; check source/platform/filters\n")
        return 1

    write_catalog(args.output, source_label, models)
    print(f"[OK] wrote {len(models)} model(s) to {args.output}")
    print(f"     source: {source_label}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))