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

        private bool _selected;
        private readonly Dictionary<string, Rectangle> _ports = new Dictionary<string, Rectangle>();
        private Point _dragOffset;

        /// <summary>
        /// コンポーネント記述子
        /// 作成日: 2025-08-10
        /// </summary>
        public ComponentDescriptor Descriptor { get; }

        /// <summary>
        /// 関連接続リスト
        /// 作成日: 2025-08-10
        /// </summary>
        public List<Connection> Connections { get; } = new List<Connection>();

        /// <summary>
        /// パラメータ辞書
        /// 作成日: 2025-08-10
        /// </summary>
        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        /// <summary>
        /// インスタンス名
        /// 作成日: 2025-08-10
        /// </summary>
        public string InstanceName { get; set; } = "comp";

        /// <summary>
        /// 選択状態
        /// 注意事項: 設定時にSelectedChangedイベントを発行する
        /// 作成日: 2025-08-10
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
        /// 注意事項: パラメータとポートを初期化する
        /// 作成日: 2025-08-10
        /// </summary>
        public ComponentControl(ComponentDescriptor descriptor)
        {
            // インスタンス名を生成 (例: "VSI_7f3a2b")
            Descriptor = descriptor;
            InitializeComponent();
            InitializeParameters();
            InitializePorts();
        }

        /// <summary>
        /// パラメータを初期化する
        /// 注意事項: デフォルト値でパラメータ辞書を設定する
        /// 作成日: 2025-08-10
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
        /// 注意事項: 入力ポートと出力ポートを設定する
        /// 作成日: 2025-08-10
        /// </summary>
        private void InitializePorts()
        {
            _ports.Add("Input", new Rectangle(0, Height / 2 - 5, 10, 10));
            _ports.Add("Output", new Rectangle(Width - 10, Height / 2 - 5, 10, 10));
        }

        /// <summary>
        /// ポート位置を取得する
        /// 注意事項: 親コントロール上の絶対位置を返す
        /// 作成日: 2025-08-10
        /// </summary>
        public Point GetPortLocation(string portName)
        {
            if (_ports.TryGetValue(portName, out var rect))
            {
                return new Point(Left + rect.X + rect.Width / 2, Top + rect.Y + rect.Height / 2);
            }
            return Location;
        }

        /// <summary>
        /// 描画処理
        /// 注意事項: 選択状態に応じて枠線を描画する
        /// 作成日: 2025-08-10
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.FillRectangle(Brushes.LightGray, ClientRectangle);

            e.Graphics.DrawString(Descriptor.Name, Font, Brushes.Black, 10, 10);

            if (IsSelected)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }

            foreach (var port in _ports)
            {
                e.Graphics.FillEllipse(Brushes.Green, port.Value);
            }
        }

        /// <summary>
        /// マウスダウンイベント処理
        /// 注意事項: ポートクリック時は接続処理、それ以外は選択処理を行う
        /// 作成日: 2025-08-10
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            foreach (var port in _ports)
            {
                if (port.Value.Contains(e.Location))
                {
                    PortClicked?.Invoke(this, new PortEventArgs(port.Key));
                    return;
                }
            }

            IsSelected = true;
            _dragOffset = e.Location;
            BringToFront();
        }

        /// <summary>
        /// マウス移動イベント処理
        /// 注意事項: 左ボタンドラッグでコンポーネントを移動する
        /// 作成日: 2025-08-10
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.Button == MouseButtons.Left && IsSelected)
            {
                int deltaX = e.X - _dragOffset.X;
                int deltaY = e.Y - _dragOffset.Y;

                Left += deltaX;
                Top += deltaY;

                _dragOffset = e.Location;
                Parent.Invalidate();
            }
        }
    }
}