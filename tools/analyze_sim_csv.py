"""Compute control metrics (ss, err, peak, ovs%, settle, IAE) from dumped CSV.

Usage:
    python tools/analyze_sim_csv.py tmp/sim_signals.csv
    python tools/analyze_sim_csv.py tmp/sim_signals.csv --target "ovenchamber.oven_current_temp=200"
"""
from __future__ import annotations
import csv, sys
from pathlib import Path


def metrics(values, target, dt):
    n = len(values)
    tail = values[int(n * 0.9):]
    ss = sum(tail) / len(tail)
    peak = max(values)
    ovs = max(0.0, (peak - target) / abs(target) * 100.0) if target else 0.0
    band = 0.02 * abs(target) if target else 0.01
    settle = -1
    for i in range(n - 1, -1, -1):
        if abs(values[i] - target) > band:
            settle = i
            break
    iae = sum(abs(target - v) for v in values) * dt
    return ss, target - ss, peak, ovs, (settle * dt if settle >= 0 else -1), iae


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(2)
    path = Path(sys.argv[1])
    targets = {}
    a = sys.argv[2:]
    i = 0
    while i < len(a):
        if a[i] == "--target":
            k, v = a[i + 1].split("=", 1)
            targets[k] = float(v)
            i += 2
        else:
            i += 1

    with path.open(encoding="utf-8") as f:
        r = csv.reader(f)
        header = next(r)
        rows = list(r)
    times = [float(row[0]) for row in rows]
    dt = times[1] - times[0] if len(times) > 1 else 0.001

    print(f"file: {path}")
    print(f"rows: {len(rows)}, dt: {dt:.6f}, duration: {times[-1]:.2f}s")
    print(f"\n{'signal':<45}{'ss':>10}{'err':>9}{'peak':>10}{'ovs%':>8}{'settle':>9}{'IAE':>12}")
    print("-" * 103)
    for j, name in enumerate(header[1:], start=1):
        try:
            vals = [float(row[j]) for row in rows]
        except (ValueError, IndexError):
            continue
        tgt = targets.get(name, vals[-1])
        ss, err, peak, ovs, settle, iae = metrics(vals, tgt, dt)
        print(f"{name:<45}{ss:>10.3f}{err:>9.3f}{peak:>10.3f}{ovs:>8.2f}{settle:>9.1f}{iae:>12.1f}")


if __name__ == "__main__":
    main()