#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PowerSimBuilder.Physics;

/// <summary>
/// Detects all physics model blocks in a Python source string.
/// Model names are discovered from the begin markers, then precise bounds
/// are obtained via the well-tested PhysicsInsertionHighlighter API.
/// </summary>
public static class PhysicsBlockDetector
{
    // Matches both "# === physics: NAME (auto-inserted) ===" and
    // "# === physics: NAME (auto-inserted in update) ===".
    // The end marker "# === /physics: NAME ===" does not match.
    private static readonly Regex MarkerRegex = new Regex(
        @"# === physics: (?<name>.+?) \(auto-inserted[^)]*\) ===",
        RegexOptions.Compiled);

    /// <summary>
    /// Returns all physics models found in the given code, sorted by name.
    /// Empty list if no code or no blocks.
    /// </summary>
    public static IReadOnlyList<PhysicsBlockInfo> FindAll(string? code)
    {
        if (string.IsNullOrEmpty(code))
            return Array.Empty<PhysicsBlockInfo>();

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in MarkerRegex.Matches(code))
        {
            var name = m.Groups["name"].Value?.Trim();
            if (!string.IsNullOrEmpty(name))
                names.Add(name);
        }

        var result = new List<PhysicsBlockInfo>();
        foreach (var name in names.OrderBy(n => n, StringComparer.Ordinal))
        {
            var init = PhysicsInsertionHighlighter.FindBlock(code, name);
            var update = PhysicsInsertionHighlighter.FindUpdateBlock(code, name);
            if (init is null && update is null)
                continue;
            result.Add(new PhysicsBlockInfo(name, init, update));
        }
        return result;
    }
}