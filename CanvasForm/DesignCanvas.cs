using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class DesignCanvas : Panel
    {
        public event EventHandler SelectionChanged;
        public event EventHandler<ComponentEventArgs> ComponentAdded;
        public event EventHandler<ComponentEventArgs> ComponentRemoved;
        public event EventHandler<ComponentEventArgs> ComponentSelected;
        public event EventHandler<ConnectionEventArgs> ConnectionAdded;
        public event EventHandler<ConnectionEventArgs> ConnectionRemoved;
        public event EventHandler<ConnectionEventArgs> ConnectionSelected;

        private Connection _currentConnection;
        private Point _dragStartPosition;
        private ComponentControl _draggedComponent;
        private bool _isDragging;

        /// <summary>
        /// キャンバス上のコンポーネントリスト
        /// 作成日: 2025-08-10
        /// </summary>
        public List<ComponentControl> Components { get; } = new List<ComponentControl>();

        /// <summary>
        /// キャンバス上の接続リスト
        /// 作成日: 2025-08-10
        /// </summary>
        public List<Connection> Connections { get; } = new List<Connection>();

        /// <summary>
        /// 選択中のコンポーネント
        /// 作成日: 2025-08-10
        /// </summary>
        public ComponentControl SelectedComponent { get; private set; }

        /// <summary>
        /// デザインキャンバスのコンストラクタ
        /// 注意事項: ダブルバッファリングとドラッグ操作を有効化する
        /// 作成日: 2025-08-10
        /// </summary>
        public DesignCanvas()
        {
            InitializeComponent();
            DoubleBuffered = true;

            // ドロップ操作を許可
            this.AllowDrop = true;
            this.DragEnter += DesignCanvas_DragEnter;
            this.DragDrop += DesignCanvas_DragDrop;
        }

        /// <summary>
        /// キャンバスの描画処理
        /// 注意事項: 接続線とコンポーネントを描画する
        /// 作成日: 2025-08-10
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // 既存の接続を描画
            foreach (var connection in Connections)
            {
                DrawConnection(e.Graphics, connection);
            }

            // 作成中の接続を描画
            if (_currentConnection != null && _currentConnection.EndComponent == null)
            {
                DrawTemporaryConnection(e.Graphics);
            }
        }

        /// <summary>
        /// 接続線を描画する
        /// 注意事項: 開始ポートと終了ポートの位置を結ぶ線を描画
        /// 作成日: 2025-08-10
        /// </summary>
        private void DrawConnection(Graphics g, Connection connection)
        {
            var startPos = connection.StartComponent.GetPortLocation(connection.StartPort);
            var endPos = connection.EndComponent.GetPortLocation(connection.EndPort);

            using (var pen = new Pen(Color.Blue, 2))
            {
                g.DrawLine(pen, startPos, endPos);
            }
        }

        /// <summary>
        /// 仮の接続線を描画する
        /// 注意事項: カーソル位置までの点線で接続を表示する
        /// 作成日: 2025-08-10
        /// </summary>
        private void DrawTemporaryConnection(Graphics g)
        {
            var startPos = _currentConnection.StartComponent.GetPortLocation(_currentConnection.StartPort);
            var endPos = PointToClient(Cursor.Position);

            using (var pen = new Pen(Color.LightBlue, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                g.DrawLine(pen, startPos, endPos);
            }
        }

        /// <summary>
        /// コンポーネントを追加する
        /// 注意事項: イベントハンドラを設定しキャンバスに追加する
        /// 作成日: 2025-08-10
        /// </summary>
        public void AddComponent(ComponentControl control)
        {
            if (!Controls.Contains(control))
            {
                control.SelectedChanged += (s, e) =>
                {
                    SelectedComponent = (ComponentControl)s;
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                    ComponentSelected?.Invoke(this, new ComponentEventArgs((ComponentControl)s));
                };

                control.PortClicked += ComponentControl_PortClicked;

                Controls.Add(control);
                Components.Add(control);
                ComponentAdded?.Invoke(this, new ComponentEventArgs(control));
                Invalidate();
            }
        }

        /// <summary>
        /// コンポーネントを追加する（位置指定版）
        /// 注意事項: 指定位置にコンポーネントを配置する
        /// 作成日: 2025-08-10
        /// </summary>
        public void AddComponent(ComponentDescriptor descriptor, Point location)
        {
            var control = new ComponentControl(descriptor) { Location = location };
            AddComponent(control);
        }

        /// <summary>
        /// コンポーネントを削除する
        /// 注意事項: 関連する接続も同時に削除する
        /// 作成日: 2025-08-10
        /// </summary>
        public void RemoveComponent(ComponentControl component)
        {
            if (Controls.Contains(component))
            {
                // 関連接続を削除
                var connectionsToRemove = new List<Connection>();
                foreach (var conn in Connections)
                {
                    if (conn.StartComponent == component || conn.EndComponent == component)
                    {
                        connectionsToRemove.Add(conn);
                    }
                }

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
        /// 注意事項: 接続の作成を開始または完了する
        /// 作成日: 2025-08-10
        /// </summary>
        private void ComponentControl_PortClicked(object sender, PortEventArgs e)
        {
            if (_currentConnection == null)
            {
                _currentConnection = new Connection
                {
                    StartComponent = (ComponentControl)sender,
                    StartPort = e.PortName
                };
            }
            else
            {
                _currentConnection.EndComponent = (ComponentControl)sender;
                _currentConnection.EndPort = e.PortName;

                AddConnection(_currentConnection);
                _currentConnection = null;
            }
        }

        /// <summary>
        /// 接続を追加する
        /// 注意事項: 重複接続は追加しない
        /// 作成日: 2025-08-10
        /// </summary>
        public void AddConnection(Connection connection)
        {
            if (!Connections.Contains(connection))
            {
                Connections.Add(connection);
                connection.StartComponent.Connections.Add(connection);
                connection.EndComponent.Connections.Add(connection);
                ConnectionAdded?.Invoke(this, new ConnectionEventArgs(connection));
                Invalidate();
            }
        }

        /// <summary>
        /// 接続を削除する
        /// 注意事項: 関連コンポーネントからも接続情報を削除する
        /// 作成日: 2025-08-10
        /// </summary>
        public void RemoveConnection(Connection connection)
        {
            if (Connections.Contains(connection))
            {
                Connections.Remove(connection);
                connection.StartComponent?.Connections.Remove(connection);
                connection.EndComponent?.Connections.Remove(connection);
                ConnectionRemoved?.Invoke(this, new ConnectionEventArgs(connection));
                Invalidate();
            }
        }

        /// <summary>
        /// キャンバスをクリアする
        /// 注意事項: すべてのコンポーネントと接続を削除する
        /// 作成日: 2025-08-10
        /// </summary>
        public void Clear()
        {
            Components.Clear();
            Connections.Clear();
            Controls.Clear();
            Invalidate();
        }

        /// <summary>
        /// マウスダウンイベント処理
        /// 注意事項: コンポーネントのドラッグ開始または接続選択を行う
        /// 作成日: 2025-08-10
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                bool componentClicked = false;

                // コンポーネントのドラッグ処理
                foreach (var component in Components)
                {
                    if (component.Bounds.Contains(e.Location))
                    {
                        _isDragging = true;
                        _draggedComponent = component;
                        _dragStartPosition = e.Location;
                        component.BringToFront();
                        componentClicked = true;
                        break;
                    }
                }

                // 接続選択処理（コンポーネントがクリックされなかった場合）
                if (!componentClicked)
                {
                    foreach (var connection in Connections)
                    {
                        Point start = connection.StartComponent.GetPortLocation(connection.StartPort);
                        Point end = connection.EndComponent.GetPortLocation(connection.EndPort);

                        if (IsPointNearLine(e.Location, start, end))
                        {
                            ConnectionSelected?.Invoke(this, new ConnectionEventArgs(connection));
                            return;
                        }
                    }
                    // 接続が選択されなかった場合
                    ConnectionSelected?.Invoke(this, new ConnectionEventArgs(null));
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                _currentConnection = null;
                Invalidate();
            }
        }

        /// <summary>
        /// マウス移動イベント処理
        /// 注意事項: ドラッグ中のコンポーネントを移動する
        /// 作成日: 2025-08-10
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDragging && _draggedComponent != null)
            {
                int deltaX = e.X - _dragStartPosition.X;
                int deltaY = e.Y - _dragStartPosition.Y;

                _draggedComponent.Left += deltaX;
                _draggedComponent.Top += deltaY;

                _dragStartPosition = e.Location;
                Invalidate();
            }
        }

        /// <summary>
        /// マウスアップイベント処理
        /// 注意事項: ドラッグ状態をリセットする
        /// 作成日: 2025-08-10
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
            _draggedComponent = null;
        }

        /// <summary>
        /// 点と線の距離判定
        /// 注意事項: 指定距離内にあるかどうかを判定する
        /// 作成日: 2025-08-10
        /// </summary>
        private bool IsPointNearLine(Point point, Point lineStart, Point lineEnd)
        {
            double distance = DistanceFromPointToLine(point, lineStart, lineEnd);
            return distance < 5; // 5ピクセル以内をヒット判定
        }

        /// <summary>
        /// 点と線分の距離計算
        /// 注意事項: ベクトル計算を使用して最短距離を求める
        /// 作成日: 2025-08-10
        /// </summary>
        private double DistanceFromPointToLine(Point point, Point lineStart, Point lineEnd)
        {
            double lineLengthSquared = Math.Pow(lineEnd.X - lineStart.X, 2) +
                                      Math.Pow(lineEnd.Y - lineStart.Y, 2);

            if (lineLengthSquared == 0)
                return Distance(point, lineStart);

            double t = Math.Max(0, Math.Min(1,
                ((point.X - lineStart.X) * (lineEnd.X - lineStart.X) +
                 (point.Y - lineStart.Y) * (lineEnd.Y - lineStart.Y)) / lineLengthSquared));

            Point projection = new Point(
                (int)(lineStart.X + t * (lineEnd.X - lineStart.X)),
                (int)(lineStart.Y + t * (lineEnd.Y - lineStart.Y)));

            return Distance(point, projection);
        }

        /// <summary>
        /// 2点間の距離計算
        /// 作成日: 2025-08-10
        /// </summary>
        private double Distance(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        /// <summary>
        /// ドラッグエンターイベント処理
        /// 注意事項: ComponentDescriptorのドラッグ操作を許可する
        /// 作成日: 2025-08-10
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
        /// ドラッグドロップイベント処理
        /// 注意事項: ドロップ位置にコンポーネントを追加する
        /// 作成日: 2025-08-10
        /// </summary>
        private void DesignCanvas_DragDrop(object sender, DragEventArgs e)
        {
            Point dropLocation = this.PointToClient(new Point(e.X, e.Y));

            if (e.Data.GetData("ComponentDescriptor") is ComponentDescriptor descriptor)
            {
                AddComponent(descriptor, dropLocation);
            }
        }
    }

    /// <summary>
    /// 接続情報クラス
    /// 作成日: 2025-08-10
    /// </summary>
    public class Connection
    {
        /// <summary>
        /// 接続開始コンポーネント
        /// 作成日: 2025-08-10
        /// </summary>
        public ComponentControl StartComponent { get; set; }

        /// <summary>
        /// 接続開始ポート名
        /// 作成日: 2025-08-10
        /// </summary>
        public string StartPort { get; set; }

        /// <summary>
        /// 接続終了コンポーネント
        /// 作成日: 2025-08-10
        /// </summary>
        public ComponentControl EndComponent { get; set; }

        /// <summary>
        /// 接続終了ポート名
        /// 作成日: 2025-08-10
        /// </summary>
        public string EndPort { get; set; }
    }

    /// <summary>
    /// コンポーネントイベント引数クラス
    /// 作成日: 2025-08-10
    /// </summary>
    public class ComponentEventArgs : EventArgs
    {
        /// <summary>
        /// 関連コンポーネント
        /// 作成日: 2025-08-10
        /// </summary>
        public ComponentControl Component { get; }

        public ComponentEventArgs(ComponentControl component)
        {
            Component = component;
        }
    }

    /// <summary>
    /// 接続イベント引数クラス
    /// 作成日: 2025-08-10
    /// </summary>
    public class ConnectionEventArgs : EventArgs
    {
        /// <summary>
        /// 関連接続
        /// 作成日: 2025-08-10
        /// </summary>
        public Connection Connection { get; }

        public ConnectionEventArgs(Connection connection)
        {
            Connection = connection;
        }
    }

    /// <summary>
    /// ポートイベント引数クラス
    /// 作成日: 2025-08-10
    /// </summary>
    public class PortEventArgs : EventArgs
    {
        /// <summary>
        /// ポート名
        /// 作成日: 2025-08-10
        /// </summary>
        public string PortName { get; }

        public PortEventArgs(string portName)
        {
            PortName = portName;
        }
    }
}