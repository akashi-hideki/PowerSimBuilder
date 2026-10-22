# Physics Model Integration (FMU)

## Overview
PowerSimBuilder imports Co-Simulation FMUs as first-class components.
Each `.fmu` becomes a single Component (`ComponentKind.Physics`).

## Get an FMU (China-accessible sources)
| Site | URL pattern | Access from CN |
|------|-------------|----------------|
| MoHub | mohub.net -> .fmu | Yes (domestic) |
| Gitee | gitee.com -> ZIP | Yes (domestic) |
| PyPI (Tsinghua mirror) | pypi.tuna.tsinghua.edu.cn -> fmpy wheel | Yes (domestic) |
| FMI Reference FMUs | github.com -> .fmu | Slow, use gh-proxy.com |
| OpenModelica | openmodelica.org -> build .fmu | Slow, use Docker image |

## Install fmpy
```powershell
pip config set global.index-url https://pypi.tuna.tsinghua.edu.cn/simple
pip install fmpy
python -c "import fmpy; print(fmpy.__version__)"
```

## Import
Toolbar -> **Import Physics Model** -> select `.fmu` -> check ports ->
**Import**. Files land in `user_physics_models/<name>/`.

## `.mo` (Modelica source) is **not** accepted
If you have a `.mo`, build an FMU first:
```powershell
docker run --rm -v ${PWD}:/work openmodelica/openmodelica:v1.22.0-omc `
  omc -s /work/MyModel.mo /work/build.mos
```
Then import the resulting `.fmu`.

## Runtime
- `FmuAdapter` wraps one Co-Simulation FMU.
- `step(dt)` calls `doStep(currentCommunicationPoint, dt)`.
- Only FMI 2.0/3.0 Co-Simulation is supported in MVP-1.
