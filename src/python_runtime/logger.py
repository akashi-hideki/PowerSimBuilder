"""CSV / matplotlib logger for SimulationData."""

from __future__ import annotations

import csv
from collections.abc import Iterable
from pathlib import Path
from typing import Any

_SECTIONS = ("inputs", "outputs", "parameters")

# ---------------------------------------------------------------------------
# CJK font configuration (Japanese + Simplified Chinese)
# ---------------------------------------------------------------------------

_FONT_CONFIGURED = False


def configure_japanese_font() -> str:
    """Configure matplotlib to render Japanese text.

    Tries, in order:

    1. Explicit override via environment variable ``PSB_JP_FONT``.
    2. Common Japanese fonts on Windows / macOS / Linux.
    3. Falls back to matplotlib default.

    Idempotent: calling it repeatedly has no extra cost.

    Returns
    -------
    str
        The font family name that was applied.
    """
    global _FONT_CONFIGURED
    if _FONT_CONFIGURED:
        import matplotlib

        family = matplotlib.rcParams.get("font.family", "sans-serif")
        if isinstance(family, list):
            return family[0] if family else "sans-serif"
        return family

    import os

    import matplotlib
    from matplotlib import font_manager

    candidates: list[str] = []
    # PSB_CJK_FONT is the preferred override; PSB_JP_FONT kept for backward
    # compatibility (both are checked).
    user_font = os.environ.get("PSB_CJK_FONT") or os.environ.get("PSB_JP_FONT")
    if user_font:
        candidates.append(user_font)

    candidates.extend(
        [
            # Japanese Windows (existing behavior: picked first on JP OS)
            "Yu Gothic",  # Windows 8.1+
            "Meiryo",  # Windows Vista+
            # Simplified Chinese Windows
            "Microsoft YaHei",  # Windows (Simplified Chinese)
            "Microsoft YaHei UI",  # Windows (Simplified Chinese variant)
            # Legacy / fallback CJK fonts on Windows
            "MS Gothic",  # Windows (legacy Japanese)
            "SimHei",  # Windows (Simplified Chinese fallback)
            "SimSun",  # Windows (Simplified Chinese fallback)
            # macOS
            "Hiragino Sans",
            "Hiragino Kaku Gothic ProN",  # macOS (older)
            # Linux
            "Noto Sans CJK JP",
            "Noto Sans CJK SC",  # Simplified Chinese
            "Noto Sans JP",
            "IPAGothic",  # Debian/Ubuntu
            "TakaoGothic",
        ]
    )

    available = {f.name for f in font_manager.fontManager.ttflist}
    chosen = next((n for n in candidates if n in available), None)

    if chosen is not None:
        # Use a fallback chain so mixed-language labels render correctly.
        # Example: a Japanese OS picks "Yu Gothic" first, but a Chinese
        # label like 电力系统 would be a tofu box because Yu Gothic lacks
        # Simplified Chinese glyphs. Listing every available CJK font in
        # font.sans-serif lets matplotlib fall back per-glyph.
        fallback_chain = [chosen] + [
            name for name in candidates if name != chosen and name in available
        ]
        # matplotlib 3.6+ enables per-glyph fallback only when font.family
        # is a list of concrete font names. Setting it to "sans-serif"
        # makes prop.get_family() return ["sans-serif"], which short-circuits
        # the fallback chain and leaves CJK glyphs as tofu boxes. Assigning
        # the list directly fixes the issue.
        matplotlib.rcParams["font.family"] = fallback_chain
        matplotlib.rcParams["font.sans-serif"] = fallback_chain
    # Always allow minus sign rendering with CJK fonts
    matplotlib.rcParams["axes.unicode_minus"] = False

    # Silence glyph warnings when Japanese glyphs are missing
    import warnings

    warnings.filterwarnings(
        "ignore",
        message=r"Glyph .* missing from font",
        category=UserWarning,
    )

    _FONT_CONFIGURED = True
    return chosen if chosen is not None else "sans-serif"


def collect_keys(logs: Iterable[dict[str, Any]]) -> list[str]:
    """ログ内に現れる全 `comp.section.port` パスを列挙する。"""
    keys: set[str] = set()
    for entry in logs:
        for cid, cdata in entry.get("components", {}).items():
            for section in _SECTIONS:
                for k in cdata.get(section, {}).keys():
                    keys.add(f"{cid}.{section}.{k}")
    return sorted(keys)


def dump_csv(
    sim_data: Any,
    path: str | Path,
    hz: float | None = None,
    columns: list[str] | None = None,
) -> Path:
    """SimulationData.logs を CSV に書き出す。

    Args:
        hz: サンプリング周波数。None / 0 以下 = 全行。例: 1.0 で 1 秒間隔
        columns: 出力する列名リスト（"time" を含む想定）。None = 全列
    """
    logs = getattr(sim_data, "logs", [])
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)

    keys = collect_keys(logs)
    all_fieldnames = ["time"] + keys
    if columns:
        missing = [c for c in columns if c not in all_fieldnames]
        if missing:
            print(f"[WARN] CSV columns not found (skipped): {missing}")
        fieldnames = [c for c in columns if c in all_fieldnames]
        if not fieldnames:
            fieldnames = ["time"]
    else:
        fieldnames = all_fieldnames

    interval = (1.0 / hz) if hz and hz > 0 else None
    last_t: float | None = None

    with path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        for entry in logs:
            if interval is not None:
                t = entry.get("time", 0.0)
                if last_t is not None and (t - last_t) < interval - 1e-9:
                    continue
                last_t = t
            row: dict[str, Any] = {"time": entry.get("time", 0.0)}
            for cid, cdata in entry.get("components", {}).items():
                for section in _SECTIONS:
                    for k, v in cdata.get(section, {}).items():
                        row[f"{cid}.{section}.{k}"] = v
            writer.writerow({k: row.get(k, "") for k in fieldnames})
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

    # Ensure Japanese labels render correctly
    configure_japanese_font()

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
