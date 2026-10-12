"""Fix component name lookup in PythonSimulationMainBuilder.cs.

- Use InstanceName instead of Name for connection endpoints.
- Same fix in plot_config generation.
"""
from pathlib import Path

TARGET = Path("SimulationGenerate/PythonSimulationMainBuilder.cs")


def replace_once(src: str, old: str, new: str, label: str) -> str:
    if old not in src:
        print(f"  SKIP [{label}]")
        return src
    src = src.replace(old, new)
    print(f"  OK   [{label}]")
    return src


def main() -> None:
    if not TARGET.exists():
        print(f"ERROR: {TARGET} not found")
        raise SystemExit(1)

    src = TARGET.read_text(encoding="utf-8")

    backup = TARGET.with_suffix(".cs.old2")
    if not backup.exists():
        backup.write_text(src, encoding="utf-8")
        print(f"  backup: {backup}")

    # --- 1. AddConnectionsListInFunction ---
    if "conn.StartComponent.InstanceName" not in src:
        src = replace_once(
            src,
            r'''                    var fromComp = conn.StartComponent.Name.ToLower();
                    var fromPort = conn.StartPort;
                    var toComp = conn.EndComponent.Name.ToLower();
                    var toPort = conn.EndPort;''',
            r'''                    var fromComp = conn.StartComponent.InstanceName.ToLower();
                    var fromPort = conn.StartPort;
                    var toComp = conn.EndComponent.InstanceName.ToLower();
                    var toPort = conn.EndPort;''',
            "AddConnectionsListInFunction",
        )
    else:
        print("  SKIP [AddConnectionsListInFunction]")

    # --- 2. GeneratePlotConfigSection ---
    if "conn.StartComponent.InstanceName.ToLower()" not in src or \
       "var fromComp = conn.StartComponent.Name.ToLower();" in src:
        src = replace_once(
            src,
            r'''                    var fromComp = conn.StartComponent.Name.ToLower();
                    var fromPort = conn.StartPort;''',
            r'''                    var fromComp = conn.StartComponent.InstanceName.ToLower();
                    var fromPort = conn.StartPort;''',
            "GeneratePlotConfigSection",
        )
    else:
        print("  SKIP [GeneratePlotConfigSection]")

    TARGET.write_text(src, encoding="utf-8")
    print(f"\nwrote: {TARGET} ({len(src)} chars)")


if __name__ == "__main__":
    main()