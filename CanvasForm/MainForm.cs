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
    /// <summary>
    /// アプリケーションのメインフォームクラス
    /// 作成日: 2025-08-12
    /// 注意点: ユーザーインターフェイスと主要ロジックを統合
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly CanvasManager _canvasManager;
        private readonly PythonCodeGenerator _codeGenerator = new PythonCodeGenerator();

        /// <summary>
        /// メインフォームのコンストラクタ
        /// 注意点: UIコンポーネントとイベントハンドラを初期化
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            InitializeEventHandlers();
            _canvasManager = new CanvasManager(designCanvas1, componentPalette1);
            SubscribeToEvents();

            // 接続レンダラーの初期設定
            designCanvas1.ConnectionRenderer = new BezierConnectionRenderer();
            designCanvas1.ConnectionSelected += DesignCanvas_ConnectionSelected;
        }

        /// <summary>
        /// イベントハンドラを初期化
        /// 注意点: ツールバーボタンとプロパティグリッドのイベントを登録
        /// </summary>
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
            arrangeButton.Click += arrangeButton_Click;
            propertyGrid1.PropertyValueChanged += propertyGrid1_PropertyValueChanged;
        }

        /// <summary>
        /// イベント購読を設定
        /// 注意点: キャンバス管理クラスのイベントを購読
        /// </summary>
        private void SubscribeToEvents()
        {
            _canvasManager.CanvasModified += CanvasManager_CanvasModified;
            _canvasManager.SelectionChanged += CanvasManager_SelectionChanged;
            componentPalette1.ComponentSelected += ComponentPalette_ComponentSelected;
        }

        /// <summary>
        /// コンポーネントパレット選択イベント
        /// 注意点: 追加ボタンの有効状態を更新
        /// </summary>
        private void ComponentPalette_ComponentSelected(object sender, EventArgs e)
        {
            addButton.Enabled = (componentPalette1.SelectedComponent != null);
        }

        /// <summary>
        /// コンポーネント追加ボタンクリック
        /// 注意点: カスタムコンポーネント追加フォームを表示
        /// </summary>
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

        /// <summary>
        /// キャンバス変更イベント
        /// 注意点: 保存状態とUNDO/REDOボタンを更新
        /// </summary>
        private void CanvasManager_CanvasModified(object sender, EventArgs e)
        {
            UpdateSaveStatus();
            UpdateUndoRedoButtons();
        }

        /// <summary>
        /// 選択変更イベント
        /// 注意点: プロパティグリッドに選択オブジェクトを表示
        /// </summary>
        private void CanvasManager_SelectionChanged(object sender, EventArgs e)
        {
            propertyGrid1.SelectedObject = _canvasManager.SelectedComponent?.Parameters;
            UpdateDeleteButton();
        }

        /// <summary>
        /// コンポーネント追加ボタンクリック
        /// 注意点: パレットで選択中のコンポーネントをキャンバスに追加
        /// </summary>
        private void addButton_Click(object sender, EventArgs e)
        {
            var selectedDescriptor = componentPalette1.SelectedComponent;
            if (selectedDescriptor != null)
            {
                Point center = new Point(designCanvas1.Width / 2, designCanvas1.Height / 2);
                _canvasManager.AddComponent(selectedDescriptor, center);

                // 自動配置実行
                designCanvas1.ArrangeComponents();
            }
        }

        /// <summary>
        /// 削除ボタンクリック
        /// 注意点: 選択中のコンポーネントまたは接続を削除
        /// </summary>
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

        /// <summary>
        /// 保存ボタンクリック
        /// 注意点: JSON形式でシステム構成を保存
        /// </summary>
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

        /// <summary>
        /// 読み込みボタンクリック
        /// 注意点: JSONファイルからシステム構成を復元
        /// </summary>
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
                designCanvas1.ArrangeComponents();
                UpdateSaveStatus();
                UpdateUndoRedoButtons();
                UpdateDeleteButton();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイルの読み込みに失敗しました: {ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// UNDOボタンクリック
        /// 注意点: 直前の操作を取り消し
        /// </summary>
        private void undoButton_Click(object sender, EventArgs e)
        {
            _canvasManager.Undo();
            designCanvas1.Invalidate();
        }

        /// <summary>
        /// REDOボタンクリック
        /// 注意点: 取り消した操作を再実行
        /// </summary>
        private void redoButton_Click(object sender, EventArgs e)
        {
            _canvasManager.Redo();
            designCanvas1.Invalidate();
        }

        /// <summary>
        /// 配置整理ボタンクリック
        /// 注意点: 自動レイアウトを実行
        /// </summary>
        private void arrangeButton_Click(object sender, EventArgs e)
        {
            designCanvas1.ArrangeComponents();
        }

        /// <summary>
        /// 実行ボタンクリック
        /// 注意点: Pythonシミュレーションコードを生成・実行
        /// </summary>
        private void runButton_Click(object sender, EventArgs e)
        {
            string pythonCode = GenerateSimulationCode();
            var previewForm = new CodePreviewForm(pythonCode);
            previewForm.Show();
            RunPythonSimulation(pythonCode);
        }

        /// <summary>
        /// 接続選択イベントハンドラ
        /// 注意点: プロパティグリッドに接続プロパティを表示
        /// </summary>
        private void DesignCanvas_ConnectionSelected(object sender, ConnectionEventArgs e)
        {
            if (e.Connection != null)
            {
                propertyGrid1.SelectedObject = new ConnectionProperties(e.Connection);
            }
            else
            {
                propertyGrid1.SelectedObject = null;
            }
        }

        /// <summary>
        /// プロパティ値変更イベント
        /// 注意点: 故障監視システムの条件編集をトリガー
        /// </summary>
        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (_canvasManager.SelectedComponent?.Descriptor.Type == "FaultMonitor" &&
                e.ChangedItem.Label == "Conditions")
            {
                OpenFaultConditionEditor();
            }
        }

        /// <summary>
        /// 故障条件エディタを開く
        /// 注意点: 選択中の故障監視システムに対して実行
        /// </summary>
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

        /// <summary>
        /// シミュレーションコードを生成
        /// 注意点: システム構成からPythonコードを生成
        /// </summary>
        private string GenerateSimulationCode()
        {
            var code = new StringBuilder();
            code.AppendLine("from power_system_simulator import *");
            code.AppendLine("import numpy as np");
            code.AppendLine();

            // コンポーネントコード生成
            foreach (var component in _canvasManager.GetAllComponents())
            {
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
                               $"'{connection.StartPort}', " +
                               $"'{connection.EndComponent.InstanceName}', " +
                               $"'{connection.EndPort}')");
            }

            code.AppendLine("    manager.run_simulation(t_total=0.5, dt=0.001)");

            return code.ToString();
        }

        /// <summary>
        /// コンポーネント別コード生成
        /// 注意点: コンポーネントタイプごとに生成ロジックを分岐
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
        /// 注意点: 熱パラメータを特別扱い
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
        /// 注意点: 条件設定をループ処理
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
        /// 注意点: パラメータ辞書を適切にフォーマット
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
        /// 注意点: 全接続関係をconnectメソッドで表現
        /// </summary>
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

        /// <summary>
        /// 値のフォーマット処理
        /// 注意点: 文字列はクォーテーションで囲む
        /// </summary>
        private string FormatValue(object value)
        {
            if (value is string strValue)
                return $"'{strValue}'";

            return value.ToString();
        }

        /// <summary>
        /// Pythonシミュレーションを実行
        /// 注意点: Python.Runtimeを使用
        /// </summary>
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
                MessageBox.Show("シミュレーションが正常に完了しました", "成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"シミュレーションエラー: {ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 保存状態を更新
        /// 注意点: 保存ボタンの有効状態を制御
        /// </summary>
        private void UpdateSaveStatus()
        {
            saveButton.Enabled = _canvasManager.IsModified;
        }

        /// <summary>
        /// UNDO/REDOボタン状態を更新
        /// </summary>
        private void UpdateUndoRedoButtons()
        {
            undoButton.Enabled = _canvasManager.CanUndo;
            redoButton.Enabled = _canvasManager.CanRedo;
        }

        /// <summary>
        /// 削除ボタン状態を更新
        /// 注意点: 選択対象がある場合のみ有効化
        /// </summary>
        private void UpdateDeleteButton()
        {
            deleteButton.Enabled =
                _canvasManager.SelectedComponent != null ||
                _canvasManager.SelectedConnection != null;
        }

        /// <summary>
        /// コードプレビューフォームクラス
        /// 注意点: 生成されたPythonコードを表示
        /// </summary>
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