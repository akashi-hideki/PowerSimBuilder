"""Tests for tools/fmu_catalog_builder.py."""

import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[3] / "tools"
if str(TOOLS) not in sys.path:
    sys.path.insert(0, str(TOOLS))

import fmu_catalog_builder as b  # noqa: E402


def test_parse_cross_check_path_ok():
    p = "fmus/2.0/cs/win64/Dymola/2019FD01/Rectifier/Rectifier.fmu"
    m = b._parse_cross_check_path(p)
    assert m == {
        "fmi_version": "2.0",
        "fmi_type": "cs",
        "platform": "win64",
        "tool_id": "Dymola",
        "tool_version": "2019FD01",
        "model_name": "Rectifier",
    }


def test_parse_cross_check_path_not_fmu():
    assert b._parse_cross_check_path("fmus/2.0/cs/win64/X/Y/Z/README.md") is None


def test_parse_cross_check_path_too_short():
    assert b._parse_cross_check_path("fmus/2.0/cs/win64/X.fmu") is None


def test_parse_cross_check_path_wrong_prefix():
    assert b._parse_cross_check_path("other/2.0/cs/win64/A/B/C/C.fmu") is None


def test_raw_url():
    url = b._raw_url("modelica", "fmi-cross-check", "master", "fmus/2.0/cs/win64/A/B/C/C.fmu")
    assert (
        url
        == "https://raw.githubusercontent.com/modelica/fmi-cross-check/master/fmus/2.0/cs/win64/A/B/C/C.fmu"
    )


def test_strip_meta_removes_underscore_keys():
    models = [{"name": "A", "url": "u", "_meta": {"x": 1}}]
    out = b._strip_meta(models)
    assert out == [{"name": "A", "url": "u"}]


def test_write_catalog_creates_file(tmp_path):
    out = tmp_path / "catalog.json"
    b.write_catalog(str(out), "test-source", [{"name": "A", "url": "u"}])
    import json

    data = json.loads(out.read_text(encoding="utf-8"))
    assert data["version"] == 1
    assert data["source"] == "test-source"
    assert len(data["models"]) == 1
    assert "generated_at" in data
