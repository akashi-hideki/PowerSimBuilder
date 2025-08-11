using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class ComponentControl : UserControl
    {
        public event EventHandler SelectedChanged;
        public event EventHandler<PortEventArgs> PortClicked;
        public event EventHandler DoubleClicked;

        private bool _selected;
        private readonly Dictionary<string, Rectangle> _ports = new Dictionary<string, Rectangle>();
        private Point _dragOffset;

        /// <summary>
        /// コンポーネント記述子
        /// </summary>
        public ComponentDescriptor Descriptor { get; }

        /// <summary>
        /// 関連接続リスト
        /// </summary>
        public List<Connection> Connections { get; } = new List<Connection>();

        /// <summary>
        /// パラメータ辞書
        /// </summary>
        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        /// <summary>
        /// インスタンス名
        /// </summary>
        public string InstanceName { get; set; } = "comp";

        /// <summary>
        /// 選択状態
        /// </summary>
        public bool IsSelected
        {
            get => _selected;
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    Invalidate();
                    SelectedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// コンポーネントコントロールのコンストラクタ
        /// </summary>
        public ComponentControl(ComponentDescriptor descriptor)
        {
            Descriptor = descriptor;
            InitializeComponent();
            InitializeParameters();
            InitializePorts();
            InitializeEvents();
        }

        private void InitializeComponentParts()
        {
            this.SuspendLayout();
            // 
            // ComponentControl
            // 
            this.BackColor = System.Drawing.Color.LightGray;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Size = new System.Drawing.Size(100, 70);
            this.ResumeLayout(false);
        }

        /// <summary>
        /// イベントハンドラを初期化
        /// </summary>
        private void InitializeEvents()
        {
            this.DoubleClick += (s, e) => DoubleClicked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// パラメータを初期化する
        /// </summary>
        private void InitializeParameters()
        {
            foreach (var param in Descriptor.Parameters)
            {
                Parameters[param.Name] = param.DefaultValue;
            }
        }

        /// <summary>
        /// ポートを初期化する
        /// </summary>
        public  void InitializePorts()
        {
            _ports.Clear();
            int portSize = 10;

            // 入力ポートを左側に配置
            if (Descriptor.InputPorts != null)
            {
                for (int i = 0; i < Descriptor.InputPorts.Length; i++)
                {
                    string portName = Descriptor.InputPorts[i];
                    int yPos = (i + 1) * Height / (Descriptor.InputPorts.Length + 1);
                    _ports[portName] = new Rectangle(0, yPos - portSize / 2, portSize, portSize);
                }
            }

            // 出力ポートを右側に配置
            if (Descriptor.OutputPorts != null)
            {
                for (int i = 0; i < Descriptor.OutputPorts.Length; i++)
                {
                    string portName = Descriptor.OutputPorts[i];
                    int yPos = (i + 1) * Height / (Descriptor.OutputPorts.Length + 1);
                    _ports[portName] = new Rectangle(Width - portSize, yPos - portSize / 2, portSize, portSize);
                }
            }
        }

        /// <summary>
        /// ポート位置を取得する
        /// </summary>
        public Point GetPortLocation(string portName)
        {
            if (_ports.TryGetValue(portName, out var rect))
            {
                return PointToScreen(new Point(
                    rect.X + rect.Width / 2,
                    rect.Y + rect.Height / 2));
            }
            return PointToScreen(Location);
        }

        /// <summary>
        /// 描画処理
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // 背景を描画
            e.Graphics.FillRectangle(Brushes.LightGray, ClientRectangle);

            // コンポーネント名を描画
            using (var font = new Font("Arial", 8))
            {
                e.Graphics.DrawString(InstanceName, font, Brushes.Black, 10, 10);
            }

            // 選択状態の枠を描画
            if (IsSelected)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }

            // ポートを描画
            foreach (var port in _ports)
            {
                // ポートタイプ判定部分の修正
                if (port.Key.IndexOf("Input", StringComparison.Ordinal) >= 0)
                {
                    e.Graphics.FillEllipse(Brushes.Blue, port.Value);
                }
                else if (port.Key.IndexOf("Output", StringComparison.Ordinal) >= 0)
                {
                    e.Graphics.FillEllipse(Brushes.Green, port.Value);
                }

                // ポート名を描画（デバッグ用）
                // e.Graphics.DrawString(port.Key, Font, Brushes.Black, port.Value.X, port.Value.Y);
            }
        }

        /// <summary>
        /// サイズ変更時の処理
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            InitializePorts();
            Invalidate();
        }

        /// <summary>
        /// マウスダウンイベント処理
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            this.Focus();

            // ポートクリックチェック
            foreach (var port in _ports)
            {
                if (port.Value.Contains(e.Location))
                {
                    PortClicked?.Invoke(this, new PortEventArgs(port.Key));
                    return;
                }
            }

            // ポート以外をクリックした場合は選択状態を更新
            IsSelected = true;
            _dragOffset = e.Location;
            BringToFront();
        }

        /// <summary>
        /// マウス移動イベント処理
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.Button == MouseButtons.Left && IsSelected)
            {
                // ドラッグ中はカーソルを移動カーソルに変更
                Cursor = Cursors.SizeAll;

                int deltaX = e.X - _dragOffset.X;
                int deltaY = e.Y - _dragOffset.Y;

                Left += deltaX;
                Top += deltaY;

                _dragOffset = e.Location;
                Parent.Invalidate();
            }
            else
            {
                // ポート上にカーソルがある場合は手のアイコンに変更
                bool overPort = false;
                foreach (var port in _ports)
                {
                    if (port.Value.Contains(e.Location))
                    {
                        overPort = true;
                        break;
                    }
                }
                Cursor = overPort ? Cursors.Hand : Cursors.Default;
            }
        }

        /// <summary>
        /// マウスアップイベント処理
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            Cursor = Cursors.Default;
        }
    }
}