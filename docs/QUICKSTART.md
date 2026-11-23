# Quick Start

First-project walkthrough for PowerSimBuilder v1.11.0.

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

## FMU Catalog Quick Notes
Important specifications when using the FMU Catalog feature:
- **API Token**: The token is saved **only when "Load" is executed** with a `github:` format source (typing it alone does not save it).
- **Network Access**: A confirmation dialog is displayed when downloading via URL. Selecting "Always allow" **does not skip** the confirmation on subsequent downloads.
- For detailed specifications, constraints, and operating procedures, please refer to [FMU_CATALOG_GUIDE.md](FMU_CATALOG_GUIDE.md).



## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `AttributeError: has no build_runtime()` | stale `generated_path` | reload project (L2 auto-repair); or set `generated_path` to a `.py` with `def build_runtime` |
| `KeyError: <port_name>` | missing `output_globals` | regenerate `demo_main.py` (L1 fix in v0.6.0) |
| `ModuleNotFoundError: python_runtime` | editable install missing | `python -m pip install -e .` from repo root |


## 6. Import a Physics Model (FMU)

1. Open **Import Physics Model** and select the **Catalog** tab.
2. Select a built-in source or enter a custom source. For an FMI Cross Check-style GitHub tree, an example is:

   `github:modelica/fmi-cross-check|2.0|cs|win64`

3. Optionally enter a GitHub API token, then click **Load**.
4. Select an FMU and click **Download** (or double-click the row).
5. Review the network-access confirmation. If approved, the FMU is downloaded and parsed.
6. In the **File** tab, select the input/output variables to expose as ports, set the model name, and click **Import**.

The current importer accepts **Co-Simulation FMUs only**. See [FMU Catalog and Import Guide](FMU_CATALOG_GUIDE.md) for source syntax, token behavior, limitations, and troubleshooting.
