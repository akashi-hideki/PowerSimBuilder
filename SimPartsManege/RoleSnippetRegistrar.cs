using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerSimBuilder.SimPartsManege
{
    /// <summary>
    /// Extracts a role function (F_xxx) from the Python editor text at the caret
    /// and registers it as a reusable snippet via <see cref="RoleSnippetStorage"/>.
    /// Storage layout (templates/role_snippets/):
    ///   index.json
    ///   &lt;Layer&gt;.&lt;Func&gt;.py
    /// </summary>
    public class RoleSnippetRegistrar
    {
        private static readonly Regex DefLineRegex = new Regex(
            @"^(?<indent>[ \t]*)def\s+(?<name>F_[A-Za-z0-9_]+)\s*\(",
            RegexOptions.Compiled);

        private static readonly Regex StableStartRegex = new Regex(
            @"#\s*STABLE_USER_CODE_START:(?<layer>[A-Za-z0-9_]+)\.(?<func>F_[A-Za-z0-9_]+)",
            RegexOptions.Compiled);

        private static readonly Regex StableEndRegex = new Regex(
            @"#\s*STABLE_USER_CODE_END:(?<layer>[A-Za-z0-9_]+)\.(?<func>F_[A-Za-z0-9_]+)",
            RegexOptions.Compiled);

        // Anchored start-of-line marker used for visibility detection.
        // Matches "    # STABLE_USER_CODE_START:Layer.F_Func" but NOT a
        // similar substring inside a string literal.
        private static readonly Regex MarkerLineRegex = new Regex(
            @"^\s*#\s*STABLE_USER_CODE_START:",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private readonly string? _root;

        /// <param name="root">
        /// Optional explicit snippet folder (for tests). When null, falls back to
        /// <see cref="RoleSnippetStorage.ResolveFolder"/> (repo templates/role_snippets).
        /// </param>
        public RoleSnippetRegistrar(string? root = null) => _root = root;

        private string ResolveFolder() => _root ?? RoleSnippetStorage.ResolveFolder();

        /// <summary>
        /// True when <paramref name="text"/> contains at least one
        /// STABLE_USER_CODE_START marker at the start of a line. Used by
        /// <see cref="PythonEditorForm"/> to show/hide the Register Snippet
        /// entry points only for files that actually contain role functions.
        /// </summary>
        public static bool ContainsRoleMarkers(string? text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return MarkerLineRegex.IsMatch(text);
        }

        // ---- extraction (unchanged from B-1) ----

        public class ExtractedFunction
        {
            public string Name { get; set; } = "";
            public string LayerName { get; set; } = "";
            public string Body { get; set; } = "";
            public int StartLine { get; set; }
            public int EndLine { get; set; }
        }

        public static ExtractedFunction? ExtractFunctionAtCursor(string text, int caretLine)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            if (caretLine < 0 || caretLine >= lines.Length) return null;

            int defLine = -1; string fnName = "", fnIndent = "";
            for (int i = caretLine; i >= 0; i--)
            {
                var m = DefLineRegex.Match(lines[i]);
                if (!m.Success) continue;
                defLine = i;
                fnName = m.Groups["name"].Value;
                fnIndent = m.Groups["indent"].Value;
                break;
            }
            if (defLine < 0) return null;

            int endLine = lines.Length - 1;
            for (int i = defLine + 1; i < lines.Length; i++)
            {
                if (lines[i].Trim().Length == 0) continue;
                int ind = lines[i].Length - lines[i].TrimStart().Length;
                if (ind <= fnIndent.Length) { endLine = i - 1; break; }
            }

            int startMarker = -1, endMarker = -1; string layerName = "";
            for (int i = defLine + 1; i <= endLine; i++)
            {
                var sm = StableStartRegex.Match(lines[i]);
                if (sm.Success && sm.Groups["func"].Value == fnName)
                { startMarker = i; layerName = sm.Groups["layer"].Value; }
                var em = StableEndRegex.Match(lines[i]);
                if (em.Success && em.Groups["func"].Value == fnName && startMarker >= 0)
                { endMarker = i; break; }
            }

            List<string> bodyLines;
            if (startMarker >= 0 && endMarker > startMarker)
                bodyLines = lines.Skip(startMarker + 1).Take(endMarker - startMarker - 1).ToList();
            else
                bodyLines = lines.Skip(defLine + 1).Take(endLine - defLine).ToList();

            return new ExtractedFunction
            {
                Name = fnName,
                LayerName = layerName,
                Body = DedentAndReindent(bodyLines, fnIndent.Length + 4),
                StartLine = defLine,
                EndLine = endLine,
            };
        }

        private static string DedentAndReindent(List<string> bodyLines, int targetIndent)
        {
            int minIndent = int.MaxValue;
            foreach (var l in bodyLines)
            {
                if (l.Trim().Length == 0) continue;
                int ind = l.Length - l.TrimStart().Length;
                if (ind < minIndent) minIndent = ind;
            }
            if (minIndent == int.MaxValue) minIndent = 0;
            string pad = new string(' ', targetIndent);
            var sb = new StringBuilder();
            foreach (var l in bodyLines)
            {
                if (l.Trim().Length == 0) { sb.Append('\n'); continue; }
                var stripped = l.Length >= minIndent ? l.Substring(minIndent) : l.TrimStart();
                sb.Append(pad).Append(stripped).Append('\n');
            }
            return sb.ToString();
        }

        // ---- registration (delegates to RoleSnippetStorage) ----

        public class RegisterResult
        {
            public bool Success { get; set; }
            public bool RequiresOverwrite { get; set; }
            public string? SavedPath { get; set; }
            public string? Error { get; set; }
        }

        public bool Exists(string layerName, string functionName)
            => !string.IsNullOrEmpty(layerName) && !string.IsNullOrEmpty(functionName)
               && RoleSnippetStorage.LoadAll(ResolveFolder())
                    .Any(s => s.Layer == layerName && s.Func == functionName);

        public RegisterResult Register(
            ExtractedFunction fn, string sourceFile, string? notes = null, bool allowOverwrite = false)
        {
            if (fn == null) return new RegisterResult { Error = "No function at cursor" };
            if (string.IsNullOrEmpty(fn.LayerName))
                return new RegisterResult { Error = "LayerName is required (STABLE_USER_CODE markers missing?)" };
            if (string.IsNullOrEmpty(fn.Name) || !fn.Name.StartsWith("F_"))
                return new RegisterResult { Error = "Function name must start with F_" };

            var folder = ResolveFolder();
            if (Exists(fn.LayerName, fn.Name) && !allowOverwrite)
                return new RegisterResult { RequiresOverwrite = true };

            try
            {
                Directory.CreateDirectory(folder);
                var snippet = new RoleSnippet
                {
                    Layer = fn.LayerName,
                    Func = fn.Name,
                    DisplayName = fn.Name,
                    Description = notes ?? "",
                };
                RoleSnippetStorage.Save(snippet, fn.Body, folder);
                return new RegisterResult
                {
                    Success = true,
                    SavedPath = Path.Combine(folder, snippet.File),
                };
            }
            catch (Exception ex)
            {
                return new RegisterResult { Error = ex.Message };
            }
        }

        public IReadOnlyList<RoleSnippet> ListSnippets(string? layerName = null)
        {
            var all = RoleSnippetStorage.LoadAll(ResolveFolder());
            return layerName == null ? all : all.Where(s => s.Layer == layerName).ToList();
        }
    }
}
