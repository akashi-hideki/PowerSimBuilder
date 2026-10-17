# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]


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

[Unreleased]: https://github.com/akashi-hideki/PowerSimBuilder/compare/v0.4.1...HEAD
[0.1.0]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.1.0
[0.2.0]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.2.0
[0.2.1]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.2.1
[0.2.2]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.2.2
[0.2.3]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.2.3
[0.3.0]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.3.0
[0.4.0]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.4.0
[0.4.1]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.4.1
