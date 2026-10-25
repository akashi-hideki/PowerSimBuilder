"""FMPy-based wrapper providing a PyFMI-compatible load_fmu API."""

from __future__ import annotations

import os
import shutil
from typing import Any


class _FmuWrapper:
    def __init__(self, fmu_path: str, instance_name: str):
        from fmpy import extract, read_model_description
        from fmpy.fmi2 import FMU2Slave

        if not os.path.isfile(fmu_path):
            raise FileNotFoundError(f"FMU not found: {fmu_path}")

        self._md = read_model_description(fmu_path)
        if self._md.coSimulation is None:
            raise ValueError(f"FMU has no CoSimulation interface: {fmu_path}")
        self._unzip_dir = extract(fmu_path)
        self._fmu = FMU2Slave(
            guid=self._md.guid,
            unzipDirectory=self._unzip_dir,
            modelIdentifier=self._md.coSimulation.modelIdentifier,
            instanceName=instance_name,
        )
        self._vr: dict[str, int] = {v.name: v.valueReference for v in self._md.modelVariables}
        self._type: dict[str, str] = {v.name: v.type for v in self._md.modelVariables}

    def instantiate(self, _visible: bool = False, _logging_on: bool = False) -> None:
        self._fmu.instantiate()

    def setup_experiment(
        self,
        start_time: float = 0.0,
        stop_time: float | None = None,
        tolerance: float | None = None,
    ) -> None:
        self._fmu.setupExperiment(startTime=start_time, stopTime=stop_time, tolerance=tolerance)

    def enter_initialization_mode(self) -> None:
        self._fmu.enterInitializationMode()

    def exit_initialization_mode(self) -> None:
        self._fmu.exitInitializationMode()

    def terminate(self) -> None:
        try:
            self._fmu.terminate()
        finally:
            self.free_instance()

    def free_instance(self) -> None:
        try:
            self._fmu.freeInstance()
        except Exception:
            pass
        shutil.rmtree(self._unzip_dir, ignore_errors=True)

    def set(self, name: str, value: Any) -> None:
        vr = self._vr[name]
        t = self._type[name]
        if t in ("Real", "Float32", "Float64"):
            self._fmu.setReal([vr], [float(value)])
        elif t in ("Integer", "Int32", "UInt32"):
            self._fmu.setInteger([vr], [int(value)])
        elif t == "Boolean":
            self._fmu.setBoolean([vr], [bool(value)])
        elif t == "String":
            self._fmu.setString([vr], [str(value)])
        else:
            raise TypeError(f"Unsupported FMI type {t!r} for {name!r}")

    def get(self, name: str) -> Any:
        vr = self._vr[name]
        t = self._type[name]
        if t in ("Real", "Float32", "Float64"):
            return self._fmu.getReal([vr])[0]
        if t in ("Integer", "Int32", "UInt32"):
            return self._fmu.getInteger([vr])[0]
        if t == "Boolean":
            return self._fmu.getBoolean([vr])[0]
        if t == "String":
            return self._fmu.getString([vr])[0]
        raise TypeError(f"Unsupported FMI type {t!r} for {name!r}")

    def do_step(self, current_time: float, step_size: float) -> None:
        self._fmu.doStep(
            currentCommunicationPoint=current_time,
            communicationStepSize=step_size,
        )

    doStep = do_step  # noqa: N815


def load_fmu(fmu_path: str, instance_name: str | None = None) -> _FmuWrapper:
    """PyFMI-compatible load_fmu, backed by FMPy FMU2Slave (FMI 2.0 CoSim only)."""
    if instance_name is None:
        instance_name = os.path.splitext(os.path.basename(fmu_path))[0]
    return _FmuWrapper(fmu_path, instance_name)
