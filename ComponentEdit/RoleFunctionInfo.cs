using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PowerSimBuilder.ComponentEdit
{
    public enum RoleFunctionKind
    {
        Implemented,
        Stub,
    }

    public class RoleFunctionInfo
    {
        public string Name { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public RoleFunctionKind Kind { get; set; }

        public string DisplayName
        {
            get
            {
                return Kind == RoleFunctionKind.Implemented
                    ? "\u2705 " + Name
                    : "\u26A0\uFE0F " + Name;
            }
        }

        public override string ToString() { return DisplayName; }
    }

    /// <summary>
    /// Parses STABLE_USER_CODE_START/END regions and the def blocks inside them.
    /// Accepts both marker forms:
    ///   "# STABLE_USER_CODE_START"              (bare)
    ///   "# STABLE_USER_CODE_START:Layer.F_xxx"  (named)
    /// </summary>
    public static class RoleFunctionParser
    {
        private static readonly Regex StartMarkerRegex = new Regex(
            @"^\s*#\s*STABLE_USER_CODE_START(?::(?<name>[\w\.]+))?",
            RegexOptions.Compiled);

        private static readonly Regex EndMarkerRegex = new Regex(
            @"^\s*#\s*STABLE_USER_CODE_END",
            RegexOptions.Compiled);

        private static readonly Regex DefRegex = new Regex(
            @"^(?<indent>[ \t]*)def\s+(?<name>\w+)\s*\(",
            RegexOptions.Compiled);

        public static IReadOnlyList<RoleFunctionInfo> Parse(string code)
        {
            var result = new List<RoleFunctionInfo>();
            if (string.IsNullOrEmpty(code)) return result;

            var lines = code.Replace("\r\n", "\n").Split('\n');
            int i = 0;
            while (i < lines.Length)
            {
                var m = StartMarkerRegex.Match(lines[i]);
                if (!m.Success) { i++; continue; }

                string markerName = m.Groups["name"].Success ? m.Groups["name"].Value : null;
                if (!string.IsNullOrEmpty(markerName) && markerName.Contains("."))
                {
                    markerName = markerName.Substring(markerName.LastIndexOf('.') + 1);
                }

                int startIdx = i;
                int endIdx = lines.Length - 1;
                for (int j = i + 1; j < lines.Length; j++)
                {
                    if (EndMarkerRegex.IsMatch(lines[j])) { endIdx = j; break; }
                }

                var found = new List<RoleFunctionInfo>();
                int k = startIdx + 1;
                while (k < endIdx)
                {
                    var defMatch = DefRegex.Match(lines[k]);
                    if (!defMatch.Success) { k++; continue; }

                    string defName = defMatch.Groups["name"].Value;
                    string indent = defMatch.Groups["indent"].Value;
                    int bodyEnd = FindBodyEnd(lines, k + 1, endIdx - 1, indent);
                    bool isStub = IsStubBody(lines, k + 1, bodyEnd);
                    found.Add(new RoleFunctionInfo
                    {
                        Name = defName,
                        StartLine = k + 1,
                        EndLine = bodyEnd + 1,
                        Kind = isStub ? RoleFunctionKind.Stub : RoleFunctionKind.Implemented,
                    });
                    k = bodyEnd + 1;
                }

                if (found.Count == 0 && !string.IsNullOrEmpty(markerName))
                {
                    found.Add(new RoleFunctionInfo
                    {
                        Name = markerName,
                        StartLine = startIdx + 1,
                        EndLine = endIdx + 1,
                        Kind = RoleFunctionKind.Implemented,
                    });
                }

                result.AddRange(found);
                i = endIdx + 1;
            }
            if (result.Count == 0)
            {
                // Fallback mode: no STABLE_USER_CODE markers found.
                // Extract all top-level `def` methods inside a class, skipping
                // __init__/update/reset boilerplate.
                for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
                {
                    var defMatch = DefRegex.Match(lines[lineIdx]);
                    if (!defMatch.Success) continue;
                    string defName = defMatch.Groups["name"].Value;
                    if (defName == "__init__" || defName == "update" || defName == "reset") continue;
                    // Skip physics helper functions (internal use, not user-editable)
                    if (defName != null && defName.StartsWith("_psb_", StringComparison.Ordinal)) continue;
                    string indent = defMatch.Groups["indent"].Value;
                    int bodyEnd = FindBodyEnd(lines, lineIdx + 1, lines.Length - 1, indent);
                    bool isStub = IsStubBody(lines, lineIdx + 1, bodyEnd);
                    result.Add(new RoleFunctionInfo
                    {
                        Name = defName,
                        StartLine = lineIdx + 1,
                        EndLine = bodyEnd + 1,
                        Kind = isStub ? RoleFunctionKind.Stub : RoleFunctionKind.Implemented,
                    });
                }
            }

            return result;
        }

        private static int FindBodyEnd(string[] lines, int start, int maxEnd, string defIndent)
        {
            int lastNonEmpty = start - 1;
            int defIndentLen = defIndent.Length;
            for (int i = start; i <= maxEnd && i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                int thisIndent = CountLeadingSpaces(line);
                if (thisIndent <= defIndentLen) return i - 1;
                lastNonEmpty = i;
            }
            return lastNonEmpty;
        }

        private static bool IsStubBody(string[] lines, int start, int end)
        {
            for (int i = start; i <= end && i < lines.Length; i++)
            {
                string t = lines[i].Trim();
                if (t.Length == 0) continue;
                if (t.StartsWith("#")) continue;
                if (t == "pass") continue;
                if (t == "...") continue;
                if (t.StartsWith("\"\"\"") || t.StartsWith("'''")) continue;
                if (t == "return" || t == "return None") continue;
                return false;
            }
            return true;
        }

        private static int CountLeadingSpaces(string s)
        {
            int n = 0;
            foreach (var c in s)
            {
                if (c == ' ') n++;
                else if (c == '\t') n += 4;
                else break;
            }
            return n;
        }
    }
}
