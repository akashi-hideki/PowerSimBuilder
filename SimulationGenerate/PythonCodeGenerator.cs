using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// Pythonシミュレーションコード生成クラス
    /// 作成日: 2025-08-13
    /// 注意点: システム構成からPythonシミュレーションコードを生成・管理
    /// </summary>
    public class PythonCodeGenerator
    {
        private int _componentCounter = 1;
        private string _lastGeneratedCode = string.Empty;
        private string _lastFilePath = string.Empty;

        /// <summary>
        /// 最後に生成されたPythonコード
        /// </summary>
        public string LastGeneratedCode => _lastGeneratedCode;

        /// <summary>
        /// 最後に保存されたファイルパス
        /// </summary>
        public string LastFilePath => _lastFilePath;

        /// <summary>
        /// シミュレーションコードを生成
        /// </summary>
        public string GenerateSimulationCode(IEnumerable<ComponentControl> components, List<Connection> connections)
        {
            _lastGeneratedCode = BuildSimulationCode(components, connections);
            return _lastGeneratedCode;
        }

        /// <summary>
        /// コードをファイルに保存
        /// </summary>
        public bool SaveCodeToFile(string defaultDirectory = null)
        {
            if (string.IsNullOrEmpty(_lastGeneratedCode))
            {
                MessageBox.Show("生成されたコードがありません", "エラー", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                string directory = defaultDirectory ?? Application.StartupPath;
                string fileName = $"simulation_{DateTime.Now:yyyyMMdd_HHmmss}.py";
                _lastFilePath = Path.Combine(directory, fileName);

                File.WriteAllText(_lastFilePath, _lastGeneratedCode);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイルの保存に失敗しました: {ex.Message}", "エラー", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// コードビューアーを表示
        /// </summary>
        public void ShowCodeViewer()
        {
            if (string.IsNullOrEmpty(_lastGeneratedCode))
            {
                MessageBox.Show("生成されたコードがありません", "エラー", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var viewer = new CodeViewerForm(_lastGeneratedCode, _lastFilePath);
            viewer.Show();
        }

        /// <summary>
        /// Pythonコードを実行
        /// </summary>
        public void RunPythonSimulation(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) filePath = _lastFilePath;
            
            if (!File.Exists(filePath))
            {
                MessageBox.Show("実行可能なファイルが見つかりません", "エラー", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // 実際のPython実行処理（例: プロセス起動）
                // 注意: 環境にPythonがインストールされている必要がある
                System.Diagnostics.Process.Start("python", filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"シミュレーション実行エラー: {ex.Message}", "エラー", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string BuildSimulationCode(IEnumerable<ComponentControl> components, List<Connection> connections)
        {
            var code = new StringBuilder();
            code.AppendLine("from power_system_simulator import *");
            code.AppendLine("import numpy as np");
            code.AppendLine();
            code.AppendLine("# 自動生成された電力システムシミュレーションコード");
            code.AppendLine($"# 生成日時: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            code.AppendLine();

            _componentCounter = 1;
            
            foreach (var component in components)
            {
                component.InstanceName = $"comp{_componentCounter++}";
                code.AppendLine(GenerateComponentCode(component));
            }
            code.AppendLine();

            code.AppendLine(GenerateConnectionsCode(connections));
            code.AppendLine();

            code.AppendLine("def run_simulation():");
            code.AppendLine("    manager = SimulationManager()");

            foreach (var component in components)
            {
                code.AppendLine($"    manager.add_component({component.InstanceName})");
            }

            foreach (var connection in connections)
            {
                code.AppendLine($"    manager.connection_manager.add_connection(" +
                               $"'{connection.StartComponent.InstanceName}', " +
                               $"'{connection.StartPort}', " +
                               $"'{connection.EndComponent.InstanceName}', " +
                               $"'{connection.EndPort}')");
            }

            code.AppendLine("    manager.run_simulation(t_total=0.5, dt=0.001)");
            code.AppendLine();
            code.AppendLine("if __name__ == '__main__':");
            code.AppendLine("    run_simulation()");

            return code.ToString();
        }

        /// <summary>
        /// コンポーネント別コード生成
        /// </summary>
        private string GenerateComponentCode(ComponentControl component)
        {
            switch (component.Descriptor.Type)
            {
                case "InductionMotor":
                    return GenerateMotorCode(component);
                case "FaultMonitor":
                    return GenerateFaultMonitorCode(component);
                default:
                    return GenerateDefaultComponentCode(component);
            }
        }

        /// <summary>
        /// モーター用コード生成
        /// </summary>
        private string GenerateMotorCode(ComponentControl motor)
        {
            var code = new StringBuilder();
            code.AppendLine($"{motor.InstanceName} = InductionMotor(");
            code.AppendLine($"    name='{motor.InstanceName}',");
            code.AppendLine($"    params={{");

            bool firstParam = true;
            foreach (var param in motor.Parameters)
            {
                if (!firstParam) code.AppendLine(",");

                if (param.Key == "ThermalResistance" || param.Key == "ThermalCapacity")
                {
                    code.Append($"        '{param.Key}': {param.Value}");
                }
                else
                {
                    code.Append($"        '{param.Key}': {FormatValue(param.Value)}");
                }

                firstParam = false;
            }

            code.AppendLine();
            code.AppendLine($"    }}");
            code.AppendLine($")");
            return code.ToString();
        }

        /// <summary>
        /// 故障監視システム用コード生成
        /// </summary>
        private string GenerateFaultMonitorCode(ComponentControl monitor)
        {
            var code = new StringBuilder();
            code.AppendLine($"{monitor.InstanceName} = FaultMonitor()");

            if (monitor.Parameters.TryGetValue("Conditions", out var conditionsObj) &&
                conditionsObj is List<FaultCondition> conditions)
            {
                foreach (var condition in conditions)
                {
                    code.AppendLine($"{monitor.InstanceName}.add_condition(");
                    code.AppendLine($"    component_name='{condition.ComponentName}',");
                    code.AppendLine($"    state_name='{condition.StateName}',");
                    code.AppendLine($"    condition_type='{condition.ConditionType}'");
                    code.AppendLine($")");
                }
            }

            return code.ToString();
        }

        /// <summary>
        /// デフォルトコンポーネント用コード生成
        /// </summary>
        private string GenerateDefaultComponentCode(ComponentControl component)
        {
            var code = new StringBuilder();
            code.Append($"{component.InstanceName} = {component.Descriptor.Type}(");
            code.Append($"name='{component.InstanceName}'");

            if (component.Parameters.Count > 0)
            {
                code.AppendLine(",");
                code.AppendLine("    params={");

                bool first = true;
                foreach (var param in component.Parameters)
                {
                    if (!first) code.AppendLine(",");
                    code.Append($"        '{param.Key}': {FormatValue(param.Value)}");
                    first = false;
                }

                code.AppendLine();
                code.Append("    }");
            }

            code.AppendLine(")");
            return code.ToString();
        }

        /// <summary>
        /// 接続コード生成
        /// </summary>
        private string GenerateConnectionsCode(List<Connection> connections)
        {
            var code = new StringBuilder();
            foreach (var connection in connections)
            {
                code.AppendLine($"{connection.StartComponent.InstanceName}.connect(" +
                               $"{connection.EndComponent.InstanceName}, " +
                               $"'{connection.StartPort}', '{connection.EndPort}')");
            }
            return code.ToString();
        }

        /// <summary>
        /// 値のフォーマット処理
        /// </summary>
        private string FormatValue(object value)
        {
            if (value is string strValue)
                return $"'{strValue}'";

            return value.ToString();
        }
    }

    /// <summary>
    /// Pythonコードビューアーフォーム
    /// </summary>
    public class CodeViewerForm : Form
    {
        private readonly string _filePath;

        public CodeViewerForm(string code, string filePath)
        {
            _filePath = filePath;
            InitializeForm(code);
        }

        private void InitializeForm(string code)
        {
            Text = "Pythonコードビューアー - " + Path.GetFileName(_filePath);
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;

            var textBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                Text = code,
                Font = new Font("Consolas", 10),
                ScrollBars = ScrollBars.Both,
                ReadOnly = true
            };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40
            };

            var runButton = new Button
            {
                Text = "シミュレーション実行",
                Width = 150,
                Height = 30,
                Top = 5,
                Left = 10
            };

            var openFolderButton = new Button
            {
                Text = "フォルダを開く",
                Width = 100,
                Height = 30,
                Top = 5,
                Left = 170
            };

            runButton.Click += (s, e) => 
            {
                var generator = new PythonCodeGenerator();
                generator.RunPythonSimulation(_filePath);
            };

            openFolderButton.Click += (s, e) =>
            {
                try
                {
                    string folder = Path.GetDirectoryName(_filePath);
                    System.Diagnostics.Process.Start(folder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"フォルダを開けませんでした: {ex.Message}", "エラー", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            buttonPanel.Controls.Add(runButton);
            buttonPanel.Controls.Add(openFolderButton);

            Controls.Add(textBox);
            Controls.Add(buttonPanel);
        }
    }
}