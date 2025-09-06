using System;
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
            "            f_c, f_p = conn[\"from\"].split(\".\")",
            "            t_c, t_p = conn[\"to\"].split(\".\")",
            "            components[t_c].inputs[t_p] = components[f_c].outputs[f_p]"
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
            "    for t in np.arange(0, t_end, dt):"
        };

        /// <summary>エラーメッセージの固定テーブル</summary>
        private static readonly Dictionary<ErrorCategory, string> ErrorMessageTable = new Dictionary<ErrorCategory, string>
        {
            { ErrorCategory.BuildError, "ファイル生成中にエラーが発生しました: {0}" },
            { ErrorCategory.ValidationError, "{0}" },
            { ErrorCategory.GenerationError, "コード生成中にエラーが発生しました: {0}" },
            { ErrorCategory.ComponentError, "コンポーネント '{0}' の処理中にエラーが発生しました: {1}" },
            { ErrorCategory.ConnectionError, "接続の処理中にエラーが発生しました: {0}" },
            { ErrorCategory.FileWriteError, "ファイル書き込み中にエラーが発生しました: {0}" },
            { ErrorCategory.SectionAccessError, "セクション '{0}' が見つかりません。" }
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

        /// <summary>コードセクション辞書（静的）</summary>
        private static readonly Dictionary<string, List<string>> codeSections = new Dictionary<string, List<string>>
        {
            {"imports", new List<string>()},
            {"function_def", new List<string>()},
            {"plot_config", new List<string>()},
            {"plotting_functions", new List<string>()}
        };

        #endregion

        #region Properties

        /// <summary>出力ファイルパス</summary>
        public string FilePath { get; set; }

        /// <summary>コンポーネント記述子リスト</summary>
        public List<ComponentDescriptor> Components { get; set; }

        /// <summary>接続リスト</summary>
        public List<Connection> Connections { get; set; }

        /// <summary>エラーハンドラーデリゲート</summary>
        public Action<ErrorCategory, string> ErrorHandler { get; set; } = DefaultErrorHandler;

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
            if (codeSections.ContainsKey(sectionName))
                return new List<string>(codeSections[sectionName]);

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
            if (codeSections.ContainsKey(sectionName))
                codeSections[sectionName] = lines;
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
            if (codeSections.ContainsKey(sectionName))
                codeSections[sectionName].Add(line);
            else
                throw new ArgumentException($"Section '{sectionName}' not found.");
        }

        /// <summary>
        /// 指定されたセクションの指定位置に行を挿入する
        /// </summary>
        /// <param name="sectionName">セクション名</param>
        /// <param name="index">挿入位置</param>
        /// <param name="line">挿入する行</param>
        public void InsertLineToSection(string sectionName, int index, string line)
        {
            if (codeSections.ContainsKey(sectionName))
                codeSections[sectionName].Insert(index, line);
            else
                throw new ArgumentException($"Section '{sectionName}' not found.");
        }

        /// <summary>
        /// 指定されたセクションをクリアする
        /// </summary>
        /// <param name="sectionName">セクション名</param>
        public void ClearSection(string sectionName)
        {
            if (codeSections.ContainsKey(sectionName))
                codeSections[sectionName].Clear();
            else
                throw new ArgumentException($"Section '{sectionName}' not found.");
        }

        /// <summary>
        /// 新しいセクションを追加する
        /// </summary>
        /// <param name="sectionName">セクション名</param>
        public void AddNewSection(string sectionName)
        {
            if (!codeSections.ContainsKey(sectionName))
                codeSections.Add(sectionName, new List<string>());
        }

        /// <summary>
        /// 指定されたセクションを削除する
        /// </summary>
        /// <param name="sectionName">セクション名</param>
        public void RemoveSection(string sectionName)
        {
            if (codeSections.ContainsKey(sectionName))
                codeSections.Remove(sectionName);
        }

        /// <summary>
        /// すべてのセクションを取得する
        /// </summary>
        /// <returns>セクション辞書</returns>
        public Dictionary<string, List<string>> GetAllSections()
        {
            return new Dictionary<string, List<string>>(codeSections);
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
        /// コードセクションを初期化する
        /// </summary>
        private static void InitializeCodeSections()
        {
            foreach (var key in codeSections.Keys.ToList())
            {
                codeSections[key] = new List<string>();
            }
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
            if (ErrorMessageTable.TryGetValue(category, out string format))
            {
                return string.Format(format, args);
            }
            return "不明なエラーが発生しました。";
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
            codeSections["imports"].Add("import numpy as np");
            
            // 必要な追加インポートをチェック
            if (NeedsDateTimeImport())
            {
                codeSections["imports"].Add("from datetime import datetime");
            }
            
            codeSections["imports"].Add("");
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
            codeSections["function_def"].Add("def main():");
            AddTimeStepDefinition();
            AddSimulationDataDefinition();
            GenerateComponentParametersInFunction();
            AddComponentDictionaryInFunction();
            AddConnectionsListInFunction();
            AddSimulationLoopInFunction();
            
            // メイン関数の最後にプロット呼び出しを追加
            codeSections["function_def"].Add("");
            codeSections["function_def"].Add("    # シミュレーション実行後のプロット");
            codeSections["function_def"].Add("    plot_signals(sim_data, plot_config)");
            codeSections["function_def"].Add("    print(\"シミュレーション完了\")");
            
            codeSections["function_def"].Add("");
        }

        /// <summary>
        /// 時間ステップ定義を追加する
        /// </summary>
        private void AddTimeStepDefinition()
        {
            codeSections["function_def"].Add("    dt = 0.001");
            codeSections["function_def"].Add("    t_end = 5.0");
        }

        /// <summary>
        /// シミュレーションデータ定義を追加する
        /// </summary>
        private void AddSimulationDataDefinition()
        {
            codeSections["function_def"].Add("    sim_data = SimulationData()");
            codeSections["function_def"].Add("    sim_data.time_step = dt");
            codeSections["function_def"].Add("    sim_data.logs = []");
        }

        /// <summary>
        /// 関数内でコンポーネントパラメータを初期化するコードを追加する
        /// </summary>
        private void GenerateComponentParametersInFunction()
        {
            if (Components == null) return;

            foreach (var comp in Components)
            {
                if (comp.Parameters != null && comp.Parameters.Any())
                {
                    codeSections["function_def"].Add($"    # {comp.Name} のパラメータ設定");
                    
                    foreach (var param in comp.Parameters)
                    {
                        var defaultValue = GetPythonDefaultValue(param.DefaultValue, param.Type);
                        codeSections["function_def"].Add($"    sim_data.add_parameter('{comp.Name}', '{param.Name}', {defaultValue})");
                    }
                    
                    codeSections["function_def"].Add("");
                }
            }
        }

        /// <summary>
        /// C#のデフォルト値をPythonのデフォルト値に変換する
        /// </summary>
        /// <param name="defaultValue">デフォルト値</param>
        /// <param name="type">パラメータタイプ</param>
        /// <returns>Python形式のデフォルト値</returns>
        private string GetPythonDefaultValue(object defaultValue, string type)
        {
            if (PythonValueConverters.TryGetValue(type, out Func<object, string> converter))
            {
                return converter(defaultValue);
            }
            
            // デフォルトの処理
            return defaultValue != null ? $"\"{defaultValue}\"" : "None";
        }

        /// <summary>
        /// 関数内でコンポーネント辞書を追加する
        /// </summary>
        private void AddComponentDictionaryInFunction()
        {
            codeSections["function_def"].Add("    # コンポーネント辞書の定義");
            codeSections["function_def"].Add("    components = {");
            
            foreach (var comp in Components)
            {
                try
                {
                    if (string.IsNullOrEmpty(comp.Name) || string.IsNullOrEmpty(comp.Type))
                        continue;
                        
                    var compName = comp.Name.ToLower();
                    codeSections["function_def"].Add($"        \"{compName}\": {comp.Type}(\"{compName}\",\"{comp.Type}\"),");
                }
                catch (Exception ex)
                {
                    HandleError(ErrorCategory.ComponentError, comp.Name, ex.Message);
                }
            }
            
            codeSections["function_def"].Add("    }");
            codeSections["function_def"].Add("");
        }

        /// <summary>
        /// 関数内で接続リストを追加する
        /// </summary>
        private void AddConnectionsListInFunction()
        {
            codeSections["function_def"].Add("    # 接続リストの定義");
            codeSections["function_def"].Add("    connections = [");
            
            foreach (var conn in Connections)
            {
                try
                {
                    if (conn.StartComponent == null || string.IsNullOrEmpty(conn.StartPort) ||
                        conn.EndComponent == null || string.IsNullOrEmpty(conn.EndPort))
                        continue;
                        
                    var fromComp = conn.StartComponent.Name.ToLower();
                    var fromPort = conn.StartPort;
                    var toComp = conn.EndComponent.Name.ToLower();
                    var toPort = conn.EndPort;

                    codeSections["function_def"].Add($"        {{\"from\":\"{fromComp}.outputs.{fromPort}\",\"to\":\"{toComp}.inputs.{toPort}\"}},");
                }
                catch (Exception ex)
                {
                    HandleError(ErrorCategory.ConnectionError, ex.Message);
                }
            }
            
            codeSections["function_def"].Add("    ]");
            codeSections["function_def"].Add("");
        }

        /// <summary>
        /// 関数内でシミュレーションループを追加する
        /// </summary>
        private void AddSimulationLoopInFunction()
        {
            // シミュレーションループ開始
            foreach (var line in SimulationLoopStartCodeTable)
            {
                codeSections["function_def"].Add(line);
            }
            
            // コンポーネント更新ループ
            foreach (var line in ComponentUpdateCodeTable)
            {
                codeSections["function_def"].Add(line);
            }
            codeSections["function_def"].Add("");
            
            // 接続更新ループ
            foreach (var line in ConnectionUpdateCodeTable)
            {
                codeSections["function_def"].Add(line);
            }
            codeSections["function_def"].Add("");
            
            // ロギングセクション
            foreach (var line in LoggingCodeTable)
            {
                codeSections["function_def"].Add(line);
            }
        }

        /// <summary>
        /// プロット設定セクションを生成する
        /// </summary>
        private void GeneratePlotConfigSection()
        {
            codeSections["plot_config"].Add("# プロット設定（ベースデータ）");
            codeSections["plot_config"].Add("# 接続情報から自動生成された信号設定");
            codeSections["plot_config"].Add("plot_config = {");
            codeSections["plot_config"].Add("    'signals': [");
            
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
                    
                    var fromComp = conn.StartComponent.Name.ToLower();
                    var fromPort = conn.StartPort;
                    
                    // 信号名を生成（コンポーネント名_ポート名）
                    var signalName = $"{fromComp}_{fromPort}";
                    
                    // 色と線種を循環して割り当て
                    var color = colorPalette[colorIndex % colorPalette.Count];
                    var lineStyle = lineStyles[lineStyleIndex % lineStyles.Count];
                    
                    codeSections["plot_config"].Add($"        {{'name': '{signalName}', 'component': '{fromComp}', 'port': '{fromPort}', 'enabled': True, 'color': '{color}', 'line_style': '{lineStyle}'}},");
                    
                    colorIndex++;
                    if (colorIndex % 3 == 0) lineStyleIndex++; // 3つの信号ごとに線種を変更
                }
            }
            else
            {
                // 接続情報がない場合のデフォルト信号
                codeSections["plot_config"].Add("        {'name': 'engine_torque', 'component': 'engine', 'port': 'torque', 'enabled': True, 'color': '#FF5733', 'line_style': '-'},");
                codeSections["plot_config"].Add("        {'name': 'generator_voltage', 'component': 'generator', 'port': 'voltage', 'enabled': True, 'color': '#33FF57', 'line_style': '-'},");
            }

            codeSections["plot_config"].Add("    ],");
            codeSections["plot_config"].Add("    'global_settings': {");
            codeSections["plot_config"].Add("        'figure_size': (12, 8),");
            codeSections["plot_config"].Add("        'title': 'シミュレーション信号',");
            codeSections["plot_config"].Add("        'xlabel': '時間 (秒)',");
            codeSections["plot_config"].Add("        'ylabel': '値',");
            codeSections["plot_config"].Add("        'grid': True,");
            codeSections["plot_config"].Add("        'legend_location': 'upper right',");
            codeSections["plot_config"].Add("        'save_format': 'png',");
            codeSections["plot_config"].Add("        'dpi': 300");
            codeSections["plot_config"].Add("    }");
            codeSections["plot_config"].Add("}");
            codeSections["plot_config"].Add("");
        }

        /// <summary>
        /// プロット関数セクションを生成する
        /// </summary>
        private void GeneratePlottingFunctionsSection()
        {
            codeSections["plotting_functions"].Add("# プロット関数（コード）");
            codeSections["plotting_functions"].Add("def plot_signals(sim_data, plot_config):");
            codeSections["plotting_functions"].Add("    \"\"\"");
            codeSections["plotting_functions"].Add("    設定に基づいて信号をプロットする");
            codeSections["plotting_functions"].Add("    ");
            codeSections["plotting_functions"].Add("    Parameters:");
            codeSections["plotting_functions"].Add("    sim_data: SimulationData");
            codeSections["plotting_functions"].Add("        シミュレーションデータオブジェクト");
            codeSections["plotting_functions"].Add("    plot_config: dict");
            codeSections["plotting_functions"].Add("        プロット設定");
            codeSections["plotting_functions"].Add("    \"\"\"");
            codeSections["plotting_functions"].Add("    import matplotlib.pyplot as plt");
            codeSections["plotting_functions"].Add("    import numpy as np");
            codeSections["plotting_functions"].Add("    ");
            codeSections["plotting_functions"].Add("    # グローバル設定の適用");
            codeSections["plotting_functions"].Add("    settings = plot_config['global_settings']");
            codeSections["plotting_functions"].Add("    plt.figure(figsize=settings['figure_size'])");
            codeSections["plotting_functions"].Add("    plt.title(settings['title'])");
            codeSections["plotting_functions"].Add("    plt.xlabel(settings['xlabel'])");
            codeSections["plotting_functions"].Add("    plt.ylabel(settings['ylabel'])");
            codeSections["plotting_functions"].Add("    ");
            codeSections["plotting_functions"].Add("    if settings['grid']:");
            codeSections["plotting_functions"].Add("        plt.grid(True, linestyle='--', alpha=0.7)");
            codeSections["plotting_functions"].Add("    ");
            codeSections["plotting_functions"].Add("    # 有効な信号のみをフィルタリング");
            codeSections["plotting_functions"].Add("    enabled_signals = [s for s in plot_config['signals'] if s['enabled']]");
            codeSections["plotting_functions"].Add("    ");
            codeSections["plotting_functions"].Add("    # 各信号のデータを収集してプロット");
            codeSections["plotting_functions"].Add("    for signal in enabled_signals:");
            codeSections["plotting_functions"].Add("        comp_name = signal['component']");
            codeSections["plotting_functions"].Add("        port_name = signal['port']");
            codeSections["plotting_functions"].Add("        ");
            codeSections["plotting_functions"].Add("        # 時間と値を格納するリスト");
            codeSections["plotting_functions"].Add("        times = []");
            codeSections["plotting_functions"].Add("        values = []");
            codeSections["plotting_functions"].Add("        ");
            codeSections["plotting_functions"].Add("        # 各時間ステップのデータを収集");
            codeSections["plotting_functions"].Add("        for log in sim_data.logs:");
            codeSections["plotting_functions"].Add("            time = log['time']");
            codeSections["plotting_functions"].Add("            comp_data = log['components'].get(comp_name, {})");
            codeSections["plotting_functions"].Add("            outputs = comp_data.get('outputs', {})");
            codeSections["plotting_functions"].Add("            value = outputs.get(port_name, 0)");
            codeSections["plotting_functions"].Add("            ");
            codeSections["plotting_functions"].Add("            times.append(time)");
            codeSections["plotting_functions"].Add("            values.append(value)");
            codeSections["plotting_functions"].Add("        ");
            codeSections["plotting_functions"].Add("        # データをプロット");
            codeSections["plotting_functions"].Add("        if times and values:");
            codeSections["plotting_functions"].Add("            color = signal.get('color', None)");
            codeSections["plotting_functions"].Add("            line_style = signal.get('line_style', '-')");
            codeSections["plotting_functions"].Add("            ");
            codeSections["plotting_functions"].Add("            plt.plot(times, values, label=signal['name'], color=color, linestyle=line_style, linewidth=2)");
            codeSections["plotting_functions"].Add("    ");
            codeSections["plotting_functions"].Add("    # 凡例を追加");
            codeSections["plotting_functions"].Add("    plt.legend(loc=settings['legend_location'])");
            codeSections["plotting_functions"].Add("    ");
            codeSections["plotting_functions"].Add("    # レイアウト調整");
            codeSections["plotting_functions"].Add("    plt.tight_layout()");
            codeSections["plotting_functions"].Add("    ");
            codeSections["plotting_functions"].Add("    # グラフを表示");
            codeSections["plotting_functions"].Add("    plt.show()");
            codeSections["plotting_functions"].Add("    ");
            codeSections["plotting_functions"].Add("    # グラフを保存");
            codeSections["plotting_functions"].Add("    save_format = settings['save_format']");
            codeSections["plotting_functions"].Add("    dpi = settings['dpi']");
            codeSections["plotting_functions"].Add("    plt.savefig(f'simulation_signals.{save_format}', bbox_inches='tight', dpi=dpi)");
            codeSections["plotting_functions"].Add("    print(f\"グラフを 'simulation_signals.{save_format}' に保存しました\")");
            codeSections["plotting_functions"].Add("");
            codeSections["plotting_functions"].Add("");
            codeSections["plotting_functions"].Add("def toggle_signal(plot_config, signal_name, enabled=None):");
            codeSections["plotting_functions"].Add("    \"\"\"");
            codeSections["plotting_functions"].Add("    信号の有効/無効を切り替える");
            codeSections["plotting_functions"].Add("    ");
            codeSections["plotting_functions"].Add("    Parameters:");
            codeSections["plotting_functions"].Add("    plot_config: dict");
            codeSections["plotting_functions"].Add("        プロット設定");
            codeSections["plotting_functions"].Add("    signal_name: str");
            codeSections["plotting_functions"].Add("        信号名");
            codeSections["plotting_functions"].Add("    enabled: bool, optional");
            codeSections["plotting_functions"].Add("        設定する状態（Noneの場合はトグル）");
            codeSections["plotting_functions"].Add("    ");
            codeSections["plotting_functions"].Add("    Returns:");
            codeSections["plotting_functions"].Add("    bool: 新しい状態");
            codeSections["plotting_functions"].Add("    \"\"\"");
            codeSections["plotting_functions"].Add("    for signal in plot_config['signals']:");
            codeSections["plotting_functions"].Add("        if signal['name'] == signal_name:");
            codeSections["plotting_functions"].Add("            if enabled is None:");
            codeSections["plotting_functions"].Add("                # トグル");
            codeSections["plotting_functions"].Add("                signal['enabled'] = not signal['enabled']");
            codeSections["plotting_functions"].Add("            else:");
            codeSections["plotting_functions"].Add("                signal['enabled'] = enabled");
            codeSections["plotting_functions"].Add("            ");
            codeSections["plotting_functions"].Add("            status = '有効' if signal['enabled'] else '無効'");
            codeSections["plotting_functions"].Add("            print(f\"信号 '{signal_name}' を {status} に設定しました\")");
            codeSections["plotting_functions"].Add("            return signal['enabled']");
            codeSections["plotting_functions"].Add("    ");
            codeSections["plotting_functions"].Add("    print(f\"エラー: 信号 '{signal_name}' が見つかりませんでした\")");
            codeSections["plotting_functions"].Add("    return False");
            codeSections["plotting_functions"].Add("");
            codeSections["plotting_functions"].Add("");
            codeSections["plotting_functions"].Add("def list_signals(plot_config):");
            codeSections["plotting_functions"].Add("    \"\"\"");
            codeSections["plotting_functions"].Add("    すべての信号とその状態をリスト表示する");
            codeSections["plotting_functions"].Add("    ");
            codeSections["plotting_functions"].Add("    Parameters:");
            codeSections["plotting_functions"].Add("    plot_config: dict");
            codeSections["plotting_functions"].Add("        プロット設定");
            codeSections["plotting_functions"].Add("    \"\"\"");
            codeSections["plotting_functions"].Add("    print(\"利用可能な信号:\")");
            codeSections["plotting_functions"].Add("    print(\"-\" * 50)");
            codeSections["plotting_functions"].Add("    for signal in plot_config['signals']:");
            codeSections["plotting_functions"].Add("        status = \"有効\" if signal['enabled'] else \"無効\"");
            codeSections["plotting_functions"].Add("        print(f\"{signal['name']}: {status} (コンポーネント: {signal['component']}, ポート: {signal['port']})\")");
            codeSections["plotting_functions"].Add("");
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

            var sectionsOrder = new[] { 
                "imports", "function_def", 
                "plot_config", "plotting_functions"
            };

            foreach (var section in sectionsOrder)
            {
                allLines.AddRange(codeSections[section]);
                if (section != "plotting_functions")
                    allLines.Add("");
            }

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