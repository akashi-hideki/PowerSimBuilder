#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PowerSimBuilder;

/// <summary>
/// C#-side generator for README.md and Python samples.
/// Mirrors the Python manifest.to_usage_markdown() output.
/// </summary>
public static class PythonManifestGenerator
{
    public static string BuildReadme(
        string name,
        string modelDir,
        IReadOnlyList<FmuVariable> selected,
        string fmiType)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name is required", nameof(name));

        var yamlPath = Path.Combine(modelDir, "model.yaml");
        var inputs = selected.Where(v => v.Causality == "input").ToList();
        var outputs = selected.Where(v => v.Causality == "output").ToList();

        var inputRows = inputs.Count > 0
            ? string.Join("\n", inputs.Select(v => $"| input | {v.Name} | {v.Name} | - |"))
            : "| _(none)_ | | | |";
        var outputRows = outputs.Count > 0
            ? string.Join("\n", outputs.Select(v => $"| output | {v.Name} | {v.Name} | - |"))
            : "| _(none)_ | | | |";

        var firstInput = inputs.FirstOrDefault()?.Name;
        var firstOutput = outputs.FirstOrDefault()?.Name;

        var inputLine = firstInput != null
            ? $"fmu.inputs[\"{firstInput}\"] = 1.0"
            : "# no inputs defined";
        var outputLine = firstOutput != null
            ? $"print(f\"{firstOutput} = \" + str(fmu.outputs['{firstOutput}']))"
            : "print(\"no outputs defined\")";

        var sb = new StringBuilder();
        sb.Append("# ").Append(name).AppendLine(" (Physics Model)");
        sb.AppendLine();
        sb.AppendLine("Co-Simulation FMU imported via PowerSimBuilder's \"Import Physics Model\" dialog.");
        sb.AppendLine();
        sb.AppendLine("## Quick start (Python)");
        sb.AppendLine();
        sb.AppendLine("```python");
        sb.AppendLine("from python_runtime.physics import load_manifest, FmuAdapter");
        sb.AppendLine();
        sb.AppendLine("# 1. Load the model manifest");
        sb.Append("manifest = load_manifest(r\"").Append(yamlPath).AppendLine("\")");
        sb.AppendLine();
        sb.AppendLine("# 2. Instantiate the FMU adapter");
        sb.AppendLine("fmu = FmuAdapter(manifest)");
        sb.AppendLine();
        sb.AppendLine("# 3. Set an input (if any)");
        sb.AppendLine(inputLine);
        sb.AppendLine();
        sb.AppendLine("# 4. Step forward 10 ms");
        sb.AppendLine("fmu.step(0.01)");
        sb.AppendLine();
        sb.AppendLine("# 5. Read outputs");
        sb.AppendLine(outputLine);
        sb.AppendLine();
        sb.AppendLine("# 6. Cleanup");
        sb.AppendLine("fmu.close()");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Interface");
        sb.AppendLine();
        sb.AppendLine("| Direction | Port | FMU variable | Unit |");
        sb.AppendLine("|-----------|------|--------------|------|");
        sb.AppendLine(inputRows);
        sb.AppendLine(outputRows);
        sb.AppendLine();
        sb.AppendLine("## Timing");
        sb.AppendLine("- Mode: `fixed_step`");
        sb.AppendLine("- Internal dt: `0.001` s");
        sb.AppendLine();
        sb.AppendLine("## Source");
        sb.AppendLine("- File: `source/*.fmu`");
        sb.AppendLine("- License: (unspecified)");
        sb.AppendLine("- URL: (none)");
        sb.AppendLine();
        sb.AppendLine("## Files in this directory");
        sb.AppendLine("- `model.yaml` - manifest (edit to remap ports/parameters)");
        sb.AppendLine("- `source/*.fmu` - the FMU archive");
        sb.AppendLine("- `README.md` - this file");
        sb.AppendLine();
        sb.AppendLine("## Regenerate this README");
        sb.AppendLine();
        sb.AppendLine("After editing `model.yaml`, run:");
        sb.AppendLine();
        sb.AppendLine("```powershell");
        sb.Append("python -m python_runtime.physics.manifest \"").Append(yamlPath).AppendLine("\" --write-readme");
        sb.AppendLine("```");
        return sb.ToString();
    }

    public static string BuildPythonSample(
        string modelDir,
        IReadOnlyList<FmuVariable> selected)
    {
        var yamlPath = Path.Combine(modelDir, "model.yaml");
        var firstInput = selected.FirstOrDefault(v => v.Causality == "input")?.Name;
        var firstOutput = selected.FirstOrDefault(v => v.Causality == "output")?.Name;

        var inputLine = firstInput != null
            ? $"fmu.inputs[\"{firstInput}\"] = 1.0"
            : "# no inputs defined";
        var outputLine = firstOutput != null
            ? $"print(f\"{firstOutput} = \" + str(fmu.outputs['{firstOutput}']))"
            : "print(\"no outputs defined\")";

        var sb = new StringBuilder();
        sb.AppendLine("from python_runtime.physics import load_manifest, FmuAdapter");
        sb.AppendLine();
        sb.Append("manifest = load_manifest(r\"").Append(yamlPath).AppendLine("\")");
        sb.AppendLine("fmu = FmuAdapter(manifest)");
        sb.AppendLine(inputLine);
        sb.AppendLine("fmu.step(0.01)");
        sb.AppendLine(outputLine);
        sb.AppendLine("fmu.close()");
        return sb.ToString();
    }
}