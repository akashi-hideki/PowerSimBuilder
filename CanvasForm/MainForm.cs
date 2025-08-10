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
        
        /// <summary>
        /// メインフォームのコンストラクタ
        /// 注意事項: イベントハンドラの初期設定とCanvasManagerの初期化を行う
        /// 作成日: 2025-08-10
        /// </summary>
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
        
        /// <summary>
        /// コンポーネントパレット選択イベントハンドラ
        /// 注意事項: 追加ボタンの有効/無効を制御する
        /// 作成日: 2025-08-10
        /// </summary>
        private void ComponentPalette_ComponentSelected(object sender, EventArgs e)
        {
            // 追加ボタンを有効化
            addButton.Enabled = (componentPalette1.SelectedComponent != null);
        }
        
        /// <summary>
        /// コンポーネント追加ボタンクリックイベント
        /// 注意事項: AddComponentFormを表示して新しいコンポーネントを追加する
        /// 作成日: 2025-08-10
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
        /// キャンバス変更イベントハンドラ
        /// 注意事項: 保存状態とUndo/Redoボタンを更新する
        /// 作成日: 2025-08-10
        /// </summary>
        private void CanvasManager_CanvasModified(object sender, EventArgs e)
        {
            UpdateSaveStatus();
            UpdateUndoRedoButtons();
        }
        
        /// <summary>
        /// 選択状態変更イベントハンドラ
        /// 注意事項: プロパティグリッドと削除ボタンを更新する
        /// 作成日: 2025-08-10
        /// </summary>
        private void CanvasManager_SelectionChanged(object sender, EventArgs e)
        {
            propertyGrid1.SelectedObject = _canvasManager.SelectedComponent?.Parameters;
            UpdateDeleteButton();
        }
        
        /// <summary>
        /// コンポーネント追加ボタンクリックイベント
        /// 注意事項: 選択されたコンポーネントをキャンバス中央に追加する
        /// 作成日: 2025-08-10
        /// </summary>
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
        
        /// <summary>
        /// 削除ボタンクリックイベント
        /// 注意事項: 選択中のコンポーネントまたは接続を削除する
        /// 作成日: 2025-08-10
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
        /// 保存ボタンクリックイベント
        /// 注意事項: 現在のモデルをJSON形式で保存する
        /// 作成日: 2025-08-10
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

            var json = System.Text.Json.JsonSerializer.Serialize(model, options);
            File.WriteAllText("system.json", json);
            
            // 保存状態を更新
            _canvasManager.IsModified = false;
            UpdateSaveStatus();
        }

        /// <summary>
        /// 読み込みボタンクリックイベント
        /// 注意事項: JSONファイルからモデルを読み込む
        /// 作成日: 2025-08-10
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
        
        /// <summary>
        /// Undoボタンクリックイベント
        /// 注意事項: 直前の操作を元に戻す
        /// 作成日: 2025-08-10
        /// </summary>
        private void undoButton_Click(object sender, EventArgs e)
        {
            _canvasManager.Undo();
        }
        
        /// <summary>
        /// Redoボタンクリックイベント
        /// 注意事項: 取り消した操作を再実行する
        /// 作成日: 2025-08-10
        /// </summary>
        private void redoButton_Click(object sender, EventArgs e)
        {
            _canvasManager.Redo();
        }
        
        /// <summary>
        /// 実行ボタンクリックイベント
        /// 注意事項: Pythonコードを生成してシミュレーションを実行する
        /// 作成日: 2025-08-10
        /// </summary>
        private void runButton_Click(object sender, EventArgs e)
        {
            string pythonCode = _codeGenerator.GenerateCode(_canvasManager.GetAllComponents());
            
            var previewForm = new CodePreviewForm(pythonCode);
            previewForm.Show();
            
            RunPythonSimulation(pythonCode);
        }
        
        /// <summary>
        /// Pythonシミュレーションを実行する
        /// 注意事項: Pythonランタイムを使用してコードを実行する
        /// 作成日: 2025-08-10
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
        /// 保存状態を更新する
        /// 注意事項: 変更状態に基づいて保存ボタンの有効/無効を切り替える
        /// 作成日: 2025-08-10
        /// </summary>
        private void UpdateSaveStatus()
        {
            saveButton.Enabled = _canvasManager.IsModified;
        }
        
        /// <summary>
        /// Undo/Redoボタンの状態を更新する
        /// 注意事項: 操作履歴の状態に基づいてボタンの有効/無効を切り替える
        /// 作成日: 2025-08-10
        /// </summary>
        private void UpdateUndoRedoButtons()
        {
            undoButton.Enabled = _canvasManager.CanUndo;
            redoButton.Enabled = _canvasManager.CanRedo;
        }
        
        /// <summary>
        /// 削除ボタンの状態を更新する
        /// 注意事項: 選択状態に基づいて削除ボタンの有効/無効を切り替える
        /// 作成日: 2025-08-10
        /// </summary>
        private void UpdateDeleteButton()
        {
            deleteButton.Enabled = 
                _canvasManager.SelectedComponent != null || 
                _canvasManager.SelectedConnection != null;
        }
        
        /// <summary>
        /// Pythonコードプレビュー用フォーム
        /// 注意事項: 生成されたPythonコードを表示する
        /// 作成日: 2025-08-10
        /// </summary>
        public class CodePreviewForm : Form
        {
            /// <summary>
            /// コードプレビューフォームのコンストラクタ
            /// 注意事項: テキストボックスにPythonコードを表示する
            /// 作成日: 2025-08-10
            /// </summary>
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