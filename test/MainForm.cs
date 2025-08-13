using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Configuration;

namespace PowerSimBuilder
{
    /// <summary>
    /// メインフォームクラス
    /// 作成日: 2025-08-15
    /// 変更内容: XMLベースコンポーネント定義システムの統合
    /// 注意点: アプリケーションの主要UIとコントロールを管理
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly CanvasManager _canvasManager;
        private readonly PythonCodeGenerator _codeGenerator = new PythonCodeGenerator();
        private readonly ComponentDefinitionManager _definitionManager = new ComponentDefinitionManager();

        /// <summary>
        /// コンストラクタ
        /// 作成日: 2025-08-15
        /// 処理内容: UIコンポーネントと定義マネージャーを初期化
        /// </summary>
        public MainForm()
        {
            // 定義マネージャーの初期化
            _definitionManager.LoadDefinitions();
            
            // UIコンポーネントの初期化
            InitializeComponent();
            
            // パレットに定義マネージャーを設定
            componentPalette1.SetDefinitionManager(_definitionManager);
            
            // キャンバス管理の初期化
            _canvasManager = new CanvasManager(designCanvas1, componentPalette1);
            InitializeEventHandlers();
            SubscribeToEvents();

            // 接続レンダラーの設定
            designCanvas1.ConnectionRenderer = new BezierConnectionRenderer();
            designCanvas1.ConnectionSelected += DesignCanvas_ConnectionSelected;
        }

        /// <summary>
        /// イベントハンドラを初期化
        /// 作成日: 2025-08-15
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
        /// 作成日: 2025-08-15
        /// </summary>
        private void SubscribeToEvents()
        {
            _canvasManager.CanvasModified += CanvasManager_CanvasModified;
            _canvasManager.SelectionChanged += CanvasManager_SelectionChanged;
            componentPalette1.ComponentSelected += ComponentPalette_ComponentSelected;
        }

        /// <summary>
        /// コンポーネントパレット選択イベント
        /// 作成日: 2025-08-15
        /// </summary>
        private void ComponentPalette_ComponentSelected(object sender, EventArgs e)
        {
            addButton.Enabled = (componentPalette1.SelectedDefinition != null);
        }

        /// <summary>
        /// コンポーネント追加ボタンクリック
        /// 作成日: 2025-08-15
        /// </summary>
        private void AddComponentToolStripButton_Click(object sender, EventArgs e)
        {
            using (var form = new AddComponentForm(_definitionManager))
            {
                if (form.ShowDialog() == DialogResult.OK && form.NewComponentDefinition != null)
                {
                    // パレットに追加
                    componentPalette1.AddComponent(form.NewComponentDefinition);
                    
                    // パレットを更新
                    componentPalette1.RefreshPalette();
                }
            }
        }

        /// <summary>
        /// キャンバス変更イベント
        /// 作成日: 2025-08-15
        /// </summary>
        private void CanvasManager_CanvasModified(object sender, EventArgs e)
        {
            UpdateSaveStatus();
            UpdateUndoRedoButtons();
        }

        /// <summary>
        /// 選択変更イベント
        /// 作成日: 2025-08-15
        /// </summary>
        private void CanvasManager_SelectionChanged(object sender, EventArgs e)
        {
            propertyGrid1.SelectedObject = _canvasManager.SelectedComponent?.Parameters;
            UpdateDeleteButton();
        }

        /// <summary>
        /// コンポーネント追加ボタンクリック
        /// 作成日: 2025-08-15
        /// </summary>
        private void addButton_Click(object sender, EventArgs e)
        {
            var selectedDefinition = componentPalette1.SelectedDefinition;
            if (selectedDefinition != null)
            {
                Point center = new Point(designCanvas1.Width / 2, designCanvas1.Height / 2);
                AddComponentToCanvas(selectedDefinition, center);
            }
        }

        /// <summary>
        /// キャンバスにコンポーネントを追加
        /// 作成日: 2025-08-15
        /// </summary>
        private void AddComponentToCanvas(ComponentDefinition definition, Point location)
        {
            // ディスクリプタに変換
            var descriptor = new ComponentDescriptor
            {
                Name = definition.Name,
                Type = definition.Type,
                Category = definition.Category,
                Parameters = ConvertParameters(definition.Parameters),
                InputPorts = definition.InputPorts.ToArray(),
                OutputPorts = definition.OutputPorts.ToArray()
            };
            
            _canvasManager.AddComponent(descriptor, location);
            designCanvas1.ArrangeComponents();
        }

        /// <summary>
        /// パラメータ定義を変換
        /// 作成日: 2025-08-15
        /// </summary>
        private List<ParameterDescriptor> ConvertParameters(List<ParameterDefinition> parameters)
        {
            var result = new List<ParameterDescriptor>();
            foreach (var param in parameters)
            {
                // デフォルト値の型変換
                object defaultValue = param.DefaultValue;
                try
                {
                    switch (param.Type)
                    {
                        case "double":
                            defaultValue = double.Parse(param.DefaultValue);
                            break;
                        case "int":
                            defaultValue = int.Parse(param.DefaultValue);
                            break;
                        case "bool":
                            defaultValue = bool.Parse(param.DefaultValue);
                            break;
                        // string とその他の型はそのまま
                    }
                }
                catch
                {
                    // 変換失敗時は文字列のまま
                }
                
                result.Add(new ParameterDescriptor
                {
                    Name = param.Name,
                    Type = param.Type,
                    DefaultValue = defaultValue
                });
            }
            return result;
        }

        /// <summary>
        /// 削除ボタンクリック
        /// 作成日: 2025-08-15
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
        /// 作成日: 2025-08-15
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
        /// 作成日: 2025-08-15
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
        /// 作成日: 2025-08-15
        /// </summary>
        private void undoButton_Click(object sender, EventArgs e)
        {
            _canvasManager.Undo();
            designCanvas1.Invalidate();
        }

        /// <summary>
        /// REDOボタンクリック
        /// 作成日: 2025-08-15
        /// </summary>
        private void redoButton_Click(object sender, EventArgs e)
        {
            _canvasManager.Redo();
            designCanvas1.Invalidate();
        }

        /// <summary>
        /// 配置整理ボタンクリック
        /// 作成日: 2025-08-15
        /// </summary>
        private void arrangeButton_Click(object sender, EventArgs e)
        {
            designCanvas1.ArrangeComponents();
        }

        /// <summary>
        /// 実行ボタンクリック
        /// 作成日: 2025-08-15
        /// </summary>
        private void runButton_Click(object sender, EventArgs e)
        {
            var components = _canvasManager.GetAllComponents();
            var connections = _canvasManager.GetAllConnections();
            
            // コード生成
            string pythonCode = _codeGenerator.GenerateSimulationCode(components, connections);
            
            // ファイル保存（デフォルトディレクトリはアプリケーションデータフォルダ）
            string outputDir = GetPythonOutputDirectory();
            if (_codeGenerator.SaveCodeToFile(outputDir))
            {
                // ビューアー表示
                _codeGenerator.ShowCodeViewer();
            }
        }

        /// <summary>
        /// Python出力ディレクトリを取得
        /// 作成日: 2025-08-15
        /// </summary>
        private string GetPythonOutputDirectory()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string outputDir = Path.Combine(appData, "PowerSimBuilder", "PythonOutput");
            
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            
            return outputDir;
        }

        /// <summary>
        /// 接続選択イベントハンドラ
        /// 作成日: 2025-08-15
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
        /// 作成日: 2025-08-15
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
        /// 作成日: 2025-08-15
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
        /// 保存状態を更新
        /// 作成日: 2025-08-15
        /// </summary>
        private void UpdateSaveStatus()
        {
            saveButton.Enabled = _canvasManager.IsModified;
        }

        /// <summary>
        /// UNDO/REDOボタン状態を更新
        /// 作成日: 2025-08-15
        /// </summary>
        private void UpdateUndoRedoButtons()
        {
            undoButton.Enabled = _canvasManager.CanUndo;
            redoButton.Enabled = _canvasManager.CanRedo;
        }

        /// <summary>
        /// 削除ボタン状態を更新
        /// 作成日: 2025-08-15
        /// </summary>
        private void UpdateDeleteButton()
        {
            deleteButton.Enabled =
                _canvasManager.SelectedComponent != null ||
                _canvasManager.SelectedConnection != null;
        }
    }
}