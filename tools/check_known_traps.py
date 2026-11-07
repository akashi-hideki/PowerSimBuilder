"""Detect known traps before build.

Trap #81: duplicate public non-partial class in *Test*.cs files under
          PowerSimBuilder.Tests/ (CS0101 + CS0111)
Trap #82: <ItemGroup> nested inside <ItemGroup> in *.csproj (MSB4232)

Exit 0 = clean
Exit 1 = at least one violation
"""
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def find_duplicate_test_classes(tests_dir: Path):
    """Trap #81: same public (non-partial) class in 2+ *Test*.cs files."""
    class_to_files: dict[str, list[Path]] = {}
    for cs_file in tests_dir.rglob("*Test*.cs"):
        try:
            text = cs_file.read_text(encoding="utf-8", errors="replace")
        except Exception:
            continue
        for m in re.finditer(
            r"^\s*public\s+(?:sealed\s+)?(?:partial\s+)?class\s+(\w+)",
            text,
            re.MULTILINE,
        ):
            is_partial = "partial" in m.group(0)
            if is_partial:
                continue  # legitimate partial class split
            class_to_files.setdefault(m.group(1), []).append(cs_file)

    return [
        (name, files)
        for name, files in class_to_files.items()
        if len(files) > 1
    ]


def find_nested_itemgroup(csproj: Path):
    """Trap #82: <ItemGroup> inside <ItemGroup>."""
    try:
        tree = ET.parse(csproj)
    except ET.ParseError as e:
        return [f"{csproj}: XML parse error: {e}"]

    violations: list[str] = []

    def walk(elem, parent_tag: str, path: str):
        tag = elem.tag.split("}")[-1]  # strip xmlns
        if tag == "ItemGroup" and parent_tag == "ItemGroup":
            violations.append(f"{csproj}: ItemGroup nested at {path}/ItemGroup")
        for child in elem:
            walk(child, tag, f"{path}/{tag}")

    walk(tree.getroot(), "", "")
    return violations


def main() -> int:
    repo = Path(".")
    violations: list[str] = []

    # Trap #81
    tests_dir = repo / "PowerSimBuilder.Tests"
    if tests_dir.is_dir():
        for name, files in find_duplicate_test_classes(tests_dir):
            rel = ", ".join(str(f.relative_to(repo)) for f in files)
            violations.append(f"[Trap #81] duplicate class '{name}' in: {rel}")

    # Trap #82
    for csproj in repo.rglob("*.csproj"):
        if "obj" in csproj.parts or "bin" in csproj.parts:
            continue
        violations.extend(find_nested_itemgroup(csproj))

    if violations:
        print("[FAIL] known traps detected:", file=sys.stderr)
        for v in violations:
            print(f"  {v}", file=sys.stderr)
        return 1

    print("[OK] no known traps (Trap #81 / #82)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
