# PMSM (Physics Model)

Co-Simulation FMU imported via PowerSimBuilder's "Import Physics Model" dialog.

## Quick start (Python)

```python
from python_runtime.physics import load_manifest, FmuAdapter

# 1. Load the model manifest
manifest = load_manifest(r"C:\dev\PowerSimBuilder\user_physics_models\PMSM\model.yaml")

# 2. Instantiate the FMU adapter
fmu = FmuAdapter(manifest)

# 3. Set an input (if any)
fmu.inputs["V_dc"] = 1.0

# 4. Step forward 10 ms
fmu.step(0.01)

# 5. Read outputs
print(f"omega_m = " + str(fmu.outputs['omega_m']))

# 6. Cleanup
fmu.close()
```

## Interface

| Direction | Port | FMU variable | Unit |
|-----------|------|--------------|------|
| input | V_dc | V_dc | - |
| input | T_load | T_load | - |
| output | omega_m | omega_m | - |
| output | I_a | I_a | - |

## Timing
- Mode: `fixed_step`
- Internal dt: `0.001` s

## Source
- File: `source/PMSM.fmu`
- License: MIT
- URL: https://mohub.net/

## Files in this directory
- `model.yaml` - manifest (edit to remap ports/parameters)
- `source/PMSM.fmu` - the FMU archive
- `README.md` - this file

## Regenerate this README

After editing `model.yaml`, run:

```powershell
python -m python_runtime.physics.manifest "C:\dev\PowerSimBuilder\user_physics_models\PMSM\model.yaml" --write-readme
```
