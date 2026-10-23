#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PowerSimBuilder.Physics;

public static class PhysicsSnippetBuilder
{
    public const string HelperFunctionName = "_psb_resolve_fmu";

    public static string MarkerBegin(string modelName)
        => "# === physics: " + modelName + " (auto-inserted) ===";

    public static string MarkerEnd(string modelName)
        => "# === /physics: " + modelName + " ===";

    public static string UpdateMarkerBegin(string modelName)
        => "# === physics: " + modelName + " (auto-inserted in update) ===";

    public static string UpdateMarkerEnd(string modelName)
        => "# === /physics: " + modelName + " ===";

    public static string BuildHelper()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# --- physics helper (auto-inserted once) ---");
        sb.AppendLine("def " + HelperFunctionName + "(relative_path: str) -> str:");
        sb.AppendLine("    \"\"\"Resolve an FMU path under user_physics_models.\"\"\"");
        sb.AppendLine("    from pathlib import Path");
        sb.AppendLine("    here = Path(__file__).resolve()");
        sb.AppendLine("    for base in [here.parent, *here.parents]:");
        sb.AppendLine("        candidate = base / \"user_physics_models\" / relative_path");
        sb.AppendLine("        if candidate.is_file():");
        sb.AppendLine("            return str(candidate)");
        sb.AppendLine("    raise FileNotFoundError(");
        sb.AppendLine("        \"FMU not found under user_physics_models: \" + relative_path");
        sb.AppendLine("    )");
        sb.AppendLine("# --- /physics helper ---");
        return sb.ToString();
    }

    public static string BuildInsertionBlock(
        FmuInfo info,
        string modelRelativePath,
        string indent = "        ")
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        if (string.IsNullOrWhiteSpace(modelRelativePath))
            throw new ArgumentException("modelRelativePath is required", nameof(modelRelativePath));

        var varName = ToPythonIdentifier(info.ModelName);

        var sb = new StringBuilder();
        sb.AppendLine(indent + MarkerBegin(info.ModelName));
        sb.AppendLine(indent + "# source    : user_physics_models/" + modelRelativePath);
        sb.AppendLine(indent + "# fmi       : " + info.FmiVersion + " (" + info.FmiType + ")");
        sb.AppendLine(indent + "# inputs    : " + JoinOrDash(info.Inputs));
        sb.AppendLine(indent + "# outputs   : " + JoinOrDash(info.Outputs));
        sb.AppendLine(indent + "# parameters: " + FormatParameterSummary(info.Parameters));
        sb.AppendLine(indent + "# timing    : " + FormatTiming(info.Timing));
        sb.AppendLine(indent + "self." + varName + " = load_fmu(" + HelperFunctionName
            + "(\"" + modelRelativePath + "\"))");
        sb.AppendLine(indent + "self." + varName + ".enter_initialization_mode()");

        foreach (var p in info.Parameters)
        {
            if (p.StartValue == "# TODO")
                sb.AppendLine(indent + "# TODO: set(\"" + p.Name + "\", <value>)  # non-Real parameter");
            else
                sb.AppendLine(indent + "self." + varName + ".set(\""
                    + p.Name + "\", " + p.StartValue + ")");
        }

        sb.AppendLine(indent + "self." + varName + ".exit_initialization_mode()");
        sb.AppendLine(indent + "self._" + varName + "_time = 0.0");
        sb.AppendLine(indent + MarkerEnd(info.ModelName));
        return sb.ToString();
    }

    public static string BuildUpdateBlock(
        FmuInfo info,
        string indent = "        ")
    {
        if (info == null) throw new ArgumentNullException(nameof(info));

        var varName = ToPythonIdentifier(info.ModelName);

        var sb = new StringBuilder();
        sb.AppendLine(indent + UpdateMarkerBegin(info.ModelName));
        sb.AppendLine(indent + "# inputs -> FMU, FMU -> outputs");
        foreach (var input in info.Inputs)
        {
            sb.AppendLine(indent + "self." + varName + ".set(\"" + input + "\", "
                + "self.inputs.get(\"" + input + "\", 0.0))");
        }
        sb.AppendLine(indent + "self." + varName + ".doStep(self._" + varName + "_time, dt)");
        sb.AppendLine(indent + "self._" + varName + "_time += dt");
        foreach (var output in info.Outputs)
        {
            sb.AppendLine(indent + "self.outputs[\"" + output + "\"] = self."
                + varName + ".get(\"" + output + "\")");
        }
        sb.AppendLine(indent + UpdateMarkerEnd(info.ModelName));
        return sb.ToString();
    }

    private static string JoinOrDash(IReadOnlyList<string> items)
        => items.Count == 0 ? "(none)" : string.Join(", ", items);

    private static string FormatParameterSummary(IReadOnlyList<FmuParameter> parameters)
    {
        if (parameters.Count == 0) return "(none)";
        return string.Join(", ", parameters.Select(p =>
            p.StartValue == "# TODO" ? p.Name + "=?" : p.Name + "=" + p.StartValue));
    }

    private static string FormatTiming(FmuTiming? timing)
    {
        if (timing == null) return "(none)";
        var parts = new List<string>();
        if (timing.StartTime.HasValue) parts.Add("start=" + timing.StartTime.Value.ToString("G", CultureInfo.InvariantCulture));
        if (timing.StopTime.HasValue) parts.Add("stop=" + timing.StopTime.Value.ToString("G", CultureInfo.InvariantCulture));
        if (timing.StepSize.HasValue) parts.Add("step=" + timing.StepSize.Value.ToString("G", CultureInfo.InvariantCulture));
        return parts.Count == 0 ? "(none)" : string.Join(", ", parts);
    }

    private static string ToPythonIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return "_physics_model";
        var sb = new StringBuilder(name.Length + 1);
        foreach (var c in name)
            sb.Append(char.IsLetterOrDigit(c) || c == '_' ? char.ToLowerInvariant(c) : '_');
        if (char.IsDigit(sb[0])) sb.Insert(0, '_');
        return sb.ToString();
    }
}