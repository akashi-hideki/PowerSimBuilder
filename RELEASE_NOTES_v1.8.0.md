## Highlights

### GUI FMU E2E
`SmokeTests.AddPhysicsAndRun_Dahlquist_Completes` verifies the full path
**Add Physics -> Dahlquist -> Run** through the WinForms UI using FlaUI.
The test asserts that Run completes and `lblStatus` updates to a terminal state.

### Python runtime spec
- [`docs/SPEC_PYTHON_RUNTIME.en.md`](docs/SPEC_PYTHON_RUNTIME.en.md) (English)
- [`docs/SPEC_PYTHON_RUNTIME.zh.md`](docs/SPEC_PYTHON_RUNTIME.zh.md) (中文)

Installation is now a single command:

```powershell
pip install -r requirements.txt
```

### Dependency audit
- **Added** `pyyaml` (used by `src/python_runtime/physics/manifest.py`)
- **Removed** `lxml` (stdlib `xml.etree` suffices)
- Verified via the new [`tools/scan_imports.py`](tools/scan_imports.py) (AST-based import scanner)

## Fixed

- **Run status** now updates on completion (previously stayed at `Running...`).
- **New component Run** no longer fails on `Path.GetDirectoryName(null)`; falls back to
  `ProjectDir` -> `SuggestedInitialDirectory` -> CWD -> `%TEMP%`.

## Testing

| Suite | Result |
|---|---|
| C# unit tests | 528 (526 pass / 2 skip / 0 fail) |
| UI E2E | 12 (11 pass / 1 skip / 0 fail) |
| Python | 463 pass |

UI tests now run sequentially (`xunit.runner.json`), eliminating prior flakiness.

## Changelog

See [CHANGELOG.md](CHANGELOG.md#180---2026-11-01).
