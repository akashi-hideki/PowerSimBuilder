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
            {"components", new List<string>()},
            {"connections", new List<string>()},
            {"simulation_loop", new List<string>()},
            {"simulation_data_init", new List<string>()},
            {"plotting", new List<string>()}
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
                GenerateComponentsDictionarySection();
                GenerateConnectionsListSection();
                GenerateSimulationDataInitSection();
                GenerateSimulationLoopSection();
                GeneratePlottingMethodSection();
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
        /// シミュレーションデータ初期化セクションを生成する
        /// </summary>
        private void GenerateSimulationDataInitSection()
        {
            codeSections["simulation_data_init"].Add("    # シミュレーション�ードの初期化");
            codeSections["simulation_data_init"].Add("    sim_data = SimulationData()");
            codeSections["simulation_data_init"].Add("    sim_data.time_step = dt");
            codeSections["simulation_data_init"].Add("    sim_data.logs = []");
            
            // コンポーネントのパラメータから初期化コードを生成
            GenerateComponentParametersInit();
            
            codeSections["simulation_data_init"].Add("");
        }

        /// <summary>
        /// コンポーネントパラメータの初期化コードを生成する
        /// </summary>
        private void GenerateComponentParametersInit()
        {
            if (Components == null) return;

            foreach (var comp in Components)
            {
                if (comp.Parameters != null && comp.Parameters.Any())
                {
                    codeSections["simulation_data_init"].Add($"    # {comp.Name} のパラメータ設定");
                    
                    foreach (var param in comp.Parameters)
                    {
                        var defaultValue = GetPythonDefaultValue(param.DefaultValue, param.Type);
                        codeSections["simulation_data_init"].Add($"    sim_data.add_parameter('{comp.Name}', '{param.Name}', {defaultValue})");
                    }
                    
                    codeSections["simulation_data_init"].Add("");
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
        /// コンポーネント辞書セクションを生成する
        /// </summary>
        private void GenerateComponentsDictionarySection()
        {
            codeSections["components"].Add("    components = {");
            AddComponentEntries();
            codeSections["components"].Add("    }");
            codeSections["components"].Add("");
        }

        /// <summary>
        /// コンポーネントエントリを追加する
        /// </summary>
        private void AddComponentEntries()
        {
            foreach (var comp in Components)
            {
                try
                {
                    AddComponentEntry(comp);
                }
                catch (Exception ex)
                {
                    HandleError(ErrorCategory.ComponentError, comp.Name, ex.Message);
                }
            }
        }

        /// <summary>
        /// コンポーネントエントリを追加する
        /// </summary>
        /// <param name="comp">コンポーネント記述子</param>
        private void AddComponentEntry(ComponentDescriptor comp)
        {
            if (string.IsNullOrEmpty(comp.Name))
            {
                HandleError(ErrorCategory.ComponentError, "", "コンポーネント名が空です。");
                return;
            }

            if (string.IsNullOrEmpty(comp.Type))
            {
                HandleError(ErrorCategory.ComponentError, comp.Name, "タイプが空です。");
                return;
            }

            var compName = comp.Name.ToLower();
            // 文字列補間を使用
            codeSections["components"].Add($"        \"{compName}\": {comp.Type}(\"{compName}\",\"{comp.Type}\"),");
        }

        /// <summary>
        /// 接続リストセクションを生成する
        /// </summary>
        private void GenerateConnectionsListSection()
        {
            codeSections["connections"].Add("    connections = [");
            AddConnectionEntries();
            codeSections["connections"].Add("    ]");
            codeSections["connections"].Add("");
        }

        /// <summary>
        /// 接続エントリを追加する
        /// </summary>
        private void AddConnectionEntries()
        {
            foreach (var conn in Connections)
            {
                try
                {
                    AddConnectionEntry(conn);
                }
                catch (Exception ex)
                {
                    HandleError(ErrorCategory.ConnectionError, ex.Message);
                }
            }
        }

        /// <summary>
        /// 接続エントリを追加する
        /// </summary>
        /// <param name="conn">接続オブジェクト</param>
        private void AddConnectionEntry(Connection conn)
        {
            if (conn.StartComponent == null)
            {
                HandleError(ErrorCategory.ConnectionError, "開始コンポーネントがnullです。");
                return;
            }

            if (string.IsNullOrEmpty(conn.StartPort))
            {
                HandleError(ErrorCategory.ConnectionError, "開始ポートが空です。");
                return;
            }

            if (conn.EndComponent == null)
            {
                HandleError(ErrorCategory.ConnectionError, "終了コンポーネントがnullです。");
                return;
            }

            if (string.IsNullOrEmpty(conn.EndPort))
            {
                HandleError(ErrorCategory.ConnectionError, "終了ポートが空です。");
                return;
            }

            var fromComp = conn.StartComponent.Name.ToLower();
            var fromPort = conn.StartPort;
            var toComp = conn.EndComponent.Name.ToLower();
            var toPort = conn.EndPort;

            // 文字列補間を使用
            codeSections["connections"].Add($"        {{\"from\":\"{fromComp}.outputs.{fromPort}\",\"to\":\"{toComp}.inputs.{toPort}\"}},");
        }

        /// <summary>
        /// シミュレーションループセクションを生成する
        /// </summary>
        private void GenerateSimulationLoopSection()
        {
            AddSimulationLoopStart();
            AddComponentUpdateLoop();
            AddConnectionUpdateLoop();
            AddLoggingSection();
        }

        /// <summary>
        /// シミュレーションループ開始を追加する
        /// </summary>
        private void AddSimulationLoopStart()
        {
            foreach (var line in SimulationLoopStartCodeTable)
            {
                codeSections["simulation_loop"].Add(line);
            }
        }

        /// <summary>
        /// コンポーネント更新ループを追加する
        /// </summary>
        private void AddComponentUpdateLoop()
        {
            foreach (var line in ComponentUpdateCodeTable)
            {
                codeSections["simulation_loop"].Add(line);
            }
            codeSections["simulation_loop"].Add("");
        }

        /// <summary>
        /// 接続更新ループを追加する
        /// </summary>
        private void AddConnectionUpdateLoop()
        {
            foreach (var line in ConnectionUpdateCodeTable)
            {
                codeSections["simulation_loop"].Add(line);
            }
            codeSections["simulation_loop"].Add("");
        }

        /// <summary>
        /// ロギングセクションを追加する
        /// </summary>
        private void AddLoggingSection()
        {
            foreach (var line in LoggingCodeTable)
            {
                codeSections["simulation_loop"].Add(line);
            }
        }

        /// <summary>
        /// 接続データをMatplotlibで描画するメソッドを生成する
        /// </summary>
        private void GeneratePlottingMethodSection()
        {
            codeSections["plotting"].Add("");
            codeSections["plotting"].Add("def plot_connections(sim_data, connections):");
            codeSections["plotting"].Add("    \"\"\"");
            codeSections["plotting"].Add("    すべての接続データを一つのグラフに描画する");
            codeSections["plotting"].Add("    ");
            codeSections["plotting"].Add("    Parameters:");
            codeSections["plotting"].Add("    sim_data: SimulationData");
            codeSections["plotting"].Add("        シミュレーションデータオブジェクト");
            codeSections["plotting"].Add("    connections: list");
            codeSections["plotting"].Add("        接続情報のリスト");
            codeSections["plotting"].Add("    \"\"\"");
            codeSections["plotting"].Add("    import matplotlib.pyplot as plt");
            codeSections["plotting"].Add("    import numpy as np");
            codeSections["plotting"].Add("    ");
            codeSections["plotting"].Add("    # グラフの設定");
            codeSections["plotting"].Add("    plt.figure(figsize=(12, 8))");
            codeSections["plotting"].Add("    plt.title('Connection Data Over Time')");
            codeSections["plotting"].Add("    plt.xlabel('Time (s)')");
            codeSections["plotting"].Add("    plt.ylabel('Value')");
            codeSections["plotting"].Add("    plt.grid(True, linestyle='--', alpha=0.7)");
            codeSections["plotting"].Add("    ");
            codeSections["plotting"].Add("    # カラーマップの準備（各接続に異なる色を割り当て）");
            codeSections["plotting"].Add("    colors = plt.cm.tab10(np.linspace(0, 1, len(connections)))");
            codeSections["plotting"].Add("    ");
            codeSections["plotting"].Add("    # 各接続のデータをプロット");
            codeSections["plotting"].Add("    for i, conn in enumerate(connections):");
            codeSections["plotting"].Add("        from_path = conn['from']");
            codeSections["plotting"].Add("        to_path = conn['to']");
            codeSections["plotting"].Add("        ");
            codeSections["plotting"].Add("        # 接続元のコンポーネントとポートを取得");
            codeSections["plotting"].Add("        from_comp, from_port = from_path.split('.')");
            codeSections["plotting"].Add("        from_comp = from_comp.replace('outputs.', '')");
            codeSections["plotting"].Add("        ");
            codeSections["plotting"].Add("        # 時間と値を格納するリスト");
            codeSections["plotting"].Add("        times = []");
            codeSections["plotting"].Add("        values = []");
            codeSections["plotting"].Add("        ");
            codeSections["plotting"].Add("        # 各時間ステップのデータを収集");
            codeSections["plotting"].Add("        for log in sim_data.logs:");
            codeSections["plotting"].Add("            time = log['time']");
            codeSections["plotting"].Add("            comp_data = log['components'].get(from_comp, {})");
            codeSections["plotting"].Add("            outputs = comp_data.get('outputs', {})");
            codeSections["plotting"].Add("            value = outputs.get(from_port, 0)");
            codeSections["plotting"].Add("            ");
            codeSections["plotting"].Add("            times.append(time)");
            codeSections["plotting"].Add("            values.append(value)");
            codeSections["plotting"].Add("        ");
            codeSections["plotting"].Add("        # データをプロット");
            codeSections["plotting"].Add("        if times and values:");
            codeSections["plotting"].Add("            label = f\"{from_path} → {to_path}\"");
            codeSections["plotting"].Add("            plt.plot(times, values, color=colors[i], label=label, linewidth=2)");
            codeSections["plotting"].Add("        ");
            codeSections["plotting"].Add("    # 凡例を追加（グラフ外に配置）");
            codeSections["plotting"].Add("    plt.legend(bbox_to_anchor=(1.05, 1), loc='upper left', borderaxespad=0.)");
            codeSections["plotting"].Add("    ");
            codeSections["plotting"].Add("    # レイアウト調整");
            codeSections["plotting"].Add("    plt.tight_layout()");
            codeSections["plotting"].Add("    ");
            codeSections["plotting"].Add("    # グラフを表示");
            codeSections["plotting"].Add("    plt.show()");
            codeSections["plotting"].Add("    ");
            codeSections["plotting"].Add("    # グラフを保存");
            codeSections["plotting"].Add("    plt.savefig('connection_plot.png', bbox_inches='tight', dpi=300)");
            codeSections["plotting"].Add("");
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
                "imports", "function_def", "simulation_data_init", 
                "components", "connections", "simulation_loop", "plotting" 
            };

            foreach (var section in sectionsOrder)
            {
                allLines.AddRange(codeSections[section]);
                if (section != "plotting")
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