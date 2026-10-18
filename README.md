# PowerSimBuilder

[![CI](https://github.com/akashi-hideki/PowerSimBuilder/actions/workflows/ci.yml/badge.svg)](https://github.com/akashi-hideki/PowerSimBuilder/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10%20LTS-512BD4)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/Tests-227%20pass-green.svg)]()
[![Platform](https://img.shields.io/badge/Platform-Windows%20only-lightgrey.svg)]()

**Visual simulation companion to [StaTable](https://github.com/akashi-hideki/StaTable) — load StaTable XML, build Python state-machine controllers, run end-to-end simulations with signal plots.**

---

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
- ✅ **227 C# tests**, 4-matrix CI green
- ✅ **.NET 10 LTS**, Windows-only (WinForms)

---

## Quick Start

### Requirements

| Component | Version | Notes |
|---|---|---|
| Windows | 10 / 11 | WinForms target |
| .NET SDK | **10.0.x** | Free download from <https://dot.net/download> |
| Python | **3.10+** | `python_runtime` is pip-installable via `pip install -e .` |

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

Expected: **227 test cases pass** (`[Theory]+[InlineData]` expanded).

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
