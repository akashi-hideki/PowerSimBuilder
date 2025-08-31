using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// デザインキャンバスコントロール
    /// 作成日: 2025-08-12
    /// 注意点: コンポーネントと接続の描画・管理を担当
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
            // デザイン時の基本設定
            if (IsDesignMode)
            {
                BackColor = SystemColors.Control;
                BorderStyle = BorderStyle.FixedSingle;
            }
            else
            {
                InitializeComponent();
                DoubleBuffered = true;
                AllowDrop = true;
                this.DragEnter += DesignCanvas_DragEnter;
                this.DragDrop += DesignCanvas_DragDrop;
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
        /// 注意点: すべてのコンポーネントと接続を削除
        /// </summary>
        public void Clear()
        {
            Components.Clear();
            Connections.Clear();
            Controls.Clear();
            ConnectionGraph.Clear();
            Invalidate();
        }
        /// <summary>
        /// マウスダウンイベント処理
        /// 注意点: 接続線の選択処理を実装
        /// </summary>

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // 接続線の選択チェック
            foreach (var connection in Connections)
            {
                if (IsPointNearConnection(e.Location, connection))
                {
                    ConnectionSelected?.Invoke(this, new ConnectionEventArgs(connection));
                    SelectedConnection = connection;
                    connection.Visual.IsSelected = true;
                    Invalidate();
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
            return connection.Visual.PathPoints.Any(p =>
                Math.Abs(p.X - point.X) < 10 && Math.Abs(p.Y - point.Y) < 10);
        }

        /// <summary>
        /// コンポーネントを追加
        /// 注意点: イベントハンドラを設定しキャンバスに追加
        /// </summary>
        public void AddComponent(ComponentControl control)
        {
            if (!Controls.Contains(control))
            {
                // イベントハンドラ設定
                control.SelectedChanged += (s, e) =>
                {
                    SelectedComponent = (ComponentControl)s;
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                    ComponentSelected?.Invoke(this, new ComponentEventArgs((ComponentControl)s));
                };

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
        /// 注意点: 関連接続も同時削除
        /// </summary>
        public void RemoveComponent(ComponentControl component)
        {
            if (Controls.Contains(component))
            {
                // 関連接続を削除
                var connectionsToRemove = ConnectionGraph.GetConnections(component).ToList();
                foreach (var conn in connectionsToRemove)
                {
                    RemoveConnection(conn);
                }

                // コンポーネント削除
                Controls.Remove(component);
                Components.Remove(component);
                ComponentRemoved?.Invoke(this, new ComponentEventArgs(component));
                Invalidate();
            }
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
                ConnectionAdded?.Invoke(this, new ConnectionEventArgs(connection));
                Invalidate();
            }
        }

        /// <summary>
        /// 接続を削除
        /// 注意点: 接続グラフからも削除
        /// </summary>
        public void RemoveConnection(Connection connection)
        {
            if (Connections.Contains(connection))
            {
                Connections.Remove(connection);
                ConnectionGraph.RemoveConnection(connection);
                ConnectionRemoved?.Invoke(this, new ConnectionEventArgs(connection));
                Invalidate();
            }
        }

        /// <summary>
        /// 接続の視覚情報を更新
        /// 注意点: コンポーネント位置変更時に呼び出し
        /// </summary>

        // DesignCanvas.cs 内の UpdateConnectionVisual メソッドを修正
        private void UpdateConnectionVisual(Connection connection)
        {
            if (connection.StartComponent == null || connection.EndComponent == null) return;

            // 開始ポートの位置計算
            int startIndex = Array.IndexOf(connection.StartComponent.Descriptor.OutputPorts, connection.StartPort);
            if (startIndex < 0) return;

            int startYPos = ComponentControl.BaseHeight / 2 + (startIndex * ComponentControl.PortSpacing);
            Point start = new Point(
                connection.StartComponent.Left + connection.StartComponent.Width,
                connection.StartComponent.Top + startYPos
            );

            // 終了ポートの位置計算
            int endIndex = Array.IndexOf(connection.EndComponent.Descriptor.InputPorts, connection.EndPort);
            if (endIndex < 0) return;

            int endYPos = ComponentControl.BaseHeight / 2 + (endIndex * ComponentControl.PortSpacing);
            Point end = new Point(
                connection.EndComponent.Left,
                connection.EndComponent.Top + endYPos
            );

            connection.Visual.PathPoints = new List<Point> { start, end };
        }
        /// <summary>
        /// コンポーネント配置
        /// 注意点: 自動レイアウトエンジンを使用
        /// </summary>
        public void ArrangeComponents()
        {
            AutoLayoutEngine.ArrangeComponents(this);
            UpdateAllConnectionVisuals();
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


    }
}