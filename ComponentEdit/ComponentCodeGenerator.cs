using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerSimBuilder
{
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
                    "            self.inputs: Dict[str, Any] = {}",
                    "            self.outputs: Dict[str, Any] = {}",
                    "            self.parameters: Dict[str, Any] = {}",
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
        public void GeneratePythonCode(ComponentDescriptor descriptor, string filePath)
        {
            var lines = new List<string>();
            
            try
            {
                LogcatTraceHelper.Instance.Log("DEBUG", $"GeneratePythonCode開始: {descriptor.Name}");
                
                // ヘッダー
                LogcatTraceHelper.Instance.Log("DEBUG", "ヘッダー処理開始");
                try
                {
                    lines.AddRange(CodeTemplates["Header"]
                        .Select(line => {
                            try
                            {
                                return string.Format(line, descriptor.Name, DateTime.Now);
                            }
                            catch (FormatException fex)
                            {
                                LogcatTraceHelper.Instance.Log("ERROR", $"Headerフォーマットエラー: {fex.Message}, 行: '{line}'");
                                throw;
                            }
                        }));
                }
                catch (Exception ex)
                {
                    LogcatTraceHelper.Instance.Log("ERROR", $"ヘッダー処理中にエラー: {ex.Message}");
                    throw;
                }

                // パラメータクラス
                LogcatTraceHelper.Instance.Log("DEBUG", "パラメータクラス処理開始");
                try
                {
                    lines.AddRange(CodeTemplates["ParametersClass"]
                        .Select(line => {
                            try
                            {
                                return string.Format(line, descriptor.Type, descriptor.Name);
                            }
                            catch (FormatException fex)
                            {
                                LogcatTraceHelper.Instance.Log("ERROR", $"ParametersClassフォーマットエラー: {fex.Message}, 行: '{line}'");
                                throw;
                            }
                        }));
                }
                catch (Exception ex)
                {
                    LogcatTraceHelper.Instance.Log("ERROR", $"パラメータクラス処理中にエラー: {ex.Message}");
                    throw;
                }

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
                LogcatTraceHelper.Instance.Log("DEBUG", "メインクラス処理開始");
                try
                {
                    lines.AddRange(CodeTemplates["MainClass"]
                        .Select(line => {
                            try
                            {
                                return string.Format(line, descriptor.Type, descriptor.Name);
                            }
                            catch (FormatException fex)
                            {
                                LogcatTraceHelper.Instance.Log("ERROR", $"MainClassフォーマットエラー: {fex.Message}, 行: '{line}'");
                                throw;
                            }
                        }));
                }
                catch (Exception ex)
                {
                    LogcatTraceHelper.Instance.Log("ERROR", $"メインクラス処理中にエラー: {ex.Message}");
                    throw;
                }

                // 入力ポート
                LogcatTraceHelper.Instance.Log("DEBUG", $"入力ポート処理開始 (ポート数: {descriptor.InputPorts.Length})");
                foreach (var inputPort in descriptor.InputPorts)
                {
                    try
                    {
                        lines.AddRange(CodeTemplates["InputsSection"]
                            .Select(line => {
                                try
                                {
                                    return string.Format(line, inputPort);
                                }
                                catch (FormatException fex)
                                {
                                    LogcatTraceHelper.Instance.Log("ERROR", $"InputsSectionフォーマットエラー: {fex.Message}, 行: '{line}', ポート: '{inputPort}'");
                                    throw;
                                }
                            }));
                    }
                    catch (Exception ex)
                    {
                        LogcatTraceHelper.Instance.Log("ERROR", $"入力ポート '{inputPort}' 処理中にエラー: {ex.Message}");
                        throw;
                    }
                }

                // 出力ポートセクション
                LogcatTraceHelper.Instance.Log("DEBUG", "出力ポートセクション処理開始");
                try
                {
                    lines.AddRange(CodeTemplates["OutputsSection"]);
                }
                catch (Exception ex)
                {
                    LogcatTraceHelper.Instance.Log("ERROR", $"出力ポートセクション処理中にエラー: {ex.Message}");
                    throw;
                }

                // 出力ポート
                LogcatTraceHelper.Instance.Log("DEBUG", $"出力ポート処理開始 (ポート数: {descriptor.OutputPorts.Length})");
                foreach (var outputPort in descriptor.OutputPorts)
                {
                    try
                    {
                        lines.AddRange(CodeTemplates["OutputsLine"]
                            .Select(line => {
                                try
                                {
                                    return string.Format(line, outputPort);
                                }
                                catch (FormatException fex)
                                {
                                    LogcatTraceHelper.Instance.Log("ERROR", $"OutputsLineフォーマットエラー: {fex.Message}, 行: '{line}', ポート: '{outputPort}'");
                                    throw;
                                }
                            }));
                    }
                    catch (Exception ex)
                    {
                        LogcatTraceHelper.Instance.Log("ERROR", $"出力ポート '{outputPort}' 処理中にエラー: {ex.Message}");
                        throw;
                    }
                }

                // パラメータセクション
                LogcatTraceHelper.Instance.Log("DEBUG", "パラメータセクション処理開始");
                try
                {
                    lines.AddRange(CodeTemplates["ParametersSection"]);
                }
                catch (Exception ex)
                {
                    LogcatTraceHelper.Instance.Log("ERROR", $"パラメータセクション処理中にエラー: {ex.Message}");
                    throw;
                }

                // パラメータ行
                LogcatTraceHelper.Instance.Log("DEBUG", "パラメータ行処理開始");
                foreach (var param in descriptor.Parameters)
                {
                    try
                    {
                        lines.AddRange(CodeTemplates["ParametersLine"]
                            .Select(line => {
                                try
                                {
                                    return string.Format(line, param.Name);
                                }
                                catch (FormatException fex)
                                {
                                    LogcatTraceHelper.Instance.Log("ERROR", $"ParametersLineフォーマットエラー: {fex.Message}, 行: '{line}', パラメータ: '{param.Name}'");
                                    throw;
                                }
                            }));
                    }
                    catch (Exception ex)
                    {
                        LogcatTraceHelper.Instance.Log("ERROR", $"パラメータ行 '{param.Name}' 処理中にエラー: {ex.Message}");
                        throw;
                    }
                }

                // 内部変数
                LogcatTraceHelper.Instance.Log("DEBUG", "内部変数処理開始");
                try
                {
                    lines.AddRange(CodeTemplates["InternalVariables"]);
                }
                catch (Exception ex)
                {
                    LogcatTraceHelper.Instance.Log("ERROR", $"内部変数処理中にエラー: {ex.Message}");
                    throw;
                }

                // updateメソッド
                LogcatTraceHelper.Instance.Log("DEBUG", "updateメソッド処理開始");
                try
                {
                    lines.AddRange(CodeTemplates["UpdateMethod"]);
                }
                catch (Exception ex)
                {
                    LogcatTraceHelper.Instance.Log("ERROR", $"updateメソッド処理中にエラー: {ex.Message}");
                    throw;
                }

                // update入力
                LogcatTraceHelper.Instance.Log("DEBUG", "update入力処理開始");
                foreach (var inputPort in descriptor.InputPorts)
                {
                    try
                    {
                        lines.AddRange(CodeTemplates["UpdateInputs"]
                            .Select(line => {
                                try
                                {
                                    return string.Format(line, inputPort);
                                }
                                catch (FormatException fex)
                                {
                                    LogcatTraceHelper.Instance.Log("ERROR", $"UpdateInputsフォーマットエラー: {fex.Message}, 行: '{line}', ポート: '{inputPort}'");
                                    throw;
                                }
                            }));
                    }
                    catch (Exception ex)
                    {
                        LogcatTraceHelper.Instance.Log("ERROR", $"update入力 '{inputPort}' 処理中にエラー: {ex.Message}");
                        throw;
                    }
                }

                // updateロジック
                LogcatTraceHelper.Instance.Log("DEBUG", "updateロジック処理開始");
                try
                {
                    lines.AddRange(CodeTemplates["UpdateLogic"]);
                }
                catch (Exception ex)
                {
                    LogcatTraceHelper.Instance.Log("ERROR", $"updateロジック処理中にエラー: {ex.Message}");
                    throw;
                }

                // update出力
                LogcatTraceHelper.Instance.Log("DEBUG", "update出力処理開始");
                foreach (var outputPort in descriptor.OutputPorts)
                {
                    try
                    {
                        lines.AddRange(CodeTemplates["UpdateOutputs"]
                            .Select(line => {
                                try
                                {
                                    return string.Format(line, outputPort);
                                }
                                catch (FormatException fex)
                                {
                                    LogcatTraceHelper.Instance.Log("ERROR", $"UpdateOutputsフォーマットエラー: {fex.Message}, 行: '{line}', ポート: '{outputPort}'");
                                    throw;
                                }
                            }));
                    }
                    catch (Exception ex)
                    {
                        LogcatTraceHelper.Instance.Log("ERROR", $"update出力 '{outputPort}' 処理中にエラー: {ex.Message}");
                        throw;
                    }
                }

                // update終了
                LogcatTraceHelper.Instance.Log("DEBUG", "update終了処理開始");
                try
                {
                    lines.AddRange(CodeTemplates["UpdateEnd"]);
                }
                catch (Exception ex)
                {
                    LogcatTraceHelper.Instance.Log("ERROR", $"update終了処理中にエラー: {ex.Message}");
                    throw;
                }

                // resetメソッド
                LogcatTraceHelper.Instance.Log("DEBUG", "resetメソッド処理開始");
                try
                {
                    lines.AddRange(CodeTemplates["ResetMethod"]);
                }
                catch (Exception ex)
                {
                    LogcatTraceHelper.Instance.Log("ERROR", $"resetメソッド処理中にエラー: {ex.Message}");
                    throw;
                }

                // 使用例
                LogcatTraceHelper.Instance.Log("DEBUG", "使用例処理開始");
                try
                {
                    lines.AddRange(CodeTemplates["UsageExample"]
                        .Select(line => {
                            try
                            {
                                return string.Format(line, descriptor.Name, descriptor.Type, descriptor.Type.ToLower());
                            }
                            catch (FormatException fex)
                            {
                                LogcatTraceHelper.Instance.Log("ERROR", $"UsageExampleフォーマットエラー: {fex.Message}, 行: '{line}'");
                                throw;
                            }
                        }));
                }
                catch (Exception ex)
                {
                    LogcatTraceHelper.Instance.Log("ERROR", $"使用例処理中にエラー: {ex.Message}");
                    throw;
                }

                // 使用入力
                LogcatTraceHelper.Instance.Log("DEBUG", "使用入力処理開始");
                foreach (var inputPort in descriptor.InputPorts)
                {
                    try
                    {
                        lines.AddRange(CodeTemplates["UsageInputs"]
                            .Select(line => {
                                try
                                {
                                    return string.Format(line, inputPort);
                                }
                                catch (FormatException fex)
                                {
                                    LogcatTraceHelper.Instance.Log("ERROR", $"UsageInputsフォーマットエラー: {fex.Message}, 行: '{line}', ポート: '{inputPort}'");
                                    throw;
                                }
                            }));
                    }
                    catch (Exception ex)
                    {
                        LogcatTraceHelper.Instance.Log("ERROR", $"使用入力 '{inputPort}' 処理中にエラー: {ex.Message}");
                        throw;
                    }
                }

                // 使用シミュレーション
                LogcatTraceHelper.Instance.Log("DEBUG", "使用シミュレーション処理開始");
                try
                {
                    lines.AddRange(CodeTemplates["UsageSimulation"]);
                }
                catch (Exception ex)
                {
                    LogcatTraceHelper.Instance.Log("ERROR", $"使用シミュレーション処理中にエラー: {ex.Message}");
                    throw;
                }

                // 使用出力
                LogcatTraceHelper.Instance.Log("DEBUG", "使用出力処理開始");
                foreach (var outputPort in descriptor.OutputPorts)
                {
                    try
                    {
                        lines.AddRange(CodeTemplates["UsageOutputs"]
                            .Select(line => {
                                try
                                {
                                    return string.Format(line, outputPort);
                                }
                                catch (FormatException fex)
                                {
                                    LogcatTraceHelper.Instance.Log("ERROR", $"UsageOutputsフォーマットエラー: {fex.Message}, 行: '{line}', ポート: '{outputPort}'");
                                    throw;
                                }
                            }));
                    }
                    catch (Exception ex)
                    {
                        LogcatTraceHelper.Instance.Log("ERROR", $"使用出力 '{outputPort}' 処理中にエラー: {ex.Message}");
                        throw;
                    }
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