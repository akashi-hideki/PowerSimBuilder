using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class MainForm : Form
    {
        private readonly CanvasManager _canvasManager;
        private readonly PythonCodeGenerator _codeGenerator = new PythonCodeGenerator();
        private int _componentCounter = 1;

        public MainForm()
        {
            InitializeComponent();
            InitializeEventHandlers();
            _canvasManager = new CanvasManager(designCanvas1, componentPalette1);
            SubscribeToEvents();
        }

        private void InitializeEventHandlers()
        {
            addComponentToolStripButton.Click += AddComponentToolStripButton_Click;
            addButton.Click += addButton_Click;
            deleteButton.Click += deleteButton_Click;
            saveButton.Click += saveButton_Click;
            loadButton.Click += loadButton_Click;
            undoButton.Click += undoButton_Click;
            redoButton.Click += redoButton_Click;
            runButton.Click += runButton_Click;
            propertyGrid1.PropertyValueChanged += propertyGrid1_PropertyValueChanged;
        }

        private void SubscribeToEvents()
        {
            _canvasManager.CanvasModified += CanvasManager_CanvasModified;
            _canvasManager.SelectionChanged += CanvasManager_SelectionChanged;
            componentPalette1.ComponentSelected += ComponentPalette_ComponentSelected;
        }

        private void ComponentPalette_ComponentSelected(object sender, EventArgs e)
        {
            addButton.Enabled = (componentPalette1.SelectedComponent != null);
        }

        private void AddComponentToolStripButton_Click(object sender, EventArgs e)
        {
            using (var form = new AddComponentForm())
            {
                if (form.ShowDialog() == DialogResult.OK && form.NewComponent != null)
                {
                    componentPalette1.AddComponent(form.NewComponent);
                }
            }
        }

        private void CanvasManager_CanvasModified(object sender, EventArgs e)
        {
            UpdateSaveStatus();
            UpdateUndoRedoButtons();
        }

        private void CanvasManager_SelectionChanged(object sender, EventArgs e)
        {
            propertyGrid1.SelectedObject = _canvasManager.SelectedComponent?.Parameters;
            UpdateDeleteButton();
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            var selectedDescriptor = componentPalette1.SelectedComponent;
            if (selectedDescriptor != null)
            {
                Point center = new Point(designCanvas1.Width / 2, designCanvas1.Height / 2);
                _canvasManager.AddComponent(selectedDescriptor, center);
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            if (_canvasManager.SelectedComponent != null)
            {
                _canvasManager.RemoveComponent(_canvasManager.SelectedComponent);
            }
            else if (_canvasManager.SelectedConnection != null)
            {
                _canvasManager.RemoveConnection(_canvasManager.SelectedConnection);
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            var model = _canvasManager.SaveModel();
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new PointJsonConverter() }
            };

            var json = JsonSerializer.Serialize(model, options);
            File.WriteAllText("system.json", json);
            _canvasManager.IsModified = false;
            UpdateSaveStatus();
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            try
            {
                var json = File.ReadAllText("system.json");
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new PointJsonConverter() }
                };

                var model = JsonSerializer.Deserialize<CanvasModel>(json, options);
                _canvasManager.LoadModel(model);
                UpdateSaveStatus();
                UpdateUndoRedoButtons();
                UpdateDeleteButton();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイルの読み込みに失敗しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void undoButton_Click(object sender, EventArgs e)
        {
            _canvasManager.Undo();
        }

        private void redoButton_Click(object sender, EventArgs e)
        {
            _canvasManager.Redo();
        }

        private void runButton_Click(object sender, EventArgs e)
        {
            string pythonCode = GenerateSimulationCode();
            var previewForm = new CodePreviewForm(pythonCode);
            previewForm.Show();
            RunPythonSimulation(pythonCode);
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (_canvasManager.SelectedComponent?.Descriptor.Type == "FaultMonitor" && 
                e.ChangedItem.Label == "Conditions")
            {
                OpenFaultConditionEditor();
            }
        }

        public void OpenFaultConditionEditor()
        {
            var components = _canvasManager.GetAllComponents();
            using (var form = new FaultConditionEditorForm(components))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    var faultMonitor = _canvasManager.SelectedComponent;
                    if (faultMonitor != null)
                    {
                        faultMonitor.Parameters["Conditions"] = form.Conditions;
                    }
                }
            }
        }

        private string GenerateSimulationCode()
        {
            var code = new StringBuilder();
            code.AppendLine("from power_system_simulator import *");
            code.AppendLine("import numpy as np");
            code.AppendLine();

            // コンポーネントコード生成
            foreach (var component in _canvasManager.GetAllComponents())
            {
                component.InstanceName = $"comp{_componentCounter++}";
                code.AppendLine(GenerateComponentCode(component));
            }
            code.AppendLine();

            // 接続コード生成
            code.AppendLine(GenerateConnectionsCode());
            code.AppendLine();

            // シミュレーション実行コード
            code.AppendLine("def run_simulation():");
            code.AppendLine("    manager = SimulationManager()");
            
            foreach (var component in _canvasManager.GetAllComponents())
            {
                code.AppendLine($"    manager.add_component({component.InstanceName})");
            }
            
            // 接続を追加
            foreach (var connection in _canvasManager.GetAllConnections())
            {
                code.AppendLine($"    manager.connection_manager.add_connection(" +
                               $"'{connection.StartComponent.InstanceName}', " +
                               $"'{connection.EndComponent.InstanceName}')");
            }
            
            code.AppendLine("    manager.run_simulation(t_total=0.5, dt=0.001)");
            
            return code.ToString();
        }

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

        private string GenerateConnectionsCode()
        {
            var code = new StringBuilder();
            foreach (var connection in _canvasManager.GetAllConnections())
            {
                code.AppendLine($"{connection.StartComponent.InstanceName}.connect(" +
                               $"{connection.EndComponent.InstanceName}, " +
                               $"'{connection.StartPort}', '{connection.EndPort}')");
            }
            return code.ToString();
        }

        private string FormatValue(object value)
        {
            if (value is string strValue)
                return $"'{strValue}'";

            return value.ToString();
        }

        private void RunPythonSimulation(string code)
        {
            try
            {
                using (Py.GIL())
                {
                    dynamic scope = Py.CreateScope();
                    scope.Exec(code);
                    scope.run_simulation();
                }
                MessageBox.Show("シミュレーションが正常に完了しました", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"シミュレーションエラー: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateSaveStatus()
        {
            saveButton.Enabled = _canvasManager.IsModified;
        }

        private void UpdateUndoRedoButtons()
        {
            undoButton.Enabled = _canvasManager.CanUndo;
            redoButton.Enabled = _canvasManager.CanRedo;
        }

        private void UpdateDeleteButton()
        {
            deleteButton.Enabled = 
                _canvasManager.SelectedComponent != null || 
                _canvasManager.SelectedConnection != null;
        }

        public class CodePreviewForm : Form
        {
            public CodePreviewForm(string code)
            {
                Text = "生成されたPythonコード";
                Size = new Size(800, 600);

                var textBox = new TextBox
                {
                    Multiline = true,
                    Dock = DockStyle.Fill,
                    Text = code,
                    Font = new Font("Consolas", 10),
                    ScrollBars = ScrollBars.Both,
                    ReadOnly = true
                };

                Controls.Add(textBox);
            }
        }
    }
}