using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PowerSimBuilder.ComponentEdit
{
    /// <summary>
    /// Indentation-based Python class/method parser.
    ///
    /// Extracts top-level classes and their methods (def) from arbitrary
    /// Python source. Used by EditPanel to render a "File Structure" tree.
    /// </summary>
    public static class FileStructureParser
    {
        private static readonly Regex ClassRegex = new Regex(
            @"^(?<indent>[ \t]*)class\s+(?<name>\w+)\s*(?:\(\s*(?<base>[\w\.]+)[^\)]*\))?\s*:",
            RegexOptions.Compiled);

        private static readonly Regex DefRegex = new Regex(
            @"^(?<indent>[ \t]*)def\s+(?<name>\w+)\s*\(",
            RegexOptions.Compiled);

        public static IReadOnlyList<FileClassInfo> Parse(string code)
        {
            var result = new List<FileClassInfo>();
            if (string.IsNullOrEmpty(code)) return result;

            var lines = code.Replace("\r\n", "\n").Split('\n');
            int n = lines.Length;

            for (int i = 0; i < n; i++)
            {
                var cm = ClassRegex.Match(lines[i]);
                if (!cm.Success) continue;

                string indent = cm.Groups["indent"].Value;
                string className = cm.Groups["name"].Value;
                string baseClass = cm.Groups["base"].Success ? cm.Groups["base"].Value : null;

                int classEnd = n - 1;
                for (int j = i + 1; j < n; j++)
                {
                    if (string.IsNullOrWhiteSpace(lines[j])) continue;
                    if (IndentLevel(lines[j]) <= indent.Length)
                    {
                        classEnd = j - 1;
                        break;
                    }
                }

                var methods = new List<FileMethodInfo>();
                for (int k = i + 1; k <= classEnd; k++)
                {
                    var dm = DefRegex.Match(lines[k]);
                    if (!dm.Success) continue;

                    string defIndent = dm.Groups["indent"].Value;
                    if (defIndent.Length <= indent.Length) continue;

                    string defName = dm.Groups["name"].Value;
                    int defEnd = classEnd;
                    for (int m = k + 1; m <= classEnd; m++)
                    {
                        if (string.IsNullOrWhiteSpace(lines[m])) continue;
                        if (IndentLevel(lines[m]) <= defIndent.Length)
                        {
                            defEnd = m - 1;
                            break;
                        }
                    }

                    bool isRole = defName.StartsWith("F_", StringComparison.Ordinal);
                    bool isStub = isRole && IsStubBody(lines, k + 1, defEnd);

                    methods.Add(new FileMethodInfo
                    {
                        Name = defName,
                        StartLine = k + 1,
                        EndLine = defEnd + 1,
                        IsRoleFunction = isRole,
                        IsStub = isStub,
                    });

                    k = defEnd;
                }

                result.Add(new FileClassInfo
                {
                    Name = className,
                    StartLine = i + 1,
                    EndLine = classEnd + 1,
                    BaseClass = baseClass,
                    Methods = methods,
                });

                i = classEnd;
            }

            return result;
        }

        private static int IndentLevel(string line)
        {
            int n = 0;
            foreach (var c in line)
            {
                if (c == ' ') n++;
                else if (c == '\t') n += 4;
                else break;
            }
            return n;
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
                if (t == "return" || t == "return 0" || t == "return None") continue;
                return false;
            }
            return true;
        }
    }
}