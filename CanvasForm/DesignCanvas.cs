using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
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
        public ConnectionGraph ConnectionGraph { get; } = new ConnectionGraph();
        public AutoLayoutEngine LayoutEngine { get; } = new AutoLayoutEngine();
        public ConnectionRenderer ConnectionRenderer { get; set; } = new BezierConnectionRenderer();

        /// <summary>
        /// デザインキャンバスのコンストラクタ
        /// 注意事項: ダブルバッファリングとドラッグ操作を有効化する
        /// 作成日: 2025-08-10
        /// </summary>
        public DesignCanvas()
        {
            InitializeComponent();
            DoubleBuffered = true;
            AllowDrop = true;
            
            // ドロップ操作を許可
            this.AllowDrop = true;
            this.DragEnter += DesignCanvas_DragEnter;
            this.DragDrop += DesignCanvas_DragDrop;
        }
        public void Clear()
        {
            Components.Clear();
            Connections.Clear();
            Controls.Clear();
            ConnectionGraph.Clear();
            Invalidate();
        }
        /// <summary>
        /// キャンバスの描画処理
        /// 注意事項: 接続線とコンポーネントを描画する
        /// 作成日: 2025-08-10
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
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
        }

        private void DrawTemporaryConnection(Graphics g)
        {
            Point start = GetComponentConnectionPoint(_currentConnection.StartComponent, true);
            Point end = PointToClient(Cursor.Position);
            
            using (var pen = new Pen(Color.LightBlue, 2) { DashStyle = DashStyle.Dash })
            {
                g.DrawLine(pen, start, end);
            }
        }

        private Point GetComponentConnectionPoint(ComponentControl component, bool isOutput)
        {
            int x = isOutput ? component.Right : component.Left;
            return new Point(x, component.Top + component.Height / 2);
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
                control.MouseDown += ComponentControl_MouseDown;
                control.MouseMove += ComponentControl_MouseMove;
                control.MouseUp += ComponentControl_MouseUp;

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
        /// 注意事項: 接続の作成を開始または完了する
        /// 作成日: 2025-08-10
        /// </summary>
        private void ComponentControl_PortClicked(object sender, PortEventArgs e)
        {
            var component = (ComponentControl)sender;
            
            if (_currentConnection == null)
            {
                _currentConnection = new Connection
                {
                    StartComponent = component,
                    StartPort = e.PortName,
                    Visual = new ConnectionVisual()
                };
            }
            else
            {
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
        /// 接続を追加する
        /// 注意事項: 重複接続は追加しない
        /// 作成日: 2025-08-10
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
        /// 接続を削除する
        /// 注意事項: 関連コンポーネントからも接続情報を削除する
        /// 作成日: 2025-08-10
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

        private void UpdateConnectionVisual(Connection connection)
        {
            Point start = GetComponentConnectionPoint(connection.StartComponent, true);
            Point end = GetComponentConnectionPoint(connection.EndComponent, false);
            
            connection.Visual.PathPoints = new List<Point> { start, end };
        }

        public void ArrangeComponents()
        {
            LayoutEngine.ArrangeComponents(this);
            UpdateAllConnectionVisuals();
            Invalidate();
        }

        private void UpdateAllConnectionVisuals()
        {
            foreach (var connection in Connections)
            {
                UpdateConnectionVisual(connection);
            }
        }

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

        private void UpdateComponentConnections(ComponentControl component)
        {
            foreach (var connection in ConnectionGraph.GetConnections(component))
            {
                UpdateConnectionVisual(connection);
            }
        }

        private void ComponentControl_MouseUp(object sender, MouseEventArgs e)
        {
            _isDragging = false;
            _draggedComponent = null;
        }

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