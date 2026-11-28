#!/usr/bin/env python3
r"""
package_release.py - build the PowerSimBuilder release ZIP (Windows x64, self-contained).

    python tools\package_release.py                                 # full auto (publish + catalog + zip + checks)
    python tools\package_release.py --set-version 1.11.0            # write version to VERSION + csproj, then build
    python tools\package_release.py --auto-version                  # derive version from latest git tag
    python tools\package_release.py --skip-publish Tmp\binary-test  # reuse an existing publish folder
    python tools\package_release.py --catalog-json <file>           # reuse an existing catalog JSON

Auto-handled (NO manual work needed):
  - Version mismatch between VERSION file and csproj <Version>:
        auto-writes VERSION to match csproj (prints a warning).
        Use --set-version or --auto-version to set both files to a new value.
  - Missing README_FIRST template (tools\release_assets\README_FIRST.md):
        generates a default EN+ZH template automatically.
  - Catalog build failure (no network / git not available):
        searches Tmp\ for an existing *fmu_catalog*.json and reuses it.
  - catalog_sources.json missing bundled-catalog reference:
        auto-injects a local-path preset entry.

Output: dist\PowerSimBuilder_v<version>_win-x64.zip (+ .sha256)

Then:  gh release upload v<version> dist\<zip> --clobber
"""
from __future__ import annotations
import argparse, hashlib, json, re, shutil, subprocess, sys, time, zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
RID = "win-x64"
CATALOG_NAME = "fmu_catalog_fmi2_cs_win64.json"
SKIP_DIRS = {"__pycache__", ".git", ".pytest_cache", ".mypy_cache", ".ruff_cache"}
SKIP_SUFFIX = {".pyc", ".pdb"}
KANA = re.compile(r"[\u3040-\u30ff]")          # Hiragana / Katakana
CJK = re.compile(r"[\u4e00-\u9fff]")
DOCS = ["FMU_CATALOG_GUIDE.md", "FMU_CATALOG_GUIDE.zh.md", "SPEC_PYTHON_RUNTIME.en.md", "SPEC_PYTHON_RUNTIME.zh.md"]

SETUP_PS1 = r'''# Run once after extracting. Requires Python 3.10+ on PATH.
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
python --version
python -m pip install --upgrade pip
python -m pip install -r requirements.txt
try {
    python -m pip install .
} catch {
    Write-Warning "pip install . failed. Fallback: set PYTHONPATH to '$PSScriptRoot\src' before starting PowerSimBuilder.exe"
}
python -c "import python_runtime; print('python_runtime OK:', python_runtime.__file__)"
Write-Host "Setup finished. Start PowerSimBuilder.exe"
'''

DEFAULT_README_FIRST = r"""# PowerSimBuilder {{VERSION}}

## English

PowerSimBuilder is a GUI builder for power-system simulation using FMU models on Windows.

### Quick start
1. Extract the ZIP to any folder.
2. Run `setup_python.ps1` once (requires Python 3.10+ on PATH).
3. Start `PowerSimBuilder.exe`.

### Contents
- `PowerSimBuilder.exe` - main application (self-contained, no .NET install needed)
- `catalog\fmu_catalog_fmi2_cs_win64.json` - FMU model URL list (FMI 2.0 / Co-Simulation / win64)
- `docs\FMU_CATALOG_GUIDE.md` - how to use the FMU catalog
- `examples\` - sample projects
- `Pallets\`, `Resources\` - application data
- `src\python_runtime\`, `requirements.txt`, `setup_python.ps1` - Python runtime for FMU scripting

### FMU catalog
The bundled `catalog\fmu_catalog_fmi2_cs_win64.json` contains download URLs for FMU models.
Open the Catalog tab in PowerSimBuilder to browse and load models.
See `docs\FMU_CATALOG_GUIDE.md` for details.

## 中文

PowerSimBuilder 是一个在 Windows 上使用 FMU 模型进行电力系统仿真的 GUI 构建工具。

### 快速开始
1. 将 ZIP 解压到任意文件夹。
2. 运行一次 `setup_python.ps1`（需要 PATH 中有 Python 3.10+）。
3. 启动 `PowerSimBuilder.exe`。

### 内容
- `PowerSimBuilder.exe` - 主程序（自我包含，无需安装 .NET）
- `catalog\fmu_catalog_fmi2_cs_win64.json` - FMU 模型 URL 列表（FMI 2.0 / 联合仿真 / win64）
- `docs\FMU_CATALOG_GUIDE.zh.md` - FMU 目录使用说明
- `examples\` - 示例工程
- `Pallets\`、`Resources\` - 应用数据
- `src\python_runtime\`、`requirements.txt`、`setup_python.ps1` - Python 运行时（用于 FMU 脚本）

### FMU 目录
随附的 `catalog\fmu_catalog_fmi2_cs_win64.json` 包含 FMU 模型的下载地址。
在 PowerSimBuilder 中打开 Catalog 标签页浏览并加载模型。
详见 `docs\FMU_CATALOG_GUIDE.zh.md`。
"""


def die(msg):
    print(f"[ERR] {msg}", file=sys.stderr)
    sys.exit(1)


def warn(msg):
    print(f"[WARN] {msg}", file=sys.stderr)


def rmtree(p: Path):
    for i in range(3):
        try:
            if p.exists():
                shutil.rmtree(p)
            return
        except OSError:
            time.sleep(0.5 * (i + 1))
    die(f"cannot remove {p}")


def ignore(_d, names):
    return [n for n in names if n in SKIP_DIRS or Path(n).suffix in SKIP_SUFFIX]


def copy_file(src: Path, dst: Path, required=True) -> bool:
    if not src.exists():
        (die if required else print)(f"{'' if required else '[WARN] '}missing: {src}")
        return False
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dst)
    return True


def read_csproj_version() -> str:
    cs = (ROOT / "PowerSimBuilder.csproj").read_text(encoding="utf-8-sig")
    m = re.search(r"<Version>([^<]+)</Version>", cs)
    if not m:
        die("<Version> not found in PowerSimBuilder.csproj")
    return m.group(1).strip()


def read_version_file() -> str | None:
    vf = ROOT / "VERSION"
    return vf.read_text(encoding="utf-8-sig").strip() if vf.exists() else None


def write_version_file(v: str):
    (ROOT / "VERSION").write_text(v, encoding="utf-8")


def write_csproj_version(v: str):
    cs_path = ROOT / "PowerSimBuilder.csproj"
    cs = cs_path.read_text(encoding="utf-8-sig")
    if not re.search(r"<Version>[^<]*</Version>", cs):
        die(f"could not find <Version>...</Version> in {cs_path}")
    new_cs = re.sub(r"<Version>[^<]*</Version>", f"<Version>{v}</Version>", cs)
    cs_path.write_text(new_cs, encoding="utf-8-sig")


def auto_version_from_tag() -> str:
    """Derive next version from the latest git tag (e.g. v1.10.2 -> 1.10.3)."""
    try:
        r = subprocess.run(
            ["git", "describe", "--tags", "--abbrev=0"],
            cwd=ROOT, capture_output=True, text=True, timeout=10,
        )
        if r.returncode != 0:
            die("cannot determine version: git describe failed (pass --set-version)")
        tag = r.stdout.strip()
        if tag.startswith("v"):
            tag = tag[1:]
        parts = tag.split(".")
        if len(parts) == 3 and parts[2].isdigit():
            parts[2] = str(int(parts[2]) + 1)
            return ".".join(parts)
        return tag
    except FileNotFoundError:
        die("git not found on PATH (pass --set-version)")


def ensure_readme_template() -> Path:
    """Return README_FIRST.md template path; generate a default if missing."""
    tpl = ROOT / "tools" / "release_assets" / "README_FIRST.md"
    if tpl.exists():
        return tpl
    tpl.parent.mkdir(parents=True, exist_ok=True)
    tpl.write_text(DEFAULT_README_FIRST, encoding="utf-8")
    print(f"[INFO] generated default README_FIRST template -> {tpl}")
    return tpl


def pyproject_refs(pp: Path):
    try:
        import tomllib
        data = tomllib.loads(pp.read_text(encoding="utf-8"))
    except Exception:
        return []
    proj, refs = data.get("project", {}), []
    r = proj.get("readme")
    refs += [r] if isinstance(r, str) else ([r["file"]] if isinstance(r, dict) and "file" in r else [])
    lic = proj.get("license")
    if isinstance(lic, dict) and "file" in lic:
        refs.append(lic["file"])
    refs += [x for x in (proj.get("license-files") or []) if not re.search(r"[*?\[]", x)]
    return refs

def publish(stage: Path):
    cmd = [
        "dotnet", "publish",
        str(ROOT / "PowerSimBuilder.csproj"),
        "-c", "Release",
        "-r", RID,
        "--self-contained",
        "-p:PublishSingleFile=true",
        "-p:DebugType=None",
        "-p:DebugSymbols=false",
        "-o", str(stage)
    ]

    print("[INFO]", " ".join(cmd))
    if subprocess.run(cmd, cwd=ROOT).returncode != 0:
        die("dotnet publish failed")

    # ScintillaNET のネイティブ DLL を所定の場所へ移動する
    native_dir = stage / "runtimes" / RID / "native"
    native_dir.mkdir(parents=True, exist_ok=True)

    for filename in ("Scintilla.dll", "Lexilla.dll"):
        src = stage / filename
        dst = native_dir / filename

        if not src.is_file():
            die(f"Required native DLL not found after publish: {src}")

        if dst.exists():
            die(f"Native DLL destination already exists: {dst}")

        shutil.move(str(src), str(dst))
        print(f"[INFO] Native DLL moved: {src.name} -> {dst.relative_to(stage)}")
        

def build_catalog(dst: Path) -> bool:
    """Run build_cross_check_catalog.py. Returns True on success, False on failure."""
    dst.parent.mkdir(parents=True, exist_ok=True)
    builder = ROOT / "tools" / "build_cross_check_catalog.py"
    if not builder.exists():
        print(f"[WARN] {builder} not found")
        return False
    cmd = [sys.executable, str(builder),
           "--platform", "win64", "--output", str(dst)]
    print("[INFO]", " ".join(cmd))
    return subprocess.run(cmd, cwd=ROOT).returncode == 0


def patch_catalog_sources(srcs: Path):
    """Auto-inject a bundled-catalog reference into catalog_sources.json if missing.

    Schema matches existing entries: id / label / url / region / kind.
    The url is a relative path; GitHubCatalogClient.cs must treat non-http,
    non-github: urls as local files relative to the app base directory.
    """
    text = srcs.read_text(encoding="utf-8-sig")
    if CATALOG_NAME in text:
        return
    local_rel = f"catalog/{CATALOG_NAME}"
    try:
        data = json.loads(text)
    except Exception as ex:
        warn(f"catalog_sources.json parse failed: {ex}; cannot auto-patch")
        return
    entry = {
        "id": "bundled-fmu-catalog",
        "label": "Bundled FMU Catalog (offline)",
        "url": local_rel,
        "region": "local",
        "kind": "json",
    }
    if isinstance(data, dict) and isinstance(data.get("sources"), list):
        data["sources"].append(entry)
        srcs.write_text(json.dumps(data, indent=4, ensure_ascii=False) + "\n", encoding="utf-8-sig")
        print(f"[INFO] patched catalog_sources.json: added bundled catalog to 'sources'")
        print(f"[INFO]   entry: id={entry['id']!r} url={entry['url']!r} kind={entry['kind']!r}")
        return
    if isinstance(data, list):
        data.append(entry)
        srcs.write_text(json.dumps(data, indent=4, ensure_ascii=False) + "\n", encoding="utf-8-sig")
        print(f"[INFO] patched catalog_sources.json: appended bundled catalog entry")
        return
    warn("could not auto-patch catalog_sources.json (unrecognized structure)")


def find_existing_catalog() -> Path | None:
    """Search Tmp/ (and the skip-publish folder) for a usable FMU catalog JSON."""
    seen = []
    for base in [ROOT / "Tmp", ROOT / "tmp"]:
        if not base.exists():
            continue
        for p in sorted(base.rglob("*fmu_catalog*.json")):
            seen.append(p)
            try:
                data = json.loads(p.read_text(encoding="utf-8"))
                if isinstance(data.get("models"), list) and data["models"]:
                    return p
            except Exception:
                continue
    if seen:
        print(f"[WARN] found catalog JSON(s) but none valid: {seen}")
    return None


def check_catalog(p: Path) -> int:
    try:
        data = json.loads(p.read_text(encoding="utf-8"))
    except Exception as ex:
        die(f"catalog JSON invalid: {ex}")
    models = data.get("models")
    if not isinstance(models, list) or not models:
        die("catalog JSON has no models")
    bad = [m for m in models if not (m.get("name") and str(m.get("url", "")).startswith("https://") and m["url"].lower().endswith(".fmu"))]
    if bad:
        die(f"catalog has {len(bad)} invalid entries (name/url/.fmu), e.g. {bad[0]}")
    return len(models)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--version", help="expect this version (must already match csproj)")
    ap.add_argument("--set-version", metavar="VER", help="write VER to VERSION file and csproj <Version>, then build")
    ap.add_argument("--auto-version", action="store_true", help="derive version from latest git tag and set both files")
    ap.add_argument("--skip-publish", metavar="DIR", help="reuse an existing publish folder")
    ap.add_argument("--catalog-json", metavar="FILE", help="use an existing catalog JSON")
    ap.add_argument("--out", default="dist")
    ap.add_argument("--min-catalog", type=int, default=0, help="fail if catalog has fewer than N FMUs (default: 0)")
    ap.add_argument("--ignore-version-file", action="store_true", help="do not auto-fix VERSION file on mismatch")
    a = ap.parse_args()

    # 0. version reconciliation (fully automatic)
    if a.set_version:
        write_version_file(a.set_version)
        write_csproj_version(a.set_version)
        print(f"[INFO] set VERSION + csproj <Version> -> {a.set_version}")
        version = a.set_version
    elif a.auto_version:
        version = auto_version_from_tag()
        write_version_file(version)
        write_csproj_version(version)
        print(f"[INFO] auto-version from git tag -> {version} (written to VERSION + csproj)")
    else:
        csproj_v = read_csproj_version()
        file_v = read_version_file()
        version = a.version or csproj_v
        if csproj_v != version:
            die(f"csproj <Version> is {csproj_v}, expected {version}")
        if file_v is not None and file_v != version:
            if a.ignore_version_file:
                warn(f"VERSION file is {file_v}, expected {version} (--ignore-version-file: skipped)")
            else:
                write_version_file(version)
                warn(f"VERSION file was {file_v}, auto-fixed to {version}")

    name = f"PowerSimBuilder_v{version}_win-x64"
    stage = ROOT / "tmp" / "release_stage" / name
    rmtree(stage)
    print(f"[INFO] version {version} -> {name}")

    # 1. application
    if a.skip_publish:
        src = Path(a.skip_publish).resolve()
        if not (src / "PowerSimBuilder.exe").exists():
            die(f"{src} has no PowerSimBuilder.exe")
        shutil.copytree(src, stage, ignore=ignore)
    else:
        publish(stage)
        for f in list(stage.rglob("*.pdb")):
            f.unlink()
        # dotnet publish only copies Content items; examples/ and Pallets/ are
        # runtime data folders that are NOT in the csproj as Content. Search the
        # source tree first, then fall back to Tmp/binary-test (previous build).
        for folder in ("examples", "Pallets"):
            dst = stage / folder
            candidates = [
                ROOT / folder,                           # source tree
                ROOT / "Tmp" / "binary-test" / folder,   # previous binary (capital T)
                ROOT / "tmp" / "binary-test" / folder,   # previous binary (lowercase)
            ]
            src_folder = next((c for c in candidates if c.exists()), None)
            if src_folder:
                shutil.copytree(src_folder, dst, ignore=ignore, dirs_exist_ok=True)
                print(f"[INFO] copied {folder}/ from {src_folder.relative_to(ROOT)}")
            else:
                print(f"[WARN] {folder}/ not found (checked source tree and Tmp/binary-test)")

    # 2. FMU URL list (auto-fallback to existing JSON on failure)
    cat = stage / "catalog" / CATALOG_NAME
    if a.catalog_json:
        copy_file(Path(a.catalog_json), cat)
    elif not build_catalog(cat):
        warn("catalog build failed; searching Tmp/ for an existing catalog JSON")
        existing = find_existing_catalog()
        if existing:
            print(f"[INFO] reusing existing catalog -> {existing}")
            copy_file(existing, cat)
        else:
            die("catalog build failed and no existing catalog found in Tmp/ (pass --catalog-json)")
    n = check_catalog(cat)
    if a.min_catalog and n < a.min_catalog:
        die(f"catalog has only {n} FMU(s), expected >= {a.min_catalog} (use --min-catalog to change)")
    print(f"[OK] catalog: {n} FMU(s)")

    # 3. python runtime + setup
    shutil.copytree(ROOT / "src" / "python_runtime", stage / "src" / "python_runtime", ignore=ignore)
    copy_file(ROOT / "requirements.txt", stage / "requirements.txt")
    if copy_file(ROOT / "pyproject.toml", stage / "pyproject.toml", required=False):
        for ref in pyproject_refs(ROOT / "pyproject.toml"):
            copy_file(ROOT / ref, stage / ref, required=False)
    (stage / "setup_python.ps1").write_text(SETUP_PS1, encoding="utf-8")

    # 4. docs (English + Chinese only) + auto-generated README_FIRST
    for d in DOCS:
        copy_file(ROOT / "docs" / d, stage / "docs" / d, required=False)
    copy_file(ROOT / "LICENSE", stage / "LICENSE")
    copy_file(ROOT / "THIRD-PARTY-NOTICES.md", stage / "THIRD-PARTY-NOTICES.md", required=False)
    # auto-inject bundled catalog reference into catalog_sources.json
    srcs_stage = stage / "Resources" / "catalog_sources.json"
    if srcs_stage.exists():
        patch_catalog_sources(srcs_stage)
    tpl = ensure_readme_template()
    (stage / "README_FIRST.md").write_text(
        tpl.read_text(encoding="utf-8").replace("{{VERSION}}", version), encoding="utf-8"
    )
    # 5. checks
    errs = []

    # Single-file publish / Scintilla native DLL placement checks
    native_dir = stage / "runtimes" / RID / "native"

    for filename in ("Scintilla.dll", "Lexilla.dll"):
        if not (native_dir / filename).is_file():
            errs.append(f"missing native DLL: runtimes/{RID}/native/{filename}")

    # アプリケーションのルートに DLL が残っていないこと
    root_dlls = list(stage.glob("*.dll"))
    if root_dlls:
        errs.append(
            "unexpected DLL(s) in application root: "
            + ", ".join(p.name for p in root_dlls)
        )

    # 複数ファイル発行時の .NET ランタイム DLL が混ざっていないこと
    runtime_dll_patterns = (
        "hostfxr.dll",
        "hostpolicy.dll",
        "coreclr.dll",
        "clrjit.dll",
    )

    for p in stage.rglob("*.dll"):
        if p.name.lower() in runtime_dll_patterns or p.name.startswith("System."):
            errs.append(
                f"unexpected .NET runtime DLL: {p.relative_to(stage)}"
            )
           
    # 5. checks
    errs = []
    need = ["PowerSimBuilder.exe", "README_FIRST.md", f"catalog/{CATALOG_NAME}", "Resources/catalog_sources.json",
            "src/python_runtime/__init__.py", "requirements.txt", "setup_python.ps1", "LICENSE",
            "docs/FMU_CATALOG_GUIDE.md", "docs/FMU_CATALOG_GUIDE.zh.md"]
    # 6. zip
    out = ROOT / a.out
    out.mkdir(parents=True, exist_ok=True)
    zpath = out / f"{name}.zip"
    zpath.unlink(missing_ok=True)
    with zipfile.ZipFile(zpath, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        for f in sorted(stage.rglob("*")):
            if f.is_file():
                z.write(f, f"{name}/{f.relative_to(stage).as_posix()}")
    sha = hashlib.sha256(zpath.read_bytes()).hexdigest()
    (out / f"{name}.zip.sha256").write_text(f"{sha}  {zpath.name}\n", encoding="utf-8")
    with zipfile.ZipFile(zpath) as z:
        if z.testzip() is not None:
            die("zip integrity check failed")
        count = len(z.namelist())
    print(f"\n[OK] {zpath}  ({zpath.stat().st_size/1048576:.1f} MB, {count} files)\n     sha256 {sha}")
    print(f"\nNext:  gh release upload v{version} {zpath.relative_to(ROOT)} --clobber")
    return 0


if __name__ == "__main__":
    sys.exit(main())
