using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerSimBuilder
{
    /// <summary>
    /// エラーカテゴリを定義するEnum
    /// </summary>
    public enum ErrorCategory
    {
        BuildError,
        ValidationError,
        GenerationError,
        ComponentError,
        ConnectionError,
        FileWriteError,
        SectionAccessError
    }

    /// <summary>
    /// Pythonシミュレーションコード生成ビルダークラス
    /// </summary>
    public class PythonSimulationMainBuilder
    {
        #region Constants

        /// <summary>ロギングコードの固定テーブル</summary>
        private static readonly List<string> LoggingCodeTable = new List<string>
        {
            "        sim_data.logs.append({",
            "            \"time\": t,",
            "            \"components\": {cid: {\"inputs\": comp.inputs.copy(),",
            "                                 \"outputs\": comp.outputs.copy(),",
            "                                 \"parameters\": comp.parameters.copy()}",
            "                           for cid, comp in components.items()}",
            "        })"
        };

        /// <summary>接続更新ループコードの固定テーブル</summary>
        private static readonly List<string> ConnectionUpdateCodeTable = new List<string>
        {
            "        for conn in connections:",
            "            src_id, src_sec, src_port = conn[\"from\"].split(\".\")",
            "            dst_id, dst_sec, dst_port = conn[\"to\"].split(\".\")",
            "            components[dst_id].inputs[dst_port] = components[src_id].outputs[src_port]"
        };

        /// <summary>コンポーネント更新ループコードの固定テーブル</summary>
        private static readonly List<string> ComponentUpdateCodeTable = new List<string>
        {
            "        for comp in components.values():",
            "            comp.update(t, dt, sim_data)"
        };

        /// <summary>シミュレーションループ開始コードの固定テーブル</summary>
        private static readonly List<string> SimulationLoopStartCodeTable = new List<string>
        {
            "    n_steps = int(round(t_end / dt))",
            "    for i in range(n_steps + 1):",
            "        t = i * dt"
        };

        /// <summary>エラーメッセージの固定テーブル</summary>
        private static readonly Dictionary<ErrorCategory, string> ErrorMessageKeys = new Dictionary<ErrorCategory, string>
        {
            { ErrorCategory.BuildError, "PythonSimulationMainBuilder.ErrBuild" },
            { ErrorCategory.ValidationError, "PythonSimulationMainBuilder.ErrValidation" },
            { ErrorCategory.GenerationError, "PythonSimulationMainBuilder.ErrGeneration" },
            { ErrorCategory.ComponentError, "PythonSimulationMainBuilder.ErrComponent" },
            { ErrorCategory.ConnectionError, "PythonSimulationMainBuilder.ErrConnection" },
            { ErrorCategory.FileWriteError, "PythonSimulationMainBuilder.ErrFileWrite" },
            { ErrorCategory.SectionAccessError, "PythonSimulationMainBuilder.ErrSectionAccess" }
        };

        /// <summary>Pythonデフォルト値変換マッピング</summary>
        private static readonly Dictionary<string, Func<object, string>> PythonValueConverters = new Dictionary<string, Func<object, string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "string", value => value != null ? $"\"{value}\"" : "None" },
            { "int", value => value != null ? value.ToString() : "0" },
            { "integer", value => value != null ? value.ToString() : "0" },
            { "double", value => value != null ? value.ToString().Replace(',', '.') : "0.0" },
            { "float", value => value != null ? value.ToString().Replace(',', '.') : "0.0" },
            { "bool", value => value != null ? ((bool)value ? "True" : "False") : "False" },
            { "boolean", value => value != null ? ((bool)value ? "True" : "False") : "False" },
            { "list", value => "[]" },
            { "array", value => "[]" },
            { "dict", value => "{}" },
            { "dictionary", value => "{}" },
            { "datetime", value => value != null ? $"datetime.strptime(\"{value}\", \"%Y-%m-%d %H:%M:%S\")" : "None" }
        };

        #endregion

        #region Properties

        /// <summary>出力ファイルパス</summary>
        public string FilePath { get; set; }

        /// <summary>コンポーネント記述子リスト</summary>
        public List<ComponentDescriptor> Components { get; set; }

        /// <summary>Task 3-C-8: ComponentControl instances (InstanceName + runtime Parameters).</summary>
        public List<ComponentControl> ComponentInstances { get; set; }

        /// <summary>接続リスト</summary>
        public List<Connection> Connections { get; set; }

        /// <summary>エラーハンドラーデリゲート</summary>
        public Action<ErrorCategory, string> ErrorHandler { get; set; } = DefaultErrorHandler;

        /// <summary>Simulation t_end (seconds). Written into the generated t_end = X line.</summary>
        public double TEnd { get; set; } = 5.0;
        public Dictionary<string, List<string>> CodeSections { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public PythonSimulationMainBuilder()
        {
            // デフォルトエラーハンドラーを設定
            ErrorHandler = DefaultErrorHandler;

            // 静的codeSectionsを初期化
            InitializeCodeSections();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Pythonシミュレーションファイルをビルドする
        /// </summary>
        public void BuildPythonSimulationFile()
        {
            try
            {
                ValidateProperties();
                GenerateAllSections();
                WriteToFile();
            }
            catch (Exception ex)
            {
                HandleError(ErrorCategory.BuildError, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 指定されたセクションのコード行を取得する
        /// </summary>
        /// <param name="sectionName">セクション名</param>
        /// <returns>コード行リスト</returns>
        public List<string> GetCodeSection(string sectionName)
        {
            if (CodeSections.ContainsKey(sectionName))
                return new List<string>(CodeSections[sectionName]);

            HandleError(ErrorCategory.SectionAccessError, sectionName);
            throw new ArgumentException($"Section '{sectionName}' not found.");
        }

        /// <summary>
        /// 指定されたセクションのコード行を設定する
        /// </summary>
        /// <param name="sectionName">セクション名</param>
        /// <param name="lines">コード行リスト</param>
        public void SetCodeSection(string sectionName, List<string> lines)
        {
            if (CodeSections.ContainsKey(sectionName))
                CodeSections[sectionName] = lines;
            else
                throw new ArgumentException($"Section '{sectionName}' not found.");
        }

        /// <summary>
        /// 指定されたセクションに行を追加する
        /// </summary>
        /// <param name="sectionName">セクション名</param>
        /// <param name="line">追加する行</param>
        public void AddLineToSection(string sectionName, string line)
        {
            if (!CodeSections.ContainsKey(sectionName))
                CodeSections.Add(sectionName, new List<string>());
            CodeSections[sectionName].Add(line);
        }

        /// <summary>
        /// 指定されたセクションの指定位置に行を挿入する
        /// </summary>
        /// <param name="sectionName">セクション名</param>
        /// <param name="index">挿入位置</param>
        /// <param name="line">挿入する行</param>
        public void InsertLineToSection(string sectionName, int index, string line)
        {
            if (CodeSections.ContainsKey(sectionName))
                CodeSections[sectionName].Insert(index, line);
            else
                throw new ArgumentException($"Section '{sectionName}' not found.");
        }

        /// <summary>
        /// 指定されたセクションをクリアする
        /// </summary>
        /// <param name="sectionName">セクション名</param>
        public void ClearSection(string sectionName)
        {
            if (CodeSections.ContainsKey(sectionName))
                CodeSections[sectionName].Clear();
            else
                throw new ArgumentException($"Section '{sectionName}' not found.");
        }

        /// <summary>
        /// 新しいセクションを追加する
        /// </summary>
        /// <param name="sectionName">セクション名</param>
        public void AddNewSection(string sectionName)
        {
            if (!CodeSections.ContainsKey(sectionName))
                CodeSections.Add(sectionName, new List<string>());
        }

        /// <summary>
        /// 指定されたセクションを削除する
        /// </summary>
        /// <param name="sectionName">セクション名</param>
        public void RemoveSection(string sectionName)
        {
            if (CodeSections.ContainsKey(sectionName))
                CodeSections.Remove(sectionName);
        }

        /// <summary>
        /// すべてのセクションを取得する
        /// </summary>
        /// <returns>セクション辞書</returns>
        public Dictionary<string, List<string>> GetAllSections()
        {
            return new Dictionary<string, List<string>>(CodeSections);
        }

        /// <summary>
        /// Python値コンバーターを追加または更新する
        /// </summary>
        /// <param name="typeName">型名</param>
        /// <param name="converter">変換関数</param>
        public static void AddOrUpdatePythonValueConverter(string typeName, Func<object, string> converter)
        {
            PythonValueConverters[typeName] = converter;
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Generate the Python import line for the components used.
        /// </summary>
        private string GenerateComponentsImportLine()
        {
            if (Components == null || Components.Count == 0)
                return "from python_runtime import components";

            var types = Components
                .Where(c => !string.IsNullOrEmpty(c.Type))
                .Select(c => c.Type)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            // v1.8.0 bug #2: when a component has a FMU-enhanced file
            // (generated/<Type>.py containing fmu_wrapper import), import from
            // that file so the FMU runs. Otherwise fall back to python_runtime.
            var fmuTypes = new List<string>();
            var runtimeTypes = new List<string>();
            foreach (var type in types)
            {
                if (IsFmuEnhancedComponent(type))
                    fmuTypes.Add(type);
                else
                    runtimeTypes.Add(type);
            }

            var importLines = new List<string>();
            foreach (var type in fmuTypes)
                importLines.Add("from " + type + " import " + type);
            if (runtimeTypes.Count > 0)
                importLines.Add("from python_runtime.components import " + string.Join(", ", runtimeTypes));

            return string.Join("\n", importLines);
        }

        /// <summary>
        /// v1.8.0 bug #2: true when generated/<Type>.py exists and contains
        /// "from python_runtime.fmu_wrapper import" (i.e. FMU-enhanced).
        /// </summary>
        private static bool IsFmuEnhancedComponent(string typeName)
        {
            var candidates = new[]
            {
                System.IO.Path.Combine("generated", typeName + ".py"),
                typeName + ".py",
            };
            foreach (var candidate in candidates)
            {
                try
                {
                    if (System.IO.File.Exists(candidate) &&
                        System.IO.File.ReadAllText(candidate)
                            .Contains("from python_runtime.fmu_wrapper import"))
                        return true;
                }
                catch
                {
                    // ignore IO errors; try next candidate
                }
            }
            return false;
        }

        /// <summary>
        /// コードセクションを初期化する
        /// </summary>
        private void InitializeCodeSections()
        {
            CodeSections = new Dictionary<string, List<string>>
            {
                {"imports", new List<string>()},
                {"function_def", new List<string>()},
                {"plot_config", new List<string>()},
                {"plotting_functions", new List<string>()}
            };
        }

        /// <summary>
        /// デフォルトエラーハンドラー
        /// </summary>
        /// <param name="category">エラーカテゴリ</param>
        /// <param name="message">エラーメッセージ</param>
        private static void DefaultErrorHandler(ErrorCategory category, string message)
        {
            LogcatTraceHelper.Instance.Log(category.ToString(), message);
        }

        /// <summary>
        /// エラーメッセージを取得する
        /// </summary>
        /// <param name="category">エラーカテゴリ</param>
        /// <param name="args">メッセージ引数</param>
        /// <returns>フォーマットされたエラーメッセージ</returns>
        private static string GetErrorMessage(ErrorCategory category, params object[] args)
        {
            if (ErrorMessageKeys.TryGetValue(category, out string key))
            {
                return I18n.T(key, args);
            }
            return I18n.T("PythonSimulationMainBuilder.ErrUnknown");
        }

        /// <summary>
        /// プロパティの検証を行う
        /// </summary>
        private void ValidateProperties()
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                var message = GetErrorMessage(ErrorCategory.ValidationError, "FilePath must be set before building.");
                throw new InvalidOperationException(message);
            }

            if (Components == null)
            {
                var message = GetErrorMessage(ErrorCategory.ValidationError, "Components must be set before building.");
                throw new InvalidOperationException(message);
            }

            if (Connections == null)
            {
                var message = GetErrorMessage(ErrorCategory.ValidationError, "Connections must be set before building.");
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// すべてのセクションを生成する
        /// </summary>
        private void GenerateAllSections()
        {
            try
            {
                GenerateImportsSection();
                GenerateFunctionDefinitionSection();
                GeneratePlotConfigSection();
                GeneratePlottingFunctionsSection();
            }
            catch (Exception ex)
            {
                HandleError(ErrorCategory.GenerationError, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// インポートセクションを生成する
        /// </summary>
        private void GenerateImportsSection()
        {
            ClearSection("imports");

            AddLineToSection("imports", "from __future__ import annotations");
            AddLineToSection("imports", "");
            AddLineToSection("imports", "import numpy as np");
            AddLineToSection("imports", "import matplotlib.pyplot as plt");
            AddLineToSection("imports", "");
            AddLineToSection("imports", "from python_runtime import SimulationData");
            // v1.8.0 bug #2: GenerateComponentsImportLine may return multiple lines
            foreach (var importLine in GenerateComponentsImportLine().Split('\n'))
                AddLineToSection("imports", importLine);

            if (NeedsDateTimeImport())
            {
                AddLineToSection("imports", "from datetime import datetime");
            }

            AddLineToSection("imports", "");
        }

        /// <summary>
        /// DateTimeインポートが必要かどうかをチェックする
        /// </summary>
        /// <returns>DateTimeインポートが必要な場合はtrue</returns>
        private bool NeedsDateTimeImport()
        {
            if (Components == null) return false;

            foreach (var comp in Components)
            {
                if (comp.Parameters != null && comp.Parameters.Any(p =>
                    p.Type != null && p.Type.Equals("datetime", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 関数定義セクションを生成する
        /// </summary>
        private void GenerateFunctionDefinitionSection()
        {
            AddLineToSection("function_def", "def main():");
            AddTimeStepDefinition();
            AddSimulationDataDefinition();
            AddComponentDictionaryInFunction();
            GenerateComponentParametersInFunction();
            AddConnectionsListInFunction();
            AddSimulationLoopInFunction();

            AddLineToSection("function_def", "");
            AddLineToSection("function_def", "    # シミュレーション実行後のプロット");
            AddLineToSection("function_def", "    plot_signals(sim_data, plot_config)");
            AddLineToSection("function_def", "    print(\"シミュレーション完了\")");

            AddLineToSection("function_def", "");
        }

        /// <summary>
        /// 時間ステップ定義を追加する
        /// </summary>
        private void AddTimeStepDefinition()
        {
            AddLineToSection("function_def", "    dt = 0.001");
            AddLineToSection("function_def",
                "    t_end = " + TEnd.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// シミュレーションデータ定義を追加する
        /// </summary>
        private void AddSimulationDataDefinition()
        {
            AddLineToSection("function_def", "    sim_data = SimulationData(time_step=dt)");
        }

        /// <summary>
        /// 関数内でコンポーネントパラメータを初期化するコードを追加する
        /// </summary>
        private void GenerateComponentParametersInFunction()
        {
            // Task 3-C-8: use ComponentInstances (runtime Parameters Dictionary)
            if (ComponentInstances != null && ComponentInstances.Count > 0)
            {
                foreach (var comp in ComponentInstances)
                {
                    if (comp == null || string.IsNullOrEmpty(comp.InstanceName)) continue;
                    if (comp.Parameters == null || comp.Parameters.Count == 0) continue;

                    AddLineToSection("function_def", $"    # {comp.InstanceName} のパラメータ設定");
                    var compName = comp.InstanceName.ToLower();
                    foreach (var kvp in comp.Parameters)
                    {
                        // Task 3-C-8: skip null parameters (keep __init__ defaults)
                        if (kvp.Value == null) continue;
                        if (kvp.Value is System.Text.Json.JsonElement je2 &&
                            (je2.ValueKind == System.Text.Json.JsonValueKind.Null ||
                             je2.ValueKind == System.Text.Json.JsonValueKind.Undefined))
                            continue;

                        var pyValue = GetPythonValueFromObject(kvp.Value);
                        AddLineToSection("function_def", $"    components[\"{compName}\"].parameters[\"{kvp.Key}\"] = {pyValue}");
                    }
                    AddLineToSection("function_def", "");
                }
                return;
            }

            // Fallback: legacy path
            if (Components == null) return;
            foreach (var comp in Components)
            {
                if (comp.Parameters != null && comp.Parameters.Any())
                {
                    AddLineToSection("function_def", $"    # {comp.Name} のパラメータ設定");
                    var compName = comp.Name.ToLower();
                    foreach (var param in comp.Parameters)
                    {
                        var defaultValue = GetPythonDefaultValue(param.DefaultValue, param.Type);
                        AddLineToSection("function_def", $"    components[\"{compName}\"].parameters[\"{param.Name}\"] = {defaultValue}");
                    }
                    AddLineToSection("function_def", "");
                }
            }
        }

        /// <summary>Task 3-C-8: convert runtime object to Python literal.
        /// Handles System.Text.Json.JsonElement (from system.json) and
        /// numeric-string coercion (e.g. "50" -> 50.0).</summary>
        private static string GetPythonValueFromObject(object value)
        {
            if (value == null) return "None";

            // System.Text.Json deserializes object as JsonElement
            if (value is System.Text.Json.JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case System.Text.Json.JsonValueKind.Null:
                    case System.Text.Json.JsonValueKind.Undefined:
                        return "None";
                    case System.Text.Json.JsonValueKind.True:  return "True";
                    case System.Text.Json.JsonValueKind.False: return "False";
                    case System.Text.Json.JsonValueKind.Number:
                        return je.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
                    case System.Text.Json.JsonValueKind.String:
                        var s = je.GetString() ?? "";
                        // numeric-string coercion
                        if (double.TryParse(s, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out double d))
                            return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
                }
                return "None";
            }

            if (value is string sv)
            {
                if (double.TryParse(sv, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double sd))
                    return sd.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return "\"" + sv.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            }
            if (value is bool bv) return bv ? "True" : "False";
            if (value is double dv) return dv.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value is float fv) return fv.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value is int iv) return iv.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value is long lv) return lv.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return "\"" + value.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        /// <summary>
        /// C#のデフォルト値をPythonのデフォルト値に変換する
        /// </summary>
        /// <param name>デフォルト値</param>
        /// <param name="type">パラメータタイプ</param>
        /// <returns>Python形式のデフォルト値</returns>
        private string GetPythonDefaultValue(object defaultValue, string type)
        {
            if (PythonValueConverters.TryGetValue(type, out Func<object, string> converter))
            {
                return converter(defaultValue);
            }

            return defaultValue != null ? $"\"{defaultValue}\"" : "None";
        }

        /// <summary>
        /// ControllerBox には input_globals / output_globals を明示指定する（Task 3-C-10）。
        /// それ以外の型は従来通り Type("name") を返す。
        /// </summary>
        private static string BuildComponentCtorArgs(ComponentDescriptor desc, string compName)
        {
            if (desc != null && desc.Type == "ControllerBox")
            {
                var inputs = desc.InputPorts ?? new string[0];
                var outputs = desc.OutputPorts ?? new string[0];
                return "ControllerBox(\"" + compName + "\", input_globals=" + FormatPyList(inputs)
                     + ", output_globals=" + FormatPyList(outputs) + ")";
            }
            return (desc?.Type ?? "Component") + "(\"" + compName + "\")";
        }

        private static string FormatPyList(IEnumerable<string> items)
        {
            if (items == null) return "[]";
            var quoted = items
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => "\"" + s + "\"");
            return "[" + string.Join(", ", quoted) + "]";
        }


        /// <summary>
        /// 関数内でコンポーネント辞書を追加する
        /// </summary>
        private void AddComponentDictionaryInFunction()
        {
            AddLineToSection("function_def", "    # コンポーネント辞書の定義");
            AddLineToSection("function_def", "    components = {");

            // Task 3-C-8: prefer ComponentInstances (InstanceName + runtime)
            if (ComponentInstances != null && ComponentInstances.Count > 0)
            {
                foreach (var comp in ComponentInstances)
                {
                    try
                    {
                        if (comp == null || comp.Descriptor == null ||
                            string.IsNullOrEmpty(comp.InstanceName) ||
                            string.IsNullOrEmpty(comp.Descriptor.Type))
                            continue;

                        var compName = comp.InstanceName.ToLower();
                        AddLineToSection("function_def", $"        \"{compName}\": {BuildComponentCtorArgs(comp.Descriptor, compName)},");
                    }
                    catch (Exception ex)
                    {
                        HandleError(ErrorCategory.ComponentError, comp?.InstanceName ?? "?", ex.Message);
                    }
                }
            }
            else
            {
                // Fallback: legacy path (descriptor only)
                foreach (var comp in Components)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(comp.Name) || string.IsNullOrEmpty(comp.Type))
                            continue;
                        var compName = comp.Name.ToLower();
                        AddLineToSection("function_def", $"        \"{compName}\": {BuildComponentCtorArgs(comp, compName)},");
                    }
                    catch (Exception ex)
                    {
                        HandleError(ErrorCategory.ComponentError, comp?.Name ?? "?", ex.Message);
                    }
                }
            }

            AddLineToSection("function_def", "    }");
            AddLineToSection("function_def", "");
        }

        /// <summary>
        /// 関数内で接続リストを追加する
        /// </summary>
        private void AddConnectionsListInFunction()
        {
            AddLineToSection("function_def", "    # 接続リストの定義");
            AddLineToSection("function_def", "    connections = [");

            foreach (var conn in Connections)
            {
                try
                {
                    if (conn.StartComponent == null || string.IsNullOrEmpty(conn.StartPort) ||
                        conn.EndComponent == null || string.IsNullOrEmpty(conn.EndPort))
                        continue;

                    var fromComp = conn.StartComponent.InstanceName.ToLower();
                    var fromPort = conn.StartPort;
                    var toComp = conn.EndComponent.InstanceName.ToLower();
                    var toPort = conn.EndPort;

                    AddLineToSection("function_def", $"        {{\"from\":\"{fromComp}.outputs.{fromPort}\",\"to\":\"{toComp}.inputs.{toPort}\"}},");
                }
                catch (Exception ex)
                {
                    HandleError(ErrorCategory.ConnectionError, ex.Message);
                }
            }

            AddLineToSection("function_def", "    ]");
            AddLineToSection("function_def", "");
        }

        /// <summary>
        /// 関数内でシミュレーションループを追加する
        /// </summary>
        private void AddSimulationLoopInFunction()
        {
            // シミュレーションルoop開始
            foreach (var line in SimulationLoopStartCodeTable)
            {
                AddLineToSection("function_def", line);
            }

            // コンポーネント更新ループ
            foreach (var line in ComponentUpdateCodeTable)
            {
                AddLineToSection("function_def", line);
            }
            AddLineToSection("function_def", "");

            // 接続更新ループ
            foreach (var line in ConnectionUpdateCodeTable)
            {
                AddLineToSection("function_def", line);
            }
            AddLineToSection("function_def", "");

            // ロギングセクション
            foreach (var line in LoggingCodeTable)
            {
                AddLineToSection("function_def", line);
            }
        }

        /// <summary>
        /// プロット設定セクションを生成する
        /// </summary>
        private void GeneratePlotConfigSection()
        {
            AddLineToSection("plot_config", "# プロット設定（ベースデータ）");
            AddLineToSection("plot_config", "# 接続情報から自動生成された信号設定");
            AddLineToSection("plot_config", "plot_config = {");
            AddLineToSection("plot_config", "    'signals': [");

            // 接続情報から信号設定を生成
            if (Connections != null && Connections.Count > 0)
            {
                var colorPalette = new List<string>
                {
                    "#FF5733", "#33FF57", "#3357FF", "#F333FF", "#FF33F3",
                    "#33FFF3", "#F3FF33", "#5733FF", "#FF8333", "#33FF83"
                };

                var lineStyles = new List<string> { "-", "--", "-.", ":" };

                int colorIndex = 0;
                int lineStyleIndex = 0;

                foreach (var conn in Connections)
                {
                    if (conn.StartComponent == null) continue;

                    var fromComp = conn.StartComponent.InstanceName.ToLower();
                    var fromPort = conn.StartPort;

                    // 信号名を生成（コンポーネント名_ポート名）
                    var signalName = $"{fromComp}_{fromPort}";

                    // 色と線種を循環して割り当て
                    var color = colorPalette[colorIndex % colorPalette.Count];
                    var lineStyle = lineStyles[lineStyleIndex % lineStyles.Count];

                    AddLineToSection("plot_config", $"        {{'name': '{signalName}', 'component': '{fromComp}', 'port': '{fromPort}', 'enabled': True, 'color': '{color}', 'line_style': '{lineStyle}'}},");

                    colorIndex++;
                    if (colorIndex % 3 == 0) lineStyleIndex++; // 3つの信号ごとに線種を変更
                }
            }
            else
            {
                // 接続情報がない場合のデフォルト信号
                // v1.8.0 task 6a (v2): no connections but components exist.
                // ComponentInstances is List<ComponentControl> with InstanceName.
                // Components is List<ComponentDescriptor> (no instance name), so
                // we must use ComponentInstances for the demo_main key.
                // FMU-enhanced components expose FMU values via 'output'
                // thanks to the bug #5 auto-fallback in PhysicsSnippetBuilder.
                if (ComponentInstances != null && ComponentInstances.Count > 0)
                {
                    var compColors = new List<string>
                    {
                        "#FF5733", "#33FF57", "#3357FF", "#F333FF",
                        "#FF33F3", "#33FFF3", "#F3FF33", "#5733FF"
                    };
                    int compColorIdx = 0;
                    foreach (var comp in ComponentInstances)
                    {
                        if (string.IsNullOrEmpty(comp.InstanceName)) continue;
                        var compKey = comp.InstanceName.ToLower();
                        var compColor = compColors[compColorIdx % compColors.Count];
                        AddLineToSection("plot_config",
                            $"        {{'name': '{compKey}_output', 'component': '{compKey}', 'port': 'output', 'enabled': True, 'color': '{compColor}', 'line_style': '-'}},");
                        compColorIdx++;
                    }
                }
                else if (Components != null && Components.Count > 0)
                {
                    // Fallback: no ComponentInstances populated.
                    // (Should not happen in normal GUI flow, but keep for safety.)
                    var compColors = new List<string>
                    {
                        "#FF5733", "#33FF57", "#3357FF", "#F333FF"
                    };
                    int compColorIdx = 0;
                    foreach (var comp in Components)
                    {
                        if (string.IsNullOrEmpty(comp.Name)) continue;
                        var compKey = comp.Name.ToLower();
                        var compColor = compColors[compColorIdx % compColors.Count];
                        AddLineToSection("plot_config",
                            $"        {{'name': '{compKey}_output', 'component': '{compKey}', 'port': 'output', 'enabled': True, 'color': '{compColor}', 'line_style': '-'}},");
                        compColorIdx++;
                    }
                }
                else
                {
                    AddLineToSection("plot_config", "        # no signals: no connections and no components");
                }
            }

            AddLineToSection("plot_config", "    ],");
            AddLineToSection("plot_config", "    'global_settings': {");
            AddLineToSection("plot_config", "        'figure_size': (12, 8),");
            AddLineToSection("plot_config", "        'title': 'シミュレーション信号',");
            AddLineToSection("plot_config", "        'xlabel': '時間 (秒)',");
            AddLineToSection("plot_config", "        'ylabel': '値',");
            AddLineToSection("plot_config", "        'grid': True,");
            AddLineToSection("plot_config", "        'legend_location': 'upper right',");
            AddLineToSection("plot_config", "        'save_format': 'png',");
            AddLineToSection("plot_config", "        'dpi': 300");
            AddLineToSection("plot_config", "    }");
            AddLineToSection("plot_config", "}");
            AddLineToSection("plot_config", "");
        }

        /// <summary>
        /// プロット関数セクションを生成する
        /// </summary>
        private void GeneratePlottingFunctionsSection()
        {
            AddLineToSection("plotting_functions", "# プロット関数（コード）");
            AddLineToSection("plotting_functions", "def plot_signals(sim_data, plot_config):");
            AddLineToSection("plotting_functions", "    \"\"\"");
            AddLineToSection("plotting_functions", "    設定に基づいて信号をプロットする");
            AddLineToSection("plotting_functions", "    ");
            AddLineToSection("plotting_functions", "    Parameters:");
            AddLineToSection("plotting_functions", "    sim_data: SimulationData");
            AddLineToSection("plotting_functions", "        シミュレーションデータオブジェクト");
            AddLineToSection("plotting_functions", "    plot_config: dict");
            AddLineToSection("plotting_functions", "        プロット設定");
            AddLineToSection("plotting_functions", "    \"\"\"");
            AddLineToSection("plotting_functions", "    ");
            AddLineToSection("plotting_functions", "    # グローバル設定の適用");
            AddLineToSection("plotting_functions", "    settings = plot_config['global_settings']");
            AddLineToSection("plotting_functions", "    plt.figure(figsize=settings['figure_size'])");
            AddLineToSection("plotting_functions", "    plt.title(settings['title'])");
            AddLineToSection("plotting_functions", "    plt.xlabel(settings['xlabel'])");
            AddLineToSection("plotting_functions", "    plt.ylabel(settings['ylabel'])");
            AddLineToSection("plotting_functions", "    ");
            AddLineToSection("plotting_functions", "    if settings['grid']:");
            AddLineToSection("plotting_functions", "        plt.grid(True, linestyle='--', alpha=0.7)");
            AddLineToSection("plotting_functions", "    ");
            AddLineToSection("plotting_functions", "    # 有効な信号のみをフィルタリング");
            AddLineToSection("plotting_functions", "    enabled_signals = [s for s in plot_config['signals'] if s['enabled']]");
            AddLineToSection("plotting_functions", "    ");
            AddLineToSection("plotting_functions", "    # 各信号のデータを収集してプロット");
            AddLineToSection("plotting_functions", "    for signal in enabled_signals:");
            AddLineToSection("plotting_functions", "        comp_name = signal['component']");
            AddLineToSection("plotting_functions", "        port_name = signal['port']");
            AddLineToSection("plotting_functions", "        ");
            AddLineToSection("plotting_functions", "        # 時間と値を格納するリスト");
            AddLineToSection("plotting_functions", "        times = []");
            AddLineToSection("plotting_functions", "        values = []");
            AddLineToSection("plotting_functions", "        ");
            AddLineToSection("plotting_functions", "        # 各時間ステップのデータを収集");
            AddLineToSection("plotting_functions", "        for log in sim_data.logs:");
            AddLineToSection("plotting_functions", "            time = log['time']");
            AddLineToSection("plotting_functions", "            comp_data = log['components'].get(comp_name, {})");
            AddLineToSection("plotting_functions", "            outputs = comp_data.get('outputs', {})");
            AddLineToSection("plotting_functions", "            value = outputs.get(port_name, 0)");
            AddLineToSection("plotting_functions", "            ");
            AddLineToSection("plotting_functions", "            times.append(time)");
            AddLineToSection("plotting_functions", "            values.append(value)");
            AddLineToSection("plotting_functions", "        ");
            AddLineToSection("plotting_functions", "        # データをプロット");
            AddLineToSection("plotting_functions", "        if times and values:");
            AddLineToSection("plotting_functions", "            color = signal.get('color', None)");
            AddLineToSection("plotting_functions", "            line_style = signal.get('line_style', '-')");
            AddLineToSection("plotting_functions", "            ");
            AddLineToSection("plotting_functions", "            plt.plot(times, values, label=signal['name'], color=color, linestyle=line_style, linewidth=2)");
            AddLineToSection("plotting_functions", "    ");
            AddLineToSection("plotting_functions", "    # 凡例を追加");
            AddLineToSection("plotting_functions", "    plt.legend(loc=settings['legend_location'])");
            AddLineToSection("plotting_functions", "    ");
            AddLineToSection("plotting_functions", "    # レイアウト調整");
            AddLineToSection("plotting_functions", "    plt.tight_layout()");
            AddLineToSection("plotting_functions", "    ");
            AddLineToSection("plotting_functions", "    # グラフを保存");
            AddLineToSection("plotting_functions", "    save_format = settings['save_format']");
            AddLineToSection("plotting_functions", "    dpi = settings['dpi']");
            AddLineToSection("plotting_functions", "    plt.savefig(f'simulation_signals.{save_format}', bbox_inches='tight', dpi=dpi, facecolor='white', edgecolor='white')");
            AddLineToSection("plotting_functions", "    print(f\"グラフを 'simulation_signals.{save_format}' に保存しました\")");
            AddLineToSection("plotting_functions", "    ");
            AddLineToSection("plotting_functions", "    # Graph display (after savefig)");
            AddLineToSection("plotting_functions", "    plt.show()");
            AddLineToSection("plotting_functions", "");
            AddLineToSection("plotting_functions", "");
            AddLineToSection("plotting_functions", "def toggle_signal(plot_config, signal_name, enabled=None):");
            AddLineToSection("plotting_functions", "    \"\"\"");
            AddLineToSection("plotting_functions", "    信号の有効/無効を切り替える");
            AddLineToSection("plotting_functions", "    ");
            AddLineToSection("plotting_functions", "    Parameters:");
            AddLineToSection("plotting_functions", "    plot_config: dict");
            AddLineToSection("plotting_functions", "        プロット設定");
            AddLineToSection("plotting_functions", "    signal_name: str");
            AddLineToSection("plotting_functions", "        信号名");
            AddLineToSection("plotting_functions", "    enabled: bool, optional");
            AddLineToSection("plotting_functions", "        設定する状態（Noneの場合はトグル）");
            AddLineToSection("plotting_functions", "    ");
            AddLineToSection("plotting_functions", "    Returns:");
            AddLineToSection("plotting_functions", "    bool: 新しい状態");
            AddLineToSection("plotting_functions", "    \"\"\"");
            AddLineToSection("plotting_functions", "    for signal in plot_config['signals']:");
            AddLineToSection("plotting_functions", "        if signal['name'] == signal_name:");
            AddLineToSection("plotting_functions", "            if enabled is None:");
            AddLineToSection("plotting_functions", "                # トグル");
            AddLineToSection("plotting_functions", "                signal['enabled'] = not signal['enabled']");
            AddLineToSection("plotting_functions", "            else:");
            AddLineToSection("plotting_functions", "                signal['enabled'] = enabled");
            AddLineToSection("plotting_functions", "            ");
            AddLineToSection("plotting_functions", "            status = '有効' if signal['enabled'] else '無効'");
            AddLineToSection("plotting_functions", "            print(f\"信号 '{signal_name}' を {status} に設定しました\")");
            AddLineToSection("plotting_functions", "            return signal['enabled']");
            AddLineToSection("plotting_functions", "    ");
            AddLineToSection("plotting_functions", "    print(f\"エラー: 信号 '{signal_name}' が見つかりませんでした\")");
            AddLineToSection("plotting_functions", "    return False");
            AddLineToSection("plotting_functions", "");
            AddLineToSection("plotting_functions", "");
            AddLineToSection("plotting_functions", "def list_signals(plot_config):");
            AddLineToSection("plotting_functions", "    \"\"\"");
            AddLineToSection("plotting_functions", "    すべての信号とその状態をリスト表示する");
            AddLineToSection("plotting_functions", "    ");
            AddLineToSection("plotting_functions", "    Parameters:");
            AddLineToSection("plotting_functions", "    plot_config: dict");
            AddLineToSection("plotting_functions", "        プロット設定");
            AddLineToSection("plotting_functions", "    \"\"\"");
            AddLineToSection("plotting_functions", "    print(\"利用可能な信号:\")");
            AddLineToSection("plotting_functions", "    print(\"-\" * 50)");
            AddLineToSection("plotting_functions", "    for signal in plot_config['signals']:");
            AddLineToSection("plotting_functions", "        status = \"有効\" if signal['enabled'] else \"無効\"");
            AddLineToSection("plotting_functions", "        print(f\"{signal['name']}: {status} (コンポーネント: {signal['component']}, ポート: {signal['port']})\")");
            AddLineToSection("plotting_functions", "");
        }

        /// <summary>
        /// ファイルに書き込む
        /// </summary>
        private void WriteToFile()
        {
            try
            {
                var allLines = CombineAllSections();
                File.WriteAllLines(FilePath, allLines);
            }
            catch (Exception ex)
            {
                HandleError(ErrorCategory.FileWriteError, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// すべてのセクションを結合する
        /// </summary>
        /// <returns>結合されたコード行リスト</returns>
        private List<string> CombineAllSections()
        {
            var allLines = new List<string>();

            // インポートセクションを最初に追加
            if (CodeSections.ContainsKey("imports"))
            {
                allLines.AddRange(CodeSections["imports"]);
                allLines.Add("");
            }

            // その他のセクションを追加
            var sectionsOrder = new[] {
                "function_def", "plot_config", "plotting_functions"
            };

            foreach (var section in sectionsOrder)
            {
                if (CodeSections.ContainsKey(section))
                {
                    allLines.AddRange(CodeSections[section]);
                    if (section != "plotting_functions")
                        allLines.Add("");
                }
            }

            // Entry point: actually invoke main()
            allLines.Add("");
            allLines.Add("if __name__ == \"__main__\":");
            allLines.Add("    main()");

            return allLines;
        }

        /// <summary>
        /// エラーを処理する
        /// </summary>
        /// <param name="category">エラーカテゴリ</param>
        /// <param name="args">メッセージ引数</param>
        private void HandleError(ErrorCategory category, params object[] args)
        {
            string message = GetErrorMessage(category, args);
            ErrorHandler?.Invoke(category, message);
        }

        #endregion
    }
}