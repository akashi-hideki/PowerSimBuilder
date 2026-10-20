# PowerSimBuilder

[![CI](https://github.com/akashi-hideki/PowerSimBuilder/actions/workflows/ci.yml/badge.svg)](https://github.com/akashi-hideki/PowerSimBuilder/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10%20LTS-512BD4)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/Tests-330%20pass-green.svg)]()
[![Platform](https://img.shields.io/badge/Platform-Windows%20only-lightgrey.svg)]()

**Visual simulation companion to [StaTable](https://github.com/akashi-hideki/StaTable) — load StaTable XML, build Python state-machine controllers, run end-to-end simulations with signal plots.**

---

## Editing workflow (v1.1.0)

1. **Canvas → right-click → `Edit Python Code`**
   - The editor opens immediately with in-memory code (existing content or freshly generated), titled `(unsaved) *`.
   - SaveFileDialog is deferred to the first `Save`.
2. **Left pane: File Structure**
   - `📦 ClassName : Base (Lnn)` with expandable methods.
   - `F_xxx ⚠️` = stub, `F_xxx ✅` = implemented, plain `__init__` etc.
   - Double-click → jump to line.
   - `Bundled Components` root → double-click a `🔒` item to create a `user_components/` override.
3. **Right pane: Python code**
   - Generated files contain `# STABLE_USER_CODE_START` / `# STABLE_USER_CODE_END` inside `update()`.
   - StaTable-generated `*_powersim.py` files contain `# STABLE_USER_CODE_START:Layer.F_xxx` markers.
4. **Manual C → Python conversion**
   - Open the toolbar `C to Python Reference` for the conversion cheat sheet.

## What is PowerSimBuilder?

PowerSimBuilder is a Windows desktop application that takes a state machine
designed in [StaTable](https://github.com/akashi-hideki/StaTable) and runs it
as a **Python simulation** together with surrounding physical components
(heaters, motors, power sources, sensors).

The complete pipeline:

```
StaTable (design)
   -> design.xml
   -> PowerSimBuilder (import + Canvas wiring)
   -> controller_box.py + demo_main.py
   -> Python runtime + simulation_signals.png
   -> (separately) StaTable C codegen -> firmware
```

StaTable alone answers **"does it compile?"**. PowerSimBuilder answers
**"does the behavior make sense?"** — before you flash the firmware.

---

## What's New in v1.1.0

- **Edit First flow**: `Edit Python Code` on a canvas component now opens the editor immediately with in-memory code, titled `(unsaved) *`. The `SaveFileDialog` fires only on the first `Save`, so you can inspect and cancel without committing a file path.
- **File Structure panel**: the editor's left pane now shows the opened file's class/method hierarchy (`📦 Class : Base (Lnn)` and `F_xxx ⚠️/✅ (Lnn)`). Double-click any class/method to jump to its line. A `Bundled Components` root preserves the `+Override` flow.
- **Role Function markers**: generated Python components now contain `# STABLE_USER_CODE_START` / `# STABLE_USER_CODE_END` markers inside `update()`. The `RoleFunctionParser` also falls back to top-level `def` methods when no markers are present, so legacy files still populate the Role Functions tree.
- **C → Python reference**: `docs/C_TO_PYTHON_REFERENCE.md` (toolbar: `C to Python Reference`) documents manual conversion patterns — type declarations, `g_xxx` → `self.globals`, `static` → `self._xxx`, unsupported constructs, and a complete worked example. Automated C import (tree-sitter-c) is deferred to a later release.
- **CI stability**: `pytest --basetemp=tmp/pytest` works around a Windows `pytest-of-<user>` junction cleanup bug that occasionally failed `pre-push`.

## What's New in v0.9.0

- **Simplified Chinese UI** (188 keys): auto-detected from OS UI culture, persistent via `%APPDATA%\PowerSimBuilder\settings.json`, switchable from the toolbar `Language` / `语言` menu
- **Custom Restart dialog**: `[Restart]` / `[取消]` buttons (MessageBox cannot rename its OK button); auto-restarts via `Application.Restart()`; `[Cancel]` leaves the setting untouched
- **CJK font fallback in matplotlib plots**: Microsoft YaHei / SimHei / SimSun / Noto Sans CJK SC added; mixed Japanese + Chinese labels render without tofu boxes
- **Save/Load round-trip tests**: 11 tests (CanvasModel <-> JSON)
- **Python-not-installed UX**: startup check + friendly error (v0.7.x)
- **Test suite**: 283 C# tests (283 pass / 1 skip) + 400 Python tests (99% coverage)
## What's New in v0.7.3

- **Canvas connection validation** (HIGH severity, v0.7.3): self-connection, invalid direction (out->out, in->in, in->out), and duplicate connections are now rejected
- **Selection consistency** (v0.7.2): empty-canvas click clears the current selection; selecting a new component unselects the previous one
- **Save/Load round-trip tests**: 11 new tests verifying `CanvasModel` <-> JSON serialization (components, positions, connections, parameters, selection state)
- **Python edge case tests**: 11 new tests covering error paths and boundary conditions; line coverage 98% -> 99%
- **Python-not-installed UX**: startup check with warning dialog + Create Simulation button disabled; friendly error with install link
- **Test suite**: 280 C# tests (279 pass / 1 skip) + 400 Python tests (99% coverage)

## What's New in v0.7.1

- **Canvas right-click fix**: context menu Delete now targets the clicked component even over a port; right-click no longer starts a wire drag; pending dashed line is cancelled on right-click / empty-canvas click
- **i18n Phase 1** (v0.7.0): English-only UI via `I18n.T("key")` + `Resources/i18n/en.json` (178 keys); `zh.json` slot reserved for future
- **.NET 10 LTS migration** (v0.6.0): from .NET Framework 4.7.2; SDK-style csproj; CI on `actions/setup-dotnet@v4`

## What's New in v0.6.0

### Build system

- **.NET Framework 4.7.2 -> .NET 10 LTS** (supported until 2028-11)
- SDK-style `csproj` for main + tests (`net10.0-windows`)
- CI updated to `dotnet` CLI via `actions/setup-dotnet@v4`

### Fixed

- **L1**: `ControllerBox` now receives explicit `input_globals` /
  `output_globals` (fixes `KeyError: heater_top_on` at runtime)
- **L2**: `generated_path` in saved project JSON is auto-repaired
  on load (fixes `AttributeError: has no build_runtime()`)
- **PythonEditorForm**: double-subscription of run/reload buttons
  removed (one click used to invoke `Run()` twice)
- **PythonExecutor**: `HasExited`-based double-launch guard, explicit
  `WorkingDirectory` + `PYTHONPATH`, `CreateNoWindow=true`, completion
  dialog via `Exited` with exit code and stderr tail
- **MainForm**: redundant "generated successfully" MessageBox removed

See [CHANGELOG.md](CHANGELOG.md) for the full history.

---

## Why PowerSimBuilder?

State machines that compile are not necessarily correct. If you use
StaTable to design a 7-layer controller, you want to know:

- Does the state transition fire when the sensor actually changes?
- Does the heater turn on, and does the temperature rise as expected?
- Does the safety shutdown trigger before overheat?

PowerSimBuilder closes that gap:

- **Same XML**: the StaTable design is the single source of truth.
- **Real Python**: the generated controller uses the same Python API
  (`python_runtime.statable`) as production side code.
- **Physical context**: heaters, filters, motors, and fault devices
  are first-class components on the Canvas.
- **Visual result**: `simulation_signals.png` shows every signal
  over time — no post-processing required.

---

## Key Features

- ✅ **StaTable XML import** with per-project port derivation (A+C+α)
- ✅ **Canvas wiring**: drag components, click ports, draw connections
- ✅ **Python controller generation** (`.py` from StaTable XML)
- ✅ **Simulation runtime**: StaTable 7-layer state machine + 12+ physical
  components (PowerSource, Engine, Generator, DiodeBridge, InductionMotor,
  HeaterBlock, FaultDevice, ...)
- ✅ **Signal plots**: automatic `plot_config` + `simulation_signals.png`
- ✅ **Auto-repair on load**: stale `generated_path` values are
  detected and reset to the Descriptor default
- ✅ **Demo driver** (`auto_events="demo_oven"`): drives the controller
  with synthetic events so a first simulation runs immediately
- ✅ **265 C# tests**, 4-matrix CI green
- ✅ **.NET 10 LTS**, Windows-only (WinForms)

---

## Quick Start

### Requirements

| Component | Version | Notes |
|---|---|---|
| Windows | 10 / 11 | WinForms target |
| .NET SDK | **10.0.x** | Free download from <https://dot.net/download> |
| Python | **3.10+** | **Must be on PATH** for simulation execution. Runtime package is pip-installable via `pip install -e .` |

### Install

```powershell
git clone https://github.com/akashi-hideki/PowerSimBuilder.git
cd PowerSimBuilder

# .NET 10 build
dotnet build -c Debug

# Python runtime (editable install)
python -m pip install -e .
```

### Run

```powershell
.\bin\Debug\net10.0-windows\PowerSimBuilder.exe
```

1. **File -> Open** a saved `.json` project (or `New Project`).
2. **StaTable import** (toolbar): pick a `design.xml` from StaTable.
3. Drag `ControllerBox` + components onto the Canvas, wire ports.
4. **Simulation create**: generates `demo_main.py`.
5. Click **Run** in the editor. `simulation_signals.png` appears
   in the same folder when the run completes.

---

## What It Generates

| File | Source | Purpose |
|---|---|---|
| `controller_box.py` | StaTable XML | Python state machine + role functions |
| `demo_main.py` | Canvas | Simulation driver (components, connections, plot) |
| `simulation_signals.png` | matplotlib | Signal plot after the run |

The `demo_main.py` explicitly sets:

```python
"cookingheatercontroller": ControllerBox(
    "cookingheatercontroller",
    input_globals=[...],   # from StaTable Descriptor
    output_globals=[...],  # from StaTable Descriptor
),
...
components["cookingheatercontroller"].parameters["generated_path"] =
    ".../controller_box.py"
components["cookingheatercontroller"].parameters["auto_events"] = "demo_oven"
```

---

## Architecture

```
┌────────────────────────────────────────────────────────────┐
│  PowerSimBuilder (C# / WinForms, .NET 10)                  │
│                                                            │
│  CanvasForm/          ComponentControl, DesignCanvas,      │
│                       MainForm, ParametersEditorForm       │
│  StaTableIntegration/ StaTableProjectReader,               │
│                       StaTableCodeGenerator,               │
│                       NCalcToPythonConverter,              │
│                       ControllerBoxDescriptorBuilder       │
│  SimulationGenerate/  PythonSimulationMainBuilder,         │
│                       PythonExecutor                       │
└──────────────────────┬─────────────────────────────────────┘
                       │ generates .py
                       ▼
┌────────────────────────────────────────────────────────────┐
│  python_runtime (Python 3.10+)                             │
│    python_runtime.statable:  7-layer state machine         │
│    python_runtime.components: 12+ physical components      │
└────────────────────────────────────────────────────────────┘
```

---

## Relation to StaTable

| Aspect | StaTable | PowerSimBuilder |
|---|---|---|
| Role | design -> **C codegen** | design -> **Python simulation** |
| Input | GUI / XML | StaTable XML + Canvas wiring |
| Output | MISRA-aware C for firmware | `demo_main.py` + `simulation_signals.png` |
| Runtime | C on target | Python + numpy + matplotlib |
| Language | Python + PySide6 | C# + .NET 10 (WinForms) |
| Platform | Win / Linux / macOS | **Windows only** (.NET 10 WinForms) |
| License | Apache 2.0 | Apache 2.0 |

Both tools are intentionally separate so that:

- StaTable remains dependency-free (PyPI + zero deps core).
- PowerSimBuilder stays Windows-native and fast (WinForms + .NET 10).
- The XML schema is the contract between them.

---

## Documentation

| Document | Description |
|---|---|
| [docs/QUICKSTART.md](docs/QUICKSTART.md) | First-project walkthrough |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Internal design (Canvas, generators) |
| [docs/PUBLICATION_AUDIT.md](docs/PUBLICATION_AUDIT.md) | Release readiness + roadmap |
| [CHANGELOG.md](CHANGELOG.md) | Version history |
| [HANDOFF_STATABLE.md](HANDOFF_STATABLE.md) | Session-by-session development log |

---

## Platform Support

| Platform | Status | Notes |
|---|---|---|
| **Windows 10 / 11** | ✅ **Tested** | .NET 10 WinForms |
| Linux | ❌ Not supported | WinForms is Windows-only |
| macOS | ❌ Not supported | Same |

---

## Development

### Build

```powershell
dotnet restore
dotnet build -c Debug
```

### Test

```powershell
dotnet test PowerSimBuilder.Tests\PowerSimBuilder.Tests.csproj --no-build
```

Expected: **265 test cases pass** (`[Theory]+[InlineData]` expanded).

### CI

`.github/workflows/ci.yml` — Build & Test (.NET 10) + Code Quality,
on `windows-latest`. `.github/workflows/python-tests.yml` — Python
runtime tests on `ubuntu-latest` (4 Python versions).

---

## License

[Apache License 2.0](LICENSE) — free for commercial and personal use.

---

## Contributing

Issues and pull requests are welcome. For larger changes, please open
an issue first to discuss the approach.

- Bug reports: use the `.github/ISSUE_TEMPLATE/bug_report.md` template
- Feature requests: use `feature_request.md`
- Pull requests: use `.github/PULL_REQUEST_TEMPLATE.md` (C#/Python test checklist)

---

## Contact

- GitHub Issues: <https://github.com/akashi-hideki/PowerSimBuilder/issues>
- Email: akashi.hideki@gmail.com

---

## Acknowledgments

- [StaTable](https://github.com/akashi-hideki/StaTable) — state machine design tool
- [.NET 10](https://dotnet.microsoft.com/) / WinForms — Microsoft
- [NumPy](https://numpy.org/) — BSD
- [Matplotlib](https://matplotlib.org/) — PSF
- [NCalc](https://github.com/sklose/NCalc2) — MIT

---

*Companion to StaTable. Apache 2.0. Windows-only.*
