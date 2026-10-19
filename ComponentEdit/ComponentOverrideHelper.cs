using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PowerSimBuilder.ComponentEdit
{
    /// <summary>
    /// C# 側の scaffold_override。Python 側 components.scaffold_override と等価。
    /// 共有ライブラリ本体は変更せず、user_components/ に全行コメント化コピーを生成する。
    /// </summary>
    public static class ComponentOverrideHelper
    {
        public static string Scaffold(string bundledFilePath, string targetDir, string name)
        {
            if (string.IsNullOrEmpty(bundledFilePath))
                throw new ArgumentNullException(nameof(bundledFilePath));
            if (!File.Exists(bundledFilePath))
                throw new FileNotFoundException("Bundled component not found: " + bundledFilePath);
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            var dst = Path.Combine(targetDir, name + ".py");
            if (File.Exists(dst))
                throw new IOException("Override already exists: " + dst);

            var lines = File.ReadAllLines(bundledFilePath);
            var header = new[]
            {
                "# override: " + Path.GetFileName(bundledFilePath),
                "# Bundled source is read-only. Only the lines you uncomment take effect.",
                "# Keep the rest commented so diffs against the bundled file remain minimal.",
                "",
            };
            var body = lines.Select(l => "# " + l);
            var content = string.Join("\n", header.Concat(body)) + "\n";
            File.WriteAllText(dst, content, new UTF8Encoding(false));
            return dst;
        }
    }
}