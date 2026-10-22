"""Physics model manifest (`model.yaml`) schema, parser, writer."""

from __future__ import annotations

from dataclasses import asdict, dataclass, field
from pathlib import Path
from typing import Any

import yaml  # type: ignore[import-untyped]

MANIFEST_FILENAME = "model.yaml"
SUPPORTED_SOURCE_TYPES = ("fmu",)
SUPPORTED_FMI_TYPES = ("CoSimulation", "ModelExchange")


class ManifestError(ValueError):
    """model.yaml is missing required fields or is malformed."""


@dataclass
class SourceSpec:
    type: str
    file: str
    license: str | None = None
    source_url: str | None = None


@dataclass
class InterfaceSpec:
    fmi_type: str = "CoSimulation"
    inputs: dict[str, str] = field(default_factory=dict)
    outputs: dict[str, str] = field(default_factory=dict)
    parameters: dict[str, float] = field(default_factory=dict)


@dataclass
class TimingSpec:
    mode: str = "fixed_step"
    dt_internal: float = 0.001


@dataclass
class PhysicsManifest:
    name: str
    source: SourceSpec
    interface: InterfaceSpec
    units: dict[str, str] = field(default_factory=dict)
    timing: TimingSpec = field(default_factory=TimingSpec)
    base_dir: Path | None = field(default=None, compare=False)

    def resolve_source_path(self) -> Path:
        if self.base_dir is None:
            raise ManifestError("base_dir is not set (internal error)")
        return (self.base_dir / self.source.file).resolve()

    def to_usage_markdown(self, model_dir: str | Path | None = None) -> str:
        """Generate README.md content explaining how to use this model."""
        base = Path(model_dir) if model_dir else self.base_dir
        yaml_path = str((base / MANIFEST_FILENAME).resolve()) if base else MANIFEST_FILENAME

        input_rows = (
            "\n".join(
                f"| input | {port} | {var} | - |" for port, var in self.interface.inputs.items()
            )
            or "| _(none)_ | | | |"
        )
        output_rows = (
            "\n".join(
                f"| output | {port} | {var} | - |" for port, var in self.interface.outputs.items()
            )
            or "| _(none)_ | | | |"
        )

        first_input = next(iter(self.interface.inputs), None)
        first_output = next(iter(self.interface.outputs), None)

        if first_input:
            input_line = f'fmu.inputs["{first_input}"] = 1.0'
        else:
            input_line = "# no inputs defined"

        if first_output:
            output_line = f'print(f"{first_output} = "' f" + str(fmu.outputs['{first_output}']))"
        else:
            output_line = 'print("no outputs defined")'

        license_str = self.source.license or "(unspecified)"
        url_str = self.source.source_url or "(none)"

        return f"""# {self.name} (Physics Model)

Co-Simulation FMU imported via PowerSimBuilder's "Import Physics Model" dialog.

## Quick start (Python)

```python
from python_runtime.physics import load_manifest, FmuAdapter

# 1. Load the model manifest
manifest = load_manifest(r"{yaml_path}")

# 2. Instantiate the FMU adapter
fmu = FmuAdapter(manifest)

# 3. Set an input (if any)
{input_line}

# 4. Step forward 10 ms
fmu.step(0.01)

# 5. Read outputs
{output_line}

# 6. Cleanup
fmu.close()
```

## Interface

| Direction | Port | FMU variable | Unit |
|-----------|------|--------------|------|
{input_rows}
{output_rows}

## Timing
- Mode: `{self.timing.mode}`
- Internal dt: `{self.timing.dt_internal}` s

## Source
- File: `{self.source.file}`
- License: {license_str}
- URL: {url_str}

## Files in this directory
- `{MANIFEST_FILENAME}` - manifest (edit to remap ports/parameters)
- `{self.source.file}` - the FMU archive
- `README.md` - this file

## Regenerate this README

After editing `{MANIFEST_FILENAME}`, run:

```powershell
python -m python_runtime.physics.manifest "{yaml_path}" --write-readme
```
"""

    def to_dict(self) -> dict[str, Any]:
        src = {k: v for k, v in asdict(self.source).items() if v is not None}
        return {
            "name": self.name,
            "source": src,
            "interface": {
                "fmi_type": self.interface.fmi_type,
                "inputs": dict(self.interface.inputs),
                "outputs": dict(self.interface.outputs),
                "parameters": dict(self.interface.parameters),
            },
            "units": dict(self.units),
            "timing": asdict(self.timing),
        }


def load_manifest(path: str | Path) -> PhysicsManifest:
    p = Path(path)
    if not p.exists():
        raise ManifestError(f"manifest not found: {p}")
    try:
        raw = yaml.safe_load(p.read_text(encoding="utf-8"))
    except yaml.YAMLError as exc:
        raise ManifestError(f"invalid YAML in {p}: {exc}") from exc
    if not isinstance(raw, dict):
        raise ManifestError(f"manifest root must be a mapping: {p}")
    name = raw.get("name")
    if not isinstance(name, str) or not name.strip():
        raise ManifestError(f"'name' is required: {p}")
    src_raw = raw.get("source")
    if not isinstance(src_raw, dict):
        raise ManifestError(f"'source' section is required: {p}")
    src_type = src_raw.get("type")
    if src_type not in SUPPORTED_SOURCE_TYPES:
        raise ManifestError(f"unsupported source.type {src_type!r}")
    src_file = src_raw.get("file")
    if not isinstance(src_file, str) or not src_file:
        raise ManifestError(f"'source.file' is required: {p}")
    iface_raw = raw.get("interface") or {}
    if not isinstance(iface_raw, dict):
        raise ManifestError(f"'interface' must be a mapping: {p}")
    fmi_type = iface_raw.get("fmi_type", "CoSimulation")
    if fmi_type not in SUPPORTED_FMI_TYPES:
        raise ManifestError(f"unsupported interface.fmi_type {fmi_type!r}")
    inputs = _coerce_str_map(iface_raw.get("inputs"), "interface.inputs", p)
    outputs = _coerce_str_map(iface_raw.get("outputs"), "interface.outputs", p)
    params_raw = iface_raw.get("parameters") or {}
    if not isinstance(params_raw, dict):
        raise ManifestError(f"'interface.parameters' must be a mapping: {p}")
    parameters = {str(k): float(v) for k, v in params_raw.items()}
    units_raw = raw.get("units") or {}
    if not isinstance(units_raw, dict):
        raise ManifestError(f"'units' must be a mapping: {p}")
    units = {str(k): str(v) for k, v in units_raw.items()}
    timing_raw = raw.get("timing") or {}
    if not isinstance(timing_raw, dict):
        raise ManifestError(f"'timing' must be a mapping: {p}")
    timing = TimingSpec(
        mode=str(timing_raw.get("mode", "fixed_step")),
        dt_internal=float(timing_raw.get("dt_internal", 0.001)),
    )
    return PhysicsManifest(
        name=name.strip(),
        source=SourceSpec(
            type=src_type,
            file=src_file,
            license=src_raw.get("license"),
            source_url=src_raw.get("source_url"),
        ),
        interface=InterfaceSpec(
            fmi_type=fmi_type,
            inputs=inputs,
            outputs=outputs,
            parameters=parameters,
        ),
        units=units,
        timing=timing,
        base_dir=p.parent.resolve(),
    )


def write_manifest(manifest: PhysicsManifest, out_path: str | Path) -> Path:
    target = Path(out_path)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(
        yaml.safe_dump(
            manifest.to_dict(),
            sort_keys=False,
            allow_unicode=True,
            default_flow_style=False,
        ),
        encoding="utf-8",
    )
    return target


def write_usage_readme(
    manifest: PhysicsManifest,
    model_dir: str | Path | None = None,
) -> Path:
    """Write README.md into the model directory. Returns the written path."""
    target_dir = Path(model_dir) if model_dir else manifest.base_dir
    if target_dir is None:
        raise ManifestError("model_dir is required when base_dir is not set")
    readme_path = target_dir / "README.md"
    readme_path.write_text(
        manifest.to_usage_markdown(model_dir=target_dir),
        encoding="utf-8",
    )
    return readme_path


def _coerce_str_map(v: Any, field_name: str, src: Path) -> dict[str, str]:
    if v is None:
        return {}
    if not isinstance(v, dict):
        raise ManifestError(f"'{field_name}' must be a mapping: {src}")
    out: dict[str, str] = {}
    for k, val in v.items():
        if not isinstance(val, str) or not val:
            raise ManifestError(f"'{field_name}.{k}' must be a non-empty string: {src}")
        out[str(k)] = val
    return out


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Physics manifest utility")
    parser.add_argument("manifest_path", help="Path to model.yaml")
    parser.add_argument(
        "--write-readme",
        action="store_true",
        help="Write README.md into the model directory",
    )
    args = parser.parse_args()

    _m = load_manifest(args.manifest_path)
    if args.write_readme:
        _out = write_usage_readme(_m)
        print(f"Wrote {_out}")
    else:
        print(_m.to_usage_markdown())
