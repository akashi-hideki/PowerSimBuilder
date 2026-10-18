# Architecture

Internal structure of PowerSimBuilder (v0.6.0).

## Layers

```
┌──────────────────────────────────────────────────────────┐
│  UI layer (C# / WinForms, .NET 10)                       │
│    CanvasForm/           ComponentControl, DesignCanvas, │
│                          MainForm, ParametersEditorForm  │
│    ComponentEdit/        PythonEditorForm                │
└──────────────────┬───────────────────────────────────────┘
                   │ uses
                   ▼
┌──────────────────────────────────────────────────────────┐
│  Integration layer (C#)                                  │
│    StaTableIntegration/  StaTableProjectReader,          │
│                          StaTableCodeGenerator,          │
│                          NCalcToPythonConverter,         │
│                          ControllerBoxDescriptorBuilder  │
│    SimulationGenerate/   PythonSimulationMainBuilder,    │
│                          PythonExecutor                  │
└──────────────────┬───────────────────────────────────────┘
                   │ generates .py
                   ▼
┌──────────────────────────────────────────────────────────┐
│  Runtime layer (Python 3.10+)                            │
│    python_runtime.statable:   7-layer state machine      │
│    python_runtime.components: 12+ physical components    │
└──────────────────────────────────────────────────────────┘
```

## Key modules

| Module | Responsibility |
|---|---|
| `ComponentControl` | Canvas widget: ports, hit-test, click dispatch |
| `DesignCanvas` | Canvas: connections, rendering, mouse routing |
| `StaTableProjectReader` | XML -> `StaTableProject` model |
| `StaTableCodeGenerator` | `StaTableProject` -> Python controller source |
| `NCalcToPythonConverter` | ncalc expressions -> Python (ternary, `F_` calls) |
| `ControllerBoxDescriptorBuilder` | Generated `.py` -> palette `ComponentDescriptor` |
| `PythonSimulationMainBuilder` | Canvas state -> `demo_main.py` (L1: explicit ports) |
| `PythonExecutor` | `python` child process, `HasExited` guard, `Exited` dialog |

## Data flow

1. **Import**: StaTable `design.xml` -> `StaTableProject` -> palette descriptor.
2. **Place**: user drags `ControllerBox` + components -> `ComponentControl` instances.
3. **Wire**: click ports -> `Connection` objects -> Canvas render.
4. **Generate**: `PythonSimulationMainBuilder` -> `demo_main.py` with explicit
   `input_globals`/`output_globals` (L1) and repaired `generated_path` (L2).
5. **Run**: `PythonExecutor` spawns `python demo_main.py`, streams stderr,
   shows result dialog, leaves `simulation_signals.png`.

## Test coverage

- **227 C# tests** (`[Fact]` + `[Theory]+[InlineData]` expanded)
- Python: `python -m pytest tests/python`
