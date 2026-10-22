# ruff: noqa: N802, N803, E701  (fmpy API uses camelCase/PascalCase)
from pathlib import Path
from types import SimpleNamespace

import pytest

from . import fmu_adapter as mod
from .manifest import InterfaceSpec, PhysicsManifest, SourceSpec


class _FakeFmu:
    def __init__(self):
        self.time = 0.0
        self.inputs: dict[int, float] = {}
        self.steps: list[float] = []

    def instantiate(self): ...
    def setupExperiment(self, startTime):
        self.time = startTime

    def enterInitializationMode(self): ...
    def exitInitializationMode(self): ...
    def setReal(self, vrs, values):
        for vr, v in zip(vrs, values, strict=False):
            self.inputs[vr] = v

    def getReal(self, vrs, out):
        for i, vr in enumerate(vrs):
            out[i] = float(vr) * 2.0

    def doStep(self, currentCommunicationPoint, communicationStepSize):
        self.steps.append(communicationStepSize)
        self.time = currentCommunicationPoint + communicationStepSize

    def terminate(self): ...
    def freeInstance(self): ...


@pytest.fixture
def manifest(tmp_path: Path) -> PhysicsManifest:
    (tmp_path / "source").mkdir()
    (tmp_path / "source" / "fake.fmu").write_bytes(b"PK\x03\x04")
    return PhysicsManifest(
        name="Fake",
        source=SourceSpec(type="fmu", file="source/fake.fmu"),
        interface=InterfaceSpec(
            fmi_type="CoSimulation",
            inputs={"u": "u"},
            outputs={"y": "y"},
            parameters={"k": 1.0},
        ),
        base_dir=tmp_path,
    )


def _install_fakes(monkeypatch):
    fake = _FakeFmu()
    md = SimpleNamespace(
        modelVariables=[
            SimpleNamespace(name="u", valueReference=1),
            SimpleNamespace(name="y", valueReference=2),
            SimpleNamespace(name="k", valueReference=3),
        ]
    )
    monkeypatch.setattr(mod, "read_model_description", lambda p: md)
    monkeypatch.setattr(mod, "instantiate_fmu", lambda *_a, **_kw: fake)
    monkeypatch.setattr(mod, "_FMPY_IMPORT_ERROR", None)
    return fake


def test_adapter_step_advances(monkeypatch, manifest):
    fake = _install_fakes(monkeypatch)
    a = mod.FmuAdapter(manifest)
    a.inputs["u"] = 1.5
    a.step(0.01)
    assert fake.steps == [0.01]
    assert fake.inputs[1] == 1.5
    assert a.outputs["y"] == 4.0


def test_adapter_rejects_non_cosimulation(monkeypatch, manifest):
    _install_fakes(monkeypatch)
    manifest.interface.fmi_type = "ModelExchange"
    with pytest.raises(NotImplementedError):
        mod.FmuAdapter(manifest)


def test_adapter_missing_port_variable(monkeypatch, manifest):
    md = SimpleNamespace(modelVariables=[SimpleNamespace(name="u", valueReference=1)])
    monkeypatch.setattr(mod, "read_model_description", lambda p: md)
    monkeypatch.setattr(mod, "instantiate_fmu", lambda *_a, **_kw: _FakeFmu())
    monkeypatch.setattr(mod, "_FMPY_IMPORT_ERROR", None)
    with pytest.raises(Exception, match="does not exist"):
        mod.FmuAdapter(manifest)


def test_adapter_step_rejects_nonpositive_dt(monkeypatch, manifest):
    _install_fakes(monkeypatch)
    a = mod.FmuAdapter(manifest)
    with pytest.raises(ValueError):
        a.step(0.0)
