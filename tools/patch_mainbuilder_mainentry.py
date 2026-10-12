"""Append 'if __name__ == "__main__": main()' to generated output."""
from pathlib import Path

TARGET = Path("SimulationGenerate/PythonSimulationMainBuilder.cs")


def main() -> None:
    if not TARGET.exists():
        print(f"ERROR: {TARGET} not found")
        raise SystemExit(1)

    src = TARGET.read_text(encoding="utf-8")

    # Idempotency check
    if "if __name__ == \\\"__main__\\\":" in src:
        print("  SKIP: already patched")
        return

    backup = TARGET.with_suffix(".cs.old3")
    if not backup.exists():
        backup.write_text(src, encoding="utf-8")
        print(f"  backup: {backup}")

    old = r'''            foreach (var section in sectionsOrder)
            {
                if (CodeSections.ContainsKey(section))
                {
                    allLines.AddRange(CodeSections[section]);
                    if (section != "plotting_functions")
                        allLines.Add("");
                }
            }

            return allLines;'''

    new = r'''            foreach (var section in sectionsOrder)
            {
                if (CodeSections.ContainsKey(section))
                {
                    allLines.AddRange(CodeSections[section]);
                    if (section != "plotting_functions")
                        allLines.Add("");
                }
            }

            // Entry point: actually invoke main()
            allLines.Add("");
            allLines.Add("if __name__ == \"__main__\":");
            allLines.Add("    main()");

            return allLines;'''

    if old not in src:
        print("ERROR: target block not found in CombineAllSections")
        raise SystemExit(1)

    src = src.replace(old, new)
    TARGET.write_text(src, encoding="utf-8")
    print(f"  OK: appended main entry point")
    print(f"\nwrote: {TARGET} ({len(src)} chars)")


if __name__ == "__main__":
    main()