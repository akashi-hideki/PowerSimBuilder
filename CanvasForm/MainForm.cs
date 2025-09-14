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
        private CanvasManager _canvasManager;
        private List<ComponentDescriptor> _components;
        private DesignCanvas designCanvas1;
        private ComponentPalette componentPalette1;

        /// <summary>
        /// メインフォームのコンストラクタ
        /// 注意点: UIコンポーネントとイベントハンドラを初期化
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Load Event Override
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // デザインキャンバスとコンポーネントパレットの初期化
            designCanvas1 = new DesignCanvas();
            componentPalette1 = new ComponentPalette();

            // コントロールの置換前にサイズを設定
            designCanvas1.Size = canvasPanel.Size;
            componentPalette1.Size = palettePanel.Size;

            // ダブルバッファリングを有効化（ちらつき防止）
            // designCanvas1.DoubleBuffered = true;
            SetDoubleBuffered(componentPalette1);

            // コントロールの置換
            ReplaceControl(canvasPanel, designCanvas1);
            ReplaceControl(palettePanel, componentPalette1);

            InitializeRuntimeComponents();

            // 境界線の設定
            designCanvas1.BorderStyle = BorderStyle.FixedSingle;
            designCanvas1.BackColor = Color.White;

            // レイアウトを一時的に停止
            this.SuspendLayout();

            // コンポーネントの初期配置
            designCanvas1.ArrangeComponents();

            // レイアウトを再開
            this.ResumeLayout(true);
            this.PerformLayout();
        }

        // ダブルバッファリングを設定するヘルパーメソッド
        private void SetDoubleBuffered(Control control)
        {
            if (SystemInformation.TerminalServerSession) return;

            System.Reflection.PropertyInfo prop =
                typeof(Control).GetProperty(
                    "DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

            if (prop != null)
            {
                prop.SetValue(control, true, null);
            }
        }
        private void ReplaceControl(Panel placeholder, Control realControl)
        {
            // 基本プロパティのコピー
            realControl.Dock = placeholder.Dock;
            realControl.Location = placeholder.Location;
            realControl.Size = placeholder.Size;
            realControl.Anchor = placeholder.Anchor;
            realControl.Margin = placeholder.Margin;
            realControl.Visible = placeholder.Visible;

            // 背景色のコピー
            realControl.BackColor = placeholder.BackColor;

            // Panel の場合のみ BorderStyle をコピー
            if (realControl is Panel panel)
            {
                panel.BorderStyle = placeholder.BorderStyle;
            }
            // FlowLayoutPanel の場合は独自の方法で境界線を処理（後述）

            var parent = placeholder.Parent;
            int index = parent.Controls.IndexOf(placeholder);

            parent.Controls.Remove(placeholder);
            parent.Controls.Add(realControl);
            parent.Controls.SetChildIndex(realControl, index);

            placeholder.Dispose();
        }
        private void InitializeRuntimeComponents()
        {
            InitializeEventHandlers();

            _components = ComponentPaletteStorage.LoadOrCreateDefault();
            componentPalette1.AvailableComponents = _components;

            _canvasManager = new CanvasManager(designCanvas1, componentPalette1);
            SubscribeToEvents();

            // デザイナーファイルから移動した設定
            designCanvas1.ConnectionRenderer = new BezierConnectionRenderer();
            designCanvas1.ConnectionSelected += DesignCanvas_ConnectionSelected;
        }
        /// <summary>
        /// イベントハンドラを初期化
        /// 注意点: ツールバーボタンとプロパティグリッドのイベントを登録
        /// </summary>

        private void InitializeEventHandlers()
        {
            addButton.Click += addComponentButton_Click; // 追加: イベントハンドラ登録
            saveButton.Click += saveButton_Click;
            loadButton.Click += loadButton_Click;
            undoButton.Click += undoButton_Click;
            redoButton.Click += redoButton_Click;
            createSimButton.Click += createSimButton_Click;
            arrangeButton.Click += arrangeButton_Click;
            editButton.Click += editPaletteButton_Click;
            removePalletButton.Click += deletePaletteButton_Click;
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
            editButton.Enabled = (componentPalette1.SelectedComponent != null);
            LogcatTraceHelper.Instance.SimpleMessage("Pallet Event", "pallet Selected ");
        }

        /// <summary>
        /// コンポーネント追加ボタンクリック
        /// 注意点: カスタムコンポーネント追加フォームを表示
        /// </summary>
        private void addComponentButton_Click(object sender, EventArgs e)
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
        /// コンポーネント編集ボタンクリック
        /// 注意点: パレットで選択中のコンポーネントをキャンバスに追加
        /// </summary>

        private void editPaletteButton_Click(object sender, EventArgs e)
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
        private void deletePaletteButton_Click(object sender, EventArgs e)
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
        private void createSimButton_Click(object sender, EventArgs e)
        {
            string file_path = BuildPythonSimulationWithFileDialog(_canvasManager);
            if ((!String.IsNullOrEmpty(file_path)) && (File.Exists(file_path)))
            {
                // ファイルパスを指定してフォームを表示
                var editorForm = new PythonEditorForm(file_path, true);

                editorForm.Executor = new PythonExecutor();
                if (editorForm.ShowDialog() == DialogResult.OK)
                {
                    // 保存が選択された場合、編集されたコードを取得
                    string editedCode = editorForm.GetPythonCode();
                    // 必要に応じて editedCode を使用
                }
                else
                {
                    // キャンセルされた場合の処理
                }
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
            removePalletButton.Enabled =
                _canvasManager.SelectedComponent != null ||
                _canvasManager.SelectedConnection != null;
        }
        /// <summary>
        /// CanvasManagerからコンポーネントと接続情報を取得し、SaveFileDialogを使用して
        /// Pythonシミュレーションファイルを生成します
        /// </summary>
        /// <param name="manager">コンポーネントと接続情報を含むCanvasManagerインスタンス</param>
        /// <returns>生成されたPythonファイルのパス。ユーザーがキャンセルした場合やエラーが発生した場合は空文字列を返します</returns>
        public string BuildPythonSimulationWithFileDialog(CanvasManager manager)
        {
            List<ComponentDescriptor> components = manager.UsedComponentDescriptors;
            List<Connection> connections = manager.Connections;
            string pyfile = string.Empty;
            try
            {
                // SaveFileDialogのインスタンスを作成（Windows FormsではOpenFileDialogではなくSaveFileDialogを使用）
                SaveFileDialog saveFileDialog = new SaveFileDialog();

                // ダイアログの設定
                saveFileDialog.Title = "Pythonシミュレーションファイルの保存先を選択";
                saveFileDialog.Filter = "Pythonファイル|*.py|すべてのファイル|*.*";
                saveFileDialog.DefaultExt = ".py";
                saveFileDialog.AddExtension = true;

                // 初期ディレクトリを設定（オプション）
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                // ダイアログを表示し、ユーザーがOKをクリックしたか確認
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // PythonSimulationMainBuilderのインスタンスを作成
                    PythonSimulationMainBuilder builder = new PythonSimulationMainBuilder();

                    // ファイルパスを設定
                    builder.FilePath = saveFileDialog.FileName;
                    pyfile = builder.FilePath;

                    // コンポーネントと接続を引数から設定
                    builder.Components = components;
                    builder.Connections = connections;

                    // エラーハンドラーを設定（オプション）
                    builder.ErrorHandler = (category, message) =>
                    {
                        MessageBox.Show($"エラー ({category}): {message}", "エラー",
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    };

                    // ファイルをビルド
                    builder.BuildPythonSimulationFile();

                    MessageBox.Show($"ファイルが正常に生成されました:\n{saveFileDialog.FileName}",
                                  "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイル生成中にエラーが発生しました:\n{ex.Message}",
                              "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return pyfile;
        }
    }
}