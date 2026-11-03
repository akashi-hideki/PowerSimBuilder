using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace PowerSimBuilder.UITests;

/// <summary>
/// Reads and asserts on the JSON Lines edit log produced by DesignCanvas.EditLogEvent.
/// Path is set via PSB_EDIT_LOG environment variable at app launch.
/// </summary>
public static class EditLogReader
{
    public static List<JsonElement> Read(string path)
    {
        var list = new List<JsonElement>();
        if (!File.Exists(path)) return list;
        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                list.Add(doc.RootElement.Clone());
            }
            catch { /* skip malformed */ }
        }
        return list;
    }

    public static List<string> EventNames(string path) =>
        Read(path).Select(e => GetString(e, "event") ?? "").ToList();

    public static bool HasEvent(string path, string evt) =>
        EventNames(path).Contains(evt);

    public static JsonElement? FirstEvent(string path, string evt)
    {
        foreach (var e in Read(path))
        {
            if (GetString(e, "event") == evt) return e;
        }
        return null;
    }

    public static string? GetString(JsonElement e, string prop)
        => e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    public static void WaitForFile(string path, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(path) && new FileInfo(path).Length > 0) return;
            Thread.Sleep(200);
        }
    }

    public static void WaitForEvent(string path, string evt, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (HasEvent(path, evt)) return;
            Thread.Sleep(200);
        }
    }

    public static string NewLogPath(string tag) =>
        Path.Combine(Path.GetTempPath(), $"psb_{tag}_{Guid.NewGuid():N}.log");
}
