using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerSimBuilder
{
    /// <summary>
    /// 生成済み controller_box_powersim.py を解析し、Canvas パレット用の
    /// ComponentDescriptor を動的生成する。
    ///
    /// Task 3-C-6: StaTable XML から生成されたコントローラごとに
    /// 固有のポート構成を持つ Descriptor を作る（α 案）。
    /// </summary>
    public static class ControllerBoxDescriptorBuilder
    {
        // self.globals["x"] の読み取り
        private static readonly Regex ReadBracketRegex =
            new Regex(@"self\.globals\[""([A-Za-z_]\w*)""\]", RegexOptions.Compiled);

        // self.globals.get("x", ...) の読み取り
        private static readonly Regex ReadGetRegex =
            new Regex(@"self\.globals\.get\(\s*""([A-Za-z_]\w*)""", RegexOptions.Compiled);

        // self.globals["x"] = ... の書き込み
        private static readonly Regex WriteRegex =
            new Regex(@"self\.globals\[""([A-Za-z_]\w*)""\]\s*=", RegexOptions.Compiled);

        // 変数名として有効（末尾 _ は動的キー構築の断片なので除外）
        private static readonly Regex ValidVarRegex =
            new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        // ControllerBox が自動設定するため Input/Output に含めない
        private static readonly HashSet<string> ExcludedFromPorts =
            new HashSet<string>(StringComparer.Ordinal)
        {
            "g_system_tick",
        };

        public static ComponentDescriptor Build(
            string projectName,
            string generatedPyPath,
            string sourceXmlPath = null)
        {
            if (string.IsNullOrWhiteSpace(projectName))
                throw new ArgumentException("projectName is empty", nameof(projectName));
            if (string.IsNullOrWhiteSpace(generatedPyPath))
                throw new ArgumentException("generatedPyPath is empty", nameof(generatedPyPath));
            if (!File.Exists(generatedPyPath))
                throw new FileNotFoundException("Generated .py not found", generatedPyPath);

            var py = File.ReadAllText(generatedPyPath, Encoding.UTF8);
            var (inputs, outputs) = ExtractPorts(py);

            var parameters = new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Name = "generated_path",
                    Type = "string",
                    DefaultValue = Path.GetFullPath(generatedPyPath),
                },
                new ParameterDescriptor
                {
                    Name = "auto_events",
                    Type = "string",
                    DefaultValue = "",
                },
            };

            return new ComponentDescriptor
            {
                Name = projectName,
                Type = "ControllerBox",
                Category = "Control",
                Parameters = parameters,
                InputPorts = inputs.ToArray(),
                OutputPorts = outputs.ToArray(),
            };
        }

        private static (List<string> inputs, List<string> outputs) ExtractPorts(string pySource)
        {
            var readVars = new HashSet<string>(StringComparer.Ordinal);
            var writtenVars = new HashSet<string>(StringComparer.Ordinal);

            foreach (Match m in ReadBracketRegex.Matches(pySource))
                AddIfValid(m.Groups[1].Value, readVars);
            foreach (Match m in ReadGetRegex.Matches(pySource))
                AddIfValid(m.Groups[1].Value, readVars);
            foreach (Match m in WriteRegex.Matches(pySource))
                AddIfValid(m.Groups[1].Value, writtenVars);

            // Read-only -> inputs, Write -> outputs
            var inputs = readVars
                .Where(v => !writtenVars.Contains(v))
                .OrderBy(v => v, StringComparer.Ordinal)
                .ToList();
            var outputs = writtenVars
                .OrderBy(v => v, StringComparer.Ordinal)
                .ToList();

            return (inputs, outputs);
        }

        private static void AddIfValid(string name, HashSet<string> dest)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (!ValidVarRegex.IsMatch(name)) return;
            if (name.EndsWith("_", StringComparison.Ordinal)) return;
            if (ExcludedFromPorts.Contains(name)) return;
            dest.Add(name);
        }
    }
}