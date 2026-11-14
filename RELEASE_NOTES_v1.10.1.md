## Highlights

Patch release fixing the `.psblog` Import pipeline found during real-hardware
integration testing (StaTable v3.6.0, 5000-sample logs).

### Fixed

- **`plot_config` port name**: `Create Simulation` now emits the actual first
  output port of each component (e.g. `'value'` for `RecordedSignal`) instead
  of the hardcoded `'output'`. This fixes the symptom where the PNG showed a
  flat line because `plot_signals()` looked up a non-existent key.
- **`GetFirstOutputPortName`** now uses `ComponentControl.Descriptor.OutputPorts[0]`
  directly (was: reflection on `Type` which `ComponentControl` does not have).
- **`Add to Canvas`** dialog now closes reliably even when `PSB_PSBLOG_IMPORT`
  env var is still set from a previous session. `_btnAddToCanvas.DialogResult`
  was restored; `PSB_E2E_TEST=1` is now required for auto-open at startup.
- **`recorded_signal`** export from `components/__init__.py` (Phase 3 bug
  surfaced by v1.10.0; already fixed in the `v1.10.0` follow-up commit but now
  covered by regression tests).
- **`psblog` repo root** search: `PsblogParser` locates the repo root from
  `AppContext.BaseDirectory` (was: from the `.psblog` file location, which
  fails for user logs outside the repo).

### Added

- **`oscillating` waveform category** in `signal_classifier`:
  - Detects triangle / sine / saw patterns via direction-change count.
  - `oven_current_temp` and `heater_top_duty` are now auto-checked at
    `confidence = 0.85` when imported.
- **Large-ramp demotion**: linear ramps with `range > 10000` (e.g.
  `g_system_tick`) get `confidence = 0.3` and are no longer auto-checked.
  Prevents Y-axis domination that made triangle waves look flat.
- **Regression tests**:
  - `test_components_init.py` (3 tests): AST-based check that every component
    class in `components/*.py` is exported from `__init__.py`.
  - `test_recorded_signal_integration.py` (3 tests): `.psblog` → `RecordedSignal`
    non-zero replay, missing-file safety, missing-var safety.
  - `test_signal_classifier.py::test_oscillating` and
    `::test_large_range_ramp_not_auto_selected` (2 new tests).
- **`PSB_E2E_TEST=1`** environment flag: `OnPsblogImportTestShown` now requires
  this in addition to `PSB_PSBLOG_IMPORT`, preventing accidental auto-open
  when a developer forgets to unset the env var.

### Changed

- **`TraceDiag`** in `MainForm` and `PsblogImportForm` is now a no-op in
  Release builds (`#if DEBUG` guarded).

## Testing

| Suite | Result |
|---|---|
| C# unit tests | 599 + 7 (psblog) + 2 (import form) + 2 (descriptor) |
| UI E2E | 19 (18 pass / 1 skip), incl. PsblogImport E2E (2 pass) |
| Python | 29 (parser) + 5 (RecordedSignal) + 18 (classifier) + 6 (regression) = 58+ pass |

`verify_all.ps1` sections: `[3.6]` parser, `[3.7]` sample sanity, `[4e]` import E2E.

## Changelog

See [CHANGELOG.md](CHANGELOG.md#1101---2026-11-14).
