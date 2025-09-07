using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerSimBuilder
{
    /// <summary>
    /// コンポーネントのPythonコード生成を担当するクラス
    /// </summary>

    public class ComponentCodeGenerator
    {
        // コードテンプレートをDictionaryで管理
        private static readonly Dictionary<string, List<string>> CodeTemplates = new Dictionary<string, List<string>>
        {
            {
                "Header", new List<string>
                {
                    "\"\"\"",
                    "{0}コンポーネント",
                    "自動生成コード",
                    "生成日時: {1}",
                    "\"\"\"",
                    "",
                    "import math",
                    "from typing import Dict, List, Tuple, Any",
                    "from dataclasses import dataclass",
                    "from enum import Enum",
                    "from abc import ABC, abstractmethod",
                    "",
                    "try:",
                    "    from component_base import Component",
                    "except ImportError:",
                    "    # フォールバック定義",
                    "    class Component:",
                    "        def __init__(self, comp_id: str, comp_type: str):",
                    "            self.id = comp_id",
                    "            self.type = comp_type",
                    "            self.inputs = {}",
                    "            self.outputs = {}",
                    "            self.parameters = {}",
                    "",
                    "        def update(self, t: float, dt: float, sim_data: Any = None):",
                    "            pass",
                    ""
                }
            },
            {
                "ParametersClass", new List<string>
                {
                    "@dataclass",
                    "class {0}Parameters:",
                    "    \"\"\"{1}パラメータのデータクラス\"\"\""
                }
            },
            {
                "MainClass", new List<string>
                {
                    "class {0}(Component):",
                    "    \"\"\"{1}コンポーネント\"\"\"",
                    "",
                    "    def __init__(self, comp_id: str, params: {0}Parameters = None):",
                    "        super().__init__(comp_id, \"{0}\")",
                    "        self.params = params if params else {0}Parameters()",
                    "",
                    "        # 入力ポートの定義",
                    "        self.inputs = {"
                }
            },
            {
                "InputsSection", new List<string>
                {
                    "            '{0}': 0.0,"
                }
            },
            {
                "OutputsSection", new List<string>
                {
                    "        }",
                    "",
                    "        # 出力ポートの定義",
                    "        self.outputs = {"
                }
            },
            {
                "OutputsLine", new List<string>
                {
                    "            '{0}': 0.0,"
                }
            },
            {
                "ParametersSection", new List<string>
                {
                    "        }",
                    "",
                    "        # パラメータの設定",
                    "        self.parameters = {"
                }
            },
            {
                "ParametersLine", new List<string>
                {
                    "            '{0}': self.params.{0},"
                }
            },
            {
                "InternalVariables", new List<string>
                {
                    "        }",
                    "",
                    "        # 内部状態変数",
                    "        self._last_update_time = 0.0",
                    ""
                }
            },
            {
                "UpdateMethod", new List<string>
                {
                    "    def update(self, t: float, dt: float, sim_data: Any = None) -> None:",
                    "        \"\"\"コンポーネントの状態を更新\"\"\"",
                    "        # 入力値の取得"
                }
            },
            {
                "UpdateInputs", new List<string>
                {
                    "        {0} = self.inputs['{0}']"
                }
            },
            {
                "UpdateLogic", new List<string>
                {
                    "",
                    "        # ここにコンポーネントのロジックを実装",
                    "        # 例: 簡単な計算や状態更新",
                    "",
                    "        # 出力値の設定"
                }
            },
            {
                "UpdateOutputs", new List<string>
                {
                    "        self.outputs['{0}'] = 0.0  # 実際の計算結果に置き換える"
                }
            },
            {
                "UpdateEnd", new List<string>
                {
                    "",
                    "        self._last_update_time = t",
                    ""
                }
            },
            {
                "ResetMethod", new List<string>
                {
                    "    def reset(self):",
                    "        \"\"\"状態をリセット\"\"\"",
                    "        self._last_update_time = 0.0",
                    "",
                    "        # 出力を初期化",
                    "        for key in self.outputs:",
                    "            if isinstance(self.outputs[key], list):",
                    "                self.outputs[key] = [0.0] * len(self.outputs[key])",
                    "            else:",
                    "                self.outputs[key] = 0.0",
                    ""
                }
            },
            {
                "UsageExample", new List<string>
                {
                    "# 使用例",
                    "if __name__ == \"__main__\":",
                    "    # {0}のインスタンスを作成",
                    "    component = {1}(\"{2}\")",
                    "",
                    "    # 入力の設定"
                }
            },
            {
                "UsageInputs", new List<string>
                {
                    "    component.inputs['{0}'] = 0.0  # 適切な値に設定"
                }
            },
            {
                "UsageSimulation", new List<string>
                {
                    "",
                    "    # シミュレーションループの例",
                    "    dt = 0.001  # 時間ステップ",
                    "    for t in range(100):",
                    "        component.update(t * dt, dt)",
                    "        # 結果の利用"
                }
            },
            {
                "UsageOutputs", new List<string>
                {
                    "        # {0}: {{component.outputs['{0}']}}"
                }
            }
        };

        // テンプレートの種類と必要な引数の数をマッピング
        private static readonly Dictionary<string, int> TemplateArgCounts = new Dictionary<string, int>
        {
            { "Header", 2 },
            { "ParametersClass", 2 },
            { "MainClass", 2 },
            { "InputsSection", 1 },
            { "OutputsSection", 0 },
            { "OutputsLine", 1 },
            { "ParametersSection", 0 },
            { "ParametersLine", 1 },
            { "InternalVariables", 0 },
            { "UpdateMethod", 0 },
            { "UpdateInputs", 1 },
            { "UpdateLogic", 0 },
            { "UpdateOutputs", 1 },
            { "UpdateEnd", 0 },
            { "ResetMethod", 0 },
            { "UsageExample", 3 },
            { "UsageInputs", 1 },
            { "UsageSimulation", 0 },
            { "UsageOutputs", 1 }
        };
        /// <summary>
        /// コンポーネント記述子に基づいてPythonコードを生成し、指定されたファイルパスに保存します
        /// </summary>
        /// <param name="descriptor">コード生成のためのコンポーネント記述子</param>
        /// <param name="filePath">生成されたコードを保存するファイルパス</param>
        /// <exception cref="ArgumentNullException">descriptorがnullの場合</exception>
        /// <exception cref="ArgumentException">filePathが無効な場合</exception>

        public void GeneratePythonCode(ComponentDescriptor descriptor, string filePath)
        {
            var lines = new List<string>();
            
            try
            {
                LogcatTraceHelper.Instance.Log("DEBUG", $"GeneratePythonCode開始: {descriptor.Name}");
                
                // ヘッダー
                AddTemplateLines(lines, "Header", descriptor.Name, DateTime.Now);

                // パラメータクラス
                AddTemplateLines(lines, "ParametersClass", descriptor.Type, descriptor.Name);

                // パラメータ処理
                LogcatTraceHelper.Instance.Log("DEBUG", $"パラメータ処理開始 (パラメータ数: {descriptor.Parameters.Count})");
                foreach (var param in descriptor.Parameters)
                {
                    try
                    {
                        LogcatTraceHelper.Instance.Log("DEBUG", $"パラメータ処理: {param.Name}, 型: {param.Type}, 値: {param.DefaultValue}");
                        
                        string pythonType = ConvertToPythonType(param.Type);
                        string defaultValue = ConvertToPythonValue(param.DefaultValue);
                        
                        LogcatTraceHelper.Instance.Log("DEBUG", $"変換後 - 型: {pythonType}, 値: {defaultValue}");
                        
                        lines.Add($"    {param.Name}: {pythonType} = {defaultValue}");
                    }
                    catch (Exception ex)
                    {
                        LogcatTraceHelper.Instance.Log("ERROR", $"パラメータ '{param.Name}' 処理中にエラー: {ex.Message}");
                        throw;
                    }
                }
                lines.Add("");

                // メインクラス
                AddTemplateLines(lines, "MainClass", descriptor.Type, descriptor.Name);

                // 入力ポート
                LogcatTraceHelper.Instance.Log("DEBUG", $"入力ポート処理開始 (ポート数: {descriptor.InputPorts.Length})");
                foreach (var inputPort in descriptor.InputPorts)
                {
                    AddTemplateLines(lines, "InputsSection", inputPort);
                }

                // 出力ポートセクション
                AddTemplateLines(lines, "OutputsSection");

                // 出力ポート
                LogcatTraceHelper.Instance.Log("DEBUG", $"出力ポート処理開始 (ポート数: {descriptor.OutputPorts.Length})");
                foreach (var outputPort in descriptor.OutputPorts)
                {
                    AddTemplateLines(lines, "OutputsLine", outputPort);
                }

                // パラメータセクション
                AddTemplateLines(lines, "ParametersSection");

                // パラメータ行
                LogcatTraceHelper.Instance.Log("DEBUG", "パラメータ行処理開始");
                foreach (var param in descriptor.Parameters)
                {
                    AddTemplateLines(lines, "ParametersLine", param.Name);
                }

                // 内部変数
                AddTemplateLines(lines, "InternalVariables");

                // updateメソッド
                AddTemplateLines(lines, "UpdateMethod");

                // update入力
                LogcatTraceHelper.Instance.Log("DEBUG", "update入力処理開始");
                foreach (var inputPort in descriptor.InputPorts)
                {
                    AddTemplateLines(lines, "UpdateInputs", inputPort);
                }

                // updateロジック
                AddTemplateLines(lines, "UpdateLogic");

                // update出力
                LogcatTraceHelper.Instance.Log("DEBUG", "update出力処理開始");
                foreach (var outputPort in descriptor.OutputPorts)
                {
                    AddTemplateLines(lines, "UpdateOutputs", outputPort);
                }

                // update終了
                AddTemplateLines(lines, "UpdateEnd");

                // resetメソッド
                AddTemplateLines(lines, "ResetMethod");

                // 使用例
                AddTemplateLines(lines, "UsageExample", descriptor.Name, descriptor.Type, descriptor.Type.ToLower());

                // 使用入力
                LogcatTraceHelper.Instance.Log("DEBUG", "使用入力処理開始");
                foreach (var inputPort in descriptor.InputPorts)
                {
                    AddTemplateLines(lines, "UsageInputs", inputPort);
                }

                // 使用シミュレーション
                AddTemplateLines(lines, "UsageSimulation");

                // 使用出力
                LogcatTraceHelper.Instance.Log("DEBUG", "使用出力処理開始");
                foreach (var outputPort in descriptor.OutputPorts)
                {
                    AddTemplateLines(lines, "UsageOutputs", outputPort);
                }

                // ファイルに書き込み
                LogcatTraceHelper.Instance.Log("DEBUG", "ファイル書き込み開始");
                try
                {
                    File.WriteAllLines(filePath, lines);
                    LogcatTraceHelper.Instance.Log("DEBUG", "ファイル書き込み完了");
                }
                catch (Exception ex)
                {
                    LogcatTraceHelper.Instance.Log("ERROR", $"ファイル書き込み中にエラー: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.Log("ERROR", $"GeneratePythonCode全体でエラー: {ex.Message}");
                throw;
            }
        }
       /// <summary>
        /// 指定されたテンプレートキーに対応するフォーマット済みテンプレート行をリストに追加します
        /// </summary>
        /// <param name="lines">行を追加する対象の文字列リスト</param>
        /// <param name="templateKey">使用するテンプレートのキー</param>
        /// <param name="args">テンプレートのフォーマットに使用する引数</param>
        private void AddTemplateLines(List<string> lines, string templateKey, params object[] args)
        {
            if (!CodeTemplates.ContainsKey(templateKey))
            {
                LogcatTraceHelper.Instance.Log("ERROR", $"テンプレートキー '{templateKey}' が見つかりません");
                return;
            }

            int expectedArgCount = TemplateArgCounts.ContainsKey(templateKey) ? TemplateArgCounts[templateKey] : 0;
            
            if (args.Length != expectedArgCount)
            {
                LogcatTraceHelper.Instance.Log("WARN", $"テンプレート '{templateKey}' の引数数が期待値と異なります。期待: {expectedArgCount}, 実際: {args.Length}");
            }

            foreach (var line in CodeTemplates[templateKey])
            {
                try
                {
                    // フォーマットプレースホルダーがある行だけをフォーマットする
                    if (line.Contains("{") && args.Length > 0)
                    {
                        string formattedLine = string.Format(line, args);
                        lines.Add(formattedLine);
                    }
                    else
                    {
                        // プレースホルダーがない行はそのまま追加
                        lines.Add(line);
                    }
                }
                catch (FormatException fex)
                {
                    LogcatTraceHelper.Instance.Log("ERROR", $"テンプレート '{templateKey}' フォーマットエラー: {fex.Message}, 行: '{line}'");
                    // フォーマットに失敗した行はそのまま追加
                    lines.Add(line);
                }
            }
        }
        /// <summary>
        /// C#の型名をPythonの型名に変換します
        /// </summary>
        /// <param name="csharpType">変換するC#の型名</param>
        /// <returns>対応するPythonの型名。マッピングがない場合は"Any"を返します</returns>
        private string ConvertToPythonType(string csharpType)
        {
            try
            {
                LogcatTraceHelper.Instance.Log("DEBUG", $"ConvertToPythonType: {csharpType}");
                
                var typeMapping = new Dictionary<string, string>
                {
                    { "int", "int" },
                    { "double", "float" },
                    { "string", "str" },
                    { "bool", "bool" },
                    { "list<string>", "List[str]" },
                    { "dictionary<string, double>", "Dict[str, float]" }
                };

                string lowerType = csharpType.ToLower();

                if (typeMapping.TryGetValue(lowerType, out string pythonType))
                {
                    LogcatTraceHelper.Instance.Log("DEBUG", $"型マッピング成功: {csharpType} -> {pythonType}");
                    return pythonType;
                }

                LogcatTraceHelper.Instance.Log("WARN", $"型マッピングが見つかりません: {csharpType}");
                return "Any";
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.Log("ERROR", $"ConvertToPythonTypeでエラー: {ex.Message}");
                return "Any";
            }
        }
        /// <summary>
        /// C#のオブジェクト値をPythonのリテラル表現に変換します
        /// </summary>
        /// <param name="value">変換するC#オブジェクト値</param>
        /// <returns>Pythonリテラルとして表現された文字列。変換できない場合は"None"を返します</returns>

        private string ConvertToPythonValue(object value)
        {
            try
            {
                LogcatTraceHelper.Instance.Log("DEBUG", $"ConvertToPythonValue: {value} (型: {value?.GetType()?.Name})");
                
                if (value == null)
                    return "None";

                if (value is string str)
                    return $"\"{str}\"";

                if (value is bool b)
                    return b ? "True" : "False";

                // リスト型の処理
                if (value is System.Collections.IEnumerable enumerable && !(value is string))
                {
                    var items = new List<string>();
                    foreach (var item in enumerable)
                    {
                        items.Add(ConvertToPythonValue(item));
                    }
                    return $"[{string.Join(", ", items)}]";
                }

                // 辞書型の処理
                if (value is System.Collections.IDictionary dictionary)
                {
                    var items = new List<string>();
                    foreach (System.Collections.DictionaryEntry entry in dictionary)
                    {
                        items.Add($"{ConvertToPythonValue(entry.Key)}: {ConvertToPythonValue(entry.Value)}");
                    }
                    return $"{{{string.Join(", ", items)}}}";
                }

                // その他の型
                return value.ToString();
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.Log("ERROR", $"ConvertToPythonValueでエラー: {ex.Message}, 値: {value}");
                return "None";
            }
        }
    }
}