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
        string indent = "        ",
        PhysicsPortMapping? mapping = null)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        if (string.IsNullOrWhiteSpace(modelRelativePath))
            throw new ArgumentException("modelRelativePath is required", nameof(modelRelativePath));
        mapping ??= PhysicsPortMapping.Identity;

        var varName = ToPythonIdentifier(info.ModelName);

        var sb = new StringBuilder();
        sb.AppendLine(indent + MarkerBegin(info.ModelName));
        sb.AppendLine(indent + "# model     : " + info.ModelName);
        sb.AppendLine(indent + "# fmu_file  : " + GetFileName(modelRelativePath));
        sb.AppendLine(indent + "# source    : user_physics_models/" + modelRelativePath);
        sb.AppendLine(indent + "# fmi       : " + info.FmiVersion + " (" + info.FmiType + ")");
        sb.AppendLine(indent + "# inputs    : " + JoinMappingSummary(info.Inputs, info.InputTypes, mapping.Inputs));
        sb.AppendLine(indent + "# outputs   : " + JoinMappingSummary(info.Outputs, info.OutputTypes, mapping.Outputs));
        sb.AppendLine(indent + "# parameters: " + FormatParameterSummary(info.Parameters));
        sb.AppendLine(indent + "# timing    : " + FormatTiming(info.Timing));
        sb.AppendLine(indent + "self." + varName + " = load_fmu(" + HelperFunctionName
            + "(\"" + modelRelativePath + "\"))");
        sb.AppendLine(indent + "self." + varName + ".enter_initialization_mode()");

        foreach (var p in info.Parameters)
        {
            if (p.StartValue == "# TODO")
                sb.AppendLine(indent + "# TODO: set(\"" + p.Name + "\", <value>)  # "
                    + p.Type + " parameter");
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
        string indent = "        ",
        PhysicsPortMapping? mapping = null)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        mapping ??= PhysicsPortMapping.Identity;

        var varName = ToPythonIdentifier(info.ModelName);

        var sb = new StringBuilder();
        sb.AppendLine(indent + UpdateMarkerBegin(info.ModelName));
        sb.AppendLine(indent + "# inputs -> FMU, FMU -> outputs");

        foreach (var input in info.Inputs)
        {
            var fmuType = LookupType(info.InputTypes, input);
            var comp    = mapping.ComponentPortFor(input);
            sb.AppendLine(indent + BuildSetLine(varName, input, comp, fmuType));
        }

        // Timing warning before step
        if (info.Timing?.StepSize is double step && step > 0)
        {
            sb.AppendLine(indent + "# timing check: FMU stepSize=" + FormatDouble(step));
            sb.AppendLine(indent + "if abs(dt - " + FormatDouble(step) + ") > 1e-9:");
            sb.AppendLine(indent + "    import warnings");
            sb.AppendLine(indent + "    warnings.warn(");
            sb.AppendLine(indent + "        \"physics: dt (%.6g) differs from FMU stepSize (%.6g)\"");
            sb.AppendLine(indent + "        % (dt, " + FormatDouble(step) + "), RuntimeWarning)");
        }

        sb.AppendLine(indent + "self." + varName + ".doStep(self._" + varName + "_time, dt)");
        sb.AppendLine(indent + "self._" + varName + "_time += dt");

        foreach (var output in info.Outputs)
        {
            var fmuType = LookupType(info.OutputTypes, output);
            var comp    = mapping.ComponentOutputFor(output);
            sb.AppendLine(indent + BuildGetLine(varName, output, comp, fmuType));
        }

        // v1.8.0 bug #5: when the FMU exposes exactly one output whose mapped
        // name differs from "output", ALSO write it under outputs["output"] so
        // standard components (FirstOrderLag: outputs={"output":0.0}) and
        // demo_main.py see the FMU value. Triggered when either:
        //   - the mapping is empty (Identity), or
        //   - the mapping is a self-map (e.g. "x" -> "x", from model.yaml).
        // Explicit mappings to a different name are left untouched; multi-output
        // FMUs are ignored (ambiguous).
        if (info.Outputs.Count == 1 && info.Outputs[0] != "output")
        {
            var onlyOut = info.Outputs[0];
            var onlyComp = mapping.ComponentOutputFor(onlyOut);
            if (onlyComp == onlyOut)
            {
                sb.AppendLine(indent + "# Auto-fallback: expose FMU output under standard name 'output'");
                sb.AppendLine(indent + "self.outputs[\"output\"] = self.outputs[\"" + onlyOut + "\"]");
            }
        }

        sb.AppendLine(indent + UpdateMarkerEnd(info.ModelName));
        return sb.ToString();
    }

    private static string BuildSetLine(string varName, string fmuPort, string compPort, string type)
    {
        var defaultSrc = "self.inputs.get(\"" + compPort + "\", " + DefaultValueFor(type) + ")";
        switch (type)
        {
            case "Real":
                return "self." + varName + ".set(\"" + fmuPort + "\", float(" + defaultSrc + "))";
            case "Integer":
                return "self." + varName + ".set(\"" + fmuPort + "\", int(" + defaultSrc + "))";
            case "Boolean":
                return "self." + varName + ".set(\"" + fmuPort + "\", bool(" + defaultSrc + "))";
            case "String":
                return "self." + varName + ".set(\"" + fmuPort + "\", str(" + defaultSrc + "))";
            default:
                return "# TODO: set(\"" + fmuPort + "\", <" + type + ">)";
        }
    }

    private static string BuildGetLine(string varName, string fmuPort, string compPort, string type)
    {
        var getSrc = "self." + varName + ".get(\"" + fmuPort + "\")";
        switch (type)
        {
            case "Real":
                return "self.outputs[\"" + compPort + "\"] = float(" + getSrc + ")";
            case "Integer":
                return "self.outputs[\"" + compPort + "\"] = int(" + getSrc + ")";
            case "Boolean":
                return "self.outputs[\"" + compPort + "\"] = bool(" + getSrc + ")";
            case "String":
                return "self.outputs[\"" + compPort + "\"] = str(" + getSrc + ")";
            default:
                return "# TODO: self.outputs[\"" + compPort + "\"] = self." + varName
                    + ".get(\"" + fmuPort + "\")  # " + type;
        }
    }

    private static string DefaultValueFor(string type) => type switch
    {
        "Real"    => "0.0",
        "Integer" => "0",
        "Boolean" => "False",
        "String"  => "\"\"",
        _         => "0.0",
    };

    private static string LookupType(IReadOnlyDictionary<string, string>? types, string name)
    {
        if (types != null && types.TryGetValue(name, out var t) && !string.IsNullOrEmpty(t))
            return t;
        return "Real";
    }

    private static string JoinMappingSummary(
        IReadOnlyList<string> names,
        IReadOnlyDictionary<string, string>? types,
        IReadOnlyDictionary<string, string> mapping)
    {
        if (names.Count == 0) return "(none)";
        return string.Join(", ", names.Select(n =>
        {
            var t = LookupType(types, n);
            var comp = mapping.TryGetValue(n, out var c) && !string.IsNullOrEmpty(c) ? c : n;
            return n == comp
                ? n + ":" + t
                : n + ":" + t + "->" + comp;
        }));
    }

    private static string FormatParameterSummary(IReadOnlyList<FmuParameter> parameters)
    {
        if (parameters.Count == 0) return "(none)";
        return string.Join(", ", parameters.Select(p =>
            p.StartValue == "# TODO" ? p.Name + "(" + p.Type + ")=?" : p.Name + "=" + p.StartValue));
    }

    private static string FormatTiming(FmuTiming? timing)
    {
        if (timing == null) return "(none)";
        var parts = new List<string>();
        if (timing.StartTime.HasValue) parts.Add("start=" + FormatDouble(timing.StartTime.Value));
        if (timing.StopTime.HasValue)  parts.Add("stop="  + FormatDouble(timing.StopTime.Value));
        if (timing.StepSize.HasValue)  parts.Add("step="  + FormatDouble(timing.StepSize.Value));
        return parts.Count == 0 ? "(none)" : string.Join(", ", parts);
    }

    private static string FormatDouble(double v)
        => v.ToString("G", CultureInfo.InvariantCulture);

    private static string GetFileName(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return "";
        var slash = relativePath.LastIndexOf('/') ;
        return slash >= 0 ? relativePath.Substring(slash + 1) : relativePath;
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