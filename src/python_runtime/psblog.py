"""psblog/v1 parser — see docs/SPEC_PSBLOG.md and docs/SPEC_PSBLOG_IMPORT.md.

Variable set is per-log (SPEC §1). The parser trusts the VarTable verbatim.
PSB never communicates: @source / @protocol are opaque strings.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path


@dataclass
class VarMeta:
    name: str
    type: str  # "uint8_t" / "float32" / "bool" / "raw"
    unit: str  # "-" は "" に正規化
    group: str
    description: str = ""
    role: str = ""  # SPEC §6.2, 未指定は ""


@dataclass
class SignalTrace:
    header: dict[str, str] = field(default_factory=dict)
    vars: dict[str, VarMeta] = field(default_factory=dict)
    rows: list[tuple[int, str, float | int | bool | str]] = field(default_factory=list)
    footer: dict[str, str] = field(default_factory=dict)
    warnings: int = 0
    source_files: list[Path] = field(default_factory=list)

    def series(self, var: str) -> list[tuple[float, float]]:
        """(t_sec, value) を到着順のまま返す。"""
        return [(t / 1000.0, float(v)) for (t, name, v) in self.rows if name == var]

    def captures_at(self) -> datetime:
        iso = self.header.get("captured_at", "")
        if not iso:
            raise ValueError("missing @captured_at")
        return datetime.fromisoformat(iso)


_TYPE_INT = {"uint8_t", "uint16_t", "uint32_t", "int8_t", "int16_t", "int32_t"}
_TYPE_FLOAT = {"float32", "float64", "float"}


def _read_lines(path: Path) -> list[str]:
    # Trap #45: encoding 明示
    return path.read_text(encoding="utf-8-sig").splitlines()


def _is_comment(line: str) -> bool:
    s = line.lstrip()
    return not s or s.startswith("#")


def _split_var_line(s: str) -> list[str]:
    """Split respecting "..." quoting (for <description>)."""
    result: list[str] = []
    buf = ""
    in_quote = False
    for ch in s:
        if ch == '"':
            in_quote = not in_quote
            buf += ch
        elif ch.isspace() and not in_quote:
            if buf:
                result.append(buf)
                buf = ""
        else:
            buf += ch
    if buf:
        result.append(buf)
    return result


def _parse_header(lines: list[str], trace: SignalTrace) -> int:
    """@key value 行を格納。戻り値 = VarTable 開始 index."""
    for i in range(len(lines)):
        line = lines[i]
        if _is_comment(line):
            continue
        s = line.strip()
        if not s.startswith("@"):
            break
        if s.startswith("@var "):
            break
        parts = s[1:].split(None, 1)
        if not parts:
            continue
        key = parts[0]
        if key.isdigit():
            break
        trace.header[key] = parts[1].strip() if len(parts) > 1 else ""
    else:
        i = len(lines)
    return i


def _parse_vartable(lines: list[str], trace: SignalTrace, start: int) -> int:
    i = start
    for i in range(start, len(lines)):
        line = lines[i]
        if _is_comment(line):
            continue
        s = line.strip()
        if not s.startswith("@var "):
            break
        tokens = _split_var_line(s[5:])
        if len(tokens) < 4:
            trace.warnings += 1
            continue
        name, type_, unit_raw, group = tokens[0], tokens[1], tokens[2], tokens[3]
        unit = "" if unit_raw == "-" else unit_raw
        desc = ""
        role = ""
        for tok in tokens[4:]:
            if tok.startswith("role="):
                role = tok[5:]
            else:
                desc = tok.strip('"')
        trace.vars[name] = VarMeta(
            name=name,
            type=type_,
            unit=unit,
            group=group,
            description=desc,
            role=role,
        )
    else:
        i = len(lines)
    return i


def _coerce_value(raw: str, meta: VarMeta) -> int | float | bool | str | None:
    t = meta.type
    if t in _TYPE_INT:
        try:
            return int(raw)
        except ValueError:
            return None
    if t in _TYPE_FLOAT:
        try:
            return float(raw)
        except ValueError:
            return None
    if t == "bool":
        try:
            return bool(int(raw))
        except ValueError:
            return None
    if t == "raw":
        return raw
    # 未知型: int → float → str
    for conv in (int, float):
        try:
            return conv(raw)
        except ValueError:
            pass
    return raw


def _parse_data_line(line: str, trace: SignalTrace) -> None:
    s = line.strip()
    if not s.startswith("@"):
        trace.warnings += 1
        return
    parts = s[1:].split(None, 1)
    if not parts or not parts[0].isdigit():
        trace.warnings += 1
        return
    t_ms = int(parts[0])
    if len(parts) < 2:
        return
    for kv in parts[1].split():
        if "=" not in kv:
            trace.warnings += 1
            continue
        name, _, raw = kv.partition("=")
        meta = trace.vars.get(name)
        if meta is None:
            trace.warnings += 1
            continue
        val = _coerce_value(raw, meta)
        if val is None:
            trace.warnings += 1
            continue
        trace.rows.append((t_ms, name, val))


def _parse_footer(lines: list[str], trace: SignalTrace, start: int) -> None:
    for line in lines[start:]:
        if _is_comment(line):
            continue
        s = line.strip()
        if not s.startswith("@"):
            continue
        parts = s[1:].split(None, 1)
        if not parts:
            continue
        key = parts[0]
        if key in ("end", "samples", "dropped"):
            trace.footer[key] = parts[1].strip() if len(parts) > 1 else ""


def parse_psblog(path: str | Path) -> SignalTrace:
    p = Path(path)
    if not p.is_file():
        raise FileNotFoundError(f"psblog file not found: {p}")
    lines = _read_lines(p)
    trace = SignalTrace()
    trace.source_files.append(p)

    i = _parse_header(lines, trace)
    schema = trace.header.get("schema", "")
    if schema != "psblog/v1":
        raise ValueError(f"unsupported schema: {schema!r}")
    if "captured_at" not in trace.header:
        raise ValueError("missing @captured_at")

    i = _parse_vartable(lines, trace, i)

    j = i
    for j in range(i, len(lines)):
        line = lines[j]
        if _is_comment(line):
            continue
        s = line.strip()
        if s.startswith("@end") or s.startswith("@samples") or s.startswith("@dropped"):
            break
        if not s.startswith("@"):
            trace.warnings += 1
            continue
        first = s[1:].split(None, 1)
        if first and first[0].isdigit():
            _parse_data_line(line, trace)
        elif first and first[0].startswith("@"):
            trace.warnings += 1
        # 未知 @meta は無視（SPEC §10.1）
    else:
        j = len(lines)

    _parse_footer(lines, trace, j)
    return trace


def parse_psblog_glob(base: str | Path) -> SignalTrace:
    """Parse <base>.psblog and <base>.N.psblog (SPEC §4.3).

    Files are sorted by @captured_at; later files' t_ms are offset by
    the elapsed time from the first file's @captured_at. Rotation
    boundary sample loss is allowed by SPEC §4.3.
    """
    p = Path(base)
    parent = p.parent
    stem = p.stem
    pattern = f"{stem}*.psblog"
    files = sorted(parent.glob(pattern))
    if not files:
        raise FileNotFoundError(f"no psblog files matching {parent / pattern}")

    traces = [parse_psblog(f) for f in files]
    traces.sort(key=lambda t: t.captures_at())
    base_dt = traces[0].captures_at()

    merged = SignalTrace()
    merged.header = traces[0].header.copy()
    merged.vars = traces[0].vars.copy()
    merged.source_files = [t.source_files[0] for t in traces]

    for trace in traces:
        offset_ms = int((trace.captures_at() - base_dt).total_seconds() * 1000)
        for t_ms, name, val in trace.rows:
            merged.rows.append((t_ms + offset_ms, name, val))
        merged.warnings += trace.warnings

    total_dropped = sum(int(t.footer.get("dropped", 0) or 0) for t in traces)
    merged.footer["dropped"] = str(total_dropped)
    merged.footer["samples"] = str(len(merged.rows))
    if traces[-1].footer.get("end"):
        merged.footer["end"] = traces[-1].footer["end"]

    return merged


def to_json(
    trace: SignalTrace,
    out_path: str | Path,
    include_classifications: bool = False,
) -> None:
    """Write SignalTrace as JSON (for C# subprocess consumption, Trap #49)."""
    import json

    data = {
        "header": trace.header,
        "vars": {
            name: {
                "name": m.name,
                "type": m.type,
                "unit": m.unit,
                "group": m.group,
                "description": m.description,
                "role": m.role,
            }
            for name, m in trace.vars.items()
        },
        "rows": [[t, name, val] for t, name, val in trace.rows],
        "footer": trace.footer,
        "warnings": trace.warnings,
        "source_files": [str(p) for p in trace.source_files],
    }
    if include_classifications:
        from python_runtime.analysis.signal_classifier import classify_all

        data["classifications"] = [
            {
                "var_name": c.var_name,
                "category": c.category,
                "confidence": c.confidence,
                "proposed_component": c.proposed_component,
                "reasoning": c.reasoning,
            }
            for c in classify_all(trace)
        ]
    Path(out_path).write_text(
        json.dumps(data, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )


def main(argv: list[str] | None = None) -> int:
    """CLI entry point. Writes JSON via --json-out (Trap #49)."""
    import argparse
    import sys

    parser = argparse.ArgumentParser(prog="python_runtime.psblog")
    parser.add_argument("path", help=".psblog file or base path")
    parser.add_argument(
        "--json-out",
        default=None,
        help="Write SignalTrace JSON to this path (recommended; Trap #49)",
    )
    parser.add_argument(
        "--glob",
        action="store_true",
        help="Treat <path> as base for rotated files (SPEC §4.3)",
    )
    parser.add_argument(
        "--classify",
        action="store_true",
        help="Include waveform classifications (signal_classifier)",
    )
    args = parser.parse_args(argv)

    try:
        if args.glob:
            trace = parse_psblog_glob(args.path)
        else:
            trace = parse_psblog(args.path)
    except (FileNotFoundError, ValueError) as e:
        print(f"error: {e}", file=sys.stderr)
        return 1

    if args.json_out:
        to_json(trace, args.json_out, include_classifications=args.classify)
    else:
        print(f"vars={len(trace.vars)} rows={len(trace.rows)} " f"warnings={trace.warnings}")
    return 0


if __name__ == "__main__":
    import sys

    sys.exit(main())
