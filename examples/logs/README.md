# Sample `.psblog` Logs

`.psblog` = signal log format defined by `docs/SPEC_PSBLOG.md` (v1 confirmed).

Variable sets are **per-log** (SPEC §1). Each sample below demonstrates a
different subset, which is the normal usage pattern — no fixed schema.

| File | Vars | Purpose |
|---|---:|---|
| `sample_minimal.psblog` | 1 | Minimum viable log (1 variable) |
| `sample_pid_tuning.psblog` | 4 | PID tuning subset (`role=` hints used) |
| `sample_diagnostic.psblog` | 3 | Hardware diagnostic subset |
| `sample_cooking_heater.psblog` | 9 | SPEC §11 full example (oven closed loop) |

## Variable sources

Sample variable names come from `generated/CookingHeaterController_powersim.py`
(`self.globals`, 34 variables total). `.psbproj` files are **independent**
(physical plant wiring) and are not the source of `.psblog` variables.

## User recordings

Recorded logs go to `user_logs/` (gitignored) — they may contain sensitive
plant data and must not be committed.
