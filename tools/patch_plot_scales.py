#!/usr/bin/env python3
"""
patch_plot_scales.py - generated simulation graph: one y-scale per variable + 1.5x wider figure.

Edits SimulationGenerate/PythonSimulationMainBuilder.cs (the C# code that emits demo_main.py):
  * 'figure_size' (12, 8) -> (18, 8)       width x 1.5; 8 is now the MINIMUM height
  * new global_settings: 'layout': 'stacked' | 'overlay', 'row_height', 'max_height'
  * plot_signals(): 'stacked' (default) = one subplot per signal, shared time axis, each with its OWN y-scale
                    'overlay'           = previous behaviour (all signals on one y-axis)
  * optional per-signal fixed range: add  'ylim': (min, max)  to an entry of plot_config['signals']

    python tools\\patch_plot_scales.py --dry-run
    python tools\\patch_plot_scales.py

Idempotent, atomic, backup in tmp\\patch_backup_plot_<time>\\, BOM / line endings preserved.
Only NEWLY generated code changes; code already stored in a project keeps the old plot until regenerated.
"""
import argparse, datetime, re, shutil, sys
from pathlib import Path

REL = "SimulationGenerate/PythonSimulationMainBuilder.cs"
IND = " " * 12

NEW_PLOT_BODY = '''\
    # グローバル設定の適用
    settings = plot_config['global_settings']

    # 有効な信号のみをフィルタリング
    enabled_signals = [s for s in plot_config['signals'] if s['enabled']]

    # 各信号のデータを収集
    series = []
    for signal in enabled_signals:
        comp_name = signal['component']
        port_name = signal['port']

        # 時間と値を格納するリスト
        times = []
        values = []

        # 各時間ステップのデータを収集
        for log in sim_data.logs:
            time = log['time']
            comp_data = log['components'].get(comp_name, {})
            outputs = comp_data.get('outputs', {})
            value = outputs.get(port_name, 0)

            times.append(time)
            values.append(value)

        if times and values:
            series.append((signal, times, values))

    layout = settings.get('layout', 'stacked')
    width, base_height = settings['figure_size']

    if layout == 'stacked' and series:
        # 変数ごとに独立した縦軸（時間軸は共有）
        n = len(series)
        height = min(max(base_height, n * settings.get('row_height', 1.8)), settings.get('max_height', 30))
        fig, axes = plt.subplots(n, 1, figsize=(width, height), sharex=True, squeeze=False)
        for ax, (signal, times, values) in zip(axes[:, 0], series):
            color = signal.get('color', None)
            line_style = signal.get('line_style', '-')
            ax.plot(times, values, color=color, linestyle=line_style, linewidth=2)
            ax.set_title(signal['name'], loc='left', fontsize=10)
            if signal.get('ylim'):
                ax.set_ylim(*signal['ylim'])
            if settings['grid']:
                ax.grid(True, linestyle='--', alpha=0.7)
        fig.suptitle(settings['title'])
        axes[-1, 0].set_xlabel(settings['xlabel'])
        fig.tight_layout(rect=(0, 0, 1, 0.97))
    else:
        # 従来表示: すべての信号を1つの縦軸に重ねる
        plt.figure(figsize=(width, base_height))
        plt.title(settings['title'])
        plt.xlabel(settings['xlabel'])
        plt.ylabel(settings['ylabel'])
        if settings['grid']:
            plt.grid(True, linestyle='--', alpha=0.7)
        for signal, times, values in series:
            color = signal.get('color', None)
            line_style = signal.get('line_style', '-')
            plt.plot(times, values, label=signal['name'], color=color, linestyle=line_style, linewidth=2)
        if series:
            plt.legend(loc=settings['legend_location'])
        plt.tight_layout()'''

SETTINGS_PY = [
    "        'figure_size': (18, 8),  # 幅 x 最小高さ（インチ）。幅は従来(12)の1.5倍",
    "        'layout': 'stacked',  # 'stacked'=変数ごとに縦軸 / 'overlay'=1つの縦軸に重ねる",
    "        'row_height': 1.8,  # stacked時の1変数あたりの高さ（インチ）",
    "        'max_height': 30,  # stacked時の図の最大高さ（インチ）",
]


def cs_line(section, text):
    esc = text.replace("\\", "\\\\").replace('"', '\\"')
    return f'{IND}AddLineToSection("{section}", "{esc}");'


def read(p):
    raw = p.read_bytes()
    bom = raw.startswith(b"\xef\xbb\xbf")
    t = raw.decode("utf-8-sig")
    crlf = "\r\n" in t
    return t.replace("\r\n", "\n"), bom, crlf


def write(p, t, bom, crlf):
    if crlf:
        t = t.replace("\n", "\r\n")
    p.write_bytes((b"\xef\xbb\xbf" if bom else b"") + t.encode("utf-8"))


def apply(t):
    steps = []
    # 1. settings
    if "'layout': 'stacked'" in t:
        steps.append(("SKIP", "global_settings (figure_size / layout)"))
    else:
        old = cs_line("plot_config", "        'figure_size': (12, 8),")
        if t.count(old) != 1:
            raise SystemExit(f"FAIL: figure_size line found {t.count(old)} time(s), expected 1")
        t = t.replace(old, "\n".join(cs_line("plot_config", s) for s in SETTINGS_PY))
        steps.append(("APPLY", "global_settings (figure_size 12 -> 18, layout, row_height, max_height)"))
    # 2. plot_signals body
    if "series.append((signal, times, values))" in t:
        steps.append(("SKIP", "plot_signals body"))
    else:
        start = cs_line("plotting_functions", "    # グローバル設定の適用")
        end = cs_line("plotting_functions", "    plt.tight_layout()")
        i, j = t.find(start), t.find(end)
        if t.count(start) != 1 or t.count(end) != 1 or not (0 <= i < j):
            raise SystemExit("FAIL: plot_signals anchors not found exactly once (file differs from the expected version)")
        j += len(end)
        body = "\n".join(cs_line("plotting_functions", ln) if ln else cs_line("plotting_functions", "")
                         for ln in NEW_PLOT_BODY.split("\n"))
        t = t[:i] + body + t[j:]
        steps.append(("APPLY", "plot_signals body (stacked / overlay)"))
    return t, steps


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--root", default=None)
    ap.add_argument("--dry-run", action="store_true")
    a = ap.parse_args()
    root = Path(a.root).resolve() if a.root else Path(__file__).resolve().parent.parent
    p = root / REL
    if not p.exists():
        print(f"FAIL: {REL} not found"); return 1
    t, bom, crlf = read(p)
    new, steps = apply(t)
    print(f"Root: {root}\n")
    for st, label in steps:
        print(f"  [{st:5}] {REL}  ({label})")
    if new == t:
        print("\nNothing to change."); return 0
    if a.dry_run:
        print("\nDRY RUN: 1 file would change."); return 0
    bdir = root / "tmp" / f"patch_backup_plot_{datetime.datetime.now():%Y%m%d_%H%M%S}" / "SimulationGenerate"
    bdir.mkdir(parents=True, exist_ok=True)
    shutil.copy2(p, bdir / p.name)
    write(p, new, bom, crlf)
    chk, _, _ = read(p)
    if "'layout': 'stacked'" not in chk or "series.append((signal, times, values))" not in chk:
        print("VERIFY FAILED"); return 2
    print(f"\nBackup: {bdir}\nDone.\nNext: dotnet build -c Debug ; dotnet test PowerSimBuilder.Tests\\PowerSimBuilder.Tests.csproj")
    return 0


if __name__ == "__main__":
    sys.exit(main())
