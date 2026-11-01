"""FMU-backed physics component (MVP-1: Co-Simulation only)."""

from __future__ import annotations

from typing import Any

try:
    from fmpy import extract, instantiate_fmu, read_model_description

    _FMPY_IMPORT_ERROR: Exception | None = None
except ImportError as exc:
    read_model_description = None
    instantiate_fmu = None
    _FMPY_IMPORT_ERROR = exc

from .manifest import ManifestError, PhysicsManifest

try:
    from ..component import Component as _BaseComponent
except Exception:
    _BaseComponent = object


class FmuAdapter(_BaseComponent):
    """Co-Simulation FMU adapter."""

    def __init__(
        self,
        manifest: PhysicsManifest,
        *,
        start_time: float = 0.0,
        parameter_overrides: dict[str, float] | None = None,
    ) -> None:
        if _FMPY_IMPORT_ERROR is not None:
            raise RuntimeError(
                "fmpy is required for FMU components. pip install fmpy"
            ) from _FMPY_IMPORT_ERROR
        if manifest.source.type != "fmu":
            raise ManifestError(f"unsupported source.type: {manifest.source.type!r}")
        if manifest.interface.fmi_type != "CoSimulation":
            raise NotImplementedError("MVP-1 only supports CoSimulation FMUs.")
        fmu_path = manifest.resolve_source_path()
        if not fmu_path.exists():
            raise ManifestError(f"FMU file not found: {fmu_path}")

        self.manifest = manifest
        self._start_time = float(start_time)
        self._time = self._start_time
        self._fmu: Any = None
        self._unzipdir: str | None = None
        self._md = read_model_description(str(fmu_path))
        self._vrs: dict[str, int] = {v.name: v.valueReference for v in self._md.modelVariables}

        self._input_map = dict(manifest.interface.inputs)
        self._output_map = dict(manifest.interface.outputs)
        self._param_map = dict(manifest.interface.parameters)
        if parameter_overrides:
            self._param_map.update(parameter_overrides)

        for port, fmu_var in {**self._input_map, **self._output_map}.items():
            if fmu_var not in self._vrs:
                raise ManifestError(
                    f"port {port!r} maps to FMU variable {fmu_var!r}, "
                    f"which does not exist in {fmu_path.name}"
                )
        self.inputs: dict[str, float] = {p: 0.0 for p in self._input_map}
        self.outputs: dict[str, float] = {p: 0.0 for p in self._output_map}
        self._instantiate()

    def _instantiate(self) -> None:
        fmu_path = self.manifest.resolve_source_path()
        self._unzipdir = extract(str(fmu_path))
        self._fmu = instantiate_fmu(self._unzipdir, self._md, fmi_type="CoSimulation")
        self._fmu.instantiate()
        self._fmu.setupExperiment(startTime=self._time)
        self._fmu.enterInitializationMode()
        self._apply_parameters()
        self._apply_inputs()
        self._fmu.exitInitializationMode()
        self._refresh_outputs()

    def reset(self) -> None:
        self._free_instance()
        self._time = self._start_time
        self.inputs = {p: 0.0 for p in self._input_map}
        self._instantiate()

    def close(self) -> None:
        self._free_instance()

    def __del__(self):  # pragma: no cover
        self._free_instance()

    def _free_instance(self) -> None:
        if self._fmu is None:
            return
        try:
            self._fmu.terminate()
        except Exception:
            pass
        try:
            self._fmu.freeInstance()
        except Exception:
            pass
        self._fmu = None
        if self._unzipdir is not None:
            import shutil

            try:
                shutil.rmtree(self._unzipdir, ignore_errors=True)
            except Exception:
                pass
            self._unzipdir = None

    def step(self, dt: float) -> None:
        if self._fmu is None:
            raise RuntimeError("FMU is not instantiated")
        dt = float(dt)
        if dt <= 0.0:
            raise ValueError(f"dt must be > 0, got {dt}")
        self._apply_inputs()
        self._fmu.doStep(currentCommunicationPoint=self._time, communicationStepSize=dt)
        self._time += dt
        self._refresh_outputs()

    def _apply_inputs(self) -> None:
        for port, value in self.inputs.items():
            vr = self._vrs[self._input_map[port]]
            self._fmu.setReal([vr], [float(value)])

    def _apply_parameters(self) -> None:
        for fmu_var, value in self._param_map.items():
            vr = self._vrs.get(fmu_var)
            if vr is None:
                continue
            self._fmu.setReal([vr], [float(value)])

    def _refresh_outputs(self) -> None:
        for port, fmu_var in self._output_map.items():
            vr = self._vrs[fmu_var]
            values = self._fmu.getReal([vr])
            self.outputs[port] = float(values[0])
