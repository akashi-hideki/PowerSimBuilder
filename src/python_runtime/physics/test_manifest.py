from pathlib import Path

import pytest

from .manifest import (
    InterfaceSpec,
    ManifestError,
    PhysicsManifest,
    SourceSpec,
    load_manifest,
    write_manifest,
)


def _write(tmp_path: Path, text: str) -> Path:
    p = tmp_path / "model.yaml"
    p.write_text(text, encoding="utf-8")
    return p


def test_load_minimal_manifest(tmp_path):
    p = _write(
        tmp_path,
        """
name: PMSM
source: { type: fmu, file: source/PMSM.fmu }
interface:
  fmi_type: CoSimulation
  inputs: { V_dc: V_dc }
  outputs: { omega_m: omega_m }
  parameters: { R_s: 0.1 }
""",
    )
    m = load_manifest(p)
    assert m.name == "PMSM"
    assert m.source.type == "fmu"
    assert m.interface.inputs == {"V_dc": "V_dc"}
    assert m.interface.outputs == {"omega_m": "omega_m"}
    assert m.interface.parameters == {"R_s": 0.1}
    assert m.base_dir == tmp_path.resolve()


def test_load_missing_name_raises(tmp_path):
    p = _write(tmp_path, "source: {type: fmu, file: x.fmu}\n")
    with pytest.raises(ManifestError, match="name"):
        load_manifest(p)


def test_load_bad_yaml_raises(tmp_path):
    p = _write(tmp_path, "name: [\n : :\n")
    with pytest.raises(ManifestError, match="invalid YAML"):
        load_manifest(p)


def test_unsupported_fmi_type_raises(tmp_path):
    p = _write(
        tmp_path, "name: X\nsource: {type: fmu, file: x.fmu}\ninterface: {fmi_type: Weird}\n"
    )
    with pytest.raises(ManifestError, match="fmi_type"):
        load_manifest(p)


def test_roundtrip(tmp_path):
    m = PhysicsManifest(
        name="Demo",
        source=SourceSpec(type="fmu", file="source/demo.fmu", license="MIT"),
        interface=InterfaceSpec(inputs={"u": "u"}, outputs={"y": "y"}, parameters={"k": 1.5}),
        units={"voltage": "V"},
    )
    out = write_manifest(m, tmp_path / "model.yaml")
    loaded = load_manifest(out)
    assert loaded.name == "Demo"
    assert loaded.source.license == "MIT"
    assert loaded.interface.parameters == {"k": 1.5}
    assert loaded.units == {"voltage": "V"}
