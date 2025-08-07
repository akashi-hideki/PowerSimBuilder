using System;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
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
            // 選択されたコンポーネントを追加
            var selectedDescriptor = paletteListBox.SelectedItem as ComponentDescriptor;
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
            var json = JsonConvert.SerializeObject(model, Formatting.Indented);
            File.WriteAllText("design.json", json);
        }
        
        private void loadButton_Click(object sender, EventArgs e)
        {
            var json = File.ReadAllText("design.json");
            var model = JsonConvert.DeserializeObject<CanvasModel>(json);
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