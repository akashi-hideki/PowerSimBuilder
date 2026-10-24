#!/usr/bin/env python3
"""Probe catalog sources for reachability and content.

Usage:
    python tools/catalog_probe.py
    python tools/catalog_probe.py --sources Resources/catalog_sources.json
    python tools/catalog_probe.py --timeout 3 --json
    python tools/catalog_probe.py --source https://example.com/catalog.json

Stdlib only. Never invoked by the GUI. Safe to run manually.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
import time
import urllib.error
import urllib.request
from typing import Any


DEFAULT_SOURCES = "Resources/catalog_sources.json"
USER_AGENT = "PowerSimBuilder-CatalogProbe/1.0"
GITHUB_RE = re.compile(r"github\.com/([^/\s]+)/([^/\s#?]+?)(?:\.git)?(?:/|$)")


def http_get(url: str, timeout: float) -> tuple[int, bytes]:
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(req, timeout=timeout) as resp:
        return resp.status, resp.read()


def count_json_models(body: bytes) -> int:
    data = json.loads(body.decode("utf-8"))
    models = data.get("models")
    if not isinstance(models, list):
        raise ValueError("missing 'models' array")
    return len(models)


def _resolve_github_api(url: str) -> str | None:
    m = GITHUB_RE.search(url)
    if not m:
        return None
    return "https://api.github.com/repos/{0}/{1}/releases".format(m.group(1), m.group(2))


def count_github_fmu_assets(url: str, timeout: float) -> int:
    api = _resolve_github_api(url)
    if api is None:
        raise ValueError("not a github.com URL")
    _, body = http_get(api, timeout)
    releases = json.loads(body.decode("utf-8"))
    if not isinstance(releases, list):
        raise ValueError("github API returned non-array")
    count = 0
    for rel in releases:
        for asset in rel.get("assets", []) or []:
            name = (asset.get("name") or "").lower()
            if name.endswith(".fmu"):
                count += 1
    return count


def probe_one(source: dict, timeout: float) -> dict:
    sid = source.get("id", "?")
    label = source.get("label", sid)
    url = source.get("url", "")
    kind = source.get("kind", "json")
    result = {
        "id": sid, "label": label, "url": url, "kind": kind,
        "ok": False, "status": None, "elapsed_ms": None,
        "models": None, "error": None,
    }
    t0 = time.perf_counter()
    try:
        if kind == "json":
            status, body = http_get(url, timeout)
            result["status"] = status
            result["models"] = count_json_models(body)
        elif kind == "github":
            status, _ = http_get(url, timeout)
            result["status"] = status
            result["models"] = count_github_fmu_assets(url, timeout)
        else:
            raise ValueError("unknown kind: {0}".format(kind))
        result["ok"] = True
    except (urllib.error.URLError, TimeoutError) as exc:
        result["error"] = "network: {0}".format(exc)
    except (ValueError, json.JSONDecodeError) as exc:
        result["error"] = "parse: {0}".format(exc)
    finally:
        result["elapsed_ms"] = int((time.perf_counter() - t0) * 1000)
    return result


def _fmt(result: dict) -> str:
    tag = "[OK]" if result["ok"] else "[NG]"
    sid = (result["id"] + " " * 22)[:22]
    if result["ok"]:
        st = str(result["status"]) if result["status"] is not None else "-"
        ms = "{0}ms".format(result["elapsed_ms"]) if result["elapsed_ms"] is not None else "-"
        return "{0} {1} {2:>4} {3:>7}  {4} model(s)".format(tag, sid, st, ms, result["models"])
    err = (result["error"] or "unknown")[:60]
    return "{0} {1} {2:>4} {3:>7}  {4}".format(tag, sid, "", "", err)


def main(argv):
    ap = argparse.ArgumentParser(description="Probe catalog sources.")
    ap.add_argument("--sources", default=DEFAULT_SOURCES)
    ap.add_argument("--source", help="probe a single URL (ad-hoc)")
    ap.add_argument("--kind", default="json", choices=["json", "github"])
    ap.add_argument("--timeout", type=float, default=5.0)
    ap.add_argument("--json", action="store_true")
    args = ap.parse_args(argv)

    if args.source:
        sources = [{"id": "adhoc", "label": args.source, "url": args.source, "kind": args.kind}]
    else:
        try:
            with open(args.sources, "r", encoding="utf-8") as f:
                sources = json.load(f).get("sources", [])
        except FileNotFoundError:
            print("[NG] sources file not found: {0}".format(args.sources), file=sys.stderr)
            return 2
        except json.JSONDecodeError as exc:
            print("[NG] sources file invalid JSON: {0}".format(exc), file=sys.stderr)
            return 2

    results = [probe_one(s, args.timeout) for s in sources]

    if args.json:
        print(json.dumps(results, indent=2, ensure_ascii=False))
    else:
        for r in results:
            print(_fmt(r))
        ok = sum(1 for r in results if r["ok"])
        print("\n{0}/{1} reachable".format(ok, len(results)))

    return 0 if any(r["ok"] for r in results) else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
