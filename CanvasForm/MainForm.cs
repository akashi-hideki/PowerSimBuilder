using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Text.Json;
using Python.Runtime;

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
            this.addComponentToolStripButton.Click += AddComponentToolStripButton_Click;
            this.addButton.Click += addButton_Click;
            this.deleteButton.Click += deleteButton_Click;
            this.saveButton.Click += saveButton_Click;
            this.loadButton.Click += loadButton_Click;
            this.undoButton.Click += undoButton_Click;
            this.redoButton.Click += redoButton_Click;
            this.runButton.Click += runButton_Click;
            
            // CanvasManager の初期化
            _canvasManager = new CanvasManager(designCanvas1, componentPalette1);
            
            // イベントのサブスクライブ
            _canvasManager.CanvasModified += CanvasManager_CanvasModified;
            _canvasManager.SelectionChanged += CanvasManager_SelectionChanged;
            
            // コンポーネント選択イベントの購読
            componentPalette1.ComponentSelected += ComponentPalette_ComponentSelected;
            
            // 初期状態の更新
            UpdateSaveStatus();
            UpdateUndoRedoButtons();
            UpdateDeleteButton();
        }
        
        private void ComponentPalette_ComponentSelected(object sender, EventArgs e)
        {
            // 追加ボタンを有効化
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
                Point center = new Point(
                    designCanvas1.Width / 2,
                    designCanvas1.Height / 2
                );
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

            var json = System.Text.Json.JsonSerializer.Serialize(model, options);
            File.WriteAllText("system.json", json);
            
            // 保存状態を更新
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

                var model = System.Text.Json.JsonSerializer.Deserialize<CanvasModel>(json, options);
                _canvasManager.LoadModel(model);
                
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
            string pythonCode = _codeGenerator.GenerateCode(_canvasManager.GetAllComponents());
            
            var previewForm = new CodePreviewForm(pythonCode);
            previewForm.Show();
            
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
                MessageBox.Show("シミュレーションが正常に完了しました", "成功", 
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"シミュレーションエラー: {ex.Message}", "エラー", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
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