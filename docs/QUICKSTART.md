# Quick Start

First-project walkthrough for PowerSimBuilder (v0.6.0).

## Prerequisites

- Windows 10 / 11
- .NET 10 SDK: <https://dot.net/download>
- Python 3.10+: <https://www.python.org/downloads/>

## 1. Build

```powershell
git clone https://github.com/akashi-hideki/PowerSimBuilder.git
cd PowerSimBuilder
dotnet build -c Debug
python -m pip install -e .
```

## 2. Launch

```powershell
.\bin\Debug\net10.0-windows\PowerSimBuilder.exe
```

## 3. Import a StaTable design

1. File -> Open: pick a saved `.json` project (or `New Project`).
2. Toolbar **StaTable import**: select `design.xml` from
   [StaTable](https://github.com/akashi-hideki/StaTable).
3. The generated `controller_box.py` becomes a palette entry.

## 4. Build the simulation

1. Drag `ControllerBox` + physical components onto the Canvas.
2. Click ports to wire them (green = output, blue = input).
3. Toolbar **Simulation create**: generates `demo_main.py`.

## 5. Run

1. In `PythonEditorForm`, click **Run**.
2. On completion, `simulation_signals.png` appears next to
   `demo_main.py` and the result dialog is shown.

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `AttributeError: has no build_runtime()` | stale `generated_path` | reload project (L2 auto-repair); or set `generated_path` to a `.py` with `def build_runtime` |
| `KeyError: <port_name>` | missing `output_globals` | regenerate `demo_main.py` (L1 fix in v0.6.0) |
| `ModuleNotFoundError: python_runtime` | editable install missing | `python -m pip install -e .` from repo root |
