# PowerSimBuilder

[![CI](https://github.com/akashi-hideki/PowerSimBuilder/actions/workflows/ci.yml/badge.svg)](https://github.com/akashi-hideki/PowerSimBuilder/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10%20LTS-512BD4)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/Tests-599%20C%23%20%2B%2016%20E2E%20%2B%20457%20Py%20pass-green.svg)]()
[![Platform](https://img.shields.io/badge/Platform-Windows%20only-lightgrey.svg)]()

**Visual simulation companion to [StaTable](https://github.com/akashi-hideki/StaTable) — load StaTable XML, build Python state-machine controllers, run end-to-end simulations with signal plots.**

## Try it (5 minutes)

### Standalone Python (fastest, no .NET needed)
python examples\demos\demo_pid_first_order_lag.py

text

### Full setup (Windows + .NET 10 + Python 3.10+)
git clone https://github.com/akashi-hideki/PowerSimBuilder.git
cd PowerSimBuilder
dotnet build -c Debug
python -m pip install -e .
.\bin\Debug\net10.0-windows\PowerSimBuilder.exe

text

File -> Open -> `examples\demos\oven_control_statable.psbproj` -> Create Simulation -> Run.

### Verify everything
.\tools\verify_all.ps1

text

Expected: all sections exit=0 OK (599 C# + 16 E2E + 29 RoleSnippet + 460 Python).

## Demos

Five ready-to-run demos in `examples/demos/` (see [README](examples/demos/README.md)):

| Demo | Type | Quantitative result |
|---|---|---|
| `demo_pid_first_order_lag.py` | standalone Python | `y = 0.9738` (target 1.0) |
| `demo_statable_runtime_loop.py` | standalone Python (StaTable 7-layer) | `T = 200.00 degC` |
| `pid_closed_loop.psbproj` | GUI project | ss=0.991, settle 3.9s |
| `oven_control.psbproj` | GUI project (PID + OvenChamber) | ss=200.68, ovs=1.45%, settle 64.7s |
| `oven_control_statable.psbproj` | GUI project (StaTable gating + PID + OvenChamber) | ss=200.68, ovs=1.45%, settle 64.6s |

### StaTable gating demo (`oven_control_statable.psbproj`)

The 7-layer StaTable state machine acts as a **safety interlock**: PID output
reaches the plant only while `MwOven` is in `PidHeating`. Open the project in the GUI,
click **Create Simulation**, then **Run**. Verify quantitatively:

```powershell
python tools\verify_statable_psbproj.py examples\demos\oven_control_statable.psbproj
python tools\verify_controller_box.py examples\demos\oven_control_statable.psbproj
```

---

## Editing workflow

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

4.5. **Physics Management (left panel)**
   - The left panel shows a list of physics models currently inserted
     into the open Python file (detected from `# === physics: ... ===` markers).
   - `[Physics]` button opens the canonical `PhysicsModelPickerForm` dialog
     (same as the canvas right-click "Add Physics" flow).
   - Select a model in the list and click **Remove Selected Model** to
     delete the corresponding block from the source code.
   - The list is refreshed automatically on every text change.
5. **Role Snippets**
   - Right-click inside any `F_xxx` function -> **Register as Snippet...**
   - Toolbar **Register Snippet** works the same way (visible only when the file contains `STABLE_USER_CODE_START`)
   - **Insert Snippet** (existing) offers `(snippet's own)` and cross-function modes
6. **Physics models (MVP-1.2)**
   - Double-click any non-`ControllerBox` component -> click **Add Physics** in the left panel.
   - Pick a model from `user_physics_models/`; snippets are inserted into `__init__` and `update()`.
   - See [Physics Model Integration](#physics-model-integration-mvp-12) below.

## Physics Model Integration (MVP-1.2)

Physics models bring **continuous-time plant dynamics** into the Canvas.
Any component other than a StaTable-imported `ControllerBox` can be
augmented with an FMU (Functional Mock-up Unit) that simulates a motor,
filter, heater, or other physical plant.

### Quick Start

1. Place a component on the Canvas (e.g. `FirstOrderLag`).
2. Double-click it to open the Python editor.
3. In the left panel click **Add Physics**.
4. Pick a model from the picker (the right side shows typed I/O, parameters, timing).
5. Click **Insert**.

The editor now contains two new blocks:

``````python
# === physics: PMSM (auto-inserted) ===
# model     : PMSM
# fmu_file  : model.fmu
# source    : user_physics_models/PMSM/source/model.fmu
# fmi       : 2.0 (CoSimulation)
# inputs    : u_d:Real, u_q:Real
# outputs   : i_d:Real, i_q:Real, torque:Real
# parameters: R=0.5, Ld=0.001
# timing    : start=0, stop=1, step=0.001
self.pmsm = load_fmu(_psb_resolve_fmu("PMSM/source/model.fmu"))
self.pmsm.enter_initialization_mode()
self.pmsm.set("R", 0.5)
self.pmsm.set("Ld", 0.001)
self.pmsm.exit_initialization_mode()
self._pmsm_time = 0.0
# === /physics: PMSM ===

# === physics: PMSM (auto-inserted in update) ===
self.pmsm.set("u_d", float(self.inputs.get("u_d", 0.0)))
self.pmsm.set("u_q", float(self.inputs.get("u_q", 0.0)))
# timing check: FMU stepSize=0.001
if abs(dt - 0.001) > 1e-9:
    import warnings
    warnings.warn(...)
self.pmsm.doStep(self._pmsm_time, dt)
self._pmsm_time += dt
self.outputs["i_d"] = float(self.pmsm.get("i_d"))
self.outputs["i_q"] = float(self.pmsm.get("i_q"))
self.outputs["torque"] = float(self.pmsm.get("torque"))
# === /physics: PMSM ===
``````

### Port Mapping (`model.yaml`)

By default, FMU port names are used as component port names. To rename,
add an `interface` block to `model.yaml`:

``````yaml
interface:
  inputs:
    u_d: u_d_input     # fmu_port: component_port
    u_q: u_q_input
  outputs:
    i_d: i_d_output
``````

The generated `set()` / `get()` calls use the component-side names.

### Robust Path Resolution

Inserted code calls `_psb_resolve_fmu("<relative_path>")` instead of a
literal path. The helper (inserted once per file) walks up from the
Python file:

``````python
def _psb_resolve_fmu(relative_path: str) -> str:
    from pathlib import Path
    here = Path(__file__).resolve()
    for base in [here.parent, *here.parents]:
        candidate = base / "user_physics_models" / relative_path
        if candidate.is_file():
            return str(candidate)
    raise FileNotFoundError(...)
``````

The same generated code works regardless of install path or working
directory.

### Re-insertion Guard

Each block is bracketed by markers:

``````
# === physics: <name> (auto-inserted) ===              (in __init__)
# === physics: <name> (auto-inserted in update) ===    (in update)
# === /physics: <name> ===
``````

Attempting to insert the same model twice is detected and skipped.
Removing a model is a matter of deleting everything between the markers.

### Exclusions

- **ControllerBox** (StaTable-imported) is excluded because its
  `__init__` has a different shape and role. Attempts to add physics
  to it show a warning.
- **ModelExchange** FMUs are currently rejected; only CoSimulation is
  supported (MVP-2 candidate).
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

## What's New in v1.10.0

- **`.psblog` Import**: read UART monitor logs (`.psblog`, `psblog/v1`) written by StaTable.
  - **File → Import Signal Log...** (or toolbar) opens `PsblogImportForm`.
  - Variable list + waveform classification (`constant` / `ramp` / `step` / `exponential` / `pwm` / `digital`).
  - `role=` hints (SPEC §6.2) override the classifier with `confidence = 1.0`.
  - Selected variables become `RecordedSignal` components on the Canvas.
  - **Variable sets are per-log** (SPEC §1): no fixed schema, PSB trusts the VarTable verbatim.
- **Python API** (`python_runtime.psblog`): `parse_psblog`, `parse_psblog_glob`, `to_json`
  with `python -m python_runtime.psblog <file> --json-out <out> [--classify]` CLI.
- **`signal_classifier`** (`python_runtime.analysis.signal_classifier`): `classify` / `classify_all`.
- **`RecordedSignal`** component registered in the palette (Category: Source).
- **Sample logs**: `examples/logs/` (4 shapes + rotation concat).
- **Testing**: `verify_all.ps1` sections `[3.6]` (29 psblog tests), `[3.7]` (5 sample sanity), `[4e]` (2 FlaUI E2E).

## What's New in v1.9.0

- **Role Snippets (Register)**: extract and reuse role function implementations.
  - **Right-click `Register as Snippet...`** or toolbar **Register Snippet** to snapshot the `F_xxx` function at the caret.
  - `STABLE_USER_CODE_START` marker presence gates the entry points — works for any file with role functions, not just `ControllerBox`.
  - **Insert Snippet** dialog (3 columns: Target / Snippets / Code) with `(snippet's own)` and cross-function modes.
  - i18n 12 new keys (en / zh, 374 total).
- **FlaUI E2E for Register Snippet** (`RoleSnippetRegisterE2ETests`): verifies register dialog -> file creation -> `RegisterSnippetSuccess` log.
- **`verify_all.ps1` `[4c]` / `[4d]`**: 29 RoleSnippet unit tests + 1 FlaUI E2E, one-command regression.
- **Tooling hardening**: `gcommit.ps1` auto-stops PowerSimBuilder before git ops (Trap #80); `verify_all.ps1` retries flaky E2E once (Trap #85).
- **Fixed**: `SetPythonCode` / `LoadFile` now auto-refresh Register Snippet visibility; `menuItemRoleSnippets` follows marker presence; Win32 `EnumWindows` fallback for `ShowInTaskbar=false` modals (Trap #86).

## What's New in v1.8.0

- **GUI FMU E2E**: `AddPhysicsAndRun_Dahlquist_Completes` verifies the full path Add Physics -> Dahlquist -> Run in the WinForms UI.
- **Python runtime spec** (`docs/SPEC_PYTHON_RUNTIME.en.md` / `.zh.md`): install with a single `requirements.txt`.
- **`tools/scan_imports.py`**: AST-based dependency detection (`--check` / `--exclude`).
- **`pyyaml` added** to `requirements.txt` (used by `src/python_runtime/physics/manifest.py`); `lxml` removed (stdlib `xml.etree` suffices).
- **Fixed**: Run status now shows completion (previously stuck at "Running...").
- **Fixed**: Running a new (unsaved) component no longer throws on `Path.GetDirectoryName(null)`.

## What's New in v1.6.1

- **Cleanup release**: 9 dead files removed from `parts/` (~2,000 LOC) after a reference-counting audit.
- **NuGet slimmed**: 5 -> 1 package (NCalc only). `csvhelper`, `Pythonnet`, `Costura.Fody`, and `Fody` removed.
- **IP documentation**: `docs/ip/` with FAQ, OSS compliance, white paper, license inventory, git history, and lawyer candidates.
- **`THIRD-PARTY-NOTICES.md`**: generated by ThirdLicense for binary distribution compliance.
- **HANDOFF traps #38-#43**: PowerShell redirection, for-each-ref, annotated tag peeling, gitignore ordering, lawyer-doc accuracy, same-name class disambiguation.

## What's New in v1.11.0

- **GitHub FMU catalog browsing**: browse FMI Cross Check-style GitHub repository layouts through the GitHub API.
- **Catalog filters**: select FMI version, interface directory (`cs` for Co-Simulation or `me` for Model Exchange), and platform.
- **Optional GitHub API token**: authenticates repository-tree requests for `github:` sources. The value is saved to application settings when **Load** is run with a `github:` source (entering it alone does not save it). It is not used for GitHub Releases and is not encrypted.
- **Catalog → URL → File workflow**: select an FMU, approve network access, download and parse it, then select input/output variables as component ports.
- **Network access**: every URL download asks for confirmation. **Always allow** saves the URL to settings, but the confirmation is still shown on the next download.
- **GitHub Releases source**: only `.fmu` assets of the latest release are listed; ZIP assets are not extracted.
- **Import restriction**: the current importer accepts Co-Simulation FMUs only. Model Exchange entries can be browsed but cannot be imported.

See [FMU Catalog and Import Guide](docs/FMU_CATALOG_GUIDE.md) and [Physics Model Integration](docs/PHYSICS_MODEL_INTEGRATION.md).

## Earlier Changes: v1.6.0

- **Catalog tab**: fetch models from real FMU catalogs (FMI Cross Check: 226 models) with one click.
- **Sources dropdown**: switch between built-in catalog sources (Custom URL / Reference-FMUs / ghproxy / FMI Cross Check release asset).
- **NetworkAccessDialog**: explicit consent before any URL fetch; allowed URLs persisted to settings.
- **FMI 2.0 / win64 priority**: generated catalogs sort to prefer modern platforms.
- **`release.py --with-catalog`**: attach `generated_catalog.json` to GitHub Releases automatically.

## What's New in v1.2.0

- **Physics Model Integration (FMU)**: a canvas component can now embed an FMU physics model. Double-click a component -> **Add Physics** in the left panel -> pick a model -> snippets are auto-inserted into `__init__` and `update()`.
- **Type-aware I/O**: generated `set()` / `get()` calls cast each variable to its declared FMU type (`Real` -> `float`, `Integer` -> `int`, `Boolean` -> `bool`, `String` -> `str`). Unsupported types (e.g. `Enumeration`) become `# TODO` comments.
- **Port mapping via `model.yaml`**: the `interface.inputs` / `interface.outputs` block can rename FMU ports to component port names (`fmu_port: component_port`). Unmapped ports default to identical names.
- **Timing mismatch warning**: when the FMU declares a `stepSize`, the generated `update()` block emits an inline `warnings.warn(...)` guard if `dt` differs from the FMU step size.
- **FMU info viewer**: the model picker has a detail panel (model / FMI version / typed ports / parameters / timing). The editor status bar shows the model name and line range while the cursor is inside a physics block.
- **Robust path resolution**: inserted code uses `_psb_resolve_fmu()`, which walks up from the Python file so the same code works regardless of install location or working directory.
- **Non-destructive insertion**: each block is wrapped in `# === physics: <name> (auto-inserted) ===` / `# === /physics: <name> ===` markers. Re-insertion of the same model is detected and skipped.
- **ControllerBox guard**: StaTable-imported `ControllerBox` components are excluded from physics insertion.
- **Highlighting**: after insertion, both the `__init__` and `update()` blocks are tinted in the editor.
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
- ✅ **Role Snippets**: register / insert reusable `F_xxx` implementations
  (marker-gated, cross-function insert, FlaUI E2E)
- ✅ **Simulation runtime**: StaTable 7-layer state machine + 12+ physical
  components (PowerSource, Engine, Generator, DiodeBridge, InductionMotor,
  HeaterBlock, FaultDevice, ...)
- ✅ **Signal plots**: automatic `plot_config` + `simulation_signals.png`
- ✅ **Auto-repair on load**: stale `generated_path` values are
  detected and reset to the Descriptor default
- ✅ **Demo driver** (`auto_events="demo_oven"`): drives the controller
  with synthetic events so a first simulation runs immediately
- **FMU physics integration** (v1.2.0): embed a CoSimulation FMU into any non-ControllerBox component; typed I/O, yaml port mapping, timing warnings, and FMU info viewer
- ✅ **599 C# tests + 457 Python tests**, 4-matrix CI green
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
| [docs/ip/FAQ.md](docs/ip/FAQ.md) | Product FAQ (companion to StaTable, Windows-only) |
| [docs/ip/OSS_COMPLIANCE.md](docs/ip/OSS_COMPLIANCE.md) | OSS license compliance (Apache-2.0; no GPL/AGPL) |
| [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) | Redistributed NuGet package licenses |
| [docs/SPEC_PYTHON_RUNTIME.en.md](docs/SPEC_PYTHON_RUNTIME.en.md) | Python runtime setup (English) |
| [docs/SPEC_PYTHON_RUNTIME.zh.md](docs/SPEC_PYTHON_RUNTIME.zh.md) | Python runtime setup (中文) |
| [docs/SPEC_ROLE_SNIPPETS.md](docs/SPEC_ROLE_SNIPPETS.md) | Role Snippets SPEC (Register / Insert) |
| [docs/TUTORIAL_STATABLE_DEMO.ja.md](docs/TUTORIAL_STATABLE_DEMO.ja.md) | StaTable XML → PSB demo tutorial (日本語) |
| [docs/TUTORIAL_STATABLE_DEMO.en.md](docs/TUTORIAL_STATABLE_DEMO.en.md) | StaTable XML → PSB demo tutorial (English) |
| [docs/TUTORIAL_STATABLE_DEMO.zh.md](docs/TUTORIAL_STATABLE_DEMO.zh.md) | StaTable XML → PSB demo tutorial (中文) |

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

Expected: **599 test cases pass** (2 skipped; `[Theory]+[InlineData]` expanded).

### CI

`.github/workflows/ci.yml` — Build & Test (.NET 10) + Code Quality,
on `windows-latest`. `.github/workflows/python-tests.yml` — Python
runtime tests on `ubuntu-latest` (4 Python versions).

---

---

## Development Notes (Handoff)

- **Current state**: v1.9.0 released (Role Snippets B complete, tag `v1.9.0`); MVP-1.2 physics integration still on `main`.
- **Physics tests**: 66 (all green; see `PowerSimBuilder.Tests/Physics*` and `FmuInfoReaderTests` / `ModelYamlTests`).
- **Known limitations**
  - Only CoSimulation FMUs are supported; ModelExchange is rejected.
  - `Enumeration` typed ports are left as `# TODO` comments.
  - Port mapping is read from `model.yaml`; there is no in-app editor yet.
  - There is no UI for removing an inserted physics block (delete the marker-delimited range manually).
  - Multiple physics models run in insertion order; no dependency-based ordering yet.
- **Next candidates**
  - E2E automation for the "generate -> insert -> run" path. (B-5 FlaUI E2E for Register Snippet is already done; Physics E2E remains.)
  - Picker search / filter for large model catalogs.
  - Physics block removal / update UI.
  - Restore `_physicsCache` when opening an existing file (status bar detail).
  - MVP-2: `.whl` / `.zip` package import, non-Real FMU support (`.mo` -> MVP-3).
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
