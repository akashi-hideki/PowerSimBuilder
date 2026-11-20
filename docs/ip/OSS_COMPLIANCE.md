# OSS License Compliance Statement

**Product**: PowerSimBuilder v1.10.1
**Date**: 2026-11-21
**Author**: Hideki Akashi

## Summary

PowerSimBuilder is compliant with all OSS licenses of its dependencies
and is safe for commercial distribution, including sale to third parties.

## Runtime Dependencies

### .NET / NuGet

PowerSimBuilder is built on **.NET 10** (MIT license). Third-party NuGet
packages are listed in `licenses_full.csv`. All are permissive licenses
(MIT / Apache-2.0 / BSD).

**Verification**:
```powershell
dotnet list PowerSimBuilder.csproj package --include-transitive
```

### Python (fmpy)

PowerSimBuilder invokes **`fmpy`** (BSD-2-Clause) for FMU execution.
`fmpy` is **not bundled** — the user installs it separately via pip.

**Verification**:
```powershell
pip show fmpy
pip-licenses --packages fmpy
```

## No GPL/AGPL Contamination

Runtime dependencies of PowerSimBuilder (both .NET and Python) contain
**no GPL or AGPL packages**. Confirmed by:

- `dotnet list ... --include-transitive` — all licenses are MIT / Apache-2.0 / BSD
- `pip-licenses` for fmpy and its dependencies

## Build Tools (not distributed)

- **.NET 10 SDK** — MIT
- **PyInstaller** (if used for distribution) — GPLv2 with Bootloader Exception
- **pre-commit** — MIT (development only)

## Distribution Artifacts

| Artifact | Contents | License |
|---|---|---|
| `PowerSimBuilder.exe` | Compiled C# from this repo | Apache-2.0 |
| `Resources/catalog_sources.json` | Configuration | Apache-2.0 |
| External fmpy runtime | Installed separately by user | BSD-2-Clause |

No third-party binaries are bundled with PowerSimBuilder itself.

## Compliance Checklist

- [x] No GPL/AGPL in runtime dependencies
- [x] .NET 10 runtime is MIT (framework, not bundled)
- [x] fmpy is BSD-2-Clause (not bundled)
- [x] All NuGet packages are permissive
- [x] License inventory stored in `docs/ip/licenses_full.csv`

## Conclusion

PowerSimBuilder v1.10.1 can be commercially distributed and sold without
any OSS license restrictions on its own source code.


---

## Appendix: Environment Isolation

`licenses_full.csv` contains **only PowerSimBuilder's runtime dependencies**:

- **NuGet (5)**: Costura.Fody (MIT), CsvHelper (MS-PL OR Apache-2.0),
  NCalc (MIT), Pythonnet (MIT), Fody (MIT)
- **Python / fmpy (18)**: FMPy (BSD-2-Clause), attrs (MIT), cmake
  (Apache/BSD), Jinja2 (BSD), MarkupSafe (BSD), lark (MIT), lxml (BSD),
  msgpack (Apache-2.0), nbformat (BSD), fastjsonschema (BSD),
  jsonschema (MIT), jsonschema-specifications (MIT), referencing (MIT),
  rpds-py (MIT), jupyter_core (BSD), platformdirs (MIT),
  traitlets (BSD), numpy (BSD)

The developer's global Python environment contains additional packages
(e.g., PyQt5, PyMuPDF, Django from unrelated projects), but **none of
these are dependencies of PowerSimBuilder** and are excluded.

**Verification commands**:
```powershell
pipdeptree --packages fmpy
dotnet list PowerSimBuilder.csproj package --include-transitive
```
