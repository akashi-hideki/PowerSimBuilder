using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
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
    [System.ComponentModel.DesignerCategory("Code")]// デザイナーコード生成を抑制
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
        // デザイン時に問題になるプロパティを非表示
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
                { Keys.Up, () => NavigateConnectionHistory(-1) },
                { Keys.Down, () => NavigateConnectionHistory(1) },
                { Keys.Delete, DeleteSelectedConnection }
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
                // 必要に応じて最小スクロール領域を設定（例: 幅2000, 高さ2000）
                this.AutoScrollMinSize = new System.Drawing.Size(2000, 2000);
                DoubleBuffered = true;
                AllowDrop = true;
                
                // キーボード操作を有効化（PanelにはKeyPreviewがないため個別設定）
                this.TabStop = true; // タブストップを有効化
                this.SetStyle(ControlStyles.Selectable, true); // 選択可能に設定
                this.KeyDown += DesignCanvas_KeyDown;
                
                this.DragEnter += DesignCanvas_DragEnter;
                this.DragDrop += DesignCanvas_DragDrop;
                
                // コンテキストメニューを設定
                InitializeContextMenu();
            }
        }
        
        private bool IsDesignMode
        {
            get
            {
                return (LicenseManager.UsageMode == LicenseUsageMode.Designtime) ||
                       (Site != null && Site.DesignMode);
            }
        }
        
        /// <summary>
        /// マウスクリック時にフォーカスを取得するようにオーバーライド
        /// 注意点: Panelはデフォルトでフォーカスを受け取らないため明示的に設定
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!this.Focused)
            {
                this.Focus();
            }
            
            // 接続線の選択チェック
            foreach (var connection in Connections)
            {
                if (IsPointNearConnection(e.Location, connection))
                {
                    SelectConnection(connection);
                    return;
                }
            }

            // ポートクリックチェック
            foreach (Control control in Controls)
            {
                if (control is ComponentControl component)
                {
                    var clientPos = component.PointToClient(PointToScreen(e.Location));

                    // 入力ポートチェック
                    if (component.Descriptor.InputPorts != null)
                    {
                        for (int i = 0; i < component.Descriptor.InputPorts.Length; i++)
                        {
                            float ratio = (i + 1f) / (component.Descriptor.InputPorts.Length + 1);
                            int yPos = (int)(component.Height * ratio);
                            var portRect = new Rectangle(5, yPos - 5, 10, 10);

                            if (portRect.Contains(clientPos))
                            {
                                ComponentControl_PortClicked(component, new PortEventArgs(component.Descriptor.InputPorts[i]));
                                return;
                            }
                        }
                    }

                    // 出力ポートチェック
                    if (component.Descriptor.OutputPorts != null)
                    {
                        for (int i = 0; i < component.Descriptor.OutputPorts.Length; i++)
                        {
                            float ratio = (i + 1f) / (component.Descriptor.OutputPorts.Length + 1);
                            int yPos = (int)(component.Height * ratio);
                            var portRect = new Rectangle(component.Width - 15, yPos - 5, 10, 10);

                            if (portRect.Contains(clientPos))
                            {
                                ComponentControl_PortClicked(component, new PortEventArgs(component.Descriptor.OutputPorts[i]));
                                return;
                            }
                        }
                    }
                }
            }

            // 接続線以外をクリックした場合
            ConnectionSelected?.Invoke(this, new ConnectionEventArgs(null));
            DeselectAllConnections();
            Invalidate();
        }
        
        /// <summary>
        /// コンテキストメニューの初期化
        /// 注意点: 接続線の削除メニューを追加
        /// </summary>
        private void InitializeContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            
            var deleteConnectionItem = new ToolStripMenuItem("接続線を削除");
            deleteConnectionItem.Click += (s, e) => DeleteSelectedConnection();
            deleteConnectionItem.ShortcutKeys = Keys.Delete;
            
            contextMenu.Items.Add(deleteConnectionItem);
            this.ContextMenuStrip = contextMenu;
        }
        
        /// <summary>
        /// キーダウンイベント処理
        /// 注意点: Dictionaryを使用してキー操作を処理
        /// </summary>
        private void DesignCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            // 追加ログ: キー入力情報
            LogcatTraceHelper.Instance.SimpleMessage("KeyDown",
                $"Key pressed: {e.KeyCode}, SelectedConnection: {SelectedConnection != null}, " +
                $"SelectedComponent: {SelectedComponent != null}");
            LogThreadState("KeyDown");

            if (e.KeyCode == Keys.Delete)
            {
                // 接続線が選択されている場合は接続線を削除
                if (SelectedConnection != null)
                {
                    LogcatTraceHelper.Instance.SimpleMessage("KeyDown", "Deleting selected connection");
                    RemoveConnection(SelectedConnection);
                    e.Handled = true;
                }
                // コンポーネントが選択されている場合はコンポーネントを削除
                else if (SelectedComponent != null)
                {
                    LogcatTraceHelper.Instance.SimpleMessage("KeyDown", "Deleting selected component");
                    RemoveComponent(SelectedComponent);
                    e.Handled = true;
                }
            }
            else if (_keyHandlers.TryGetValue(e.KeyCode, out Action handler))
            {
                handler();
                e.Handled = true;
            }
        }
        /// <summary>
        /// 接続線ヒストリー内を移動して接続線を選択します
        /// </summary>
        /// <param name="direction">移動方向（-1: 前へ、1: 次へ）</param>
        /// <remarks>
        /// 接続線は作成された順にヒストリーに記録され、上下キーで順次選択できます。
        /// ヒストリーは最大50件まで保持され、それを超えると古いものから削除されます。
        /// </remarks>
        private void NavigateConnectionHistory(int direction)
        {
            if (_connectionHistory.Count == 0)
                return;
                
            // 現在の選択位置を更新
            _currentHistoryIndex += direction;
            
            // 範囲チェック
            if (_currentHistoryIndex < 0)
                _currentHistoryIndex = 0;
            else if (_currentHistoryIndex >= _connectionHistory.Count)
                _currentHistoryIndex = _connectionHistory.Count - 1;
                
            // 接続線を選択
            if (_currentHistoryIndex >= 0 && _currentHistoryIndex < _connectionHistory.Count)
            {
                SelectConnection(_connectionHistory[_currentHistoryIndex]);
            }
        }
        
        /// <summary>
        /// 接続線をヒストリーに追加します
        /// </summary>
        /// <param name="connection">追加する接続線</param>
        /// <remarks>
        /// 既に存在する接続線はヒストリーから削除されてから先頭に追加されます。
        /// これにより、最近操作した接続線が常にヒストリーの先頭に来るようになります。
        /// </remarks>
        private void AddToConnectionHistory(Connection connection)
        {
            // 既存の接続線を削除（重複防止）
            _connectionHistory.RemoveAll(c => c == connection);
            
            // 新しい接続線を先頭に追加
            _connectionHistory.Insert(0, connection);
            
            // ヒストリーサイズ制限（最大50件）
            if (_connectionHistory.Count > 50)
            {
                _connectionHistory.RemoveAt(_connectionHistory.Count - 1);
            }
            
            // 現在の選択位置を先頭にリセット
            _currentHistoryIndex = 0;
        }

        /// <summary>
        /// 接続線をヒストリーから削除します
        /// </summary>
        /// <param name="connection">削除する接続線</param>
        /// <remarks>
        /// 接続線が削除された場合、ヒストリーからも削除されます。
        /// 現在選択中の接続線が削除された場合は、選択状態がクリアされます。
        /// </remarks>
        /// <summary>
        /// 接続線をヒストリーから削除します
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

                // 追加ログ: ヒストリー操作前
                LogConnectionState(connection, "Before RemoveFromHistory");
                LogcatTraceHelper.Instance.SimpleMessage("ConnectionHistory",
                    $"History count before: {_connectionHistory.Count}, Current index: {_currentHistoryIndex}");

                int removeIndex = _connectionHistory.IndexOf(connection);

                if (removeIndex >= 0)
                {
                    _connectionHistory.RemoveAt(removeIndex);

                    // 現在の選択位置を調整
                    if (_currentHistoryIndex > removeIndex)
                    {
                        _currentHistoryIndex--;
                    }
                    else if (_currentHistoryIndex == removeIndex)
                    {
                        // 削除された要素が現在選択中の場合は選択をクリア
                        _currentHistoryIndex = -1;
                        if (SelectedConnection == connection)
                        {
                            SelectedConnection = null;
                        }
                    }

                    // インデックスが範囲外にならないように調整
                    if (_currentHistoryIndex >= _connectionHistory.Count)
                    {
                        _currentHistoryIndex = _connectionHistory.Count - 1;
                    }
                }

                // 追加ログ: ヒストリー操作後
                LogcatTraceHelper.Instance.SimpleMessage("ConnectionHistory",
                    $"History count after: {_connectionHistory.Count}, Current index: {_currentHistoryIndex}");
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.LogError("ConnectionHistory",
                    $"ヒストリー削除エラー: {ex.Message}");
            }
        }
        /// <summary>
        /// ヒストリーの整合性をチェックするメソッド
        /// </summary>
        private void ValidateConnectionHistory()
        {
            // 存在しない接続線をヒストリーから削除
            _connectionHistory.RemoveAll(conn =>
                conn == null ||
                !Connections.Contains(conn) ||
                conn.StartComponent == null ||
                conn.EndComponent == null ||
                !Controls.Contains(conn.StartComponent) ||
                !Controls.Contains(conn.EndComponent));

            // 現在のインデックスを調整
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
        /// 定期的なヒストリー検証を追加
        /// </summary>
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            // レイアウト時にヒストリーの整合性をチェック
            if (!IsDesignMode)
            {
                LogCollectionState("OnLayout");
                ValidateConnectionHistory();

                // 接続グラフの同期状態を確認
                ConnectionGraph.SynchronizeConnections(Connections);
            }
        }
        /// <summary>
        /// 選択中の接続線を削除します
        /// </summary>
        /// <remarks>
        /// 削除操作はDelキーまたはコンテキストメニューから実行できます。
        /// 接続線が削除されると、ヒストリーからも削除されます。
        /// </remarks>
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
        /// 指定した接続線を選択状態にします
        /// </summary>
        /// <param name="connection">選択する接続線</param>
        /// <remarks>
        /// 接続線が選択されると視覚的に強調表示され、ヒストリー内の位置が更新されます。
        /// 既に選択中の接続線は選択が解除されます。
        /// </remarks>
        private void SelectConnection(Connection connection)
        {
            // 全ての接続線の選択を解除
            DeselectAllConnections();
            
            // 指定した接続線を選択状態にする
            connection.Visual.IsSelected = true;
            SelectedConnection = connection;
            
            // ヒストリー内の位置を更新
            _currentHistoryIndex = _connectionHistory.IndexOf(connection);
            
            // 選択イベントを発行
            ConnectionSelected?.Invoke(this, new ConnectionEventArgs(connection));
            
            // 再描画
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (IsDesignMode)
            {
                // デザイン時の簡易描画
                e.Graphics.Clear(BackColor);
                using (var pen = new Pen(SystemColors.ControlDark))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
                TextRenderer.DrawText(e.Graphics, "Design Canvas", Font,
                    new Point(10, 10), ForeColor);
                return;
            }

            // 実行時の描画処理
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // 接続線を描画
            foreach (var connection in Connections)
            {
                ConnectionRenderer.Render(e.Graphics, connection);
            }

            // 仮接続線を描画
            if (_currentConnection != null && _currentConnection.EndComponent == null)
            {
                DrawTemporaryConnection(e.Graphics);
            }

            // 選択中の接続線を強調表示
            if (SelectedConnection != null)
            {
                using (var highlightPen = new Pen(Color.Yellow, 5))
                {
                    highlightPen.DashStyle = DashStyle.Dot;
                    
                    if (SelectedConnection.Visual.PathPoints.Count >= 4)
                    {
                        e.Graphics.DrawBeziers(highlightPen, SelectedConnection.Visual.PathPoints.ToArray());
                    }
                    else if (SelectedConnection.Visual.PathPoints.Count >= 2)
                    {
                        e.Graphics.DrawLine(highlightPen, 
                            SelectedConnection.Visual.PathPoints[0], 
                            SelectedConnection.Visual.PathPoints[SelectedConnection.Visual.PathPoints.Count - 1]);
                    }
                }
            }

            // 接続情報を表示（デバッグ用）
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
                        e.Graphics.DrawString(info, font, Brushes.Red, midPoint);
                    }
                }
            }
        }
        
        /// <summary>
        /// キャンバスをクリア
        /// 注意点: すべてのコンポーネントと接続を削除、イベントハンドラもクリーンアップ
        /// </summary>
        public void Clear()
        {
            try
            {
                // すべてのイベントハンドラを解除
                foreach (ComponentControl component in Components.ToList())
                {
                    component.SelectedChanged -= Component_SelectedChanged;
                    component.PortClicked -= ComponentControl_PortClicked;
                    component.MouseDown -= ComponentControl_MouseDown;
                    component.MouseMove -= ComponentControl_MouseMove;
                    component.MouseUp -= ComponentControl_MouseUp;
                }

                // すべてのコントロールを削除
                Controls.Clear();
                Components.Clear();
                Connections.Clear();
                ConnectionGraph.Clear();
                _connectionHistory.Clear();
                _currentHistoryIndex = -1;

                // 選択状態をリセット
                SelectedComponent = null;
                SelectedConnection = null;

                Invalidate();
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.LogError("Delete Error", $"キャンバスクリアエラー: {ex.Message}");
            }
        }
        /// <summary>
        /// 仮接続線を描画
        /// 注意点: ドラッグ中の接続作成時に表示
        /// </summary>
        private void DrawTemporaryConnection(Graphics g)
        {
            Point start = GetComponentConnectionPoint(_currentConnection.StartComponent, true);
            Point end = PointToClient(Cursor.Position);

            // 接続線のスタイルを状態に応じて変更
            Color lineColor = Color.LightBlue;
            DashStyle dashStyle = DashStyle.Dash;

            // ポート上にホバーしている場合は接続可能状態を示す
            foreach (Control control in Controls)
            {
                if (control is ComponentControl component &&
                    component.Bounds.Contains(end))
                {
                    lineColor = Color.LightGreen;
                    dashStyle = DashStyle.Solid;
                    break;
                }
            }

            using (var pen = new Pen(lineColor, 2) { DashStyle = dashStyle })
            {
                g.DrawLine(pen, start, end);

                // 接続ポイントを示すマーカー
                g.FillEllipse(Brushes.White, end.X - 4, end.Y - 4, 8, 8);
                g.DrawEllipse(Pens.Black, end.X - 4, end.Y - 4, 8, 8);
            }
        }

        /// <summary>
        /// コンポーネントの接続点を取得
        /// 注意点: isOutput=trueで出力ポート
        /// </summary>
        private Point GetComponentConnectionPoint(ComponentControl component, bool isOutput)
        {
            int x = isOutput ? component.Right : component.Left;
            return new Point(x, component.Top + component.Height / 2);
        }

        /// <summary>
        /// 接続近接判定
        /// 注意点: 簡易実装（実際は線分との距離計算が必要）
        /// </summary>
        private bool IsPointNearConnection(Point point, Connection connection)
        {
            if (connection.Visual.PathPoints.Count < 2)
                return false;
                
            // 線分との距離計算を行う改良版
            for (int i = 0; i < connection.Visual.PathPoints.Count - 1; i++)
            {
                Point p1 = connection.Visual.PathPoints[i];
                Point p2 = connection.Visual.PathPoints[i + 1];
                
                // 線分と点の距離を計算
                double distance = PointToLineDistance(point, p1, p2);
                if (distance < 10) // 10ピクセル以内なら近接と判定
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 点と線分の距離を計算
        /// </summary>
        private double PointToLineDistance(Point point, Point lineStart, Point lineEnd)
        {
            double lineLengthSquared = Math.Pow(lineEnd.X - lineStart.X, 2) + Math.Pow(lineEnd.Y - lineStart.Y, 2);
            
            if (lineLengthSquared == 0) // 線分の長さが0の場合
                return Math.Sqrt(Math.Pow(point.X - lineStart.X, 2) + Math.Pow(point.Y - lineStart.Y, 2));
                
            // 点から線分への射影のパラメータを計算
            double t = Math.Max(0, Math.Min(1, 
                ((point.X - lineStart.X) * (lineEnd.X - lineStart.X) + 
                 (point.Y - lineStart.Y) * (lineEnd.Y - lineStart.Y)) / lineLengthSquared));
                 
            // 射影点を計算
            Point projection = new Point(
                (int)(lineStart.X + t * (lineEnd.X - lineStart.X)),
                (int)(lineStart.Y + t * (lineEnd.Y - lineStart.Y))
            );
            
            // 点と射影点の距離を返す
            return Math.Sqrt(Math.Pow(point.X - projection.X, 2) + Math.Pow(point.Y - projection.Y, 2));
        }
 
        /// <summary>
        /// コンポーネントを追加
        /// 注意点: イベントハンドラを設定しキャンバスに追加
        /// </summary>
        public void AddComponent(ComponentControl control)
        {
            if (control == null || Controls.Contains(control)) return;

            try
            {
                // 既存のイベントハンドラを解除（二重登録防止）
                control.SelectedChanged -= Component_SelectedChanged;
                control.PortClicked -= ComponentControl_PortClicked;
                control.MouseDown -= ComponentControl_MouseDown;
                control.MouseMove -= ComponentControl_MouseMove;
                control.MouseUp -= ComponentControl_MouseUp;

                // 新しいイベントハンドラを登録
                control.SelectedChanged += Component_SelectedChanged;
                control.PortClicked += ComponentControl_PortClicked;
                control.MouseDown += ComponentControl_MouseDown;
                control.MouseMove += ComponentControl_MouseMove;
                control.MouseUp += ComponentControl_MouseUp;

                // コントロール追加
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
        /// コンポーネントを追加（位置指定）
        /// 注意点: ディスクリプタからコンポーネント生成
        /// </summary>
        public void AddComponent(ComponentDescriptor descriptor, Point location)
        {
            var control = new ComponentControl(descriptor) { Location = location };
            AddComponent(control);
        }
        /// <summary>
        /// コンポーネントを削除
        /// 注意点: 関連接続も同時削除、イベントハンドラもクリーンアップ
        /// </summary>
        /// <summary>
        /// コンポーネントを削除
        /// 注意点: 関連接続も同時削除
        /// </summary>
        /// <summary>
        /// コンポーネントを削除
        /// 注意点: 関連接続も同時削除
        /// </summary>
        public void RemoveComponent(ComponentControl component)
        {
            if (component == null)
            {
                LogcatTraceHelper.Instance.SimpleMessage("RemoveComponent", "Attempted to remove null component");
                return;
            }

            if (!Controls.Contains(component))
            {
                LogcatTraceHelper.Instance.SimpleMessage("RemoveComponent",
                    $"Component {component.InstanceName} not found in Controls collection");
                return;
            }

            try
            {
                // 追加ログ: 削除前の状態
                LogComponentState(component, "Before RemoveComponent");
                LogCollectionState("Before RemoveComponent");
                LogThreadState("RemoveComponent");

                // デバッグ: 削除前の状態をダンプ
                Debug.WriteLine("=== Before RemoveComponent ===");
                DumpState();

                // 関連接続を強制的に削除
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

                // ConnectionGraphからもコンポーネントに関連する接続を削除
                ConnectionGraph.RemoveAllConnections(component);

                // イベントハンドラを解除
                component.SelectedChanged -= Component_SelectedChanged;
                component.PortClicked -= ComponentControl_PortClicked;
                component.MouseDown -= ComponentControl_MouseDown;
                component.MouseMove -= ComponentControl_MouseMove;
                component.MouseUp -= ComponentControl_MouseUp;

                // 選択状態の更新
                if (SelectedComponent == component)
                {
                    SelectedComponent = null;
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }

                // コンポーネント削除
                Controls.Remove(component);
                Components.Remove(component);

                // 追加ログ: 削除後の状態
                LogComponentState(component, "After RemoveComponent");
                LogCollectionState("After RemoveComponent");
                LogObjectLifetime(component, "After RemoveComponent");

                ComponentRemoved?.Invoke(this, new ComponentEventArgs(component));

                // デバッグ: 削除後の状態をダンプ
                Debug.WriteLine("=== After RemoveComponent ===");
                DumpState();

                Invalidate();
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.LogError("RemoveComponent",
                    $"コンポーネント削除エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 接続を削除
        /// 注意点: 接続グラフからも削除、ヒストリーからも削除
        /// </summary>
        /// <summary>
        /// 接続を削除
        /// 注意点: 接続グラフからも削除、ヒストリーからも削除
        /// </summary>
        public void RemoveConnection(Connection connection)
        {
            if (connection == null || !Connections.Contains(connection)) return;

            try
            {
                // 追加ログ: 削除前の状態
                LogConnectionState(connection, "Before RemoveConnection");
                LogCollectionState("Before RemoveConnection");

                Connections.Remove(connection);
                ConnectionGraph.RemoveConnection(connection);

                // ヒストリーからも削除
                RemoveFromConnectionHistory(connection);

                // 選択状態の更新
                if (SelectedConnection == connection)
                {
                    SelectedConnection = null;
                    ConnectionSelected?.Invoke(this, new ConnectionEventArgs(null));
                }

                // 追加ログ: 削除後の状態
                LogConnectionState(connection, "After RemoveConnection");
                LogCollectionState("After RemoveConnection");

                ConnectionRemoved?.Invoke(this, new ConnectionEventArgs(connection));
                Invalidate();
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.LogError("RemoveConnection",
                    $"接続線削除エラー: {ex.Message}");
            }
        }
 
        // 専用のイベントハンドラメソッドを追加
        private void Component_SelectedChanged(object sender, EventArgs e)
        {
            var component = (ComponentControl)sender;
            SelectedComponent = component;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            ComponentSelected?.Invoke(this, new ComponentEventArgs(component));
        }

        /// <summary>
        /// ポートクリックイベントハンドラ
        /// 注意点: 接続の作成を開始/完了
        /// </summary>
        private void ComponentControl_PortClicked(object sender, PortEventArgs e)
        {
            var component = (ComponentControl)sender;

            if (_currentConnection == null)
            {
                // 接続開始
                _currentConnection = new Connection
                {
                    StartComponent = component,
                    StartPort = e.PortName,
                    Visual = new ConnectionVisual()
                };
            }
            else
            {
                // 接続完了
                _currentConnection.EndComponent = component;
                _currentConnection.EndPort = e.PortName;

                if (!ConnectionGraph.ConnectionExists(_currentConnection))
                {
                    AddConnection(_currentConnection);
                }

                _currentConnection = null;
            }

            Invalidate();
        }

        /// <summary>
        /// 接続を追加
        /// 注意点: 重複接続は追加しない
        /// </summary>
        public void AddConnection(Connection connection)
        {
            if (!Connections.Contains(connection))
            {
                Connections.Add(connection);
                ConnectionGraph.AddConnection(connection);
                UpdateConnectionVisual(connection);
                
                // ヒストリーに追加
                AddToConnectionHistory(connection);
                
                ConnectionAdded?.Invoke(this, new ConnectionEventArgs(connection));
                Invalidate();
            }
        }
        /// <summary>
        /// 接続の視覚情報を更新
        /// 注意点: コンポーネント位置変更時に呼び出し
        /// </summary>
        private void UpdateConnectionVisual(Connection connection)
        {
            if (connection.StartComponent == null || connection.EndComponent == null) return;

            // 出力ポート位置の取得
            int startIndex = Array.IndexOf(connection.StartComponent.Descriptor.OutputPorts, connection.StartPort);
            if (startIndex < 0) return;

            Rectangle startPortRect = connection.StartComponent.GetOutputPortBounds(startIndex);
            Point start = new Point(startPortRect.Right, startPortRect.Top + startPortRect.Height / 2);

            // 入力ポート位置の取得
            int endIndex = Array.IndexOf(connection.EndComponent.Descriptor.InputPorts, connection.EndPort);
            if (endIndex < 0) return;

            Rectangle endPortRect = connection.EndComponent.GetInputPortBounds(endIndex);
            Point end = new Point(endPortRect.Left, endPortRect.Top + endPortRect.Height / 2);

            connection.Visual.PathPoints = new List<Point> { start, end };
        }

        /// <summary>
        /// コンポーネント配置
        /// 注意点: 自動レイアウトエンジンを使用
        /// </summary>
        public void ArrangeComponents()
        {
            // レイアウトを一時停止
            this.SuspendLayout();

            try
            {
                AutoLayoutEngine.ArrangeComponents(this);
                UpdateAllConnectionVisuals();
            }
            finally
            {
                // レイアウトを再開して一度だけ再描画
                this.ResumeLayout(true);
                this.PerformLayout();
            }

            Invalidate();
        }
        
        /// <summary>
        /// 全接続の視覚情報を更新
        /// 注意点: 配置変更後に呼び出し
        /// </summary>
        private void UpdateAllConnectionVisuals()
        {
            foreach (var connection in Connections)
            {
                UpdateConnectionVisual(connection);
            }
        }

        /// <summary>
        /// コンポーネントドラッグ開始処理
        /// </summary>
        private void ComponentControl_MouseDown(object sender, MouseEventArgs e)
        {
            var component = (ComponentControl)sender;
            component.BringToFront();

            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _draggedComponent = component;
                _dragStartPosition = e.Location;
            }
        }

        /// <summary>
        /// コンポーネントドラッグ処理
        /// 注意点: 接続線も同時更新
        /// </summary>
        private void ComponentControl_MouseMove(object sender, MouseEventArgs e)
        {
            var component = (ComponentControl)sender;

            if (_isDragging && _draggedComponent == component)
            {
                int deltaX = e.X - _dragStartPosition.X;
                int deltaY = e.Y - _dragStartPosition.Y;

                component.Left += deltaX;
                component.Top += deltaY;

                UpdateComponentConnections(component);
                Invalidate();
            }
        }

        /// <summary>
        /// コンポーネント関連接続を更新
        /// </summary>
        private void UpdateComponentConnections(ComponentControl component)
        {
            foreach (var connection in ConnectionGraph.GetConnections(component))
            {
                UpdateConnectionVisual(connection);
            }
        }

        /// <summary>
        /// コンポーネントドラッグ終了処理
        /// </summary>
        private void ComponentControl_MouseUp(object sender, MouseEventArgs e)
        {
            _isDragging = false;
            _draggedComponent = null;
        }

        /// <summary>
        /// 全接続の選択解除
        /// 注意点: 視覚状態を更新
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

        /// <summary>
        /// ドラッグエンターイベント
        /// 注意点: コンポーネント追加のドラッグ操作を許可
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
        /// ドロップイベント処理
        /// 注意点: パレットからコンポーネントを追加
        /// </summary>
        private void DesignCanvas_DragDrop(object sender, DragEventArgs e)
        {
            Point dropLocation = this.PointToClient(new Point(e.X, e.Y));

            if (e.Data.GetData("ComponentDescriptor") is ComponentDescriptor descriptor)
            {
                AddComponent(descriptor, dropLocation);
                ArrangeComponents();
            }
        }
        /// <summary>
        /// 接続グラフの整合性を検証
        /// </summary>
        public void ValidateConnectionGraph()
        {
            ConnectionGraph.ValidateIntegrity(Components);
        }
        /// <summary>
        /// 接続グラフの状態をダンプするデバッグメソッド
        /// </summary>
        public void DumpConnectionGraph()
        {
            LogcatTraceHelper.Instance.SimpleMessage("Connection","=== ConnectionGraph State Dump ===");

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
        /// ConnectionGraphの状態を詳細にダンプするメソッド
        /// </summary>
        public void DumpConnectionGraphDetails()
        {
            LogcatTraceHelper.Instance.SimpleMessage("Connection","=== ConnectionGraph Detailed State Dump ===");

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

            // 存在しないコンポーネントへの参照をチェック
            LogcatTraceHelper.Instance.SimpleMessage("Connection", "Orphaned References Check:");
            var allComponents = new HashSet<ComponentControl>(Components);

            foreach (var comp in ConnectionGraph.GetConnectionsFromData().Keys.Where(c => !allComponents.Contains(c)))
            {
                LogcatTraceHelper.Instance.SimpleMessage("Connection", $"  Orphaned From Reference: {comp.InstanceName} (Hash: {comp.GetHashCode()})");
            }

            foreach (var comp in ConnectionGraph.GetConnectionsToData().Keys.Where(c => !allComponents.Contains(c)))
            {
                LogcatTraceHelper.Instance.SimpleMessage("Connection",$"  Orphaned To Reference: {comp.InstanceName} (Hash: {comp.GetHashCode()})");
            }

            LogcatTraceHelper.Instance.SimpleMessage("Connection", "===============================");
        }
        /// <summary>
        /// オブジェクトの生存状態を確認
        /// </summary>
        private void LogObjectLifetime(ComponentControl component, string context)
        {
            try
            {
                var weakRef = new WeakReference(component);
                LogcatTraceHelper.Instance.SimpleMessage("ObjectLifetime",
                    $"{context}: {component.InstanceName}, Alive={weakRef.IsAlive}");

                // 明示的にGCを実行して生存を確認
                GC.Collect();
                GC.WaitForPendingFinalizers();

                LogcatTraceHelper.Instance.SimpleMessage("ObjectLifetime",
                    $"{context} after GC: {component.InstanceName}, Alive={weakRef.IsAlive}");
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.LogError("ObjectLifetime",
                    $"Error logging object lifetime: {ex.Message}");
            }
        }
        /// <summary>
        /// 現在の状態をダンプするデバッグメソッド
        /// </summary>
        public void DumpState()
        {
            LogcatTraceHelper.Instance.SimpleMessage ("connection", "=== DesignCanvas State Dump ===");
            LogcatTraceHelper.Instance.SimpleMessage("connection",$"Components: {Components.Count}");
            LogcatTraceHelper.Instance.SimpleMessage("connection", $"Connections: {Connections.Count}");
            LogcatTraceHelper.Instance.SimpleMessage("connection", $"History: {_connectionHistory.Count}, Index: {_currentHistoryIndex}");
            LogcatTraceHelper.Instance.SimpleMessage("connection", $"Selected Component: {SelectedComponent?.InstanceName ?? "null"}");
            LogcatTraceHelper.Instance.SimpleMessage("connection", $"Selected Connection: {SelectedConnection != null}");

            // 接続線の詳細情報
            foreach (var conn in Connections)
            {
                LogcatTraceHelper.Instance.SimpleMessage("connection", $"  Connection: {conn.StartComponent?.InstanceName}:{conn.StartPort} -> {conn.EndComponent?.InstanceName}:{conn.EndPort}");
            }
            // 接続グラフの詳細情報も出力
            DumpConnectionGraph();
            DumpConnectionGraphDetails();
            LogcatTraceHelper.Instance.SimpleMessage("connection", "===============================");
        }
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

        private void LogCollectionState(string context)
        {
            try
            {
                LogcatTraceHelper.Instance.SimpleMessage("CollectionState",
                    $"{context}: Controls.Count={Controls.Count}, Components.Count={Components.Count}, " +
                    $"Connections.Count={Connections.Count}, History.Count={_connectionHistory.Count}");

                // nullチェックを追加
                foreach (var comp in Components.Where(c => c != null))
                {
                    if (!Controls.Contains(comp))
                    {
                        LogcatTraceHelper.Instance.SimpleMessage("CollectionState",
                            $"Component {comp.InstanceName} is in Components but not in Controls!");
                    }
                }

                // nullチェックを追加
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
        /// スレッド状態をログ出力
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
        // デバッグ用に状態ダンプを呼び出すメソッドを追加
        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            base.OnInvalidated(e);

#if DEBUG
            // デバッグビルド時のみ状態をダンプ
            DumpState();
#endif
        }
    }
}