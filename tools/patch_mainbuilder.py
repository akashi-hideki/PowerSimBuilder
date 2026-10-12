"""Patch PythonSimulationMainBuilder.cs to target python_runtime.

Idempotent version: uses unique markers to detect already-applied patches.
Fixes XML comment ordering for GenerateComponentsImportLine.
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

    # Backup (only first time)
    backup = TARGET.with_suffix(".cs.old")
    if not backup.exists():
        backup.write_text(src, encoding="utf-8")
        print(f"  backup: {backup}")

    # --- 1. ConnectionUpdateCodeTable ---
    if "src_id, src_sec, src_port" not in src:
        src = replace_once(
            src,
            r'''            "            f_c, f_p = conn[\"from\"].split(\".\")",
            "            t_c, t_p = conn[\"to\"].split(\".\")",
            "            components[t_c].inputs[t_p] = components[f_c].outputs[f_p]"''',
            r'''            "            src_id, src_sec, src_port = conn[\"from\"].split(\".\")",
            "            dst_id, dst_sec, dst_port = conn[\"to\"].split(\".\")",
            "            components[dst_id].inputs[dst_port] = components[src_id].outputs[src_port]"''',
            "ConnectionUpdateCodeTable",
        )
    else:
        print("  SKIP [ConnectionUpdateCodeTable]")

    # --- 2. SimulationLoopStartCodeTable ---
    if "n_steps = int(round(t_end / dt))" not in src:
        src = replace_once(
            src,
            r'''            "    for t in np.arange(0, t_end, dt):"''',
            r'''            "    n_steps = int(round(t_end / dt))",
            "    for i in range(n_steps + 1):",
            "        t = i * dt"''',
            "SimulationLoopStartCodeTable",
        )
    else:
        print("  SKIP [SimulationLoopStartCodeTable]")

    # --- 3. GenerateImportsSection ---
    if "from python_runtime import SimulationData" not in src:
        src = replace_once(
            src,
            r"""            AddLineToSection("imports", "import numpy as np");
            AddLineToSection("imports", "import matplotlib.pyplot as plt");
""",
            r"""            AddLineToSection("imports", "from __future__ import annotations");
            AddLineToSection("imports", "");
            AddLineToSection("imports", "import numpy as np");
            AddLineToSection("imports", "import matplotlib.pyplot as plt");
            AddLineToSection("imports", "");
            AddLineToSection("imports", "from python_runtime import SimulationData");
            AddLineToSection("imports", GenerateComponentsImportLine());
""",
            "GenerateImportsSection",
        )
    else:
        print("  SKIP [GenerateImportsSection]")

    # --- 4. AddSimulationDataDefinition ---
    if "SimulationData(time_step=dt)" not in src:
        src = replace_once(
            src,
            r"""            AddLineToSection("function_def", "    sim_data = SimulationData()");
            AddLineToSection("function_def", "    sim_data.time_step = dt");
            AddLineToSection("function_def", "    sim_data.logs = []");""",
            r"""            AddLineToSection("function_def", "    sim_data = SimulationData(time_step=dt)");""",
            "AddSimulationDataDefinition",
        )
    else:
        print("  SKIP [AddSimulationDataDefinition]")

    # --- 5. GenerateComponentParametersInFunction ---
    if 'components[\\"{compName}\\"].parameters' not in src:
        src = replace_once(
            src,
            r"""                    foreach (var param in comp.Parameters)
                    {
                        var defaultValue = GetPythonDefaultValue(param.DefaultValue, param.Type);
                        AddLineToSection("function_def", $"    sim_data.add_parameter('{comp.Name}', '{param.Name}', {defaultValue})");
                    }""",
            r"""                    var compName = comp.Name.ToLower();
                    foreach (var param in comp.Parameters)
                    {
                        var defaultValue = GetPythonDefaultValue(param.DefaultValue, param.Type);
                        AddLineToSection("function_def", $"    components[\"{compName}\"].parameters[\"{param.Name}\"] = {defaultValue}");
                    }""",
            "GenerateComponentParametersInFunction",
        )
    else:
        print("  SKIP [GenerateComponentParametersInFunction]")

    # --- 6. AddComponentDictionaryInFunction ---
    if r'''{comp.Type}(\"{compName}\"),"''' not in src:
        src = replace_once(
            src,
            r"""                    AddLineToSection("function_def", $"        \"{compName}\": {comp.Type}(\"{compName}\",\"{comp.Type}\"),");""",
            r"""                    AddLineToSection("function_def", $"        \"{compName}\": {comp.Type}(\"{compName}\"),");""",
            "AddComponentDictionaryInFunction",
        )
    else:
        print("  SKIP [AddComponentDictionaryInFunction]")

    # --- 7. GenerateFunctionDefinitionSection order ---
    if (
        r"""            AddSimulationDataDefinition();
            AddComponentDictionaryInFunction();
            GenerateComponentParametersInFunction();"""
        not in src
    ):
        src = replace_once(
            src,
            r"""            AddSimulationDataDefinition();
            GenerateComponentParametersInFunction();
            AddComponentDictionaryInFunction();""",
            r"""            AddSimulationDataDefinition();
            AddComponentDictionaryInFunction();
            GenerateComponentParametersInFunction();""",
            "GenerateFunctionDefinitionSection order",
        )
    else:
        print("  SKIP [GenerateFunctionDefinitionSection order]")

    # --- 8. Add GenerateComponentsImportLine (fixed XML comment ordering) ---
    if "private string GenerateComponentsImportLine()" not in src:
        # Insert BEFORE the existing XML comment for InitializeCodeSections
        # so that comment stays attached to InitializeCodeSections.
        anchor = r"""        /// <summary>
        /// コードセクションを初期化する
        /// </summary>
        private void InitializeCodeSections()"""

        new_block = r"""        /// <summary>
        /// Generate the Python import line for the components used.
        /// </summary>
        private string GenerateComponentsImportLine()
        {
            if (Components == null || Components.Count == 0)
                return "from python_runtime import components";

            var types = Components
                .Where(c => !string.IsNullOrEmpty(c.Type))
                .Select(c => c.Type)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            return "from python_runtime.components import " + string.Join(", ", types);
        }

        /// <summary>
        /// コードセクションを初期化する
        /// </summary>
        private void InitializeCodeSections()"""

        if anchor in src:
            src = src.replace(anchor, new_block)
            print("  OK   [Add GenerateComponentsImportLine]")
        else:
            print("  WARN [Add GenerateComponentsImportLine]: anchor not found")
    else:
        print("  SKIP [Add GenerateComponentsImportLine]")

    TARGET.write_text(src, encoding="utf-8")
    print(f"\nwrote: {TARGET} ({len(src)} chars)")


if __name__ == "__main__":
    main()
