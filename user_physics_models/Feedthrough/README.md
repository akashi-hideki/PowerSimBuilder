# Feedthrough (Physics Model)

Co-Simulation FMU imported via PowerSimBuilder's "Import Physics Model" dialog.

## Quick start (Python)

```python
from python_runtime.physics import load_manifest, FmuAdapter

# 1. Load the model manifest
manifest = load_manifest(r"C:\dev\PowerSimBuilder\user_physics_models\Feedthrough\model.yaml")

# 2. Instantiate the FMU adapter
fmu = FmuAdapter(manifest)

# 3. Set an input (if any)
fmu.inputs["Float64_continuous_input"] = 1.0

# 4. Step forward 10 ms
fmu.step(0.01)

# 5. Read outputs
print("no outputs defined")

# 6. Cleanup
fmu.close()
```

## Interface

| Direction | Port | FMU variable | Unit |
|-----------|------|--------------|------|
| input | Float64_continuous_input | Float64_continuous_input | - |
| input | Float64_discrete_input | Float64_discrete_input | - |
| input | Int32_input | Int32_input | - |
| input | Boolean_input | Boolean_input | - |
| input | String_input | String_input | - |
| input | Enumeration_input | Enumeration_input | - |
| _(none)_ | | | |

## Timing
- Mode: `fixed_step`
- Internal dt: `0.001` s

## Source
- File: `source/*.fmu`
- License: (unspecified)
- URL: (none)

## Files in this directory
- `model.yaml` - manifest (edit to remap ports/parameters)
- `source/*.fmu` - the FMU archive
- `README.md` - this file

## Regenerate this README

After editing `model.yaml`, run:

```powershell
python -m python_runtime.physics.manifest "C:\dev\PowerSimBuilder\user_physics_models\Feedthrough\model.yaml" --write-readme
```
