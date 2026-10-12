"""CSV / matplotlib logger for SimulationData."""

from __future__ import annotations

import csv
from collections.abc import Iterable
from pathlib import Path
from typing import Any

_SECTIONS = ("inputs", "outputs", "parameters")


def collect_keys(logs: Iterable[dict[str, Any]]) -> list[str]:
    """ログ内に現れる全 `comp.section.port` パスを列挙する。"""
    keys: set[str] = set()
    for entry in logs:
        for cid, cdata in entry.get("components", {}).items():
            for section in _SECTIONS:
                for k in cdata.get(section, {}).keys():
                    keys.add(f"{cid}.{section}.{k}")
    return sorted(keys)


def dump_csv(sim_data: Any, path: str | Path) -> Path:
    """SimulationData.logs を CSV に書き出す。"""
    logs = getattr(sim_data, "logs", [])
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)

    keys = collect_keys(logs)
    fieldnames = ["time"] + keys

    with path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        for entry in logs:
            row: dict[str, Any] = {"time": entry.get("time", 0.0)}
            for cid, cdata in entry.get("components", {}).items():
                for section in _SECTIONS:
                    for k, v in cdata.get(section, {}).items():
                        row[f"{cid}.{section}.{k}"] = v
            writer.writerow(row)
    return path


def plot(
    sim_data: Any,
    keys: list[str] | None = None,
    output_path: str | Path | None = None,
    show: bool = False,
) -> None:
    """数値出力を時系列でプロットする。"""
    import matplotlib

    if output_path is not None and not show:
        matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    logs = getattr(sim_data, "logs", [])
    if not logs:
        raise ValueError("no logs to plot")

    times = [e["time"] for e in logs]
    if keys is None:
        keys = collect_keys(logs)

    fig, ax = plt.subplots()
    for key in keys:
        parts = key.split(".", 2)
        if len(parts) != 3:
            continue
        cid, section, port = parts
        ys: list[float] = []
        ok = True
        for entry in logs:
            v = entry.get("components", {}).get(cid, {}).get(section, {}).get(port)
            if not isinstance(v, int | float | bool):
                ok = False
                break
            ys.append(float(v))
        if ok and len(ys) == len(times):
            ax.plot(times, ys, label=key)

    ax.set_xlabel("time [s]")
    ax.set_ylabel("value")
    if ax.lines:
        ax.legend()
    ax.grid(True)

    if output_path is not None:
        fig.savefig(output_path, dpi=120, bbox_inches="tight")
    if show:
        plt.show()
    plt.close(fig)
