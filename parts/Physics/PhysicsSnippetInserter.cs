#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerSimBuilder.Physics;

public enum InsertOutcome
{
    Inserted,
    AlreadyExists,
    NoInitFound,
    NoUpdateFound,
}

public static class PhysicsSnippetInserter
{
    public static (InsertOutcome Outcome, string Code) Insert(
        string pythonCode,
        string modelName,
        string initBlock,
        string? helperBlock = null,
        string? updateBlock = null)
    {
        if (pythonCode == null) throw new ArgumentNullException(nameof(pythonCode));
        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("modelName is required", nameof(modelName));
        if (string.IsNullOrWhiteSpace(initBlock))
            throw new ArgumentException("initBlock is required", nameof(initBlock));

        var newline = pythonCode.Contains("\r\n") ? "\r\n" : "\n";
        var normalized = pythonCode.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = new List<string>(normalized.Split('\n'));

        // 1) Duplicate check (init marker)
        var initBeginMarker = PhysicsSnippetBuilder.MarkerBegin(modelName);
        if (lines.Any(l => l.Trim() == initBeginMarker.Trim()))
            return (InsertOutcome.AlreadyExists, pythonCode);

        // 2) Find __init__
        var initIdx = FindInitDefLine(lines);
        if (initIdx < 0) return (InsertOutcome.NoInitFound, pythonCode);

        // 3) If updateBlock provided, find def update
        var wantsUpdate = !string.IsNullOrWhiteSpace(updateBlock);
        var updateIdx = -1;
        if (wantsUpdate)
        {
            updateIdx = FindUpdateDefLine(lines);
            if (updateIdx < 0) return (InsertOutcome.NoUpdateFound, pythonCode);
        }

        // 4) Prelude (import + helper)
        var needLoadFmuImport = !HasLoadFmuImport(lines);
        var needHelper = !string.IsNullOrWhiteSpace(helperBlock) && !HasHelperFunction(lines);

        if (needLoadFmuImport || needHelper)
        {
            var preludeLines = new List<string>();
            if (needLoadFmuImport) preludeLines.Add("from python_runtime.fmu_wrapper import load_fmu");
            if (needHelper)
            {
                if (preludeLines.Count > 0) preludeLines.Add("");
                preludeLines.AddRange(helperBlock!.Replace("\r\n", "\n").TrimEnd('\n').Split('\n'));
            }
            preludeLines.Add("");

            var insertAt = FindSafePreludeInsertIndex(lines);
            if (insertAt < lines.Count && string.IsNullOrWhiteSpace(lines[insertAt]))
                lines.RemoveAt(insertAt);
            lines.InsertRange(insertAt, preludeLines);

            // Recompute positions after prelude
            initIdx = FindInitDefLine(lines);
            if (wantsUpdate) updateIdx = FindUpdateDefLine(lines);
        }

        // 5) Build init insert
        var initIndent = LeadingWhitespace(lines[initIdx]);
        var initBodyIndent = DetectBodyIndent(lines, initIdx + 1) ?? (initIndent + "    ");
        var initBodyEnd = FindBodyEnd(lines, initIdx, initIndent);
        var initBlockLines = ReindentBlock(initBlock, initBodyIndent);
        var initInsert = new List<string>();
        initInsert.Add("");
        initInsert.AddRange(initBlockLines);
        if (initBodyEnd < lines.Count && !string.IsNullOrWhiteSpace(lines[initBodyEnd]))
            initInsert.Add("");

        // 6) Build update insert (if wanted)
        List<string>? updateInsert = null;
        var updateBodyEnd = -1;
        if (wantsUpdate && updateIdx >= 0)
        {
            var updateIndent = LeadingWhitespace(lines[updateIdx]);
            var updateBodyIndent = DetectBodyIndent(lines, updateIdx + 1) ?? (updateIndent + "    ");
            updateBodyEnd = FindBodyEnd(lines, updateIdx, updateIndent);
            var updateBlockLines = ReindentBlock(updateBlock!, updateBodyIndent);
            updateInsert = new List<string>();
            updateInsert.Add("");
            updateInsert.AddRange(updateBlockLines);
            if (updateBodyEnd < lines.Count && !string.IsNullOrWhiteSpace(lines[updateBodyEnd]))
                updateInsert.Add("");
        }

        // 7) Apply insertions in descending position order
        var insertions = new List<(int Position, List<string> Lines)>();
        insertions.Add((initBodyEnd, initInsert));
        if (updateInsert != null) insertions.Add((updateBodyEnd, updateInsert));

        foreach (var item in insertions.OrderByDescending(x => x.Position))
        {
            lines.InsertRange(item.Position, item.Lines);
        }

        var result = string.Join(newline, lines);
        return (InsertOutcome.Inserted, result);
    }

    public static (InsertOutcome Outcome, string Code) InsertFromFmu(
        string pythonCode,
        string fmuPath,
        string modelRelativePath)
    {
        var info = FmuInfoReader.Read(fmuPath);
        var helper = PhysicsSnippetBuilder.BuildHelper();
        var initBlock = PhysicsSnippetBuilder.BuildInsertionBlock(info, modelRelativePath);
        var updateBlock = PhysicsSnippetBuilder.BuildUpdateBlock(info);
        return Insert(pythonCode, info.ModelName, initBlock, helper, updateBlock);
    }

    // ---------- helpers ----------

    private static bool HasLoadFmuImport(List<string> lines)
    {
        foreach (var line in lines)
        {
            var t = line.TrimStart();
            if (t.StartsWith("from python_runtime.fmu_wrapper import") && t.Contains("load_fmu")) return true;
            if (t.StartsWith("import fmpy")) return true;
        }
        return false;
    }

    private static bool HasHelperFunction(List<string> lines)
    {
        var needle = "def " + PhysicsSnippetBuilder.HelperFunctionName;
        return lines.Any(l => l.TrimStart().StartsWith(needle));
    }

    private static int FindSafePreludeInsertIndex(List<string> lines)
    {
        int last = FindLastTopLevelImport(lines);
        if (last >= 0) return last + 1;

        int i = 0;
        if (i < lines.Count && lines[i].StartsWith("#!")) i++;
        if (i < lines.Count &&
            (lines[i].StartsWith("# -*-") || lines[i].StartsWith("# coding"))) i++;
        if (i < lines.Count)
        {
            var t = lines[i].TrimStart();
            string? quote = t.StartsWith("\"\"\"") ? "\"\"\""
                          : t.StartsWith("'''") ? "'''"
                          : null;
            if (quote != null)
            {
                i++;
                while (i < lines.Count)
                {
                    if (lines[i].Contains(quote)) { i++; break; }
                    i++;
                }
            }
        }
        return i;
    }

    private static int FindLastTopLevelImport(List<string> lines)
    {
        int last = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Length == 0 || char.IsWhiteSpace(line[0])) continue;
            var t = line.TrimStart();
            if (t.StartsWith("import ") || t.StartsWith("from "))
                last = i;
        }
        return last;
    }

    private static int FindInitDefLine(List<string> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            var t = lines[i].TrimStart();
            if (t.StartsWith("def __init__(") ||
                t.StartsWith("def __init__ (") ||
                t.StartsWith("async def __init__"))
                return i;
        }
        return -1;
    }

    private static int FindUpdateDefLine(List<string> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            var t = lines[i].TrimStart();
            if (t.StartsWith("def update(") ||
                t.StartsWith("def update (") ||
                t.StartsWith("async def update"))
                return i;
        }
        return -1;
    }

    private static string? DetectBodyIndent(List<string> lines, int startFrom)
    {
        for (int i = startFrom; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            var t = line.TrimStart();
            if (t.StartsWith("#")) continue;
            return LeadingWhitespace(line);
        }
        return null;
    }

    private static int FindBodyEnd(List<string> lines, int defIdx, string defIndent)
    {
        int lastBody = defIdx;
        for (int i = defIdx + 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (LeadingWhitespace(line).Length <= defIndent.Length) break;
            lastBody = i;
        }
        return lastBody + 1;
    }

    private static List<string> ReindentBlock(string block, string targetIndent)
    {
        var raw = block.Replace("\r\n", "\n").Split('\n');
        string baseIndent = "";
        foreach (var l in raw)
        {
            if (string.IsNullOrWhiteSpace(l)) continue;
            baseIndent = LeadingWhitespace(l);
            break;
        }
        var result = new List<string>();
        foreach (var l in raw)
        {
            if (string.IsNullOrWhiteSpace(l)) { result.Add(""); continue; }
            if (baseIndent.Length > 0 && l.StartsWith(baseIndent))
                result.Add(targetIndent + l.Substring(baseIndent.Length));
            else if (baseIndent.Length == 0)
                result.Add(targetIndent + l);
            else
                result.Add(l);
        }
        return result;
    }

    private static string LeadingWhitespace(string line)
    {
        int i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t')) i++;
        return line.Substring(0, i);
    }
}