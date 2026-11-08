## Highlights

### `.psblog` Import — signal log reader (v1.10.0)

PowerSimBuilder can now read `.psblog` signal logs written by
[StaTable](https://github.com/akashi-hideki/StaTable)'s UART monitor.
Log format is `psblog/v1` (see `docs/SPEC_PSBLOG.md` for the Writer-side spec,
`docs/SPEC_PSBLOG_IMPORT.md` for the Reader-side implementation).

**Variable sets are per-log** (SPEC §1). PSB trusts the VarTable verbatim;
no fixed schema is required.

#### End-to-end flow

StaTable UART monitor
-> .psblog file (text)
-> [File → Import Signal Log...] or [toolbar button]
-> PsblogImportForm (summary + variable list + shape/component suggestions)
-> [Add to Canvas]
-> RecordedSignal components on the Canvas
-> Create Simulation -> Run -> simulation_signals.png

#### New components / modules

| Path | Purpose |
|---|---|
| `src/python_runtime/psblog.py` | `psblog/v1` parser (`parse_psblog`, `parse_psblog_glob`, `to_json`, CLI `--json-out --classify`) |
| `src/python_runtime/components/recorded_signal.py` | Time-series replay component (`RecordedSignal`) |
| `src/python_runtime/analysis/signal_classifier.py` | Waveform classifier (`classify`, `classify_all`) |
| `parts/PsblogImportForm.cs` | WinForms import dialog |
| `SimulationGenerate/PsblogParser.cs` | C# subprocess wrapper (`BeginOutputReadLine` + `--json-out`, Trap #49 mitigation) |
| `tools/verify_psblog.py` | Sample log regression check |

#### Waveform classification

`signal_classifier.py` proposes candidate components from the signal shape:

| Shape | Proposed component |
|---|---|
| `ramp` | `Integrator` |
| `step` | `StepSource` |
| `exponential` | `ThermalMass` / `FirstOrderLag` |
| `pwm` | `PIDController` output |
| `digital` | `DigitalInput` |
| `constant` | (ignored) |

`role=` hints (SPEC §6.2) override the classifier with `confidence = 1.0`.

### Sample logs

Five sample `.psblog` files in `examples/logs/` demonstrate the per-log
variable-set principle:

| File | Vars | Purpose |
|---|---:|---|
| `sample_minimal.psblog` | 1 | Minimum viable log |
| `sample_pid_tuning.psblog` | 4 | PID tuning subset (`role=` hints) |
| `sample_diagnostic.psblog` | 3 | Hardware diagnostic subset |
| `sample_cooking_heater.psblog` | 9 | SPEC §11 full example |
| `sample_rotated/log*.psblog` | — | Rotation concat (SPEC §4.3) |

## Testing

| Suite | Result |
|---|---|
| C# unit tests | 599 + 7 (psblog) + 2 (import form) |
| UI E2E | 19 (18 pass / 1 skip), incl. `[4e]` PsblogImport E2E (2 pass) |
| Python | 29 (parser) + 5 (RecordedSignal) + 16 (classifier) = 50 pass |

`verify_all.ps1` sections: `[3.6]` parser, `[3.7]` sample sanity, `[4e]` import E2E.

## Changelog

See [CHANGELOG.md](CHANGELOG.md#1100---2026-11-08).
