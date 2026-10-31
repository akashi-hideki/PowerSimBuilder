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
