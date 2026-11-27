#!/usr/bin/env python3
"""
gen_source_tree.py - generate docs/SOURCE_TREE.md (C# source map) from the project files.

    python tools\\gen_source_tree.py              # write docs/SOURCE_TREE.md
    python tools\\gen_source_tree.py --dry-run    # generate, print a summary, write nothing
    python tools\\gen_source_tree.py --check      # exit 1 if the file is out of date (for verify scripts)
    python tools\\gen_source_tree.py --include-tests
    python tools\\gen_source_tree.py --source git # use `git ls-files` instead of MSBuild
    python tools\\gen_source_tree.py --projects fs               # discover *.csproj by walking the folder
    python tools\\gen_source_tree.py --exclude previou_files     # skip folders/paths (repeatable, prefix or glob)

Source of truth (default --source auto)
  * Every tracked *.csproj is evaluated with `dotnet msbuild -getItem:Compile,PackageReference`,
    so exactly the files that are compiled are listed (globs / Remove / Include are resolved by MSBuild).
  * Falls back to `git ls-files "*.cs"` when dotnet / MSBuild is not available.
Project discovery (--projects): "git" (default) = tracked files only, reproducible on every machine;
"fs" = walk the folder (also finds untracked projects; bin/obj/tmp/dist/.git are always skipped).
Per project: target framework, version, NuGet packages. Per folder: file, main types, lines, <summary>.
Partial classes (Foo.cs, Foo.Bar.cs, Foo.Designer.cs) are shown as ONE entry.
Also lists tracked *.cs files that no project compiles (stray files).
"""
from __future__ import annotations
import argparse, fnmatch, json, os, re, subprocess, sys
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TYPE_RE = re.compile(r"^\s*(?:public|internal|private|protected|static|sealed|abstract|partial|unsafe|readonly|\s)*"
                     r"\b(class|record|struct|interface|enum)\s+([A-Za-z_]\w*)", re.M)
SUMMARY_RE = re.compile(r"///\s*<summary>\s*(.*?)\s*</summary>", re.S)
SKIP_PARTS = {"bin", "obj", ".git", "node_modules", "tmp", "dist"}


def git(*args):
    return subprocess.run(["git", *args], cwd=ROOT, capture_output=True, text=True, encoding="utf-8").stdout


def excluded(path: str, excludes) -> bool:
    p = path.lower()
    return any(p == e or p.startswith(e + "/") or fnmatch.fnmatch(p, e) for e in excludes)


def walk(suffix: str, excludes):
    res = []
    for dp, dn, fn in os.walk(ROOT):
        rel_dir = Path(dp).relative_to(ROOT).as_posix()
        dn[:] = [d for d in dn if d.lower() not in SKIP_PARTS
                 and not excluded(d if rel_dir == "." else f"{rel_dir}/{d}", excludes)]
        for f in fn:
            if f.lower().endswith(suffix):
                res.append(f if rel_dir == "." else f"{rel_dir}/{f}")
    return res


def find(suffix: str, mode: str = "git", excludes=()):
    """Files with the given suffix: tracked by git (default) or found by walking the folder."""
    ex = [e.strip("/").lower() for e in excludes]
    out = git("ls-files", "*" + suffix).splitlines() if mode == "git" else []
    if mode == "fs" or not out:          # fs mode, or not a git checkout
        out = walk(suffix, ex)
    return sorted(f for f in out
                  if not ({x.lower() for x in f.split("/")} & SKIP_PARTS) and not excluded(f, ex))


def is_test(name_or_path: str) -> bool:
    return "Tests" in name_or_path


def msbuild_eval(csproj: str) -> dict:
    cmd = ["dotnet", "msbuild", str(ROOT / csproj), "-getItem:Compile,PackageReference",
           "-getProperty:TargetFramework,TargetFrameworks,Version", "-nologo"]
    r = subprocess.run(cmd, cwd=ROOT, capture_output=True, text=True, encoding="utf-8", timeout=300)
    if r.returncode != 0:
        raise RuntimeError(f"{csproj}: msbuild exit {r.returncode}: {(r.stderr or r.stdout).strip()[:200]}")
    s = r.stdout
    i = s.find("{")
    if i < 0:
        raise RuntimeError(f"{csproj}: no JSON in msbuild output")
    data, _ = json.JSONDecoder().raw_decode(s[i:])
    props, items = data.get("Properties", {}), data.get("Items", {})
    files = []
    for it in items.get("Compile", []):
        fp = it.get("FullPath") or str((ROOT / csproj).parent / it.get("Identity", ""))
        try:
            rel = Path(fp).resolve().relative_to(ROOT).as_posix()
        except ValueError:
            continue                      # outside the repo
        if rel.endswith(".cs") and not (set(rel.split("/")) & {"obj", "bin"}):
            files.append(rel)
    pk = sorted((it.get("Identity", ""), it.get("Version", "")) for it in items.get("PackageReference", []))
    return dict(name=Path(csproj).stem, csproj=csproj,
                tfm=props.get("TargetFramework") or props.get("TargetFrameworks", ""),
                version=props.get("Version", ""), files=sorted(set(files)), packages=pk)


def collect(source: str, include_tests: bool, proj_mode: str = "git", excludes=()):
    all_cs = find(".cs", proj_mode, excludes)
    projects, mode = [], "git"
    if source in ("auto", "msbuild"):
        try:
            projects = [msbuild_eval(c) for c in find(".csproj", proj_mode, excludes)]
            mode = "msbuild" if projects else "git"
        except (RuntimeError, OSError, subprocess.TimeoutExpired, ValueError) as ex:
            if source == "msbuild":
                raise
            print(f"[WARN] MSBuild evaluation failed ({ex}); falling back to git ls-files", file=sys.stderr)
            projects = []
    if mode == "git":
        files = [f for f in all_cs if include_tests or not any(is_test(p) or p == "tests" for p in f.split("/")[:-1])]
        projects = [dict(name="(all tracked .cs files)", csproj="", tfm="", version="", files=files, packages=[])]
    compiled = set().union(*[set(p["files"]) for p in projects]) if projects else set()
    orphans = sorted(set(all_cs) - compiled) if mode == "msbuild" else []
    shown = projects if mode == "git" else [p for p in projects if include_tests or not is_test(p["name"])]
    return shown, orphans, mode


def read(f):
    try:
        return (ROOT / f).read_text(encoding="utf-8-sig", errors="replace")
    except OSError:
        return ""


def one_line(s: str, n=110):
    s = re.sub(r"\s*///\s*", " ", s)
    s = re.sub(r"<[^>]+>", "", s)
    s = re.sub(r"\s+", " ", s).strip()
    return (s[: n - 1] + "…") if len(s) > n else s


def stem_group(f: str):
    d, name = os.path.split(f)
    return d, name[:-3].split(".")[0]          # Foo.Bar.Designer.cs -> Foo


def describe(group_files):
    types, lines, summary, parts = [], 0, "", []
    main_stem = os.path.basename(group_files[0])[:-3].split(".")[0]
    for f in sorted(group_files, key=lambda x: (x.count("."), x)):
        text = read(f)
        lines += text.count("\n") + 1
        base = os.path.basename(f)[:-3]
        if "." in base:
            parts.append(base.split(".", 1)[1])
        for m in TYPE_RE.finditer(text):
            if m.group(2) not in types:
                types.append(m.group(2))
        if not summary:
            for m in SUMMARY_RE.finditer(text):
                after = text[m.end(): m.end() + 400]
                if re.search(r"\b(class|record|struct|interface|enum)\s+" + re.escape(main_stem) + r"\b", after):
                    summary = one_line(m.group(1)); break
            if not summary:
                m = SUMMARY_RE.search(text)
                if m:
                    summary = one_line(m.group(1))
    return types, lines, summary, parts


def build(source: str, include_tests: bool, proj_mode: str = "git", excludes=()) -> str:
    projects, orphans, mode = collect(source, include_tests, proj_mode, excludes)
    head = git("rev-parse", "--short", "HEAD").strip() or "unknown"
    where = "tracked" if proj_mode == "git" else "discovered (folder walk)"
    origin = (f"MSBuild Compile items of the {where} `*.csproj` files" if mode == "msbuild"
              else "tracked `*.cs` files (`git ls-files`)")
    out = ["# C# Source Map", "",
           f"_Generated by `tools/gen_source_tree.py` from {origin} (git `{head}`). "
           "Do not edit by hand; run the script to refresh._", ""]
    for p in projects:
        by_dir = defaultdict(lambda: defaultdict(list))
        for f in p["files"]:
            d, key = stem_group(f)
            by_dir[d or "."][key].append(f)
        units = sum(len(v) for v in by_dir.values())
        described = {d: {k: describe(fs) for k, fs in sorted(g.items(), key=lambda kv: kv[0].lower())} for d, g in by_dir.items()}
        lines = sum(x[1] for g in described.values() for x in g.values())
        out += [f"## Project: {p['name']}", ""]
        meta = [f"`{p['csproj']}`"] if p["csproj"] else []
        meta += [m for m in (p["tfm"], f"v{p['version']}" if p["version"] else "") if m]
        meta.append(f"{units} source units, {lines:,} lines, {len(by_dir)} folders")
        out += [" · ".join(meta), ""]
        if p["packages"]:
            out += ["NuGet: " + ", ".join(f"`{n} {v}`".replace(" `", "`") if not v else f"`{n} {v}`" for n, v in p["packages"]), ""]
        out += ["### Folders", "", "| Folder | Units | Lines |", "|---|---:|---:|"]
        for d in sorted(described):
            out.append(f"| `{d}/` | {len(described[d])} | {sum(x[1] for x in described[d].values())} |")
        out.append("")
        for d in sorted(described):
            out += [f"### `{d}/`", "", "| File | Main types | Lines | Summary |", "|---|---|---:|---|"]
            for key, (types, n, summ, parts) in described[d].items():
                label = f"`{key}.cs`" + (f" + {', '.join(sorted(set(parts)))}" if parts else "")
                shown = ", ".join(f"`{t}`" for t in types[:4]) + (" …" if len(types) > 4 else "")
                out.append(f"| {label} | {shown} | {n} | {summ} |")
            out.append("")
    if mode == "msbuild":
        out += ["## `.cs` files not compiled by any project", ""]
        out += [f"- `{o}`" for o in orphans] if orphans else ["None."]
        out.append("")
    return "\n".join(out).rstrip() + "\n"


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--output", default="docs/SOURCE_TREE.md")
    ap.add_argument("--source", choices=["auto", "msbuild", "git"], default="auto")
    ap.add_argument("--projects", choices=["git", "fs"], default="git",
                    help="discover *.csproj / *.cs from git-tracked files (default) or by walking the folder")
    ap.add_argument("--exclude", action="append", default=[], metavar="PATH",
                    help="exclude a folder/path prefix or glob (repeatable), e.g. previou_files")
    ap.add_argument("--include-tests", action="store_true")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--check", action="store_true")
    a = ap.parse_args()
    text = build(a.source, a.include_tests, a.projects, a.exclude)
    out = ROOT / a.output
    if a.check:
        cur = out.read_text(encoding="utf-8") if out.exists() else ""
        strip = lambda t: re.sub(r"\(git `[^`]*`\)", "", t)   # the commit hash must not make the doc stale
        ok = strip(cur) == strip(text)
        print("[OK] up to date" if ok else "[STALE] run: python tools/gen_source_tree.py")
        return 0 if ok else 1
    print(f"[INFO] {len(text.splitlines())} lines generated")
    for line in text.splitlines():
        if line.startswith("## ") or line.startswith("- `") and "not compiled" in text:
            print("      ", line)
    if a.dry_run:
        print("[DRY-RUN] nothing written"); return 0
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(text, encoding="utf-8", newline="\n")
    print(f"[OK] wrote {out.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
