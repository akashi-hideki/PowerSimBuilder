#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace PowerSimBuilder.Physics;

/// <summary>
/// Port mapping for one direction (inputs or outputs).
/// Key = FMU port name, Value = component port name.
/// If a port is not present, the component port name equals the FMU port name.
/// </summary>
public sealed record PhysicsPortMapping(
    IReadOnlyDictionary<string, string> Inputs,
    IReadOnlyDictionary<string, string> Outputs)
{
    public static PhysicsPortMapping Identity { get; } =
        new(new Dictionary<string, string>(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal));

    public string ComponentPortFor(string fmuPort)
        => Inputs.TryGetValue(fmuPort, out var v) ? v : fmuPort;

    public string ComponentOutputFor(string fmuPort)
        => Outputs.TryGetValue(fmuPort, out var v) ? v : fmuPort;
}

public static class ModelYaml
{
    /// <summary>
    /// Reads interface.inputs/outputs from a model.yaml file.
    /// Returns Identity when the file is missing or unparsable.
    /// </summary>
    public static PhysicsPortMapping ReadPortMapping(string? modelYamlPath)
    {
        if (string.IsNullOrEmpty(modelYamlPath) || !File.Exists(modelYamlPath))
            return PhysicsPortMapping.Identity;

        try
        {
            var lines = File.ReadAllLines(modelYamlPath);
            var inputs  = ParseMappingBlock(lines, "inputs");
            var outputs = ParseMappingBlock(lines, "outputs");
            return new PhysicsPortMapping(inputs, outputs);
        }
        catch
        {
            return PhysicsPortMapping.Identity;
        }
    }

    private static Dictionary<string, string> ParseMappingBlock(
        string[] lines, string blockName)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        int interfaceLine = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimEnd();
            if (trimmed == "interface:")
            {
                interfaceLine = i;
                break;
            }
        }
        if (interfaceLine < 0) return result;

        int blockLine = -1;
        int interfaceIndent = IndentOf(lines[interfaceLine]);
        for (int i = interfaceLine + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            var indent = IndentOf(line);
            if (indent <= interfaceIndent) break;
            var t = line.TrimStart();
            if (t == blockName + ":")
            {
                blockLine = i;
                break;
            }
        }
        if (blockLine < 0) return result;

        int blockIndent = IndentOf(lines[blockLine]);
        for (int i = blockLine + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            var indent = IndentOf(line);
            if (indent <= blockIndent) break;
            var t = line.TrimStart();
            var colon = t.IndexOf(':');
            if (colon <= 0) continue;
            var key = Unquote(t.Substring(0, colon).Trim());
            var val = Unquote(t.Substring(colon + 1).Trim());
            if (string.IsNullOrEmpty(key)) continue;
            result[key] = string.IsNullOrEmpty(val) ? key : val;
        }
        return result;
    }

    private static int IndentOf(string line)
    {
        int i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t')) i++;
        return i;
    }

    private static string Unquote(string s)
    {
        if (s.Length >= 2 && ((s[0] == '"' && s[^1] == '"') || (s[0] == '\'' && s[^1] == '\'')))
            return s.Substring(1, s.Length - 2);
        return s;
    }
}