"""Downsample and column-select a simulation CSV for QA documentation."""
import csv
import sys
from pathlib import Path

KEY_COLUMNS = [
    "time",
    "ovenchamber.outputs.oven_current_temp",
    "ovenchamber.outputs.thermistor_top",
    "ovenchamber.outputs.thermistor_middle",
    "ovenchamber.outputs.thermistor_bottom",
    "ovenchamber.inputs.heater_top_duty",
    "ovenchamber.inputs.heater_bottom_duty",
    "ovenchamber.outputs.heater_power",
    "pid controller.inputs.setpoint",
    "pid controller.inputs.measured",
    "pid controller.outputs.control",
    "controller box.outputs.heater_top_duty",
    "controller box.outputs.error_code",
]


def main() -> int:
    if len(sys.argv) < 3:
        print("usage: downsample_csv.py <src.csv> <dst.csv> [hz=1.0]", file=sys.stderr)
        return 2
    src = Path(sys.argv[1])
    dst = Path(sys.argv[2])
    target_hz = float(sys.argv[3]) if len(sys.argv) > 3 else 1.0
    interval = 1.0 / target_hz

    with src.open("r", encoding="utf-8", newline="") as fin:
        reader = csv.DictReader(fin)
        available = [c for c in KEY_COLUMNS if c in reader.fieldnames]
        missing = [c for c in KEY_COLUMNS if c not in reader.fieldnames]
        if missing:
            print(f"[WARN] Missing columns (skipped): {missing}", file=sys.stderr)

        dst.parent.mkdir(parents=True, exist_ok=True)
        with dst.open("w", encoding="utf-8", newline="") as fout:
            writer = csv.DictWriter(fout, fieldnames=available)
            writer.writeheader()
            last_t = None
            n = 0
            for row in reader:
                try:
                    t = float(row["time"])
                except (ValueError, KeyError):
                    continue
                if last_t is None or (t - last_t) >= interval - 1e-9:
                    writer.writerow({c: row.get(c, "") for c in available})
                    last_t = t
                    n += 1

    print(f"[OK] {n} rows -> {dst}  (target {target_hz} Hz, {len(available)} columns)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
