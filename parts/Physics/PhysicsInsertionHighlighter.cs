#nullable enable
using System;

namespace PowerSimBuilder.Physics;

/// <summary>
/// Character range and 1-based line range of an inserted physics block.
/// </summary>
public sealed record PhysicsBlockBounds(int Start, int Length, int StartLine, int EndLine);

/// <summary>
/// Block found at a cursor position.
/// </summary>
public sealed record PhysicsBlockAtCursor(
    string ModelName,
    string Kind,      // "init" or "update"
    int StartLine,
    int EndLine);

public static class PhysicsInsertionHighlighter
{
    public static PhysicsBlockBounds? FindBlock(string? code, string? modelName)
        => FindByMarkers(code, modelName,
            "# === physics: " + (modelName ?? "") + " (auto-inserted) ===",
            "# === /physics: " + (modelName ?? "") + " ===");

    public static PhysicsBlockBounds? FindUpdateBlock(string? code, string? modelName)
        => FindByMarkers(code, modelName,
            "# === physics: " + (modelName ?? "") + " (auto-inserted in update) ===",
            "# === /physics: " + (modelName ?? "") + " ===");

    /// <summary>
    /// Returns which physics block (if any) contains the given cursor position.
    /// </summary>
    public static PhysicsBlockAtCursor? FindBlockAtPosition(string? code, int position)
    {
        if (string.IsNullOrEmpty(code)) return null;
        if (position < 0 || position > code.Length) return null;

        int idx = 0;
        const string prefix = "# === physics: ";
        while ((idx = code.IndexOf(prefix, idx, StringComparison.Ordinal)) >= 0)
        {
            var headerEnd = code.IndexOf(" ===", idx + prefix.Length, StringComparison.Ordinal);
            if (headerEnd < 0) break;
            var headerLen = headerEnd - idx + 4;
            var header = code.Substring(idx, headerLen);

            var nameStart = prefix.Length;
            var nameEnd = header.IndexOf(" (auto-inserted", StringComparison.Ordinal);
            if (nameEnd < 0) { idx = headerEnd; continue; }
            var modelName = header.Substring(nameStart, nameEnd - nameStart).Trim();
            var isUpdate = header.IndexOf("in update", StringComparison.Ordinal) >= 0;

            var endMarker = "# === /physics: " + modelName + " ===";
            var endIdx = code.IndexOf(endMarker, headerEnd, StringComparison.Ordinal);
            if (endIdx < 0) { idx = headerEnd; continue; }
            var endOfBlock = endIdx + endMarker.Length;

            if (position >= idx && position <= endOfBlock)
            {
                var startLine = CountLinesUpTo(code, idx) + 1;
                var endLine = CountLinesUpTo(code, endOfBlock) + 1;
                return new PhysicsBlockAtCursor(
                    modelName,
                    isUpdate ? "update" : "init",
                    startLine,
                    endLine);
            }
            idx = endOfBlock;
        }
        return null;
    }

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