using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class MainForm : Form
    {
        // フィールド宣言
        private CanvasManager _canvasManager;
        private readonly PythonCodeGenerator _codeGenerator = new PythonCodeGenerator();
        private ComponentDefinitionManager _definitionManager;
        
        // UIコントロール (デザイナーで生成されるもの)
        // private ToolStripStatusLabel toolStripStatusLabel;
        // private ProgressBar loadingProgressBar;

        public MainForm()
        {
            InitializeComponent();
            
            // パレットに初期化中メッセージを表示
            componentPalette1.ShowInitializingMessage();
        }

        /// <summary>
        /// フォームロードイベントハンドラ
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            InitializeRuntimeComponents();
        }

        /// <summary>
        /// 実行時コンポーネントの初期化処理
        /// 
        /// 機能概要:
        ///   アプリケーションの主要コンポーネントを初期化し、相互接続を確立します。
        ///   パレット表示問題の解決を含む完全な初期化シーケンスを実装。
        /// 
        /// 処理フロー:
        ///   1. コンポーネント定義マネージャーの初期化
        ///   2. コンポーネントパレットの初期化
        ///   3. キャンバスマネージャーの初期化
        ///   4. イベントハンドラの登録
        ///   5. 接続レンダラーの設定
        ///   6. UI状態の初期更新
        /// 
        /// エラー処理:
        ///   - 主要コンポーネントの初期化失敗時はフォールバック動作
        ///   - 静的デフォルト定義を使用したパレット表示の保証
        ///   - ユーザーフレンドリーなエラーメッセージ表示
        /// 
        /// 設計上の注意点:
        ///   - 各コンポーネントの初期化順序は依存関係を厳守
        ///   - 例外処理は可能な限り機能を継続させる方向で設計
        ///   - リソース解放はfinallyブロックで保証
        /// 
        /// 更新履歴:
        ///   2025-08-15 - パレット表示問題の修正を統合した完全版
        ///   2025-08-16 - フォールバック処理の強化とエラーログ追加
        /// </summary>
        private void InitializeRuntimeComponents()
        {
            try
            {
                // ステップ1: コンポーネント定義マネージャーの初期化
                _definitionManager = new ComponentDefinitionManager();
                _definitionManager.LoadDefinitions();

                // ステップ2: コンポーネントパレットの初期化
                // 直接表示を呼び出し
                componentPalette1.ShowComponents(_definitionManager.Definitions);
                componentPalette1.ComponentSelected += ComponentPalette_ComponentSelected;

                // ステップ3: キャンバスマネージャーの初期化
                _canvasManager = new CanvasManager(designCanvas1, componentPalette1);
                _canvasManager.CanvasModified += CanvasManager_CanvasModified;
                _canvasManager.SelectionChanged += CanvasManager_SelectionChanged;
                
                // ステップ4: イベントハンドラの登録
                InitializeEventHandlers();
                SubscribeToEvents();
                
                // ステップ5: 接続レンダラーの設定
                designCanvas1.ConnectionRenderer = new BezierConnectionRenderer();
                designCanvas1.ConnectionSelected += DesignCanvas_ConnectionSelected;
                
                // ステップ6: UI状態の初期更新
                UpdateSaveStatus();
                UpdateUndoRedoButtons();
                UpdateDeleteButton();
                
                // ステータスバー更新
                toolStripStatusLabel.Text = "初期化完了 | システムの準備ができました";
            }
            catch (Exception ex)
            {
                // エラー時はデフォルト定義を表示
                componentPalette1.ShowComponents(DefaultComponentDefinitions.Definitions);
                // エラー処理: フォールバック動作
                HandleInitializationError(ex);
            }
            finally
            {
                // 進行状況インジケーターの非表示
                loadingProgressBar.Visible = false;
            }
        }

        /// <summary>
        /// 初期化エラー処理
        /// 
        /// 処理内容:
        ///   1. エラーログを出力
        ///   2. デフォルト定義でパレットを表示
        ///   3. 基本機能のみ有効化
        ///   4. ユーザーにエラー通知
        /// 
        /// 設計思想:
        ///   - 可能な限りアプリケーションを利用可能な状態に保つ
        ///   - ユーザーに問題の性質を明確に伝える
        /// </summary>
        private void HandleInitializationError(Exception ex)
        {
            // エラーログ出力
            Console.WriteLine($"初期化エラー [{DateTime.Now:HH:mm:ss}]:");
            Console.WriteLine($"メッセージ: {ex.Message}");
            Console.WriteLine($"スタックトレース: {ex.StackTrace}");
            
            try
            {
                // フォールバック1: 静的デフォルト定義でパレットを表示
                componentPalette1.ShowComponents(DefaultComponentDefinitions.Definitions);
                
                // フォールバック2: 基本キャンバス機能を有効化
                designCanvas1.AllowDrop = true;
                designCanvas1.DragEnter += DesignCanvas_DragEnter;
                designCanvas1.DragDrop += DesignCanvas_DragDrop;
                
                // UI状態の更新
                addButton.Enabled = false;
                deleteButton.Enabled = false;
                saveButton.Enabled = false;
                runButton.Enabled = false;
                
                // ステータスバー更新
                toolStripStatusLabel.Text = "エラー: 制限モードで動作中";
                toolStripStatusLabel.ForeColor = Color.Red;
            }
            catch (Exception fallbackEx)
            {
                // フォールバック処理失敗時の最終手段
                Console.WriteLine($"フォールバックエラー: {fallbackEx.Message}");
                toolStripStatusLabel.Text = "重大な初期化エラー";
            }
            
            // ユーザーへのエラー通知
            MessageBox.Show(
                "アプリケーションの初期化中に問題が発生しました:\n\n" +
                $"エラー内容: {ex.Message}\n\n" +
                "コンポーネントパレットはデフォルト設定で表示されていますが、\n" +
                "一部機能が制限された状態で動作しています。",
                "初期化エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }

        /// <summary>
        /// UIイベントハンドラの初期登録
        /// 
        /// 処理内容:
        ///   ツールバーボタン、プロパティグリッド、コンテキストメニューの
        ///   イベントハンドラを登録
        /// 
        /// 設計上の注意点:
        ///   - イベントハンドラはカテゴリ別にグループ化
        ///   - 可読性を考慮したメソッド配置
        /// </summary>
        private void InitializeEventHandlers()
        {
            // ツールバーボタンイベント
            addComponentToolStripButton.Click += AddComponentToolStripButton_Click;
            addButton.Click += addButton_Click;
            deleteButton.Click += deleteButton_Click;
            saveButton.Click += saveButton_Click;
            loadButton.Click += loadButton_Click;
            undoButton.Click += undoButton_Click;
            redoButton.Click += redoButton_Click;
            runButton.Click += runButton_Click;
            arrangeButton.Click += arrangeButton_Click;
            
            // プロパティグリッドイベント
            propertyGrid1.PropertyValueChanged += propertyGrid1_PropertyValueChanged;
        }

        /// <summary>
        /// コンポーネント間イベントの購読設定
        /// 
        /// 処理内容:
        ///   キャンバスとパレットのイベントをキャンバスマネージャーに接続
        /// 
        /// 設計思想:
        ///   - イベント連鎖を明確化
        ///   - 疎結合を維持するためのイベントブリッジ
        /// </summary>
        private void SubscribeToEvents()
        {
            // キャンバスイベント → キャンバスマネージャー
            designCanvas1.ComponentAdded += (s, e) =>
            {
                if (e.Component != null && _canvasManager != null)
                {
                    // ComponentControlからComponentDescriptorに変換
                    var descriptor = new ComponentDescriptor
                    {
                        Name = e.Component.Descriptor.Name,
                        Type = e.Component.Descriptor.Type,
                        Category = e.Component.Descriptor.Category,
                        Parameters = new List<ParameterDescriptor>(e.Component.Descriptor.Parameters),
                        InputPorts = e.Component.Descriptor.InputPorts?.ToArray(),
                        OutputPorts = e.Component.Descriptor.OutputPorts?.ToArray()
                    };

                    _canvasManager.AddComponent(descriptor, e.Component.Location);
                }
            };

            designCanvas1.ComponentRemoved += (s, e) => _canvasManager?.RemoveComponent(e.Component);
            designCanvas1.ComponentSelected += (s, e) => _canvasManager?.SelectComponent(e.Component);

            designCanvas1.ConnectionAdded += (s, e) =>
            {
                if (e.Connection != null && _canvasManager != null)
                {
                    _canvasManager.AddConnection(
                        e.Connection.StartComponent,
                        e.Connection.StartPort,
                        e.Connection.EndComponent,
                        e.Connection.EndPort);
                }
            };

            designCanvas1.ConnectionRemoved += (s, e) => _canvasManager?.RemoveConnection(e.Connection);
            designCanvas1.ConnectionSelected += (s, e) => _canvasManager?.SelectConnection(e.Connection);

            // パレットイベント → UI更新
            componentPalette1.ComponentSelected += ComponentPalette_ComponentSelected;
        }

        /// <summary>
        /// コンポーネント選択イベントハンドラ
        /// 
        /// 処理内容:
        ///   パレットでコンポーネントが選択されたとき、追加ボタンの状態を更新
        /// 
        /// 設計上の注意点:
        ///   - 選択状態に基づいたUIコントロールの有効化
        /// </summary>
        private void ComponentPalette_ComponentSelected(object sender, EventArgs e)
        {
            addButton.Enabled = (componentPalette1.SelectedDefinition != null);
        }

        /// <summary>
        /// コンポーネント追加ボタンクリックイベント
        /// 
        /// 処理内容:
        ///   1. 選択されたコンポーネント定義を取得
        ///   2. キャンバス中央にコンポーネントを追加
        ///   3. 自動配置を実行
        /// 
        /// 設計上の注意点:
        ///   - キャンバス座標系での中央位置計算
        ///   - コンポーネント追加後の自動配置
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
        /// 
        /// 処理内容:
        ///   1. コンポーネント定義からディスクリプタを生成
        ///   2. パラメータの型変換を実行
        ///   3. キャンバスマネージャー経由で追加
        ///   4. 自動配置を実行
        /// 
        /// 設計上の注意点:
        ///   - パラメータの型変換処理を含む
        ///   - キャンバスマネージャーを介した追加
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
        /// パラメータ変換処理
        /// 
        /// 処理内容:
        ///   文字列表現のパラメータ値を実際の型に変換
        /// 
        /// 設計上の注意点:
        ///   - 変換失敗時は文字列のまま保持
        ///   - 主要な型(double, int, bool)に対応
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
        /// 削除ボタンクリックイベント
        /// 
        /// 処理内容:
        ///   選択中のコンポーネントまたは接続を削除
        /// 
        /// 設計上の注意点:
        ///   - キャンバスマネージャーの選択状態を参照
        ///   - 両方の選択状態に対応
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
        /// 
        /// 処理内容:
        ///   1. キャンバスの状態をモデルとして保存
        ///   2. JSON形式でシリアライズ
        ///   3. ファイルに保存
        /// 
        /// 設計上の注意点:
        ///   - カスタムJSONシリアライザオプションを使用
        ///   - 変更状態をリセット
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
        /// 読み込みボタンクリックイベント
        /// 
        /// 処理内容:
        ///   1. JSONファイルからモデルを読み込み
        ///   2. キャンバスマネージャーにモデルをロード
        ///   3. 自動配置を実行
        /// 
        /// 設計上の注意点:
        ///   - エラー処理を含む
        ///   - UI状態の更新
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
        /// 元に戻すボタンクリックイベント
        /// 
        /// 処理内容:
        ///   キャンバスマネージャーのUndo操作を実行
        /// </summary>
        private void undoButton_Click(object sender, EventArgs e)
        {
            _canvasManager.Undo();
            designCanvas1.Invalidate();
        }

        /// <summary>
        /// やり直しボタンクリックイベント
        /// 
        /// 処理内容:
        ///   キャンバスマネージャーのRedo操作を実行
        /// </summary>
        private void redoButton_Click(object sender, EventArgs e)
        {
            _canvasManager.Redo();
            designCanvas1.Invalidate();
        }

        /// <summary>
        /// 配置ボタンクリックイベント
        /// 
        /// 処理内容:
        ///   自動レイアウトエンジンを実行
        /// </summary>
        private void arrangeButton_Click(object sender, EventArgs e)
        {
            designCanvas1.ArrangeComponents();
        }

        /// <summary>
        /// 実行ボタンクリックイベント
        /// 
        /// 処理内容:
        ///   1. シミュレーションコードを生成
        ///   2. ファイルに保存
        ///   3. コードビューアーを表示
        /// 
        /// 設計上の注意点:
        ///   - 出力ディレクトリの取得処理を含む
        /// </summary>
        private void runButton_Click(object sender, EventArgs e)
        {
            var components = _canvasManager.GetAllComponents();
            var connections = _canvasManager.GetAllConnections();
            
            // コード生成
            string pythonCode = _codeGenerator.GenerateSimulationCode(components, connections);
            
            // ファイル保存
            string outputDir = GetPythonOutputDirectory();
            if (_codeGenerator.SaveCodeToFile(outputDir))
            {
                // ビューアー表示
                _codeGenerator.ShowCodeViewer();
            }
        }
        
        /// <summary>
        /// Python出力ディレクトリの取得
        /// 
        /// 処理内容:
        ///   アプリケーションデータディレクトリに出力用ディレクトリを作成
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
        /// 
        /// 処理内容:
        ///   選択された接続のプロパティをプロパティグリッドに表示
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
        /// プロパティ変更イベントハンドラ
        /// 
        /// 処理内容:
        ///   故障監視コンポーネントの条件プロパティが変更された場合、
        ///   故障条件エディタを開く
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
        /// 
        /// 処理内容:
        ///   1. 全コンポーネントを取得
        ///   2. 故障条件エディタフォームを表示
        ///   3. 変更をコンポーネントパラメータに反映
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
        /// 
        /// 処理内容:
        ///   キャンバスの変更状態に基づいて保存ボタンの有効/無効を切り替え
        /// </summary>
        private void UpdateSaveStatus()
        {
            saveButton.Enabled = _canvasManager.IsModified;
        }

        /// <summary>
        /// 選択状態を更新
        /// 
        /// 処理内容:
        ///   現在選択されているコンポーネント/接続に基づいて
        ///   プロパティグリッドと削除ボタンの状態を更新
        /// </summary>
        private void UpdateSelection()
        {
            if (_canvasManager.SelectedComponent != null)
            {
                propertyGrid1.SelectedObject = _canvasManager.SelectedComponent?.Parameters;
            }
            else if (_canvasManager.SelectedConnection != null)
            {
                propertyGrid1.SelectedObject = new ConnectionProperties(_canvasManager.SelectedConnection);
            }
            else
            {
                propertyGrid1.SelectedObject = null;
            }
            
            UpdateDeleteButton();
        }

        /// <summary>
        /// 削除ボタンの状態更新
        /// 
        /// 処理内容:
        ///   選択されているアイテムがある場合のみ削除ボタンを有効化
        /// </summary>
        private void UpdateDeleteButton()
        {
            deleteButton.Enabled =
                _canvasManager.SelectedComponent != null ||
                _canvasManager.SelectedConnection != null;
        }

        /// <summary>
        /// Undo/Redoボタンの状態更新
        /// 
        /// 処理内容:
        ///   キャンバスマネージャーの状態に基づいてボタンの有効/無効を切り替え
        /// </summary>
        private void UpdateUndoRedoButtons()
        {
            undoButton.Enabled = _canvasManager.CanUndo;
            redoButton.Enabled = _canvasManager.CanRedo;
        }

        /// <summary>
        /// キャンバス変更イベントハンドラ
        /// 
        /// 処理内容:
        ///   キャンバスが変更されたときにUI状態を更新
        /// </summary>
        private void CanvasManager_CanvasModified(object sender, EventArgs e)
        {
            UpdateSaveStatus();
            UpdateUndoRedoButtons();
        }

        /// <summary>
        /// 選択状態変更イベントハンドラ
        /// 
        /// 処理内容:
        ///   選択が変更されたときにUI状態を更新
        /// </summary>
        private void CanvasManager_SelectionChanged(object sender, EventArgs e)
        {
            UpdateSelection();
        }

        /// <summary>
        /// カスタムコンポーネント追加ボタンクリックイベント
        /// 
        /// 処理内容:
        ///   1. コンポーネント追加フォームを表示
        ///   2. 新しいコンポーネントをパレットに追加
        /// 
        /// 設計上の注意点:
        ///   - 定義マネージャーを使用した追加処理
        /// </summary>
        private void AddComponentToolStripButton_Click(object sender, EventArgs e)
        {
            using (var form = new AddComponentForm(_definitionManager))
            {
                if (form.ShowDialog() == DialogResult.OK && form.NewComponentDefinition != null)
                {
                    // パレットに追加
                    componentPalette1.AddComponent(form.NewComponentDefinition);
                }
            }
        }

        /// <summary>
        /// ドラッグエンターイベント
        /// 
        /// 処理内容:
        ///   コンポーネントドラッグ操作を許可
        /// </summary>
        private void DesignCanvas_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ComponentDescriptor"))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        /// <summary>
        /// ドロップイベント
        /// 
        /// 処理内容:
        ///   1. ドロップ位置を取得
        ///   2. コンポーネントを追加
        ///   3. 自動配置を実行
        /// </summary>
        private void DesignCanvas_DragDrop(object sender, DragEventArgs e)
        {
            Point dropLocation = designCanvas1.PointToClient(new Point(e.X, e.Y));

            if (e.Data.GetData("ComponentDescriptor") is ComponentDescriptor descriptor)
            {
                _canvasManager.AddComponent(descriptor, dropLocation);
                designCanvas1.ArrangeComponents();
            }
        }
    }
}