from pathlib import Path

from .manifest import (
    InterfaceSpec,
    PhysicsManifest,
    SourceSpec,
    TimingSpec,
    load_manifest,
    write_manifest,
    write_usage_readme,
)


def _make_manifest(tmp_path: Path) -> PhysicsManifest:
    (tmp_path / "source").mkdir()
    (tmp_path / "source" / "test.fmu").write_bytes(b"PK\x03\x04")
    return PhysicsManifest(
        name="TestModel",
        source=SourceSpec(type="fmu", file="source/test.fmu", license="MIT"),
        interface=InterfaceSpec(
            inputs={"u": "u", "v": "v"},
            outputs={"x": "x", "y": "y"},
        ),
        timing=TimingSpec(mode="fixed_step", dt_internal=0.001),
        base_dir=tmp_path,
    )


def test_to_usage_markdown_contains_paths(tmp_path):
    m = _make_manifest(tmp_path)
    md = m.to_usage_markdown()
    assert str((tmp_path / "model.yaml").resolve()) in md
    assert "load_manifest" in md
    assert "FmuAdapter" in md


def test_to_usage_markdown_inputs_outputs(tmp_path):
    m = _make_manifest(tmp_path)
    md = m.to_usage_markdown()
    assert "| input | u | u |" in md
    assert "| input | v | v |" in md
    assert "| output | x | x |" in md
    assert "| output | y | y |" in md
    assert 'fmu.inputs["u"] = 1.0' in md
    assert "fmu.outputs['x']" in md


def test_to_usage_markdown_no_inputs(tmp_path):
    m = PhysicsManifest(
        name="NoInput",
        source=SourceSpec(type="fmu", file="x.fmu"),
        interface=InterfaceSpec(outputs={"y": "y"}),
        base_dir=tmp_path,
    )
    md = m.to_usage_markdown()
    assert "# no inputs defined" in md


def test_write_usage_readme_creates_file(tmp_path):
    m = _make_manifest(tmp_path)
    write_manifest(m, tmp_path / "model.yaml")
    out = write_usage_readme(m)
    assert out.exists()
    assert out.name == "README.md"
    content = out.read_text(encoding="utf-8")
    assert "TestModel" in content
    assert "MIT" in content
    assert "Co-Simulation FMU" in content


def test_roundtrip_via_load(tmp_path):
    m = _make_manifest(tmp_path)
    write_manifest(m, tmp_path / "model.yaml")
    loaded = load_manifest(tmp_path / "model.yaml")
    out = write_usage_readme(loaded)
    assert out.exists()
    assert "TestModel" in out.read_text(encoding="utf-8")
