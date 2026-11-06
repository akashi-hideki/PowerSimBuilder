using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PowerSimBuilder
{
    /// <summary>
    /// Role function snippet metadata (stored in templates/role_snippets/index.json).
    /// </summary>
    public class RoleSnippet
    {
        public string Layer { get; set; } = "";
        public string Func { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string File { get; set; } = "";
    }

    public class RoleSnippetIndex
    {
        public List<RoleSnippet> Snippets { get; set; } = new();
    }

    /// <summary>
    /// Storage for role function snippets (Phase 1 of SPEC_ROLE_SNIPPETS).
    /// Default location: &lt;repo-root&gt;/templates/role_snippets/.
    /// JSON files and .py files are written as UTF-8 without BOM, LF line endings.
    /// </summary>
    public static class RoleSnippetStorage
    {
        public const string FolderName = "role_snippets";
        public const string IndexFileName = "index.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>
        /// Locate the repository root (the folder containing PowerSimBuilder.sln).
        /// Mirrors ComponentManager.FindRepoRoot behavior.
        /// </summary>
        public static string ResolveRoot()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "PowerSimBuilder.sln"))) return dir;
                var parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            throw new InvalidOperationException(
                "Repository root (PowerSimBuilder.sln) not found from " + AppDomain.CurrentDomain.BaseDirectory);
        }

        public static string ResolveFolder() =>
            Path.Combine(ResolveRoot(), "templates", FolderName);

        public static string ResolveIndexPath() =>
            Path.Combine(ResolveFolder(), IndexFileName);

        public static List<RoleSnippet> LoadAll() => LoadAll(ResolveFolder());

        /// <summary>Test-friendly overload: read snippets from an explicit folder.</summary>
        public static List<RoleSnippet> LoadAll(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return new List<RoleSnippet>();
            var indexPath = Path.Combine(folderPath, IndexFileName);
            if (!File.Exists(indexPath)) return new List<RoleSnippet>();
            try
            {
                var json = File.ReadAllText(indexPath, System.Text.Encoding.UTF8);
                var index = JsonSerializer.Deserialize<RoleSnippetIndex>(json, JsonOptions);
                return index?.Snippets ?? new List<RoleSnippet>();
            }
            catch
            {
                return new List<RoleSnippet>();
            }
        }

        public static string LoadCode(RoleSnippet snippet) => LoadCode(snippet, ResolveFolder());

        public static string LoadCode(RoleSnippet snippet, string folderPath)
        {
            if (snippet == null || string.IsNullOrEmpty(snippet.File)) return "";
            var path = Path.Combine(folderPath, snippet.File);
            return File.Exists(path) ? File.ReadAllText(path, System.Text.Encoding.UTF8) : "";
        }

        public static void Save(RoleSnippet snippet, string code) => Save(snippet, code, ResolveFolder());

        public static void Save(RoleSnippet snippet, string code, string folderPath)
        {
            if (snippet == null) throw new ArgumentNullException(nameof(snippet));
            if (string.IsNullOrEmpty(snippet.File))
            {
                if (string.IsNullOrEmpty(snippet.Layer) || string.IsNullOrEmpty(snippet.Func))
                    throw new InvalidOperationException("Layer and Func are required to derive a filename.");
                snippet.File = snippet.Layer + "." + snippet.Func + ".py";
            }

            Directory.CreateDirectory(folderPath);
            var codePath = Path.Combine(folderPath, snippet.File);
            var normalizedCode = (code ?? "").Replace("\r\n", "\n").TrimEnd('\n') + "\n";
            File.WriteAllText(codePath, normalizedCode, new System.Text.UTF8Encoding(false));

            var snippets = LoadAll(folderPath);
            var idx = snippets.FindIndex(s =>
                string.Equals(s.Layer, snippet.Layer, StringComparison.Ordinal) &&
                string.Equals(s.Func, snippet.Func, StringComparison.Ordinal));
            if (idx >= 0) snippets[idx] = snippet; else snippets.Add(snippet);

            WriteIndex(snippets, folderPath);
        }

        public static void Delete(RoleSnippet snippet) => Delete(snippet, ResolveFolder());

        public static void Delete(RoleSnippet snippet, string folderPath)
        {
            if (snippet == null) return;
            var snippets = LoadAll(folderPath);
            snippets.RemoveAll(s =>
                string.Equals(s.Layer, snippet.Layer, StringComparison.Ordinal) &&
                string.Equals(s.Func, snippet.Func, StringComparison.Ordinal));

            if (!string.IsNullOrEmpty(snippet.File))
            {
                var codePath = Path.Combine(folderPath, snippet.File);
                if (File.Exists(codePath)) File.Delete(codePath);
            }

            WriteIndex(snippets, folderPath);
        }

        private static void WriteIndex(List<RoleSnippet> snippets, string folderPath)
        {
            Directory.CreateDirectory(folderPath);
            var index = new RoleSnippetIndex { Snippets = snippets };
            var json = JsonSerializer.Serialize(index, JsonOptions);
            var normalized = json.Replace("\r\n", "\n").TrimEnd('\n') + "\n";
            File.WriteAllText(Path.Combine(folderPath, IndexFileName), normalized, new System.Text.UTF8Encoding(false));
        }
    }
}
