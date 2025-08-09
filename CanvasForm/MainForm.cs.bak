using Python.Runtime;
using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class MainForm : Form
    {
        private readonly CanvasManager _canvasManager;
        private PythonCodeGenerator _codeGenerator = new PythonCodeGenerator();
        
        public MainForm()
        {
            InitializeComponent();
            // イベントハンドラの接続
            this.saveButton.Click += saveButton_Click;
            this.undoButton.Click += undoButton_Click;
            this.redoButton.Click += redoButton_Click;
            this.deleteButton.Click += deleteButton_Click;
            this.runButton.Click += runButton_Click;
            // CanvasManager の初期化
            _canvasManager = new CanvasManager(designCanvas1, componentPalette1);
            
            // イベントのサブスクライブ
            _canvasManager.CanvasModified += CanvasManager_CanvasModified;
            _canvasManager.SelectionChanged += CanvasManager_SelectionChanged;
        }
        
        private void CanvasManager_CanvasModified(object sender, EventArgs e)
        {
            // キャンバスが変更されたときの処理
            UpdateSaveStatus();
            UpdateUndoRedoButtons();
        }
        
        private void CanvasManager_SelectionChanged(object sender, EventArgs e)
        {
            // 選択状態が変わったときの処理
            propertyGrid1.SelectedObject = _canvasManager.SelectedComponent?.Parameters;
            UpdateDeleteButton();
        }
        
        private void addButton_Click(object sender, EventArgs e)
        {
            var selectedDescriptor = componentPalette1.SelectedComponent;
            if (selectedDescriptor != null)
            {
                _canvasManager.AddComponent(selectedDescriptor, new Point(100, 100));
            }
        }
        
        private void deleteButton_Click(object sender, EventArgs e)
        {
            // 選択された項目を削除
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
                Converters = { new PointJsonConverter() } // コンバーターを追加
            };

            var json = System.Text.Json.JsonSerializer.Serialize(model, options);
            File.WriteAllText("system.json", json);
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            var json = File.ReadAllText("system.json");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new PointJsonConverter() } // コンバーターを追加
            };

            var model = System.Text.Json.JsonSerializer.Deserialize<CanvasModel>(json, options);
            _canvasManager.LoadModel(model);
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
            // Pythonコードを生成
            string pythonCode = _codeGenerator.GenerateCode(_canvasManager.GetAllComponents());
            
            // コードを表示
            var previewForm = new CodePreviewForm(pythonCode);
            previewForm.Show();
            
            // Pythonで実行
            RunPythonSimulation(pythonCode);
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
            // 保存状態更新ロジック
            saveButton.Enabled = _canvasManager.IsModified;
        }
        
        private void UpdateUndoRedoButtons()
        {
            // Undo/Redoボタン状態更新
            undoButton.Enabled = _canvasManager.CanUndo;
            redoButton.Enabled = _canvasManager.CanRedo;
        }
        
        private void UpdateDeleteButton()
        {
            // 削除ボタン状態更新
            deleteButton.Enabled = 
                _canvasManager.SelectedComponent != null || 
                _canvasManager.SelectedConnection != null;
        }
    }
    
    // コードプレビューフォーム
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