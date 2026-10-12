"""logger モジュールの単体テスト。"""

import csv

import pytest

from python_runtime.component_base import Component
from python_runtime.logger import collect_keys, dump_csv, plot
from python_runtime.simulation_data import SimulationData


def _make_sim(n=3):
    sd = SimulationData(time_step=0.1)
    c1 = Component("c1", "DigitalInput")
    c2 = Component("c2", "DigitalOutput")
    for i in range(n):
        c1.inputs["state"] = i % 2
        c1.outputs["edge_rising"] = i == 1
        c2.inputs["command"] = i % 2
        c2.outputs["state"] = i % 2
        sd.log(i * 0.1, {"c1": c1, "c2": c2})
    return sd


# --- collect_keys ---


def test_collect_keys_empty():
    assert collect_keys([]) == []


def test_collect_keys_sorted_and_unique():
    sd = _make_sim()
    keys = collect_keys(sd.logs)
    assert keys == sorted(keys)
    assert "c1.inputs.state" in keys
    assert "c1.outputs.edge_rising" in keys
    assert "c2.inputs.command" in keys
    assert "c2.outputs.state" in keys


# --- dump_csv ---


def test_dump_csv_empty_logs_creates_header_only(tmp_path):
    sd = SimulationData()
    out = tmp_path / "empty.csv"
    dump_csv(sd, out)
    assert out.exists()
    lines = out.read_text(encoding="utf-8").splitlines()
    assert lines == ["time"]


def test_dump_csv_header_and_rows(tmp_path):
    sd = _make_sim(n=3)
    out = tmp_path / "out.csv"
    dump_csv(sd, out)

    with out.open(newline="", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        rows = list(reader)

    assert reader.fieldnames is not None
    assert reader.fieldnames[0] == "time"
    assert "c1.inputs.state" in reader.fieldnames
    assert "c2.outputs.state" in reader.fieldnames
    assert len(rows) == 3
    assert rows[0]["time"] == "0.0"
    assert rows[0]["c1.inputs.state"] == "0"
    assert rows[1]["c1.inputs.state"] == "1"


def test_dump_csv_creates_parent_directory(tmp_path):
    sd = _make_sim(n=1)
    out = tmp_path / "nested" / "dir" / "out.csv"
    dump_csv(sd, out)
    assert out.exists()


# --- plot ---


def test_plot_raises_on_empty_logs(tmp_path):
    sd = SimulationData()
    with pytest.raises(ValueError):
        plot(sd, output_path=tmp_path / "nope.png")


def test_plot_writes_png(tmp_path):
    sd = _make_sim(n=5)
    out = tmp_path / "plot.png"
    plot(sd, keys=["c1.inputs.state", "c2.outputs.state"], output_path=out)
    assert out.exists()
    assert out.stat().st_size > 0
