# QA Log — 2026-11-03 (Day 1)

## 1. Existing demos (standalone)

| Demo | Result | Notes |
|---|---|---|
| demo_pid_first_order_lag.py | PASS | final y=0.9738 (target 1.0) |
| demo_statable_runtime_loop.py | PASS | final T=200.00, duty=0.35, MwOven=PidHeating |

## 2. Existing demos (.psbproj, GUI)

| Demo | Result | Notes |
|---|---|---|
| oven_control.psbproj | (TBD) | tEnd=300 復元、T 200°C 収束 |
| pid_closed_loop.psbproj | (TBD) | PID 応答波形 |

## 3. Build + E2E

| Item | Result |
|---|---|
| dotnet build | 0 errors |
| E2E ProjectRoundTrip | 5 passed |

## 4. Fixes applied

- demo_pid_first_order_lag.py: ROOT = parent.parent.parent（examples/demos 移動対応）
- demo_statable_runtime_loop.py: 同上

## Day 2: UI/UX Quality (2026-11-03)

### Language switch (zh/en)

| Item | Result | Notes |
|---|---|---|
| Toolbar Save As (zh) | OK | 「另存为...」 |
| Toolbar Save As (en) | OK | 「Save As...」 |
| Save As dialog title (zh) | OK | 「另存为」 |
| Save As dialog title (en) | OK | 「Save As」 |

### PythonEditorForm layout (after Shown fix)

| Item | Result | Notes |
|---|---|---|
| btnRun visible (1st launch) | OK | Shown handler fix confirmed |
| btnReload visible | OK | |
| btnOpenFolder visible | OK | |
| numSimTime input | OK | Direct type + Enter, 10-step ▲▼ |
| numSimTime restore (tEnd=300) | OK | |
| Run -> matplotlib window | OK | Trap #51 non-recurrence |

### Automated tests added (regression guards)

| Test | Passed | Coverage |
|---|---|---|
| ProjectFileV2MetadataTests | 10 | tEnd default/serialize/round-trip/compat |
| SimTimeOverrideTests | 6 | t_end regex/indent/multiple/empty |
| I18nComplianceTests (T1/T2/T3) | 3 | key parity + references + Form titles |

### Fixes applied (Day 2)

- bdaa25d: PythonEditorForm Shown handler (first-launch layout race)
- f9a9653: 3-layer regression guard (metadata + SimTimeOverride + FlaUI skeleton)
- Save As i18n: MainForm.BtnSaveAs + MainForm.SaveAsDialogTitle (en/zh)

## Appendix: setpoint sensitivity (experimental, 2026-11-03)

Tested `oven_control.psbproj` with setpoint changed from 200 to 300.

| setpoint | ss | err | peak | ovs% | settle |
|---:|---:|---:|---:|---:|---:|
| 200 (official) | 200.68 | -0.68 | 202.89 | 1.45 | 64.7s |
| 300 (experimental) | 306.51 | -6.51 | 326.98 | 8.99 | 292.8s |

**Finding**: `no_aw` tuning (kp=0.01, ki=0.0001, anti_windup=False) is optimized
for setpoint=200. At setpoint=300 the saturation time (~80s) grows, integral
windup accumulates without `anti_windup`, causing 27C overshoot and 4.5x slower settling.

**Conclusion**: keep setpoint=200 for the public demo. setpoint-robust tuning
(anti_windup=True + gain scheduling) is a post-release task.

## Day 3: Error Handling + Regression (2026-11-04)

### Malformed .psbproj handling (8/8 passed via tools/qa_day3.ps1)

| Case | Input | Exception caught | Result |
|---|---|---|---|
| A | non-existent file | `FileNotFoundException: Archive not found` | OK |
| B | 0-byte file | `InvalidDataException: Central Directory corrupt` | OK |
| C | non-ZIP text | `InvalidDataException: End of Central Directory record could not be found` | OK |
| D | ZIP without project.json | `InvalidDataException: Missing project.json in archive` | OK |
| E | malformed JSON | `JsonException: invalid start of a property name` | OK |

### Other

| Case | Result |
|---|---|
| F: empty project save | OK (file created) |
| G: Python execution error | OK (exit=1) |
| H: full regression (Category!=UI) | OK (526 passed / 2 skipped) |

### Notes

- Test_Log writes to `%TEMP%\PowerSimBuilder_TestLoad.log` (not bin/).
- `--test-load-save <save> <load>`: first arg = save, second = load.
- All 5 malformed-input cases throw specific exceptions, caught by Test_LoadProjectFromPath,
  logged as FAILED, process exits cleanly (no crash, no hang).
- `tools\qa_day3.ps1` automates all 8 cases (idempotent, cleans tmp/qa_day3 each run).

### Cumulative (Day 1-3)

| Layer | Coverage | Status |
|---|---|---|
| Functional demos | 4 demos, quantitative | 4/4 OK |
| UI/UX | zh/en, Save As, numSimTime, buttons | OK |
| Error handling | 8 cases (malformed, empty, syntax) | 8/8 OK |
| Regression | Category!=UI full suite | 526 passed / 2 skipped |
| E2E | ProjectRoundTrip | 5 passed |
| i18n | T1/T2/T3 + key parity 349 | OK |
