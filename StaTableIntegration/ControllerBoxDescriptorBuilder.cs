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

        // A+C (Task 3-C-6+): 変数分類の既定リスト。
        // python_runtime/components/controller_box.py の
        // DEFAULT_INPUT_GLOBALS / DEFAULT_OUTPUT_GLOBALS と同期させること。
        private static readonly HashSet<string> DefaultInputGlobals =
            new HashSet<string>(StringComparer.Ordinal)
        {
            "thermistor_top",
            "thermistor_bottom",
            "thermistor_middle",
            "ir_value",
            "steam_sensor",
            "door_open",
            "mw_anode_current",
            "rx_buf",
            "seq_total",
            "stage_duration",
            "steam_target_temp",
            "mw_target_power",
            "oven_target_temp",
        };

        private static readonly HashSet<string> DefaultOutputGlobals =
            new HashSet<string>(StringComparer.Ordinal)
        {
            "heater_top_duty",
            "heater_middle_duty",
            "heater_bottom_duty",
            "fan_duty",
            "pump_on",
            "mag_on",
            "mw_power",
            "error_code",
            "seq_step",
            "stage_mode",
            "boiler_temp",
        };

        // α: "# @variables: name1, name2, ..." 行（Generator が出力）
        private static readonly Regex PortHintRegex =
            new Regex(@"^#\s*@variables\s*:\s*(.+)$",
                RegexOptions.Compiled | RegexOptions.Multiline);

        // Stage 3: GLOBAL_VARIABLES = { ... } ブロック
        private static readonly Regex GlobalVarBlockRegex =
            new Regex(@"^GLOBAL_VARIABLES\s*=\s*\{(?<body>.*?)^\}",
                RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline);

        private static readonly Regex GlobalVarKeyRegex =
            new Regex(@"""([A-Za-z_]\w*)""\s*:",
                RegexOptions.Compiled);

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
            // Stage 1: self.globals[...] を直接解析（完成済み .py 用）
            var (in1, out1) = ExtractFromGlobalsAccess(pySource);
            if (in1.Count + out1.Count > 0) return (in1, out1);

            // Stage 2: "# @variables: ..." マーカー（Generator α で追加）
            var (in2, out2) = ExtractFromPortHints(pySource);
            if (in2.Count + out2.Count > 0) return (in2, out2);

            // Stage 3: GLOBAL_VARIABLES = { ... } + DEFAULT_*_GLOBALS（安全側）
            return ExtractFromGlobalVariables(pySource);
        }

        private static (List<string> inputs, List<string> outputs) ExtractFromGlobalsAccess(string pySource)
        {
            var readVars = new HashSet<string>(StringComparer.Ordinal);
            var writtenVars = new HashSet<string>(StringComparer.Ordinal);

            foreach (Match m in ReadBracketRegex.Matches(pySource))
                AddIfValid(m.Groups[1].Value, readVars);
            foreach (Match m in ReadGetRegex.Matches(pySource))
                AddIfValid(m.Groups[1].Value, readVars);
            foreach (Match m in WriteRegex.Matches(pySource))
                AddIfValid(m.Groups[1].Value, writtenVars);

            var inputs = readVars
                .Where(v => !writtenVars.Contains(v))
                .OrderBy(v => v, StringComparer.Ordinal)
                .ToList();
            var outputs = writtenVars
                .OrderBy(v => v, StringComparer.Ordinal)
                .ToList();

            return (inputs, outputs);
        }

        private static (List<string> inputs, List<string> outputs) ExtractFromPortHints(string pySource)
        {
            var allNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in PortHintRegex.Matches(pySource))
            {
                foreach (var raw in m.Groups[1].Value.Split(','))
                {
                    var name = raw.Trim();
                    if (!string.IsNullOrEmpty(name))
                        AddIfValid(name, allNames);
                }
            }
            return ClassifyVariables(allNames);
        }

        private static (List<string> inputs, List<string> outputs) ExtractFromGlobalVariables(string pySource)
        {
            var allNames = new HashSet<string>(StringComparer.Ordinal);
            var block = GlobalVarBlockRegex.Match(pySource);
            if (block.Success)
            {
                foreach (Match m in GlobalVarKeyRegex.Matches(block.Groups["body"].Value))
                    AddIfValid(m.Groups[1].Value, allNames);
            }
            return ClassifyVariables(allNames);
        }

        private static (List<string> inputs, List<string> outputs) ClassifyVariables(HashSet<string> allNames)
        {
            var inputs = new List<string>();
            var outputs = new List<string>();

            foreach (var name in allNames.OrderBy(v => v, StringComparer.Ordinal))
            {
                bool isInput = DefaultInputGlobals.Contains(name);
                bool isOutput = DefaultOutputGlobals.Contains(name);

                if (isInput && !isOutput) inputs.Add(name);
                else if (isOutput && !isInput) outputs.Add(name);
                else
                {
                    // 不明 or 両方 → 両側に追加（安全側）
                    inputs.Add(name);
                    outputs.Add(name);
                }
            }
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