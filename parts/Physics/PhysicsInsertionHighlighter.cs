#nullable enable
using System;

namespace PowerSimBuilder.Physics;

/// <summary>
/// Character range and 1-based line range of an inserted physics block.
/// </summary>
public sealed record PhysicsBlockBounds(int Start, int Length, int StartLine, int EndLine);

public static class PhysicsInsertionHighlighter
{
    /// <summary>Finds the __init__ block marker.</summary>
    public static PhysicsBlockBounds? FindBlock(string? code, string? modelName)
        => FindByMarkers(code, modelName,
            "# === physics: " + (modelName ?? "") + " (auto-inserted) ===",
            "# === /physics: " + (modelName ?? "") + " ===");

    /// <summary>Finds the update() block marker.</summary>
    public static PhysicsBlockBounds? FindUpdateBlock(string? code, string? modelName)
        => FindByMarkers(code, modelName,
            "# === physics: " + (modelName ?? "") + " (auto-inserted in update) ===",
            "# === /physics: " + (modelName ?? "") + " ===");

    private static PhysicsBlockBounds? FindByMarkers(
        string? code, string? modelName, string beginMarker, string endMarker)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(modelName))
            return null;

        var bIdx = code.IndexOf(beginMarker, StringComparison.Ordinal);
        if (bIdx < 0) return null;

        var eIdx = code.IndexOf(endMarker, bIdx + beginMarker.Length, StringComparison.Ordinal);
        if (eIdx < 0) return null;

        var eEnd = eIdx + endMarker.Length;

        var lineStart = bIdx;
        while (lineStart > 0 && code[lineStart - 1] != '\n') lineStart--;

        var lineEnd = eEnd;
        while (lineEnd < code.Length && code[lineEnd] != '\n' && code[lineEnd] != '\r') lineEnd++;

        var startLine = CountLinesUpTo(code, lineStart) + 1;
        var endLine   = CountLinesUpTo(code, eEnd) + 1;

        if (lineEnd <= lineStart) return null;

        return new PhysicsBlockBounds(lineStart, lineEnd - lineStart, startLine, endLine);
    }

    private static int CountLinesUpTo(string code, int endExclusive)
    {
        int lines = 0;
        int limit = Math.Min(endExclusive, code.Length);
        for (int i = 0; i < limit; i++)
        {
            if (code[i] == '\n') lines++;
        }
        return lines;
    }
}