"""Tests for tools/build_cross_check_catalog.py."""

import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[3] / "tools"
if str(TOOLS) not in sys.path:
    sys.path.insert(0, str(TOOLS))

import build_cross_check_catalog as b  # noqa: E402


def test_build_catalog_parses_fmu_paths():
    paths = [
        "fmus/2.0/cs/win64/Dymola/2019/Rectifier/Rectifier.fmu",
        "fmus/1.0/cs/c-code/Test-FMUs/0.0.2/Dahlquist/Dahlquist.fmu",
        "README.md",
    ]
    models = b.build_catalog(paths, ["win64", "c-code"], 0)
    assert len(models) == 2
    assert {m["name"] for m in models} == {"Rectifier", "Dahlquist"}


def test_build_catalog_filters_platform():
    paths = [
        "fmus/2.0/cs/win64/Dymola/2019/Rectifier/Rectifier.fmu",
        "fmus/2.0/cs/c-code/Dymola/2019/Dahlquist/Dahlquist.fmu",
    ]
    models = b.build_catalog(paths, ["win64"], 0)
    assert len(models) == 1
    assert models[0]["name"] == "Rectifier"


def test_build_catalog_dedups_by_model_tool_version():
    paths = [
        "fmus/2.0/cs/win64/Dymola/2019/Rectifier/Rectifier.fmu",
        "fmus/2.0/me/win64/Dymola/2019/Rectifier/Rectifier.fmu",
    ]
    models = b.build_catalog(paths, ["win64"], 0)
    assert len(models) == 1


def test_build_catalog_max_models():
    paths = [f"fmus/2.0/cs/win64/Dymola/2019/M{i}/M{i}.fmu" for i in range(10)]
    models = b.build_catalog(paths, ["win64"], 3)
    assert len(models) == 3


def test_build_catalog_skips_short_paths():
    assert b.build_catalog(["fmus/2.0/Rectifier.fmu"], ["win64"], 0) == []


def test_build_catalog_url_format():
    paths = ["fmus/2.0/cs/win64/Dymola/2019/Rectifier/Rectifier.fmu"]
    models = b.build_catalog(paths, ["win64"], 0)
    assert models[0]["url"].startswith(
        "https://raw.githubusercontent.com/modelica/fmi-cross-check/master/fmus/"
    )
    assert models[0]["url"].endswith(".fmu")


def test_build_catalog_prioritizes_fmi2_over_fmi1():
    paths = [
        "fmus/1.0/cs/win64/Dymola/2019/A/A.fmu",
        "fmus/2.0/cs/win64/Dymola/2019/B/B.fmu",
    ]
    models = b.build_catalog(paths, ["win64"], 0)
    assert models[0]["name"] == "B"


def test_build_catalog_prioritizes_win64_over_c_code():
    paths = [
        "fmus/2.0/cs/c-code/Dymola/2019/A/A.fmu",
        "fmus/2.0/cs/win64/Dymola/2019/B/B.fmu",
    ]
    models = b.build_catalog(paths, ["win64", "c-code"], 0)
    assert models[0]["name"] == "B"


def test_sort_key_handles_short_paths():
    assert b._sort_key("fmus/2.0/X.fmu")[0] == 9
