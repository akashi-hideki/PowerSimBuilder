using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
            List<ComponentDescriptor> _components;
            InitializeComponent();
            InitializeEventHandlers();
            // 使用例
            //_paletteStorage = new ComponentPaletteStorage();
            _components = ComponentPaletteStorage.LoadOrCreateDefault();
 

            // componentPalette1.AvailableComponents = ComponentDescriptorDictionary.Descriptors.Values.ToList();
            componentPalette1.AvailableComponents = _components;
            _canvasManager = new CanvasManager(designCanvas1, componentPalette1);
            SubscribeToEvents();

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
            saveButton.Click += saveButton_Click;
            loadButton.Click += loadButton_Click;
            undoButton.Click += undoButton_Click;
            redoButton.Click += redoButton_Click;
            runButton.Click += runButton_Click;
            arrangeButton.Click += arrangeButton_Click;
            editButton.Click += EditPaletteButton_Click; // 追加
            deleteButton.Click += DeletePaletteButton_Click; // 変更（既存の削除ボタンを再利用）
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
            LogcatTraceHelper.Instance.SimpleMessage("Pallet Event", "pallet Selected ");
        }

        /// <summary>
        /// コンポーネント追加ボタンクリック
        /// 注意点: カスタムコンポーネント追加フォームを表示
        /// </summary>
        private void AddComponentToolStripButton_Click(object sender, EventArgs e)
        {
            using (var form = new AddComponentForm())
            {
                if (form.ShowDialog() == DialogResult.OK && form.ResultComponent != null)
                {
                    componentPalette1.AddComponent(form.ResultComponent);
                    ComponentPaletteStorage.SaveDescriptors(componentPalette1.AvailableComponents);
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
        private void EditPaletteButton_Click(object sender, EventArgs e)
        {
            if (componentPalette1.SelectedComponent == null)
            {
                MessageBox.Show("編集するコンポーネントを選択してください", "情報",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // キャンバスで使用中のコンポーネントがあるかチェック
            bool isUsedInCanvas = _canvasManager.GetAllComponents()
                .Any(c => c.Descriptor == componentPalette1.SelectedComponent);

            if (isUsedInCanvas)
            {
                MessageBox.Show("このコンポーネントはキャンバス上で使用されているため編集できません", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var form = new AddComponentForm(componentPalette1.SelectedComponent))
            {
                if (form.ShowDialog() == DialogResult.OK && form.ResultComponent != null)
                {
                    // パレットを更新
                    componentPalette1.UpdateComponent(componentPalette1.SelectedComponent, form.ResultComponent);

                    // 変更を保存
                    ComponentPaletteStorage.SaveDescriptors(componentPalette1.AvailableComponents);

                    MessageBox.Show("コンポーネントを更新しました", "成功",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        // パレットアイテム削除
        private void DeletePaletteButton_Click(object sender, EventArgs e)
        {
            if (componentPalette1.SelectedComponent == null)
            {
                MessageBox.Show("削除するコンポーネントを選択してください", "情報",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // キャンバスで使用中のコンポーネントがあるかチェック
            bool isUsedInCanvas = _canvasManager.GetAllComponents()
                .Any(c => c.Descriptor == componentPalette1.SelectedComponent);

            if (isUsedInCanvas)
            {
                MessageBox.Show("このコンポーネントはキャンバス上で使用されているため削除できません", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var result = MessageBox.Show(
                $"コンポーネント '{componentPalette1.SelectedComponent.Name}' を削除しますか？",
                "確認",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                componentPalette1.RemoveComponent(componentPalette1.SelectedComponent);

                // 変更を保存
                ComponentPaletteStorage.SaveDescriptors(componentPalette1.AvailableComponents);

                MessageBox.Show("コンポーネントを削除しました", "成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        /// Pythonシミュレーションコードの出力ディレクトリパスを取得します
        /// 処理内容:
        ///   - アプリケーションデータフォルダ下に専用ディレクトリを構成
        ///   - ディレクトリが存在しない場合は新規作成
        /// 注意点:
        ///   - 環境ごとの適切なアプリケーションデータフォルダを使用
        ///   - マルチユーザー環境でも安全に動作
        /// 出力例:
        ///   - Windows: C:\Users\[ユーザー名]\AppData\Roaming\PowerSimBuilder\PythonOutput\
        ///   - macOS: /Users/[ユーザー名]/.config/PowerSimBuilder/PythonOutput/
        ///   - Linux: /home/[ユーザー名]/.config/PowerSimBuilder/PythonOutput/
        /// </summary>
        /// <returns>Pythonコードファイルの保存先ディレクトリのフルパス</returns>
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
    }
}