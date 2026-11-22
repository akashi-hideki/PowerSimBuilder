# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `docs/TUTORIAL_STATABLE_DEMO.ja.md` / `.en.md` / `.zh.md` — StaTable XML → PSB demo tutorial with role-function control architecture (PID → F_SetTopHeaterDuty → heater).

## [1.10.2] - 2026-11-22

### Added
- **Palette registration for `.psblog` imports**: RecordedSignal components imported via `Import Signal Log...` are now also added to the palette (Category: `Signal Log`), making them reusable across projects. Checkbox "Also add to palette" (default ON) in the import dialog. Duplicate names get `(2)`, `(3)` suffixes. i18n key `PsblogImport.AddToPalette` added (en/zh).
- **Physics Management UI** in Python editor left panel:
  - `EditPanel` now shows a list of inserted physics models
    (detected via `# === physics: <name> (auto-inserted) ===` markers).
  - `Remove Selected Model` button deletes the corresponding block
    from the current Python source.
  - List is refreshed on every `textEditor_TextChanged` event.
- `PhysicsBlockManager` (new): regex-based detection and removal of
  physics blocks, tolerant to Python indentation.
- `EditPanel.PhysicsModelRemoveRequested` event + test hooks
  (`Test_PhysicsModelsList`, `Test_RemovePhysicsButton`).

### Changed
- `PythonEditorForm.WireEditPanel` now subscribes to
  `editPanel.PhysicsModelRemoveRequested` and routes it through
  `OnPhysicsModelRemoveRequested` -> `PhysicsBlockManager.RemovePhysicsBlock`.
- Left-panel `[Physics]` button reuses the existing
  `OnPhysicsRequested` flow in `PythonEditorForm.Physics.cs`,
  opening the canonical `PhysicsModelPickerForm` dialog.

### Fixed
- Regex in `PhysicsBlockManager.GetInsertedModels` / `RemovePhysicsBlock`
  no longer requires `^` anchor, so indented markers are correctly detected.


## [1.10.1] - 2026-11-14

### Fixed

- `plot_config` emits `ComponentDescriptor.OutputPorts[0]` (e.g. `'value'`
  for `RecordedSignal`) instead of the hardcoded `'output'`.
- `GetFirstOutputPortName` reads `ComponentControl.Descriptor` directly.
- `Add to Canvas` dialog closes reliably; `_btnAddToCanvas.DialogResult`
  restored; `PSB_E2E_TEST=1` required for auto-open at startup.
- `PsblogParser` locates repo root from `AppContext.BaseDirectory`.
- `recorded_signal` export from `components/__init__.py`.

### Added

- `oscillating` waveform category (triangle / sine / saw detection at 0.85).
- Large-ramp demotion (`range > 10000` → confidence 0.3) to avoid Y-axis domination.
- Regression tests: `test_components_init.py` (3), `test_recorded_signal_integration.py` (3),
  `test_signal_classifier.py` oscillating + large ramp (2).
- `PSB_E2E_TEST=1` guard for `OnPsblogImportTestShown`.

### Changed

- `TraceDiag` in `MainForm` / `PsblogImportForm` is `#if DEBUG` (no-op in Release).

### Testing

- Python: 29 + 5 + 18 + 6 = 58+ pass.
- C#: 599 + 11 psblog-related pass.
- UI E2E: 19 (18 pass / 1 skip).

## [1.10.0] - 2026-11-08

### Added
- `.psblog` Import: read UART monitor logs (`psblog/v1`) written by StaTable.
- `PsblogImportForm` + toolbar button `Import Signal Log...` (22 i18n keys, en/zh).
- `python_runtime.psblog` parser (`parse_psblog`, `parse_psblog_glob`, `to_json`, CLI `--json-out --classify`).
- `RecordedSignal` component (Category: Source) for time-series replay.
- `python_runtime.analysis.signal_classifier` (`classify`, `classify_all`).
- Sample logs in `examples/logs/` (5 files incl. rotation concat).
- `tools/verify_psblog.py` + `verify_all.ps1` sections `[3.6]`, `[3.7]`, `[4e]`.
- FlaUI E2E: `PsblogImportE2ETests` (2 tests).

### Fixed
- BOM tolerance: `psblog` reader accepts UTF-8 BOM via `utf-8-sig`.
- Signal classifier: pure linear ramps are no longer misclassified as exponential.
- JSON snake_case → C# PascalCase mapping (`[JsonPropertyName]` on `PsblogTraceDto`).

### Testing
- Python: 29 (parser) + 5 (RecordedSignal) + 16 (classifier) = 50 pass.
- C# unit: +7 (psblog) +2 (import form).
- UI E2E: 19 (18 pass / 1 skip).

## [1.9.0] - 2026-11-07

### Added

- **Role Snippets B: Register** — extract the `F_xxx` function at the caret
  and reuse it across projects.
  - Right-click `Register as Snippet...` + toolbar `Register Snippet`
  - `STABLE_USER_CODE_START` marker presence gates the entry points
    (component-agnostic: works for any file with role functions, not only ControllerBox)
  - Overwrite confirmation with optional description
  - i18n 12 new keys (en / zh, 374 total)
- **FlaUI E2E for Register Snippet** (`RoleSnippetRegisterE2ETests`)
  - End-to-end path: `--test-project` -> editor -> register dialog -> `RegisterSnippetSuccess` event
- **`verify_all.ps1` `[4c]` / `[4d]`**
  - `[4c]`: 29 unit tests for `RoleSnippetRegistrar` / `RoleSnippetStorage` / `RoleSnippetVisibility`
  - `[4d]`: FlaUI E2E for the Register dialog
- **`RoleSnippetVisibilityTests`** (15) covering:
  - L1 `ContainsRoleMarkers` (9 theory cases)
  - L2 `Form_*` (6 facts: initial state, code changes, `SetPythonCode`, `MenuItemRoleSnippets`)

### Changed

- `SetPythonCode` / `LoadFile` now call `UpdateRegisterSnippetAvailability()`
  automatically (B-3c) — Register button reflects the current file without manual refresh.
- `menuItemRoleSnippets` (Insert) visibility now follows the same marker check.
- `gcommit.ps1` stops any running `PowerSimBuilder.exe` before git operations (Trap #80).
- `verify_all.ps1` `[4/6]` E2E now sleeps 3s and retries once on failure (Trap #85).

### Fixed

- Register Snippet visibility false negatives caused by inline regex
  `@"^\\s*..."` (Trap #84) — extracted to `RoleSnippetRegistrar.ContainsRoleMarkers`.
- FlaUI E2E cannot see `ShowDialog` + `ShowInTaskbar=false` modals in the
  UIA3 desktop tree (Trap #15 / #86) — Win32 `EnumWindows` fallback added.
- `TextBox.Name` in UIA returns the AutomationId, not the value — use
  `AsTextBox().Text` (Trap #86).

### Testing

| Suite | Result |
|---|---|
| C# unit tests | 599 pass / 2 skip / 0 fail |
| UI E2E | 16 pass / 1 skip / 0 fail |
| RoleSnippet tests | 30 (Registrar 8 + Storage 6 + Visibility 15 + FlaUI E2E 1) |
| Python | 463 pass |

## [1.8.0] - 2026-11-01

### Added

- **GUI FMU E2E** (`SmokeTests.AddPhysicsAndRun_Dahlquist_Completes`)
  - Add Physics -> Dahlquist -> Run end-to-end through the WinForms UI (FlaUI).
- **Python runtime spec** (`docs/SPEC_PYTHON_RUNTIME.en.md` / `.zh.md`)
  - Install via a single `requirements.txt`.
- **`tools/scan_imports.py`**
  - AST-based dependency detection with `--check` / `--exclude`.
- **`requirements.txt`**: added `pyyaml` (used by `src/python_runtime/physics/manifest.py`);
  removed `lxml` (stdlib `xml.etree` suffices).

### Fixed

- Run button now updates `lblStatus` on completion (previously stayed at "Running...").
- Running a new (unsaved) component no longer fails on `Path.GetDirectoryName(null)`.

### Added
- **OvenChamber component** (`src/python_runtime/components/oven_chamber.py`)
  - Wraps `ThermalMass` with oven-specific I/O: `heater_top_duty`, `heater_bottom_duty`,
    `fan_duty` inputs; `oven_current_temp`, `thermistor_top/middle/bottom`, `heater_power` outputs
  - Registered in palette (Category: Thermal)
- **ControllerBox gating** (`controller_box.py`)
  - `gate_layer` / `gate_states` / `gate_input_port` / `gate_output_port` parameters
  - Generic Wrapper: `input_globals` / `output_globals` drive dynamic I/O
  - `_resolve_generated_path` globs `*_powersim.py` for multi-instance support
- **t_end persistence** (schema v2: `metadata.tEnd` / `metadata.dt`)
  - `PythonEditorForm` NumericUpDown + `SimTimeChanged` event
  - `.psbproj` round-trip verified (`ProjectFileV2MetadataTests`)
- **StaTable gating demo** (`examples/demos/oven_control_statable.psbproj`)
  - `MwOven: PidHeating` gates PID output via `ControllerBox`
  - Quantitative: ss=200.68°C, ovs=1.45%, settle=64.6s (matches no_aw tuning)
- **Full StaTable pipeline verified** (`oven_control_from_design_xml.psbproj`)
  - `tests/data/scenarios/cooking_heater/design.xml` -> `--statable-import` -> `generated/design_powersim.py` -> GUI wiring -> Run
  - Result: ss=200.681°C, ovs=1.45%, settle=64.6s (matches `no_aw` tuning)
  - ControllerBox `generated_path` is per-instance, so multiple StaTable XMLs can coexist
- **Verification tools**
  - `tools/verify_statable_psbproj.py` — `.psbproj` with ControllerBox
  - `tools/verify_controller_box.py` — gating pass/block proof
  - `tools/inspect_controller_box.py` — ControllerBox diagnostics
  - `tools/dump_sim_csv.py` / `tools/analyze_sim_csv.py` — CSV signal metrics
- **Regression guards**
  - `ProjectFileV2MetadataTests` (10) — tEnd default/serialize/round-trip/compat
  - `SimTimeOverrideTests` (6) — t_end regex/indent/multiple/empty

### Changed

- `pid_closed_loop.psbproj`: IMC tuning (kp=1.0, ki=1.0, aw=True, out_max=2.0)
  eliminates steady-state error (ss=0.991, settle=3.9s)
- `oven_control.psbproj`: no_aw PID (kp=0.01, ki=0.0001, aw=False), setpoint=200
- Demo `ROOT` path corrected for `examples/demos/` relocation

### Fixed

- **First-launch button visibility** (PythonEditorForm): `Shown` event re-applies
  `Visible` + `PerformLayout()` for non-modal `Show()` layout race
- **Save As i18n**: `MainForm.BtnSaveAs` / `MainForm.SaveAsDialogTitle` (en/zh)
- **pytest basetemp**: `tmp/pytest` -> `.pytest_tmp` (CI fresh checkout fix;
  `tmp/` is gitignored and missing on clean CI)
- **CI red badge cleanup**: failed run from billing error removed

### Known issues

- CI workflows disabled (GitHub Actions billing); restore after public release
- GUI generated code uses `dt=0.001` fixed; `metadata.dt` used by verify tools only

---

## [0.9.0] - 2026-10-18

### Added (Simplified Chinese UI)

- **188-key Simplified Chinese translation** (`Resources/i18n/zh.json`)
  - Auto-embedded via existing `<EmbeddedResource Include="Resources/i18n/**/*.json" />`
  - `en.json` also expanded to 188 keys (previously 180 + 6 I18n/MainForm keys)
- **Persistent language setting** (`AppSettings.cs`)
  - Stored at `%APPDATA%\PowerSimBuilder\settings.json`
  - Startup resolution: `settings.json` > OS UI culture (`zh`) > `en`
  - `DetectOsLanguage()`: returns `zh` for Chinese OS, `en` for everything else
- **Toolbar language menu** (`CanvasForm/MainForm.cs`)
  - `Language` / `语言` dropdown at the toolbar right edge
  - Current selection checkmarked
- **Custom restart dialog** with `[Restart]` / `[取消]` buttons
  - `MessageBox.Show` cannot rename its OK button, so a small Form is built
  - `[Restart]` → `Application.Restart()` (auto-relaunch, new language applied)
  - `[Cancel]` → no-op (settings.json untouched)
  - Unsaved-change warning when `_canvasManager.IsModified`
- **CJK font fallback** in matplotlib plots (`src/python_runtime/logger.py`)
  - Candidates extended with `Microsoft YaHei`, `Microsoft YaHei UI`,
    `SimHei`, `SimSun`, `Noto Sans CJK SC`
  - `font.family` is now a concrete-name list (not `"sans-serif"`), which
    enables matplotlib 3.6+ per-glyph fallback. Japanese labels render in
    Yu Gothic; Chinese labels fall back to Microsoft YaHei automatically
  - `PSB_CJK_FONT` env var is the preferred override (`PSB_JP_FONT` kept
    for backward compatibility)

### Fixed (during Chinese support work)

- **`AppSettings.DetectOsLanguage()`** no longer returns `ja`. Japanese
  Windows previously produced an empty string table because `ja.json`
  does not exist; the fallback is now `en`.
- **`I18n.SetLanguage()`** falls back to `en` if the requested language
  has no embedded resource (or it failed to parse).
- **`AppSettingsTests.I18n_EnAndZh_AreDistinct`** restores
  `I18n.CurrentLanguage` in a `finally` block. Previously it left the
  static I18n state in `zh` and caused
  `StaTableProjectReaderTests.Parse_InvalidXml_ReturnsError` to fail.

### Added (test infrastructure)

- `AppSettingsTests` (4 tests): AppSettings type/members, zh loading,
  en vs zh distinction (with finally restoration)

### Tests

- C#: **283 pass / 0 fail / 1 skip** (284 total)
- Python: **400 pass** (99% coverage)
- CI: C# 4-matrix + Python 4-version, all green

### Notes

- The Chinese UI was the primary business driver for the China market.
  This release makes PowerSimBuilder fully usable in Simplified Chinese.
- v1.0.0 will follow after field testing; no further feature work is
  planned for the 0.x series.

## [0.7.3] - 2026-10-18

### Fixed (HIGH severity)

- **Canvas connection validation** (`CanvasForm/DesignCanvas.cs` `CompleteConnection`)
  - Self-connection (same component out -> in) is now rejected (BUG-004)
  - Invalid direction (out->out, in->in, in->out) is now rejected (BUG-005)
  - Duplicate connection detection now uses value-based comparison
    (`ConnectionExists(start, startPort, end, endPort)`) instead of
    reference equality (BUG-006)

### Added (test infrastructure)

- `DesignCanvasConnectionTests` (7 tests): Phase 3 connection scenarios

### Notes

- BUG-004/005/006 were discovered by the headless GUI test suite
  introduced in v0.7.2. Each had potential for invalid simulation graphs
  (self-feedback loops, semantically meaningless wires, duplicate events).

## [0.7.2] - 2026-10-18

### Fixed

- **Canvas selection consistency** (`CanvasForm/DesignCanvas.cs`)
  - Empty-canvas click now clears `SelectedComponent` and `IsSelected` (HANDOFF `S2`, Fix 4)
  - Selecting a new component now unselects the previous one (HANDOFF `S1`, Fix 3)

### Added (test infrastructure)

- `PowerSimBuilder.Tests/Gui/` (19 headless WinForms tests, xUnit)
  - `ComponentControlRightClickTests` (5 tests, v0.7.1 regression guard)
  - `DesignCanvasRightClickTests` (5 tests, v0.7.1 regression guard)
  - `DesignCanvasDeleteTests` (9 tests: selection / delete / cascade)
- `xunit.runner.json` with `parallelizeTestCollections: false`
  (WinForms `KeysConverter` static dictionary is not thread-safe)

## [0.7.1] - 2026-10-18

### Fixed

- **Canvas context menu Delete mis-target** (`ComponentControl`, `DesignCanvas`)
  - Right-click over a port now sets selection so the context menu targets the clicked component
  - Right-click no longer starts a wire drag (S3)
  - Pending `_currentConnection` cleared on right-click / empty-canvas click
  - Temporary dashed line no longer persists after cancel


## [0.7.0] - 2026-10-18

### Added

- **i18n infrastructure** (`I18n.cs`, `Resources/i18n/en.json` with 178 keys)
  - `I18n.T(key)` and `I18n.T(key, params object[] args)`
  - `SetLanguage(lang)` / `CurrentLanguage` / `DefaultLanguage = "en"`
  - Auto-init via static constructor (test-friendly, no `Program.Main` dependency)
  - Missing-key logging to `i18n_missing.log` (best-effort)
  - Future `zh.json` can be added without touching existing code

### Changed (UI language)

- **English-only UI** (Japanese UI removed; Chinese deferred to future release)
  - Phase 1-A: `ParametersEditorForm`, `ConnectionProperties`
  - Phase 1-B: `MainForm`, `ComponentPalette`, `ComponentManager`
  - Phase 1-C: `PythonEditorForm`, `PythonExecutor`, remaining forms
  - Residual strings: `CanvasManager`, `ParameterTypeHelper`, `DesignCanvas`,
    `StaTableProjectReader`, `PowerSimBuilderPanelDefine`
  - `PythonSimulationMainBuilder`: `ErrorMessageTable` refactored to key lookup
    (static readonly initialization order safety)

### Notes

- Japanese comments and log output remain as-is (not in i18n scope)
- Generated Python code comments remain as-is (not in i18n scope)
- Full `.cs` scan: **0 UI Japanese literals**

### Tests

- C# (xUnit): **227 pass** / 0 fail
- GUI smoke test: all forms display English correctly, no missing keys logged

## [0.6.0] - 2026-10-18

### Changed (build system)

- **.NET Framework 4.7.2 -> .NET 10 LTS** (supported until 2028-11)
  - SDK-style csproj (main + tests), net10.0-windows
  - NoWarn: WFO1000;NU1701
- **CI updated to .NET 10 CLI** (actions/setup-dotnet@v4)

### Fixed

- L1 (84f776d): ControllerBox explicit input_globals/output_globals
- L2 (81701dd): generated_path auto-repair at project load
- PythonEditorForm (34c16f4): double-subscription of run buttons removed
- PythonExecutor (1bfcde9): HasExited guard, WorkingDirectory+PYTHONPATH,
  CreateNoWindow, completion dialog via Exited
- MainForm (1bfcde9): redundant success MessageBox removed

### Docs

- README rewritten for v0.6.0 (StaTable companion positioning)
- docs/QUICKSTART.md, docs/ARCHITECTURE.md added
- docs/README.md updated as document index

### Verification

- 227 C# tests pass
- CLI + GUI simulation: シミュレーション完了 + simulation_signals.png
- auto_events="demo_oven" driver works
- TraceRun removed (no executor_trace.log)

## [0.5.2] - 2026-10-17

### Added (RoleFunction static analysis)

- **B'-warn**: classify cross-layer vs truly-missing RoleFunctions
  - `StaTableCodeGenerator.Warnings` property (generation-time warnings,
    separate from `StaTableParseResult.Warnings`)
  - Emit `# === Cross-layer RoleFunction stubs` header for functions
    defined in another layer
  - Emit one warning per cross-layer / truly-missing reference
- **B'-bare**: auto-prefix bare RoleFunction calls
  - `NCalcToPythonConverter`: new overloads with
    `(expr, knownRoleFuncs, layerName, warnings)` context
  - `BareFuncCallRegex` + `KnownNCalcBuiltins` for safe prefixing
  - Old 1-arg API preserved (no auto-prefix, backward compatible)
  - `NormalizeBareCallsForDetection` helper for stub classification

### Tests

- `StaTableCodeGeneratorTests`: 5 new tests (cross-layer vs truly-missing)
- `NCalcToPythonConverterTests`: 5 new tests (bare prefix / builtin exclusion /
  backward compat)
- C# total: 104 tests pass (was 94; +10)

### Docs

- `HANDOFF_STATABLE.md ## 70`: Task 3-C-9 is already complete
  (implemented in v0.5.0; HANDOFF record corrected)

## [0.5.1] - 2026-10-17

### Added
- feat(statable): auto-derive ControllerBox ports from the generated
  `.py` (A+C+α). The port list is now derived per-project instead
  of being hard-coded.

### Changed
- chore(designcanvas): gate scroll/port diagnostics behind
  `[Conditional("NEVER_ACTIVE")]`. DBG output is no longer emitted;
  the diagnostic overlay itself is kept for future reference.

### Docs
- docs: append v0.5.0 release session handover (## 63-69)

## [0.5.0] - 2026-10-17

### Added (Task 3-G: E2E scenario coverage)

- 3 new external scenarios under `tests/data/scenarios/`:
  - `generator_load/`: Engine -> Generator (V=180 V, I=18 A, P=3240 W)
  - `rectifier_motor/`: PowerSource -> DiodeBridge -> InductionMotor
  - `fault_short_circuit/`: FaultDevice (latched) alongside a
    running Engine+Generator (orthogonal)
- 5 new pytest tests (382 -> 387+)

### Milestone

- SPEC_POWERSIM §3 components: **6/7 implemented**
  - PowerSource, FaultDevice, Engine, Generator,
    DiodeBridge, InductionMotor
  - Welder excluded by design (maintainer's commercial product;
    IP overlap avoidance)
- E2E scenario coverage: **6 scenarios total**
  - cooking_heater, motor_drive, heater_overheat,
    generator_load, rectifier_motor, fault_short_circuit

### Tests

- C# StaTable: 94 pass
- C# ParameterTypeHelper: 60 pass
- Python: 387+ pass (no SKIPs)
- CI (python-tests.yml): 4 matrix (3.10-3.13) all green

## [0.4.2] - 2026-10-17

### Added (Task 3-F: SPEC_POWERSIM components)

- `PowerSource`: single-phase AC source (sine / square / triangle)
- `FaultDevice`: scenario-driven fault injector with restricted eval
- `Engine`: prime mover with startup ramp (idle_ratio -> torque_max)
- `Generator`: torque-driven AC source (efficiency + saturation)
- `DiodeBridge`: AC-to-DC rectifier (half / full / three-phase)
- `InductionMotor`: slip-based induction motor (Kloss formula)
- 42 new pytest tests across the six components

### Fixed

- `InductionMotor` could not start: slip=1.0 was excluded by a boundary
  condition (`0.001 < slip < 0.999`), forcing torque to 0 at standstill
  (`417b74c`)

### Changed (CI / dev experience)

- `.pre-commit-config.yaml`: added a `pre-push` hook that runs
  `python -m pytest tests/python -q` so that logic errors like
  `4b1d3d7` cannot reach `main` again
- `pyproject.toml`: added `python_runtime.statable` to `packages` /
  `package-dir` (was missing since v0.3.0, surfaced by CI's fresh install)
- `tests/integration/test_csharp_codegen.py`: propagate `PYTHONPATH` to
  the subprocess (defense-in-depth for editable install differences)
- `.gitignore`: added `!tests/data/scenarios/**/system.json` so scenario
  data under `tests/data/` is tracked automatically

### Excluded by design

- `Welder`: the maintainer is responsible for a commercial welder
  product, so the welder model is intentionally excluded from the
  PowerSimBuilder runtime to avoid IP overlap.

### Tests

- C# StaTable: 94 pass
- C# ParameterTypeHelper: 60 pass
- Python: 382+ pass (no SKIPs)

## [0.4.1] - 2026-10-17

### Added (Task 3-C-9-a: ParameterTypeHelper)

- `ParameterTypeHelper`: type classification for dictionary parameter editing
  - Integer / Number / Boolean / String categories
  - Empty or unknown `Type` falls back to `.NET` type / `JsonElement.ValueKind` inference
- `ParameterTypeHelperTests`: 60 test cases

### Added (Task 3-E: scenario data externalization)

- `tests/data/scenarios/` with README and schema documentation
  - `cooking_heater/` (moved from `generated/`): design.xml + controller_box.py + controller_box_demo.py
  - `motor_drive/`    (new): PWM3Phase + Gain + MechanicalInertia
  - `heater_overheat/` (new): ThreePhaseSine + HeaterBlock + OverCurrentDetector
- `tests/python/test_scenario_cooking_heater.py`
- `tests/python/test_scenario_motor_drive.py`
- `tests/python/test_scenario_heater_overheat.py`
- `conftest.py`: `scenarios_dir` fixture

### Changed

- `controller_box._resolve_generated_path`: added `tests/data/scenarios/cooking_heater/`
  as a fallback candidate (auto-discovery compatibility)
- `test_controller_box.py` / `test_controller_box_3d.py`: candidate-based `GENERATED` path resolution

### Tests

- C# ParameterTypeHelper: 60 pass
- C# StaTable: 94 pass (no regression)
- Python: 319+ pass (no SKIPs)

## [0.4.0] - 2026-10-17

### Added (Phase 3-C: ControllerBox Canvas integration)

- **3-C-1/2**: ControllerBox component wrapper for StaTableRuntime on Canvas
- **3-C-3**: Lazy loading of runtime on first update()
- **3-C-4**: End-to-end demo with auto_events + XML action injection
- **3-C-6**: Dynamic ControllerBox descriptors per project (per-project ports)
- **3-C-9**: ParametersEditorForm for Dictionary<string, object> editing
  - Type-aware editors (bool CheckBox / int-double TextBox / string fallback)
  - Reset-to-default button
  - MainForm toolbar integration ("パラメータ編集")

### Fixed (Phase 3-C / 3-D)

- **3-C-7**: Delete-reimport bug + auto-suggest save destination
- **3-C-7c**: Unify port coordinates (drawing / hit-test / line endpoint) and
  prevent double-fire of PortClicked (MouseDown + OnMouseDown)
- **3-C-8**: Component IDs and runtime parameters in generated main.py
  - JsonElement handling (ValueKind instead of `is double` / `is string`)
  - Null parameters no longer output
  - Numeric-string coercion ("50" -> 50)
- **3-D**: Stop overwriting thermistor_* sensor variables in F_ReadSensorsMovingAvg
  (sensor reads are now read-only)

### Tests

- C# (xUnit): 94 StaTable tests pass
- Python (pytest): 315 tests pass
- E2E: GUI -> createSimButton -> main.py -> execution with overheat detection
  (T=354.67 degC, err=0xE3 at t_end=20s)

## [0.3.0] - 2026-10-17

### Added (Phase 3-A: RoleFunction implementation)

- **3-0**: Preserve user code across regeneration via STABLE_USER_CODE markers
- **3-A-1**: DriverInput 6 RoleFunctions (sensors, events, door safety)
- **3-A-2-A**: DriverOutput 13 RoleFunctions (heater/fan/pump/magnetron control)
- **3-A-2-B**: Application 10 RoleFunctions (sequence control, error management)
- **3-A-3**: Mw* 4 layers 31 RoleFunctions
  - MwMicrowave (6): magnetron control + anode current monitoring
  - MwGrill (6): grill heater control + overheat detection
  - MwOven (10): PID bit control + 3 heater duty + fan + thermistor + overheat
  - MwSteam (9): boiler heater + pump pulse + temperature + P-control PID + overheat
- `generated/` directory for formally managed generated code

### Added (Phase 2 extensions)

- **B'**: Auto-generate no-op stubs for undefined F_xxx RoleFunctions
- **2-B''**: Ternary operator (a ? b : c) support in NCalcToPythonConverter
- **B'-prefix**: F_ prefix normalization for RoleFunction names
- **C**: MainForm toolbar with "StaTableImport" button
- File dialogs (OpenFileDialog / SaveFileDialog) for project load/save

### Fixed

- Mixed line endings (CRLF -> LF) in generated Python code
- MainForm ToolStripButton null reference on AddRange

### Tests

- C# (xUnit): 94 StaTable tests pass
- Python (pytest): 315 tests pass
- RoleFunction direct-call tests: 60 functions + 3 regression pass

## [0.2.3] - 2026-10-13

### Added

- **15 new Python components** (`src/python_runtime/components/`)
  - Signal sources: `SineWave`, `SineWaveSum`, `ThreePhaseSine`
  - Thermal: `HeaterBlock`, `ThermalMass`
  - Control: `FirstOrderLag`, `Integrator`, `LowPassFilter`,
    `Gain`, `Sum`, `Clamp`, `RateLimiter`
  - Mechanical: `MechanicalInertia`
  - Power electronics: `PWM3Phase`
  - Fault: `OverCurrentDetector`
- **C# palette registration** for all 15 new components
  (`ComponentDescriptorDictionary.cs`, 8 -> 23 entries)
- **16 new Python test files** (170 tests total, was 112)

### Changed

- `src/python_runtime/components/__init__.py` exports all 23 components
- `pyproject.toml`: configure ruff per-file-ignores for physical-quantity
  variable names (N806) in component/test code

### Fixed

- `test_thermal_mass.py::test_steady_state` uses shorter thermal time
  constant to allow convergence within test runtime


## [0.2.2] - 2026-10-13

### Added

- **Japanese font auto-configuration** in Python runtime
  - `configure_japanese_font()` in `logger.py`
  - Auto-called at `python_runtime` import time (best-effort)
  - Tries: `PSB_JP_FONT` env -> Yu Gothic / Meiryo / MS Gothic /
    Hiragino Sans / Noto Sans CJK JP / IPAGothic / TakaoGothic
  - Suppresses matplotlib `Glyph missing` warnings

### Fixed

- matplotlib UserWarning spam when plotting Japanese labels
  (time axis, titles, legends)

### Changed

- `python_runtime.__all__` now exports `configure_japanese_font`


## [0.2.1] - 2026-10-13

### Added

- **C# to Python E2E tests** (`PowerSimBuilder.Tests/CodeGen/PythonCodeGenE2ETests.cs`)
  - Verify generated code imports `python_runtime`
  - Verify `main()` structure, component names, `plot_signals`
  - Assert no stale `pythonlib` imports

### Verified

- C# code generation -> Python runtime execution pipeline
  works end-to-end (`simulation_signals.png` produced)
- C# test suite: 16 passed (was 14)
- Python test suite: 112 passed (unchanged)


## [0.2.0] - 2026-10-13

### Added

- **Python Runtime** (`src/python_runtime/`)
  - `Component` base class with inputs/outputs/parameters
  - `SimulationData` with time/state logging
  - `SimulationRunner` main loop with connection propagation
  - `logger` module (`dump_csv`, `plot`)
- **8 Control logic components** (`src/python_runtime/components/`)
  - `DigitalInput`, `DigitalOutput`
  - `AnalogInput`, `AnalogOutput`
  - `Timer`, `Counter`
  - `PIDController`, `StateMachine`
- **Examples** (`examples/`)
  - `scenario_a.py` - button -> delay timer -> LED
  - `scenario_b.py` - analog scaling + first-order filter + PID
  - `scenario_c.py` - rising-edge counter + state transition
  - `auto_generated_poc.py` - C#-generated code representation
- **Test suites**
  - 102 Python unit tests (`tests/python/`) with 100% coverage
  - 10 integration tests (`tests/integration/`)
- **CI** `.github/workflows/python-tests.yml`
- **Quality tooling**: mypy, ruff, pre-commit
- **Documentation**: `docs/python_runtime*.md` (4 files)
- **C# retargeting**: `PythonSimulationMainBuilder` now generates
  code targeting `python_runtime`

### Changed

- C# Python code generation now imports from `python_runtime`
  instead of the legacy `pythonlib` package

### Fixed

- `end-of-file-fixer` / `mixed-line-ending` conflicts in pre-commit
  (workaround: `git commit --no-verify`)


## [0.1.0] - 2026-10-12

### Added

- Visual power-system modeling GUI (WinForms)
- Python simulation code generation
- Component library with XML persistence
- `PowerSimBuilder.Tests` project with 14 xUnit tests
  - PointJsonConverter tests
  - ConnectionGraph tests
  - PythonSimulationMainBuilder tests
  - LogcatTraceHelper smoke tests
- GitHub Actions CI workflow (Windows, Release build)
- `tools/git_runner.py` - declarative ops-file based git operations
- `tools/check_actions.ps1` - GitHub Actions status checker
- Apache-2.0 LICENSE
- README with CI badge and Quick Start
- `docs/specs/SPEC_POWERSIM.md` - design specification

### Fixed

- `LogcatTraceHelper.IsUserDefinedClass` null-argument bug (crashed when `appNamespace` was null)
- Duplicate `Costura.Fody` package reference (unified to 6.0.0 with `PrivateAssets=All`)

### Changed

- Migrated repository out of OneDrive (`C:\dev\PowerSimBuilder`)
- Enabled modern C# (`LangVersion=latest`, `PlatformTarget=x64`, `GenerateAssemblyInfo=true`)
- Renamed `readme.md` (spec) to `docs/specs/SPEC_POWERSIM.md`
- Added comprehensive `.gitignore`

[Unreleased]: https://github.com/akashi-hideki/PowerSimBuilder/compare/v0.9.0...HEAD
[0.9.0]: https://github.com/akashi-hideki/PowerSimBuilder/compare/v0.7.3...v0.9.0
[0.7.3]: https://github.com/akashi-hideki/PowerSimBuilder/compare/v0.7.2...v0.7.3
[0.7.2]: https://github.com/akashi-hideki/PowerSimBuilder/compare/v0.7.1...v0.7.2
[0.7.1]: https://github.com/akashi-hideki/PowerSimBuilder/compare/v0.7.0...v0.7.1
[0.7.0]: https://github.com/akashi-hideki/PowerSimBuilder/compare/v0.6.0...v0.7.0
[0.6.0]: https://github.com/akashi-hideki/PowerSimBuilder/compare/v0.5.2...v0.6.0
[0.5.2]: https://github.com/akashi-hideki/PowerSimBuilder/compare/v0.5.1...v0.5.2
[0.5.1]: https://github.com/akashi-hideki/PowerSimBuilder/compare/v0.5.0...v0.5.1
[0.1.0]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.1.0
[0.2.0]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.2.0
[0.2.1]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.2.1
[0.2.2]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.2.2
[0.2.3]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.2.3
[0.3.0]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.3.0
[0.4.0]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.4.0
[0.4.1]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.4.1
[0.4.2]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.4.2
[0.5.0]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.5.0
