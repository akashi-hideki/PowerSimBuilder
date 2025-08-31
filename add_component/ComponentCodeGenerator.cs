using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerSimBuilder
{
    public class PythonCodeGenerator
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

            // ヘッダー
            lines.AddRange(CodeTemplates["Header"]
                .Select(line => string.Format(line, descriptor.Name, DateTime.Now)));

            // パラメータクラス
            lines.AddRange(CodeTemplates["ParametersClass"]
                .Select(line => string.Format(line, descriptor.Type, descriptor.Name)));
            
            foreach (var param in descriptor.Parameters)
            {
                string pythonType = ConvertToPythonType(param.Type);
                string defaultValue = ConvertToPythonValue(param.DefaultValue);
                lines.Add($"    {param.Name}: {pythonType} = {defaultValue}");
            }
            lines.Add("");

            // メインクラス
            lines.AddRange(CodeTemplates["MainClass"]
                .Select(line => string.Format(line, descriptor.Type, descriptor.Name)));

            // 入力ポート
            foreach (var inputPort in descriptor.InputPorts)
            {
                lines.AddRange(CodeTemplates["InputsSection"]
                    .Select(line => string.Format(line, inputPort)));
            }

            // 出力ポート
            lines.AddRange(CodeTemplates["OutputsSection"]);
            foreach (var outputPort in descriptor.OutputPorts)
            {
                lines.AddRange(CodeTemplates["OutputsLine"]
                    .Select(line => string.Format(line, outputPort)));
            }

            // パラメータ
            lines.AddRange(CodeTemplates["ParametersSection"]);
            foreach (var param in descriptor.Parameters)
            {
                lines.AddRange(CodeTemplates["ParametersLine"]
                    .Select(line => string.Format(line, param.Name)));
            }

            // 内部変数
            lines.AddRange(CodeTemplates["InternalVariables"]);

            // updateメソッド
            lines.AddRange(CodeTemplates["UpdateMethod"]);
            foreach (var inputPort in descriptor.InputPorts)
            {
                lines.AddRange(CodeTemplates["UpdateInputs"]
                    .Select(line => string.Format(line, inputPort)));
            }
            
            lines.AddRange(CodeTemplates["UpdateLogic"]);
            foreach (var outputPort in descriptor.OutputPorts)
            {
                lines.AddRange(CodeTemplates["UpdateOutputs"]
                    .Select(line => string.Format(line, outputPort)));
            }
            
            lines.AddRange(CodeTemplates["UpdateEnd"]);

            // resetメソッド
            lines.AddRange(CodeTemplates["ResetMethod"]);

            // 使用例
            lines.AddRange(CodeTemplates["UsageExample"]
                .Select(line => string.Format(line, descriptor.Name, descriptor.Type, descriptor.Type.ToLower())));
            
            foreach (var inputPort in descriptor.InputPorts)
            {
                lines.AddRange(CodeTemplates["UsageInputs"]
                    .Select(line => string.Format(line, inputPort)));
            }
            
            lines.AddRange(CodeTemplates["UsageSimulation"]);
            foreach (var outputPort in descriptor.OutputPorts)
            {
                lines.AddRange(CodeTemplates["UsageOutputs"]
                    .Select(line => string.Format(line, outputPort)));
            }

            // ファイルに書き込み
            File.WriteAllLines(filePath, lines);
        }

        private string ConvertToPythonType(string csharpType)
        {
            return csharpType.ToLower() switch
            {
                "int" => "int",
                "double" => "float",
                "string" => "str",
                "bool" => "bool",
                "list<string>" => "List[str]",
                "dictionary<string, double>" => "Dict[str, float]",
                _ => "Any"
            };
        }

        private string ConvertToPythonValue(object value)
        {
            if (value is string str)
                return $"\"{str}\"";

            if (value is bool b)
                return b ? "True" : "False";

            return value.ToString();
        }
    }
}