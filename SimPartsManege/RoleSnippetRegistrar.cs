using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PowerSimBuilder.SimPartsManege
{
    /// <summary>
    /// Registers the role function at the current cursor as a reusable snippet.
    /// Layout:
    ///   user_role_snippets/
    ///     register.json            metadata list
    ///     &lt;Layer&gt;/
    ///       F_xxx.py               dedented body (STABLE markers stripped)
    /// </summary>
    public class RoleSnippetRegistrar
    {
        public const string RootFolderName = "user_role_snippets";
        public const string RegisterJsonName = "register.json";

        private static readonly Regex DefLineRegex = new Regex(
            @"^(?<indent>[ \t]*)def\s+(?<name>F_[A-Za-z0-9_]+)\s*\(",
            RegexOptions.Compiled);

        private static readonly Regex StableStartRegex = new Regex(
            @"#\s*STABLE_USER_CODE_START:(?<layer>[A-Za-z0-9_]+)\.(?<func>F_[A-Za-z0-9_]+)",
            RegexOptions.Compiled);

        private static readonly Regex StableEndRegex = new Regex(
            @"#\s*STABLE_USER_CODE_END:(?<layer>[A-Za-z0-9_]+)\.(?<func>F_[A-Za-z0-9_]+)",
            RegexOptions.Compiled);

        private readonly string _root;
        public string Root => _root;

        public RoleSnippetRegistrar(string? root = null) => _root = root ?? FindRoot();

        public static string FindRoot()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "PowerSimBuilder.sln")))
                    return Path.Combine(dir.FullName, RootFolderName);
                dir = dir.Parent;
            }
            return Path.Combine(Directory.GetCurrentDirectory(), RootFolderName);
        }

        // ---- extraction ----

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

        // ---- registration ----

        public class SnippetMetadata
        {
            public string Name { get; set; } = "";
            public string Layer { get; set; } = "";
            public string SourceFile { get; set; } = "";
            public string RegisteredAt { get; set; } = "";
            public string? Notes { get; set; }
        }

        public class RegisterResult
        {
            public bool Success { get; set; }
            public bool RequiresOverwrite { get; set; }
            public string? SavedPath { get; set; }
            public string? Error { get; set; }
        }

        public bool Exists(string layerName, string functionName)
            => !string.IsNullOrEmpty(layerName) && !string.IsNullOrEmpty(functionName)
               && File.Exists(SnippetPath(layerName, functionName));

        public string SnippetPath(string layerName, string functionName)
            => Path.Combine(_root, layerName, functionName + ".py");

        public RegisterResult Register(
            ExtractedFunction fn, string sourceFile, string? notes = null, bool allowOverwrite = false)
        {
            if (fn == null) return new RegisterResult { Error = "No function at cursor" };
            if (string.IsNullOrEmpty(fn.LayerName))
                return new RegisterResult { Error = "LayerName is required (STABLE_USER_CODE markers missing?)" };
            if (string.IsNullOrEmpty(fn.Name) || !fn.Name.StartsWith("F_"))
                return new RegisterResult { Error = "Function name must start with F_" };

            if (Exists(fn.LayerName, fn.Name) && !allowOverwrite)
                return new RegisterResult { RequiresOverwrite = true };

            try
            {
                Directory.CreateDirectory(Path.Combine(_root, fn.LayerName));
                var path = SnippetPath(fn.LayerName, fn.Name);
                File.WriteAllText(path, fn.Body, new UTF8Encoding(false));
                UpdateMetadata(fn.LayerName, fn.Name, sourceFile ?? "", notes);
                return new RegisterResult { Success = true, SavedPath = path };
            }
            catch (Exception ex)
            {
                return new RegisterResult { Error = ex.Message };
            }
        }

        private void UpdateMetadata(string layer, string name, string sourceFile, string? notes)
        {
            var list = LoadMetadata();
            list.RemoveAll(m => m.Layer == layer && m.Name == name);
            list.Add(new SnippetMetadata
            {
                Name = name, Layer = layer, SourceFile = sourceFile,
                RegisteredAt = DateTime.UtcNow.ToString("o"), Notes = notes,
            });
            SaveMetadata(list);
        }

        public List<SnippetMetadata> LoadMetadata()
        {
            var path = Path.Combine(_root, RegisterJsonName);
            if (!File.Exists(path)) return new List<SnippetMetadata>();
            try
            {
                return JsonSerializer.Deserialize<List<SnippetMetadata>>(
                           File.ReadAllText(path, Encoding.UTF8)) ?? new List<SnippetMetadata>();
            }
            catch { return new List<SnippetMetadata>(); }
        }

        private void SaveMetadata(List<SnippetMetadata> list)
        {
            Directory.CreateDirectory(_root);
            File.WriteAllText(Path.Combine(_root, RegisterJsonName),
                JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }),
                new UTF8Encoding(false));
        }

        public IReadOnlyList<SnippetMetadata> ListSnippets(string? layerName = null)
        {
            var all = LoadMetadata();
            return layerName == null ? all : all.Where(m => m.Layer == layerName).ToList();
        }
    }
}