using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// デザインキャンバスコントロール
    /// 作成日: 2025-08-12
    /// 更新日: 2025-09-13
    /// 注意点: コンポーネントと接続の描画・管理を担当
    /// 新機能: 接続線ヒストリーに基づくキーボード選択と削除機能を追加
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    [ToolboxItemFilter("PowerSimBuilder", ToolboxItemFilterType.Custom)]
    public partial class DesignCanvas : Panel
    {
        // イベント定義
        public event EventHandler SelectionChanged;
        public event EventHandler<ComponentEventArgs> ComponentAdded;
        public event EventHandler<ComponentEventArgs> ComponentRemoved;
        public event EventHandler<ComponentEventArgs> ComponentSelected;
        public event EventHandler<ConnectionEventArgs> ConnectionAdded;
        public event EventHandler<ConnectionEventArgs> ConnectionRemoved;
        [Browsable(false)]
        public event EventHandler<ConnectionEventArgs> ConnectionSelected;

        // 内部状態管理
        private Connection _currentConnection;
        private Point _dragStartPosition;
        private ComponentControl _draggedComponent;
        private bool _isDragging;

        // 接続線ヒストリー管理
        private readonly List<Connection> _connectionHistory = new List<Connection>();
        private int _currentHistoryIndex = -1;

        // キー操作ハンドラーのDictionary
        private readonly Dictionary<Keys, Action> _keyHandlers;

        // ポート関連定数
        private const int PortHitTestRadius = 10;

        // ---- Diagnostic logging (temporary) ----
        private static readonly string _dbgLogPath =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "powersimbuilder_scroll_debug.log");
        [System.Diagnostics.Conditional("NEVER_ACTIVE")]
        private static void DBG(string msg)
        {
            try
            {
                System.IO.File.AppendAllText(_dbgLogPath,
                    $"{DateTime.Now:HH:mm:ss.fff} {msg}\r\n");
            }
            catch { }
        }
        private const int PortDisplayRadius = 5;

        /// <summary>
        /// キャンバス上のコンポーネントリスト
        /// 注意点: 追加順序がZオーダーを決定
        /// </summary>
        public List<ComponentControl> Components { get; } = new List<ComponentControl>();

        /// <summary>
        /// キャンバス上の接続リスト
        /// 注意点: ConnectionGraphと同期必須
        /// </summary>
        public List<Connection> Connections { get; } = new List<Connection>();

        /// <summary>
        /// 選択中のコンポーネント
        /// 注意点: 複数選択未対応
        /// </summary>
        public ComponentControl SelectedComponent { get; private set; }

        /// <summary>
        /// 接続グラフ管理
        /// 注意点: 接続関係の検索を効率化
        /// </summary>
        public ConnectionGraph ConnectionGraph { get; } = new ConnectionGraph();

        /// <summary>
        /// 自動レイアウトエンジン
        /// 注意点: ArrangeComponentsメソッドで使用
        /// </summary>
        public AutoLayoutEngine AutoLayoutEngine { get; } = new AutoLayoutEngine();

        /// <summary>
        /// 接続レンダラー
        /// 注意点: 描画スタイルを動的変更可能
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ConnectionRenderer ConnectionRenderer { get; set; } = new BezierConnectionRenderer();

        /// <summary>
        /// 選択中の接続
        /// 注意点: 単一選択のみ対応
        /// </summary>
        public Connection SelectedConnection { get; private set; }

        /// <summary>
        /// デザインキャンバスのコンストラクタ
        /// 注意点: ダブルバッファリングとドラッグ操作を有効化
        /// </summary>
        public DesignCanvas()
        {
            // キーハンドラーの初期化
            _keyHandlers = new Dictionary<Keys, Action>
            {
                { Keys.Up, NavigateConnectionHistoryUp },
                { Keys.Down, NavigateConnectionHistoryDown },
                { Keys.Delete, DeleteSelectedItem }
            };

            // デザイン時の基本設定
            if (IsDesignMode)
            {
                BackColor = SystemColors.Control;
                BorderStyle = BorderStyle.FixedSingle;
            }
            else
            {
                InitializeComponent();
                this.AutoScroll = true;
                this.AutoScrollMinSize = new System.Drawing.Size(2000, 2000);
                DoubleBuffered = true;
                AllowDrop = true;

                this.TabStop = true;
                this.SetStyle(
                    ControlStyles.Selectable |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint,
                    true);
                this.UpdateStyles();
                this.KeyDown += DesignCanvas_KeyDown;

                this.DragEnter += DesignCanvas_DragEnter;
                this.DragDrop += DesignCanvas_DragDrop;

                InitializeContextMenu();
            }
        }

        /// <summary>
        /// デザインモード判定プロパティ
        /// 注意点: ライセンスマネージャーとサイト情報から判定
        /// </summary>
        private bool IsDesignMode
        {
            get
            {
                return (LicenseManager.UsageMode == LicenseUsageMode.Designtime) ||
                       (Site != null && Site.DesignMode);
            }
        }

        #region イベントハンドラ
        /// <summary>
        /// マウスダウンイベント処理
        /// 機能: フォーカス設定、接続線選択、ポートクリック処理
        /// 注意点: 接続線選択が優先され、次にポートクリックを処理
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!this.Focused)
            {
                this.Focus();
            }

            HandleMouseDown(e.Location);
        }

        /// <summary>
        /// キーダウンイベント処理
        /// 機能: キー操作に応じた処理の実行
        /// 注意点: Deleteキーで選択中の接続線またはコンポーネントを削除
        /// </summary>
        private void DesignCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            LogcatTraceHelper.Instance.SimpleMessage("KeyDown",
                $"Key pressed: {e.KeyCode}, SelectedConnection: {SelectedConnection != null}, " +
                $"SelectedComponent: {SelectedComponent != null}");
            LogThreadState("KeyDown");

            HandleKeyDown(e);
        }

        /// <summary>
        /// ドラッグエンターイベント処理
        /// 機能: コンポーネントディスクリプタのドラッグを許可
        /// 注意点: ComponentDescriptorデータ形式のみ許可
        /// </summary>
        private void DesignCanvas_DragEnter(object sender, DragEventArgs e)
        {
            HandleDragEnter(e);
        }

        /// <summary>
        /// ドロップイベント処理
        /// 機能: ドロップされたコンポーネントを追加
        /// 注意点: ドロップ位置にコンポーネントを配置
        /// </summary>
        private void DesignCanvas_DragDrop(object sender, DragEventArgs e)
        {
            HandleDragDrop(e);
        }

        /// <summary>
        /// コンポーネント選択変更イベント処理
        /// 機能: 選択中のコンポーネントを更新し、イベントを発行
        /// 注意点: 選択状態変更時に呼び出される
        /// </summary>
        private void Component_SelectedChanged(object sender, EventArgs e)
        {
            HandleComponentSelectedChanged(sender);
        }

        /// <summary>
        /// ポートクリックイベント処理
        /// 機能: 接続の開始または完了
        /// 注意点: 接続開始時は_currentConnectionを設定、完了時は接続を追加
        /// </summary>
        private void ComponentControl_PortClicked(object sender, PortEventArgs e)
        {
            HandlePortClicked(sender, e);
        }

        /// <summary>
        /// コンポーネントマウスダウンイベント処理
        /// 機能: コンポーネントのドラッグ開始処理
        /// 注意点: 左ボタンのみドラッグ処理
        /// </summary>
        private void ComponentControl_MouseDown(object sender, MouseEventArgs e)
        {
            // Right-click: skip drag/port logic (selection already handled by ComponentControl)
            if (e.Button != MouseButtons.Left)
            {
                _currentConnection = null;   // Cancel on right-click
                Invalidate();                // Clear temporary line
                return;
            }
            HandleComponentMouseDown(sender, e);
        }

        /// <summary>
        /// コンポーネントマウス移動イベント処理
        /// 機能: コンポーネントのドラッグ処理
        /// 注意点: ドラッグ中のみ処理
        /// </summary>
        private void ComponentControl_MouseMove(object sender, MouseEventArgs e)
        {
            HandleComponentMouseMove(sender, e);
        }

        /// <summary>
        /// コンポーネントマウスアップイベント処理
        /// 機能: コンポーネントのドラッグ終了処理
        /// 注意点: ドラッグ状態をリセット
        /// </summary>
        private void ComponentControl_MouseUp(object sender, MouseEventArgs e)
        {
            HandleComponentMouseUp(sender, e);
        }
        #endregion

        #region 公開メソッド
        /// <summary>
        /// コンポーネントを追加
        /// 機能: コンポーネントコントロールをキャンバスに追加
        /// 注意点: イベントハンドラを登録し、コレクションに追加
        /// </summary>
        public void AddComponent(ComponentControl control)
        {
            AddComponentInternal(control);
        }

        /// <summary>
        /// コンポーネントを追加（位置指定）
        /// 機能: ディスクリプタからコンポーネントを生成して追加
        /// 注意点: 指定位置にコンポーネントを配置
        /// </summary>
        public void AddComponent(ComponentDescriptor descriptor, Point location)
        {
            var control = new ComponentControl(descriptor) { Location = location };
            // InstanceNameを設定するコードを追加
            control.InstanceName = GenerateUniqueInstanceName(descriptor.Name);
            AddComponentInternal(control);
        }
        /// <summary>
        /// 一意のインスタンス名を生成
        /// 機能: 基本名に連番を付加して重複しないインスタンス名を生成
        /// 注意点: 既存のコンポーネント名と重複しないことを確認
        /// 例: "Resistor" → "Resistor1", "Resistor2" など
        /// </summary>
        /// <param name="baseName">基本名（コンポーネントタイプ名）</param>
        /// <returns>重複しない一意のインスタンス名</returns>
        private string GenerateUniqueInstanceName(string baseName)
        {
            int counter = 1;
            string instanceName = baseName;

            while (Components.Any(c => c.InstanceName == instanceName))
            {
                instanceName = $"{baseName}{counter++}";
            }

            return instanceName;
        }
 
        /// <summary>
        /// コンポーネントを削除
        /// 機能: コンポーネントとその関連接続を削除
        /// 注意点: 接続線と接続グラフからも削除する
        /// </summary>
        public void RemoveComponent(ComponentControl component)
        {
            RemoveComponentInternal(component);
        }

        /// <summary>
        /// 接続を削除
        /// 機能: 接続線を削除
        /// 注意点: 接続グラフとヒストリーからも削除する
        /// </summary>
        public void RemoveConnection(Connection connection)
        {
            RemoveConnectionInternal(connection);
        }

        /// <summary>
        /// 接続を追加
        /// 機能: 接続線を追加
        /// 注意点: 重複接続は追加しない
        /// </summary>
        public void AddConnection(Connection connection)
        {
            AddConnectionInternal(connection);
        }

        /// <summary>
        /// コンポーネント配置
        /// 機能: 自動レイアウトエンジンでコンポーネントを配置
        /// 注意点: 接続線の表示も更新する
        /// </summary>
        public void ArrangeComponents()
        {
            ArrangeComponentsInternal();
        }

        /// <summary>
        /// キャンバスをクリア
        /// 機能: すべてのコンポーネントと接続を削除
        /// 注意点: イベントハンドラもクリーンアップ
        /// </summary>
        public void Clear()
        {
            ClearCanvas();
        }

        /// <summary>
        /// 接続グラフの整合性を検証
        /// 機能: 接続グラフの状態を検証
        /// 注意点: コンポーネントリストを渡して検証
        /// </summary>
        public void ValidateConnectionGraph()
        {
            ConnectionGraph.ValidateIntegrity(Components);
        }

        /// <summary>
        /// 接続グラフの状態をダンプ
        /// 機能: 接続グラフの状態をログ出力
        /// 注意点: デバッグ用途
        /// </summary>
        public void DumpConnectionGraph()
        {
            DumpConnectionGraphInternal();
        }

        /// <summary>
        /// 接続グラフの詳細状態をダンプ
        /// 機能: 接続グラフの詳細状態をログ出力
        /// 注意点: ハッシュ値付きで詳細情報を出力
        /// </summary>
        public void DumpConnectionGraphDetails()
        {
            DumpConnectionGraphDetailsInternal();
        }

        /// <summary>
        /// 現在の状態をダンプ
        /// 機能: キャンバスの現在の状態をログ出力
        /// 注意点: デバッグ用途、すべての状態情報を出力
        /// </summary>
        public void DumpState()
        {
            DumpStateInternal();
        }
        #endregion

        #region プライベートメソッド - 主要操作
        /// <summary>
        /// マウスダウン処理
        /// 機能: 接続線選択またはポートクリックの処理
        /// 注意点: 接続線選択が優先され、次にポートクリックを処理
        /// </summary>
        private void HandleMouseDown(Point location)
        {
            // Cancel any pending connection (leftover from an incomplete start)
            _currentConnection = null;
            if (TrySelectConnection(location)) return;
            // [Fix 3-C-7c] Duplicate fire prevention

            // if (TryHandlePortClick(location)) return;

            ConnectionSelected?.Invoke(this, new ConnectionEventArgs(null));
            DeselectAllConnections();

            // FIX 4: clear component selection on empty-canvas click
            if (SelectedComponent != null)
            {
                SelectedComponent.IsSelected = false;
                SelectedComponent = null;
            }
            Invalidate();
        }

        /// <summary>
        /// キーダウン処理
        /// 機能: キーに応じた操作を実行
        /// 注意点: Deleteキーは選択項目の削除
        /// </summary>
        private void HandleKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                HandleDeleteKey();
                e.Handled = true;
            }
            else if (_keyHandlers.TryGetValue(e.KeyCode, out Action handler))
            {
                handler();
                e.Handled = true;
            }
        }

        /// <summary>
        /// ドラッグエンター処理
        /// 機能: コンポーネントディスクリプタのドラッグを許可
        /// 注意点: ComponentDescriptorデータ形式のみ許可
        /// </summary>
        private void HandleDragEnter(DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent("ComponentDescriptor") ? 
                DragDropEffects.Copy : DragDropEffects.None;
        }

        /// <summary>
        /// ドロップ処理
        /// 機能: ドロップされた位置にコンポーネントを追加
        /// 注意点: 追加後は自動レイアウトを実行
        /// </summary>
        private void HandleDragDrop(DragEventArgs e)
        {
            Point dropLocation = this.PointToClient(new Point(e.X, e.Y));
            if (e.Data.GetData("ComponentDescriptor") is ComponentDescriptor descriptor)
            {
                AddComponent(descriptor, dropLocation);
                ArrangeComponents();
            }
        }

        /// <summary>
        /// コンポーネント選択変更処理
        /// 機能: 選択中のコンポーネントを更新
        /// 注意点: 選択変更イベントを発行
        /// </summary>
        private void HandleComponentSelectedChanged(object sender)
        {
            var component = (ComponentControl)sender;
            // FIX 3: unselect previous component before assigning new one
            if (SelectedComponent != null && SelectedComponent != component)
            {
                SelectedComponent.IsSelected = false;
            }
            SelectedComponent = component;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            ComponentSelected?.Invoke(this, new ComponentEventArgs(component));
        }

        /// <summary>
        /// ポートクリック処理
        /// 機能: 接続の開始または完了
        /// 注意点: 接続開始時は_currentConnectionを設定、完了時は接続を追加
        /// </summary>
        private void HandlePortClicked(object sender, PortEventArgs e)
        {
            var component = (ComponentControl)sender;

            if (_currentConnection == null)
            {
                StartNewConnection(component, e.PortName);
            }
            else
            {
                CompleteConnection(component, e.PortName);
            }

            Invalidate();
        }

        /// <summary>
        /// コンポーネントマウスダウン処理
        /// 機能: ドラッグ開始処理
        /// 注意点: 左ボタンのみドラッグ処理
        /// </summary>
        private void HandleComponentMouseDown(object sender, MouseEventArgs e)
        {
            var component = (ComponentControl)sender;
            component.BringToFront();

            if (e.Button == MouseButtons.Left)
            {
                StartDragging(component, e.Location);
            }
        }

        /// <summary>
        /// コンポーネントマウス移動処理
        /// 機能: ドラッグ中のコンポーネント移動
        /// 注意点: ドラッグ中のみ処理、接続線も更新
        /// </summary>
        private void HandleComponentMouseMove(object sender, MouseEventArgs e)
        {
            var component = (ComponentControl)sender;

            if (_isDragging && _draggedComponent == component)
            {
                DragComponent(component, e.Location);
            }
        }

        /// <summary>
        /// コンポーネントマウスアップ処理
        /// 機能: ドラッグ終了処理
        /// 注意点: ドラッグ状態をリセット
        /// </summary>
        private void HandleComponentMouseUp(object sender, MouseEventArgs e)
        {
            StopDragging();
        }
        #endregion

        #region プライベートメソッド - 接続操作
        /// <summary>
        /// 新規接続開始
        /// 機能: 接続開始処理
        /// 注意点: _currentConnectionを新規作成
        /// </summary>
        private void StartNewConnection(ComponentControl component, string portName)
        {
            _currentConnection = new Connection
            {
                StartComponent = component,
                StartPort = portName,
                Visual = new ConnectionVisual()
            };
        }

        /// <summary>
        /// 接続完了
        /// 機能: 接続完了処理
        /// 注意点: 接続が重複しない場合のみ追加
        /// </summary>
        private void CompleteConnection(ComponentControl component, string portName)
        {
            _currentConnection.EndComponent = component;
            _currentConnection.EndPort = portName;

            if (!ConnectionGraph.ConnectionExists(_currentConnection))
            {
                AddConnection(_currentConnection);
            }

            _currentConnection = null;
        }

        /// <summary>
        /// 接続線ヒストリーを上に移動
        /// 機能: 前の接続線を選択
        /// 注意点: ヒストリーインデックスをデクリメント
        /// </summary>
        private void NavigateConnectionHistoryUp()
        {
            NavigateConnectionHistory(-1);
        }

        /// <summary>
        /// 接続線ヒストリーを下に移動
        /// 機能: 次の接続線を選択
        /// 注意点: ヒストリーインデックスをインクリメント
        /// </summary>
        private void NavigateConnectionHistoryDown()
        {
            NavigateConnectionHistory(1);
        }

        /// <summary>
        /// 接続線ヒストリー内を移動
        /// 機能: 指定方向にヒストリー内を移動して接続線を選択
        /// 注意点: インデックス範囲をチェック
        /// </summary>
        private void NavigateConnectionHistory(int direction)
        {
            if (_connectionHistory.Count == 0) return;

            _currentHistoryIndex += direction;
            _currentHistoryIndex = Math.Max(0, Math.Min(_currentHistoryIndex, _connectionHistory.Count - 1));

            if (_currentHistoryIndex >= 0 && _currentHistoryIndex < _connectionHistory.Count)
            {
                SelectConnection(_connectionHistory[_currentHistoryIndex]);
            }
        }

        /// <summary>
        /// 接続線をヒストリーに追加
        /// 機能: 接続線をヒストリーの先頭に追加
        /// 注意点: 重複する接続線は削除される
        /// </summary>
        private void AddToConnectionHistory(Connection connection)
        {
            _connectionHistory.RemoveAll(c => c == connection);
            _connectionHistory.Insert(0, connection);

            if (_connectionHistory.Count > 50)
            {
                _connectionHistory.RemoveAt(_connectionHistory.Count - 1);
            }

            _currentHistoryIndex = 0;
        }

        /// <summary>
        /// 接続線をヒストリーから削除
        /// 機能: 指定した接続線をヒストリーから削除
        /// 注意点: 現在の選択位置を調整する
        /// </summary>
        private void RemoveFromConnectionHistory(Connection connection)
        {
            try
            {
                if (connection == null)
                {
                    LogcatTraceHelper.Instance.SimpleMessage("RemoveFromHistory", "Attempted to remove null connection from history");
                    return;
                }

                LogConnectionState(connection, "Before RemoveFromHistory");
                LogcatTraceHelper.Instance.SimpleMessage("ConnectionHistory",
                    $"History count before: {_connectionHistory.Count}, Current index: {_currentHistoryIndex}");

                int removeIndex = _connectionHistory.IndexOf(connection);

                if (removeIndex >= 0)
                {
                    _connectionHistory.RemoveAt(removeIndex);

                    if (_currentHistoryIndex > removeIndex)
                    {
                        _currentHistoryIndex--;
                    }
                    else if (_currentHistoryIndex == removeIndex)
                    {
                        _currentHistoryIndex = -1;
                        if (SelectedConnection == connection)
                        {
                            SelectedConnection = null;
                        }
                    }

                    if (_currentHistoryIndex >= _connectionHistory.Count)
                    {
                        _currentHistoryIndex = _connectionHistory.Count - 1;
                    }
                }

                LogcatTraceHelper.Instance.SimpleMessage("ConnectionHistory",
                    $"History count after: {_connectionHistory.Count}, Current index: {_currentHistoryIndex}");
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.LogError("ConnectionHistory",
                    I18n.T("DesignCanvas.MsgHistoryDeleteError", ex.Message));
            }
        }
        #endregion

        #region プライベートメソッド - 選択操作
        /// <summary>
        /// 接続線選択試行
        /// 機能: 指定位置にある接続線を選択
        /// 戻り値: 接続線を選択したかどうか
        /// 注意点: 近接判定は10ピクセル以内
        /// </summary>
        private bool TrySelectConnection(Point location)
        {
            foreach (var connection in Connections)
            {
                if (IsPointNearConnection(location, connection))
                {
                    SelectConnection(connection);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// ポートクリック処理試行
        /// 機能: 指定位置にあるポートのクリック処理
        /// 戻り値: ポートをクリックしたかどうか
        /// 注意点: 入力ポートと出力ポートの両方をチェック
        /// </summary>
        private bool TryHandlePortClick(Point location)
        {
            foreach (Control control in Controls)
            {
                if (control is ComponentControl component)
                {
                    var clientPos = component.PointToClient(PointToScreen(location));

                    if (CheckPortClick(component, clientPos, true)) return true;
                    if (CheckPortClick(component, clientPos, false)) return true;
                }
            }
            DBG($"CheckPortClick MISS viewportLoc=({location.X},{location.Y})");
            return false;
        }

        /// <summary>
        /// ポートクリックチェック
        /// 機能: 指定位置がポートであるかチェックし、クリック処理を実行
        /// 戻り値: ポートをクリックしたかどうか
        /// 注意点: ポート位置は比率計算で決定
        /// </summary>
        private bool CheckPortClick(ComponentControl component, Point clientPos, bool isInput)
        {
            var ports = isInput ? component.Descriptor.InputPorts : component.Descriptor.OutputPorts;
            if (ports == null) return false;

            for (int i = 0; i < ports.Length; i++)
            {
                // Port coordinate fix: match OnPaint (circular hit test with PortRadius)
                float ratio = (i + 1f) / (ports.Length + 1);
                int yPos = (int)(component.Height * ratio);
                int xCenter = isInput
                    ? ComponentControl.PortRadius
                    : component.Width - ComponentControl.PortRadius;

                int dx = clientPos.X - xCenter;
                int dy = clientPos.Y - yPos;
                int d2 = dx * dx + dy * dy;
                int r2 = ComponentControl.HitTestRadius * ComponentControl.HitTestRadius;
                if (d2 <= r2)
                {
                    DBG($"CheckPortClick HIT comp={component.InstanceName} {(isInput ? "in" : "out")}[{i}] port={ports[i]} center=({xCenter},{yPos}) mouse=({clientPos.X},{clientPos.Y}) d2={d2}");
                    ComponentControl_PortClicked(component, new PortEventArgs(ports[i]));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 接続線を選択
        /// 機能: 指定した接続線を選択状態にする
        /// 注意点: 他の接続線の選択は解除される
        /// </summary>
        private void SelectConnection(Connection connection)
        {
            DeselectAllConnections();
            connection.Visual.IsSelected = true;
            SelectedConnection = connection;
            _currentHistoryIndex = _connectionHistory.IndexOf(connection);
            ConnectionSelected?.Invoke(this, new ConnectionEventArgs(connection));
            Invalidate();
        }

        /// <summary>
        /// 全接続線の選択解除
        /// 機能: すべての接続線の選択状態を解除
        /// 注意点: ヒストリーインデックスもリセット
        /// </summary>
        private void DeselectAllConnections()
        {
            foreach (var connection in Connections)
            {
                connection.Visual.IsSelected = false;
            }
            SelectedConnection = null;
            _currentHistoryIndex = -1;
            Invalidate();
        }
        #endregion

        #region プライベートメソッド - ドラッグ操作
        /// <summary>
        /// ドラッグ開始
        /// 機能: コンポーネントのドラッグを開始
        /// 注意点: ドラッグ状態と開始位置を記録
        /// </summary>
        private void StartDragging(ComponentControl component, Point location)
        {
            _isDragging = true;
            _draggedComponent = component;
            _dragStartPosition = location;
        }

        /// <summary>
        /// コンポーネントドラッグ
        /// 機能: コンポーネントをドラッグ位置に移動
        /// 注意点: 接続線も同時に更新
        /// </summary>
        private void DragComponent(ComponentControl component, Point location)
        {
            int deltaX = location.X - _dragStartPosition.X;
            int deltaY = location.Y - _dragStartPosition.Y;

            component.Left += deltaX;
            component.Top += deltaY;

            UpdateComponentConnections(component);
            Invalidate();
        }

        /// <summary>
        /// ドラッグ停止
        /// 機能: ドラッグ状態をリセット
        /// 注意点: すべてのドラッグ関連変数をクリア
        /// </summary>
        private void StopDragging()
        {
            _isDragging = false;
            _draggedComponent = null;
        }
        #endregion

        #region プライベートメソッド - 削除操作
        /// <summary>
        /// 選択項目削除
        /// 機能: 選択中の接続線またはコンポーネントを削除
        /// 注意点: 接続線が優先される
        /// </summary>
        private void DeleteSelectedItem()
        {
            if (SelectedConnection != null)
            {
                DeleteSelectedConnection();
            }
            else if (SelectedComponent != null)
            {
                DeleteSelectedComponent();
            }
        }

        /// <summary>
        /// 選択中の接続線を削除
        /// 機能: 選択中の接続線を削除
        /// 注意点: ヒストリーからも削除
        /// </summary>
        private void DeleteSelectedConnection()
        {
            if (SelectedConnection != null)
            {
                RemoveConnection(SelectedConnection);
                RemoveFromConnectionHistory(SelectedConnection);
                SelectedConnection = null;
                Invalidate();
            }
        }

        /// <summary>
        /// 選択中のコンポーネントを削除
        /// 機能: 選擇中のコンポーネントを削除
        /// 注意点: 關聯接続も同時に削除
        /// </summary>
        private void DeleteSelectedComponent()
        {
            if (SelectedComponent != null)
            {
                RemoveComponent(SelectedComponent);
            }
        }

        /// <summary>
        /// Deleteキー処理
        /// 機能: Deleteキーが押された時の処理
        /// 注意点: 選択中の項目を削除
        /// </summary>
        private void HandleDeleteKey()
        {
            if (SelectedConnection != null)
            {
                LogcatTraceHelper.Instance.SimpleMessage("KeyDown", "Deleting selected connection");
                RemoveConnection(SelectedConnection);
            }
            else if (SelectedComponent != null)
            {
                LogcatTraceHelper.Instance.SimpleMessage("KeyDown", "Deleting selected component");
                RemoveComponent(SelectedComponent);
            }
        }
        #endregion

        #region プライベートメソッド - 内部実装
        /// <summary>
        /// コンポーネント追加内部処理
        /// 機能: コンポーネントをコレクションに追加し、イベントハンドラを登録
        /// 注意点: 既存のイベントハンドラは解除してから登録
        /// </summary>
        private void AddComponentInternal(ComponentControl control)
        {
            if (control == null || Controls.Contains(control)) return;

            try
            {
                UnregisterComponentEvents(control);
                RegisterComponentEvents(control);

                Controls.Add(control);
                Components.Add(control);
                ComponentAdded?.Invoke(this, new ComponentEventArgs(control));
                Invalidate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"コンポーネント追加エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// コンポーネント削除内部処理
        /// 機能: コンポーネントとその關聯接続を削除
        /// 注意點: コレクション同期を確保
        /// </summary>
        private void RemoveComponentInternal(ComponentControl component)
        {
            if (component == null)
            {
                LogcatTraceHelper.Instance.SimpleMessage("RemoveComponent", "Attempted to remove null component");
                return;
            }

            bool inControls = Controls.Contains(component);
            bool inComponents = Components.Contains(component);

            if (!inControls && !inComponents)
            {
                LogcatTraceHelper.Instance.SimpleMessage("RemoveComponent",
                    $"Component {component.InstanceName} not found in any collection");
                return;
            }

            try
            {
                LogComponentState(component, "Before RemoveComponent");
                LogCollectionState("Before RemoveComponent");
                LogThreadState("RemoveComponent");

                LogcatTraceHelper.Instance.SimpleMessage("connection ", "=== Before RemoveComponent ===");
                DumpState();

                RemoveComponentConnections(component);
                ConnectionGraph.RemoveAllConnections(component);
                UnregisterComponentEvents(component);

                if (SelectedComponent == component)
                {
                    SelectedComponent = null;
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }

                if (inControls) Controls.Remove(component);
                if (inComponents) Components.Remove(component);

                LogComponentState(component, "After RemoveComponent");
                LogCollectionState("After RemoveComponent");

                ComponentRemoved?.Invoke(this, new ComponentEventArgs(component));
                Invalidate();
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.LogError("RemoveComponent",
                    I18n.T("DesignCanvas.MsgComponentDeleteError", ex.Message));
            }
        }

        /// <summary>
        /// 接続削除内部処理
        /// 機能: 接続線をコレクションから削除
        /// 注意點: 接続グラフとヒストリーからも削除
        /// </summary>
        private void RemoveConnectionInternal(Connection connection)
        {
            if (connection == null || !Connections.Contains(connection)) return;

            try
            {
                LogConnectionState(connection, "Before RemoveConnection");
                LogCollectionState("Before RemoveConnection");

                Connections.Remove(connection);
                ConnectionGraph.RemoveConnection(connection);
                RemoveFromConnectionHistory(connection);

                if (SelectedConnection == connection)
        {
            SelectedConnection = null;
            ConnectionSelected?.Invoke(this, new ConnectionEventArgs(null));
        }

        LogConnectionState(connection, "After RemoveConnection");
        LogCollectionState("After RemoveConnection");

        ConnectionRemoved?.Invoke(this, new ConnectionEventArgs(connection));
        Invalidate();
    }
    catch (Exception ex)
    {
        LogcatTraceHelper.Instance.LogError("RemoveConnection",
            I18n.T("DesignCanvas.MsgConnectionDeleteError", ex.Message));
    }
}

/// <summary>
/// 接続追加内部処理
/// 機能: 接続線をコレクションに追加
/// 注意點: 重複チェック後に追加
/// </summary>
private void AddConnectionInternal(Connection connection)
{
    if (!Connections.Contains(connection))
    {
        Connections.Add(connection);
        ConnectionGraph.AddConnection(connection);
        UpdateConnectionVisual(connection);
        AddToConnectionHistory(connection);
        ConnectionAdded?.Invoke(this, new ConnectionEventArgs(connection));
        Invalidate();
    }
}

/// <summary>
/// コンポーネント配置内部処理
/// 機能: 自動レイアウトを実行
/// 注意點: レイアウト中は描画を一時停止
/// </summary>
private void ArrangeComponentsInternal()
{
    this.SuspendLayout();

    try
    {
        AutoLayoutEngine.ArrangeComponents(this);
        UpdateAllConnectionVisuals();
    }
    finally
    {
        this.ResumeLayout(true);
        this.PerformLayout();
    }

    Invalidate();
}

/// <summary>
/// キャンバスクリア
/// 機能: すべてのコンポーネントと接続を削除
/// 注意點: イベントハンドラもクリーンアップ
/// </summary>
private void ClearCanvas()
{
    try
    {
        foreach (ComponentControl component in Components.ToList())
        {
            UnregisterComponentEvents(component);
        }

        Controls.Clear();
        Components.Clear();
        Connections.Clear();
        ConnectionGraph.Clear();
        _connectionHistory.Clear();
        _currentHistoryIndex = -1;

        SelectedComponent = null;
        SelectedConnection = null;

        Invalidate();
    }
    catch (Exception ex)
    {
        LogcatTraceHelper.Instance.LogError("Delete Error", $"キャンバスクリアエラー: {ex.Message}");
    }
}
        #endregion

        #region プライベートメソッド - コンポーネントイベント登録
        /// <summary>
        /// コンポーネントイベント登録
        /// 機能: コンポーネントのイベントハンドラを登録
        /// 注意點: すべての必要なイベントを登録
        /// </summary>
        private void RegisterComponentEvents(ComponentControl component)
        {
            component.SelectedChanged += Component_SelectedChanged;
            component.PortClicked += ComponentControl_PortClicked;
            component.MouseDown += ComponentControl_MouseDown;
            component.MouseMove += ComponentControl_MouseMove;
            component.MouseUp += ComponentControl_MouseUp;
        }

        /// <summary>
        /// コンポーネントイベント解除
        /// 機能: コンポーネントのイベントハンドラを解除
        /// 注意點: メモリリーク防止のため重要
        /// </summary>
        private void UnregisterComponentEvents(ComponentControl component)
        {
            component.SelectedChanged -= Component_SelectedChanged;
            component.PortClicked -= ComponentControl_PortClicked;
            component.MouseDown -= ComponentControl_MouseDown;
            component.MouseMove -= ComponentControl_MouseMove;
            component.MouseUp -= ComponentControl_MouseUp;
        }
        #endregion

        #region プライベートメソッド - 接続線管理
        /// <summary>
        /// コンポーネント關連接続削除
        /// 機能: コンポーネントに関連するすべての接続線を削除
        /// 注意點: 接続線のリストを事前に取得してから削除
        /// </summary>
        private void RemoveComponentConnections(ComponentControl component)
        {
            var connectionsToRemove = Connections
                .Where(c => c.StartComponent == component || c.EndComponent == component)
                .ToList();

            LogcatTraceHelper.Instance.SimpleMessage("RemoveComponent",
                $"Found {connectionsToRemove.Count} connections to remove");

            foreach (var conn in connectionsToRemove)
            {
                LogConnectionState(conn, "Removing connection");
                RemoveConnection(conn);
            }
        }

        /// <summary>
        /// 接続線視覚情報更新
        /// 機能: 接続線のパスポイントを再計算
        /// 注意點: コンポーネントの移動後に呼び出し
        /// </summary>
        private void UpdateConnectionVisual(Connection connection)
        {
            if (connection.StartComponent == null || connection.EndComponent == null) return;

            Point start = GetPortPoint(connection.StartComponent, connection.StartPort, true);
            Point end = GetPortPoint(connection.EndComponent, connection.EndPort, false);

            if (start != Point.Empty && end != Point.Empty)
            {
                connection.Visual.PathPoints = new List<Point> { start, end };
            }
        }

        /// <summary>
        /// ポート位置取得
        /// 機能: ポート名からポートの中心位置を取得
        /// 戻り値: ポートの中心位置（見つからない場合はPoint.Empty）
        /// 注意點: ポートインデックスを検索して位置計算
        /// </summary>
        private Point GetPortPoint(ComponentControl component, string portName, bool isOutput)
        {
            var ports = isOutput ? component.Descriptor.OutputPorts : component.Descriptor.InputPorts;
            if (ports == null) return Point.Empty;

            int portIndex = Array.IndexOf(ports, portName);
            if (portIndex < 0) return Point.Empty;

            var portRect = isOutput ? 
                component.GetOutputPortBounds(portIndex) : 
                component.GetInputPortBounds(portIndex);

            // component.Left/Top は AutoScroll で変化する（= viewport 座標）。
            // portRect も既に viewport 座標なので、AutoScrollPosition を足すと
            // ダブルカウントになり、スクロール時に線がずれる（根本原因）。
            var pt = new Point(
                isOutput ? portRect.Right : portRect.Left,
                portRect.Top + portRect.Height / 2
            );
            DBG($"GetPortPoint {component.InstanceName}:{portName} out={isOutput} rect={portRect} -> ({pt.X},{pt.Y}) AutoScroll={AutoScrollPosition}");
            return pt;
        }

        /// <summary>
        /// 全接続線視覚情報更新
        /// 機能: すべての接続線の視覚情報を更新
        /// 注意點: コンポーネント配置変更後に呼び出し
        /// </summary>
        private void UpdateAllConnectionVisuals()
        {
            foreach (var connection in Connections)
            {
                UpdateConnectionVisual(connection);
            }
        }

        /// <summary>
        /// コンポーネント關連接続更新
        /// 機能: コンポーネントに関連する接続線の視覚情報を更新
        /// 注意點: ドラッグ操作中に呼び出し
        /// </summary>
        private void UpdateComponentConnections(ComponentControl component)
        {
            foreach (var connection in ConnectionGraph.GetConnections(component))
            {
                UpdateConnectionVisual(connection);
            }
        }
        #endregion

        #region プライベートメソッド - 描画関連
        /// <summary>
        /// 描画処理
        /// 機能: キャンバスの描画
        /// 注意點: デザイン時と実行時で処理を分岐
        /// </summary>
        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            DBG($"OnScroll type={se.Type} old={se.OldValue} new={se.NewValue} AutoScrollPosition={AutoScrollPosition}");
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            DBG($"OnMouseWheel delta={e.Delta} AutoScrollPosition={AutoScrollPosition}");
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            DBG($"OnResize ClientSize={ClientSize.Width}x{ClientSize.Height}");
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (IsDesignMode)
            {
                DrawDesignTime(e.Graphics);
                return;
            }

            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            DBG($"OnPaint ClientSize={ClientSize.Width}x{ClientSize.Height} AutoScrollPosition={AutoScrollPosition} Connections={Connections.Count}");

            // OnScroll が発火しないスクロール経路 (AutoScroll 内部処理) があるため、
            // 毎フレーム PathPoints を現在の AutoScrollPosition で再計算する。
            // 接続数は高々数十本なのでコストは無視できる。
            if (Connections.Count > 0)
            {
                UpdateAllConnectionVisuals();
            }

            DrawConnections(e.Graphics);
            DrawTemporaryConnection(e.Graphics);
            DrawSelectedConnectionHighlight(e.Graphics);
            DrawConnectionInfo(e.Graphics);

            // === DIAGNOSTIC (temporary) ===
            try
            {
                if (Connections.Count > 0 && Connections[0].StartComponent != null)
                {
                    var comp = Connections[0].StartComponent;
                    var startPortName = Connections[0].StartPort;
                    var outPorts = comp.Descriptor.OutputPorts ?? new string[0];
                    var realIdx = System.Array.IndexOf(outPorts, startPortName);
                    var portRect = realIdx >= 0 ? comp.GetOutputPortBounds(realIdx)
                                                : new Rectangle(0, 0, 0, 0);
                    DBG($"DIAG idx={realIdx} portName={startPortName}");
                    // 参照1: マゼンタの矩形（portRect をそのまま描画）
                    using (var mag = new Pen(Color.Magenta, 1))
                        e.Graphics.DrawRectangle(mag, portRect);
                    // 参照2: シアンの小円（PathPoints[0]）
                    var p0 = Connections[0].Visual.PathPoints[0];
                    using (var cyan = new Pen(Color.Cyan, 2))
                        e.Graphics.DrawEllipse(cyan, p0.X - 4, p0.Y - 4, 8, 8);
                    // 参照3: グラフィクスの変換行列
                    var m = e.Graphics.Transform;
                    DBG($"DIAG comp.Left={comp.Left} comp.Top={comp.Top} " +
                        $"AutoScroll={AutoScrollPosition} " +
                        $"Transform=[{m.Elements[0]:F1},{m.Elements[1]:F1},{m.Elements[2]:F1},{m.Elements[3]:F1},{m.OffsetX:F1},{m.OffsetY:F1}] " +
                        $"portRect={portRect} PathPoints[0]=({p0.X},{p0.Y})");
                }
            }
            catch (Exception ex) { DBG($"DIAG err: {ex.Message}"); }
        }

        /// <summary>
        /// デザイン時描画
        /// 機能: デザイン時の簡易描画
        /// 注意點: 実行時とは異なる簡易表示
        /// </summary>
        private void DrawDesignTime(Graphics g)
        {
            g.Clear(BackColor);
            using (var pen = new Pen(SystemColors.ControlDark))
            {
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
            TextRenderer.DrawText(g, "Design Canvas", Font, new Point(10, 10), ForeColor);
        }

        /// <summary>
        /// 接続線描画
        /// 機能: すべての接続線を描画
        /// 注意點: 接続レンダラーを使用
        /// </summary>
        private void DrawConnections(Graphics g)
        {
            foreach (var connection in Connections)
            {
                if (connection.Visual?.PathPoints != null && connection.Visual.PathPoints.Count >= 2)
                {
                    DBG($"Draw {connection.StartComponent?.InstanceName}:{connection.StartPort} -> " +
                        $"({connection.Visual.PathPoints[0].X},{connection.Visual.PathPoints[0].Y}) -> " +
                        $"({connection.Visual.PathPoints[connection.Visual.PathPoints.Count-1].X},{connection.Visual.PathPoints[connection.Visual.PathPoints.Count-1].Y}) " +
                        $"count={connection.Visual.PathPoints.Count}");
                }
                ConnectionRenderer.Render(g, connection);
            }
        }

        /// <summary>
        /// 仮接続線描画
        /// 機能: ドラッグ中の仮接続線を描画
        /// 注意點: 接続可能な場合は色を変更
        /// </summary>
        private void DrawTemporaryConnection(Graphics g)
        {
            if (_currentConnection != null && _currentConnection.EndComponent == null)
            {
                Point start = GetComponentConnectionPoint(_currentConnection.StartComponent, true);
                Point viewportEnd = PointToClient(Cursor.Position);
                // start は viewport 座標、end も viewport に統一
                Point end = viewportEnd;

                Color lineColor = Color.LightBlue;
                DashStyle dashStyle = DashStyle.Dash;

                foreach (Control control in Controls)
                {
                    if (control is ComponentControl component && component.Bounds.Contains(end))
                    {
                        lineColor = Color.LightGreen;
                        dashStyle = DashStyle.Solid;
                        break;
                    }
                }

                using (var pen = new Pen(lineColor, 2) { DashStyle = dashStyle })
                {
                    g.DrawLine(pen, start, end);
                    g.FillEllipse(Brushes.White, end.X - 4, end.Y - 4, 8, 8);
                    g.DrawEllipse(Pens.Black, end.X - 4, end.Y - 4, 8, 8);
                }
            }
        }

        /// <summary>
        /// 選択中接続線強調表示
        /// 機能: 選択中の接続線を強調表示
        /// 注意點: 黄色の点線で強調
        /// </summary>
        private void DrawSelectedConnectionHighlight(Graphics g)
        {
            if (SelectedConnection != null)
            {
                using (var highlightPen = new Pen(Color.Yellow, 5))
                {
                    highlightPen.DashStyle = DashStyle.Dot;

                    if (SelectedConnection.Visual.PathPoints.Count >= 4)
                    {
                        g.DrawBeziers(highlightPen, SelectedConnection.Visual.PathPoints.ToArray());
                    }
                    else if (SelectedConnection.Visual.PathPoints.Count >= 2)
                    {
                        g.DrawLine(highlightPen,
                            SelectedConnection.Visual.PathPoints[0],
                            SelectedConnection.Visual.PathPoints[SelectedConnection.Visual.PathPoints.Count - 1]);
                    }
                }
            }
        }

        /// <summary>
        /// 接続情報描画
        /// 機能: 接続線の情報を描画（デバッグ用）
        /// 注意點: ポート名を接続線の中間に表示
        /// </summary>
        private void DrawConnectionInfo(Graphics g)
        {
            using (var font = new Font("Arial", 8))
            {
                foreach (var connection in Connections)
                {
                    if (connection.Visual.PathPoints.Count >= 2)
                    {
                        Point midPoint = new Point(
                            (connection.Visual.PathPoints[0].X + connection.Visual.PathPoints[1].X) / 2,
                            (connection.Visual.PathPoints[0].Y + connection.Visual.PathPoints[1].Y) / 2
                        );

                        string info = $"{connection.StartPort}→{connection.EndPort}";
                        g.DrawString(info, font, Brushes.Red, midPoint);
                    }
                }
            }
        }

        /// <summary>
        /// コンポーネント接続点取得
        /// 機能: コンポーネントの接続点（ポートの大体の位置）を取得
        /// 注意點: 正確なポート位置ではない
        /// </summary>
        private Point GetComponentConnectionPoint(ComponentControl component, bool isOutput)
        {
            int x = isOutput ? component.Right : component.Left;
            // component.Left/Top は AutoScroll 適用済み（viewport 座標）
            return new Point(x, component.Top + component.Height / 2);
        }

        /// <summary>
        /// 接続線近接判定
        /// 機能: 指定点が接続線に近いか判定
        /// 注意點: 10ピクセル以内を近接と判定
        /// </summary>
        private bool IsPointNearConnection(Point point, Connection connection)
        {
            if (connection.Visual.PathPoints.Count < 2) return false;

            for (int i = 0; i < connection.Visual.PathPoints.Count - 1; i++)
            {
                Point p1 = connection.Visual.PathPoints[i];
                Point p2 = connection.Visual.PathPoints[i + 1];

                double distance = PointToLineDistance(point, p1, p2);
                if (distance < 10) return true;
            }

            return false;
        }

        /// <summary>
        /// 点と線分の距離計算
        /// 機能: 点と線分の最短距離を計算
        /// 注意點: 射影点を計算して距離を求める
        /// </summary>
        private double PointToLineDistance(Point point, Point lineStart, Point lineEnd)
        {
            double lineLengthSquared = Math.Pow(lineEnd.X - lineStart.X, 2) + Math.Pow(lineEnd.Y - lineStart.Y, 2);

            if (lineLengthSquared == 0)
                return Math.Sqrt(Math.Pow(point.X - lineStart.X, 2) + Math.Pow(point.Y - lineStart.Y, 2));

            double t = Math.Max(0, Math.Min(1,
                ((point.X - lineStart.X) * (lineEnd.X - lineStart.X) +
                 (point.Y - lineStart.Y) * (lineEnd.Y - lineStart.Y)) / lineLengthSquared));

            Point projection = new Point(
                (int)(lineStart.X + t * (lineEnd.X - lineStart.X)),
                (int)(lineStart.Y + t * (lineEnd.Y - lineStart.Y))
            );

            return Math.Sqrt(Math.Pow(point.X - projection.X, 2) + Math.Pow(point.Y - projection.Y, 2));
        }
        #endregion

        #region プライベートメソッド - コンテキストメニュー
        /// <summary>
        /// コンテキストメニュー初期化
        /// 機能: コンテキストメニューの項目を設定
        /// 注意點: 接続線とコンポーネントの削除メニューを追加
        /// </summary>
        private void InitializeContextMenu()
        {
            var contextMenu = new ContextMenuStrip();

            var deleteConnectionItem = new ToolStripMenuItem(I18n.T("DesignCanvas.MenuDeleteConnection"));
            deleteConnectionItem.Click += DeleteConnectionItem_Click;
            deleteConnectionItem.ShortcutKeys = Keys.Delete;

            var deleteComponentItem = new ToolStripMenuItem(I18n.T("DesignCanvas.MenuDeleteComponent"));
            deleteComponentItem.Click += DeleteComponentItem_Click;
            deleteComponentItem.ShortcutKeys = Keys.Delete;

            contextMenu.Opening += ContextMenu_Opening;

            contextMenu.Items.Add(deleteConnectionItem);
            contextMenu.Items.Add(deleteComponentItem);
            this.ContextMenuStrip = contextMenu;
        }

        /// <summary>
        /// コンテキストメニュー表示前処理
        /// 機能: メニュー項目の表示/非表示を設定
        /// 注意點: 選択状態に応じて表示を切り替え
        /// </summary>
        private void ContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ContextMenuStrip != null)
            {
                ContextMenuStrip.Items[0].Visible = (SelectedConnection != null);
                ContextMenuStrip.Items[1].Visible = (SelectedComponent != null);
            }
        }

        /// <summary>
        /// 接続線削除メニュー項目クリック処理
        /// 機能: 選択中の接続線を削除
        /// 注意點: コンテキストメニューからの削除
        /// </summary>
        private void DeleteConnectionItem_Click(object sender, EventArgs e)
        {
            DeleteSelectedConnection();
        }

        /// <summary>
        /// コンポーネント削除メニュー項目クリック処理
        /// 機能: 選択中のコンポーネントを削除
        /// 注意點: コンテキストメニューからの削除
        /// </summary>
        private void DeleteComponentItem_Click(object sender, EventArgs e)
        {
            if (SelectedComponent != null) RemoveComponent(SelectedComponent);
        }
        #endregion

        #region プライベートメソッド - 検証と整合性チェック
        /// <summary>
        /// 接続線ヒストリー検証
        /// 機能: ヒストリー内の無効な接続線を削除
        /// 注意點: 定期的に呼び出して整合性を保つ
        /// </summary>
        private void ValidateConnectionHistory()
        {
            _connectionHistory.RemoveAll(conn =>
                conn == null ||
                !Connections.Contains(conn) ||
                conn.StartComponent == null ||
                conn.EndComponent == null ||
                !Controls.Contains(conn.StartComponent) ||
                !Controls.Contains(conn.EndComponent));

            if (_currentHistoryIndex >= _connectionHistory.Count)
            {
                _currentHistoryIndex = _connectionHistory.Count - 1;
            }

            if (_connectionHistory.Count == 0)
            {
                _currentHistoryIndex = -1;
            }
        }

        /// <summary>
        /// コレクション検証と修復
        /// 機能: コレクション間の不整合を修復
        /// 注意點: 孤立したコンポーネントと無効な接続線を削除
        /// </summary>
        private void ValidateAndRepairCollections()
        {
            var orphanedComponents = Components
                .Where(comp => !Controls.Contains(comp))
                .ToList();

            foreach (var orphan in orphanedComponents)
            {
                LogcatTraceHelper.Instance.SimpleMessage("CollectionRepair",
                    $"Removing orphaned component: {orphan.InstanceName}");
                Components.Remove(orphan);
            }

            var invalidConnections = Connections
                .Where(conn => conn.StartComponent == null ||
                              conn.EndComponent == null ||
                              !Controls.Contains(conn.StartComponent) ||
                              !Controls.Contains(conn.EndComponent))
                .ToList();

            foreach (var invalidConn in invalidConnections)
            {
                LogcatTraceHelper.Instance.SimpleMessage("CollectionRepair",
                    $"Removing invalid connection");
                RemoveConnection(invalidConn);
            }
        }

        /// <summary>
        /// レイアウトイベント処理
        /// 機能: 定期的な整合性チェックを実行
        /// 注意點: デザインモードでは実行しない
        /// </summary>
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            if (!IsDesignMode)
            {
                LogCollectionState("OnLayout");
                ValidateConnectionHistory();
                ValidateAndRepairCollections();
                ConnectionGraph.SynchronizeConnections(Connections);
            }
        }
        #endregion

        #region プライベートメソッド - デバッグとロギング
        /// <summary>
        /// 接続グラフ状態ダンプ
        /// 機能: 接続グラフの状態をログ出力
        /// 注意點: デバッグ用途
        /// </summary>
        private void DumpConnectionGraphInternal()
        {
            LogcatTraceHelper.Instance.SimpleMessage("Connection", "=== ConnectionGraph State Dump ===");

            LogcatTraceHelper.Instance.SimpleMessage("Connection", "Connections From:");
            foreach (var kvp in ConnectionGraph.GetConnectionsFromData())
            {
                LogcatTraceHelper.Instance.SimpleMessage("Connection", $"  Component: {kvp.Key.InstanceName}");
                foreach (var conn in kvp.Value)
                {
                    LogcatTraceHelper.Instance.SimpleMessage("Connection", $"    -> {conn.EndComponent?.InstanceName}:{conn.EndPort}");
                }
            }

            LogcatTraceHelper.Instance.SimpleMessage("Connection", "Connections To:");
            foreach (var kvp in ConnectionGraph.GetConnectionsToData())
            {
                LogcatTraceHelper.Instance.SimpleMessage("Connection", $"  Component: {kvp.Key.InstanceName}");
                foreach (var conn in kvp.Value)
                {
                    LogcatTraceHelper.Instance.SimpleMessage("Connection", $"    <- {conn.StartComponent?.InstanceName}:{conn.StartPort}");
                }
            }

            LogcatTraceHelper.Instance.SimpleMessage("Connection", "===============================");
        }

        /// <summary>
        /// 接続グラフ詳細状態ダンプ
        /// 機能: 接続グラフの詳細状態をログ出力
        /// 注意點: ハッシュ値付きで詳細情報を出力
        /// </summary>
        private void DumpConnectionGraphDetailsInternal()
        {
            LogcatTraceHelper.Instance.SimpleMessage("Connection", "=== ConnectionGraph Detailed State Dump ===");

            LogcatTraceHelper.Instance.SimpleMessage("Connection", "Connections From:");
            foreach (var kvp in ConnectionGraph.GetConnectionsFromData())
            {
                LogcatTraceHelper.Instance.SimpleMessage("Connection", $"  Component: {kvp.Key.InstanceName} (Hash: {kvp.Key.GetHashCode()})");
                foreach (var conn in kvp.Value)
                {
                    LogcatTraceHelper.Instance.SimpleMessage("Connection", $"    -> {conn.EndComponent?.InstanceName}:{conn.EndPort} (Hash: {conn.EndComponent?.GetHashCode()})");
                }
            }

            LogcatTraceHelper.Instance.SimpleMessage("Connection", "Connections To:");
            foreach (var kvp in ConnectionGraph.GetConnectionsToData())
            {
                LogcatTraceHelper.Instance.SimpleMessage("Connection", $"  Component: {kvp.Key.InstanceName} (Hash: {kvp.Key.GetHashCode()})");
                foreach (var conn in kvp.Value)
                {
                    LogcatTraceHelper.Instance.SimpleMessage("Connection", $"    <- {conn.StartComponent?.InstanceName}:{conn.StartPort} (Hash: {conn.StartComponent?.GetHashCode()})");
                }
            }

            LogcatTraceHelper.Instance.SimpleMessage("Connection", "Orphaned References Check:");
            var allComponents = new HashSet<ComponentControl>(Components);

            foreach (var comp in ConnectionGraph.GetConnectionsFromData().Keys.Where(c => !allComponents.Contains(c)))
            {
                LogcatTraceHelper.Instance.SimpleMessage("Connection", $"  Orphaned From Reference: {comp.InstanceName} (Hash: {comp.GetHashCode()})");
            }

            foreach (var comp in ConnectionGraph.GetConnectionsToData().Keys.Where(c => !allComponents.Contains(c)))
            {
                LogcatTraceHelper.Instance.SimpleMessage("Connection", $"  Orphaned To Reference: {comp.InstanceName} (Hash: {comp.GetHashCode()})");
            }

            LogcatTraceHelper.Instance.SimpleMessage("Connection", "===============================");
        }

        /// <summary>
        /// 状態ダンプ
        /// 機能: キャンバスの現在の状態をログ出力
        /// 注意點: すべての状態情報を出力
        /// </summary>
        private void DumpStateInternal()
        {
            LogcatTraceHelper.Instance.SimpleMessage("connection", "=== DesignCanvas State Dump ===");
            LogcatTraceHelper.Instance.SimpleMessage("connection", $"Components: {Components.Count}");
            LogcatTraceHelper.Instance.SimpleMessage("connection", $"Connections: {Connections.Count}");
            LogcatTraceHelper.Instance.SimpleMessage("connection", $"History: {_connectionHistory.Count}, Index: {_currentHistoryIndex}");
            LogcatTraceHelper.Instance.SimpleMessage("connection", $"Selected Component: {SelectedComponent?.InstanceName ?? "null"}");
            LogcatTraceHelper.Instance.SimpleMessage("connection", $"Selected Connection: {SelectedConnection != null}");

            foreach (var conn in Connections)
            {
                LogcatTraceHelper.Instance.SimpleMessage("connection", $"  Connection: {conn.StartComponent?.InstanceName}:{conn.StartPort} -> {conn.EndComponent?.InstanceName}:{conn.EndPort}");
            }

            DumpConnectionGraph();
            DumpConnectionGraphDetails();
            LogcatTraceHelper.Instance.SimpleMessage("connection", "===============================");
        }

        /// <summary>
        /// 接続線状態ログ出力
        /// 機能: 接続線の状態をログ出力
        /// 注意點: デバッグ用途
        /// </summary>
        private void LogConnectionState(Connection connection, string context)
        {
            try
            {
                if (connection != null)
                {
                    LogcatTraceHelper.Instance.SimpleMessage("ConnectionState",
                        $"{context}: {connection.StartComponent?.InstanceName}:{connection.StartPort} -> " +
                        $"{connection.EndComponent?.InstanceName}:{connection.EndPort}, " +
                        $"Hash: {connection.GetHashCode()}, InConnections: {Connections.Contains(connection)}, " +
                        $"InHistory: {_connectionHistory.Contains(connection)}");
                }
                else
                {
                    LogcatTraceHelper.Instance.SimpleMessage("ConnectionState",
                        $"{context}: Connection is null");
                }
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.LogError("ConnectionState",
                    $"Error logging connection state: {ex.Message}");
            }
        }

        /// <summary>
        /// コンポーネント状態ログ出力
        /// 機能: コンポーネントの状態をログ出力
        /// 注意點: デバッグ用途
        /// </summary>
        private void LogComponentState(ComponentControl component, string context)
        {
            try
            {
                if (component != null)
                {
                    LogcatTraceHelper.Instance.SimpleMessage("ComponentState",
                        $"{context}: {component.InstanceName}, Hash: {component.GetHashCode()}, " +
                        $"Selected: {component.IsSelected}, InControls: {Controls.Contains(component)}, " +
                        $"InComponents: {Components.Contains(component)}");
                }
                else
                {
                    LogcatTraceHelper.Instance.SimpleMessage("ComponentState",
                        $"{context}: Component is null");
                }
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.LogError("ComponentState",
                    $"Error logging component state: {ex.Message}");
            }
        }

        /// <summary>
        /// コレクション状態ログ出力
        /// 機能: コレクションの状態をログ出力
        /// 注意點: コレクション間の不整合もチェック
        /// </summary>
        private void LogCollectionState(string context)
        {
            try
            {
                LogcatTraceHelper.Instance.SimpleMessage("CollectionState",
                    $"{context}: Controls.Count={Controls.Count}, Components.Count={Components.Count}, " +
                    $"Connections.Count={Connections.Count}, History.Count={_connectionHistory.Count}");

                foreach (var comp in Components.Where(c => c != null))
                {
                    if (!Controls.Contains(comp))
                    {
                        LogcatTraceHelper.Instance.SimpleMessage("CollectionState",
                            $"Component {comp.InstanceName} is in Components but not in Controls!");
                    }
                }

                foreach (var conn in Connections.Where(c => c != null))
                {
                    if (conn.StartComponent == null || conn.EndComponent == null)
                    {
                        LogcatTraceHelper.Instance.SimpleMessage("CollectionState",
                            $"Connection has null component: {conn.StartPort} -> {conn.EndPort}");
                    }
                    else if (!Components.Contains(conn.StartComponent) || !Components.Contains(conn.EndComponent))
                    {
                        LogcatTraceHelper.Instance.SimpleMessage("CollectionState",
                            $"Connection references missing component: {conn.StartComponent.InstanceName} -> {conn.EndComponent.InstanceName}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.LogError("CollectionState",
                    $"Error logging collection state: {ex.Message}");
            }
        }

        /// <summary>
        /// スレッド状態ログ出力
        /// 機能: スレッドの状態をログ出力
        /// 注意點: UIスレッドかどうかを確認
        /// </summary>
        private void LogThreadState(string context)
        {
            try
            {
                bool isOnUiThread = !InvokeRequired;
                LogcatTraceHelper.Instance.SimpleMessage("ThreadState",
                    $"{context}: OnUIThread={isOnUiThread}, ThreadID={Thread.CurrentThread.ManagedThreadId}");
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.LogError("ThreadState",
                    $"Error logging thread state: {ex.Message}");
            }
        }

        /// <summary>
        /// 無効化イベント処理
        /// 機能: デバッグビルド時に状態をダンプ
        /// 注意點: 再描画時に状態を出力
        /// </summary>
        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            base.OnInvalidated(e);

#if DEBUG
            DumpState();
#endif
        }
        #endregion
    }
}