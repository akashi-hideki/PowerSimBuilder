using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PowerSimBuilder.ComponentEdit
{
    public static class ComponentScanner
    {
        private static readonly Regex ClassRegex = new Regex(
            @"^class\s+(?<name>\w+)\s*[\(:]",
            RegexOptions.Compiled | RegexOptions.Multiline);

        public static IReadOnlyList<ComponentInfo> ScanBundled(string rootDir)
        {
            var dir = Path.Combine(rootDir, "src", "python_runtime", "components");
            return ScanPythonClasses(dir, ComponentSourceKind.Bundled);
        }

        public static IReadOnlyList<ComponentInfo> ScanOverride(string projectDir)
        {
            var dir = Path.Combine(projectDir, "user_components");
            return ScanPythonClasses(dir, ComponentSourceKind.Override);
        }

        /// <summary>
        /// Bundled and override, merged. When a name exists in both,
        /// only the override is returned (override wins).
        /// </summary>
        public static IReadOnlyList<ComponentInfo> ScanAll(string rootDir, string projectDir)
        {
            var overrides = ScanOverride(projectDir);
            var overrideByName = new Dictionary<string, ComponentInfo>(StringComparer.Ordinal);
            foreach (var o in overrides) overrideByName[o.Name] = o;

            var result = new List<ComponentInfo>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var b in ScanBundled(rootDir))
            {
                if (overrideByName.ContainsKey(b.Name))
                {
                    if (seen.Add(b.Name)) result.Add(overrideByName[b.Name]);
                }
                else
                {
                    if (seen.Add(b.Name)) result.Add(b);
                }
            }
            foreach (var o in overrides)
            {
                if (seen.Add(o.Name)) result.Add(o);
            }
            return result;
        }

        private static IReadOnlyList<ComponentInfo> ScanPythonClasses(string dir, ComponentSourceKind kind)
        {
            var result = new List<ComponentInfo>();
            if (!Directory.Exists(dir)) return result;

            foreach (var file in Directory.GetFiles(dir, "*.py"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.StartsWith("_")) continue;

                string content;
                try { content = File.ReadAllText(file); }
                catch { continue; }

                foreach (Match m in ClassRegex.Matches(content))
                {
                    var cls = m.Groups["name"].Value;
                    if (cls.StartsWith("_")) continue;
                    result.Add(new ComponentInfo
                    {
                        Name = cls,
                        Type = cls,
                        Category = "",
                        Source = kind,
                        FilePath = file,
                    });
                }
            }
            return result;
        }
    }
}