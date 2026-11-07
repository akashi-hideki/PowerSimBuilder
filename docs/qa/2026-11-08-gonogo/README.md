# QA Snapshot - 2026-11-08-gonogo

Generated: 2026-11-08 08:07:55
Project: examples\demos\oven_control_statable.psbproj
Target: ovenchamber.outputs.oven_current_temp=200
CSV: 1 Hz

## Contents
| File | Source |
|---|---|
| environment.json | tools/capture_env.py |
| statable_psbproj.txt | tools/verify_statable_psbproj.py |
| metrics.txt | tools/analyze_sim_csv.py |
| controller_box_gating.txt | tools/verify_controller_box.py |
| simulation_signals_1hz.csv | tools/downsample_csv.py |
| simulation_signals.png | generated/simulation_signals.png |
| verify_all.log | tools/verify_all.ps1 |

## Reproduce
    .\tools\qa_snapshot.ps1 -Label 2026-11-08-gonogo -Force
