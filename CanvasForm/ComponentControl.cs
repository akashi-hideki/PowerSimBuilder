using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// コンポーネント表示用コントロールクラス
    /// 作成日: 2025-08-12
    /// 注意点: コンポーネントのUI表示とインタラクションを担当
    /// </summary>
    public partial class ComponentControl : UserControl
    {
        // ポート描画サイズとヒットエリアの拡大
        public const int PortRadius = 8; // 描画半径
        public const int HitTestRadius = 12; // ヒットテスト半径

        // コンポーネントの基本サイズ（推奨サイズ）
        public const int BaseWidth = 180;
        public const int BaseHeight = 40; // 基本高さ（ポート以外の部分）
        public const int PortSpacing = 20; // ポート間の間隔

        // マウス位置追跡用フィールド追加
        private string _hoveredPort = null;
        private bool _isInputPortHovered = false;

        // イベント定義
        public event EventHandler SelectedChanged;
        public event EventHandler<PortEventArgs> PortClicked;
        public event EventHandler DoubleClicked;

        private bool _selected;

        /// <summary>
        /// コンポーネント記述子
        /// 注意点: タイプ、パラメータ、ポート情報を含む
        /// </summary>
        public ComponentDescriptor Descriptor { get; }

        /// <summary>
        /// パラメータ辞書
        /// </summary>
        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        /// <summary>
        /// インスタンス名
        /// 注意点: シミュレーションコード内で使用
        /// </summary>
        public string InstanceName { get; set; } = "comp";

        /// <summary>
        /// 選択状態
        /// 注意点: true時は赤枠で表示
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
        /// コンストラクタ
        /// 注意点: ディスクリプタからコンポーネントを初期化
        /// </summary>
        public ComponentControl(ComponentDescriptor descriptor)
        {
            Descriptor = descriptor;
            InitializeComponent();
            InitializeParameters();
            InitializeEvents();
            CalculateOptimalSize();

            // ダブルクリックイベント登録
            this.DoubleClick += (s, e) => DoubleClicked?.Invoke(this, EventArgs.Empty);
        }

        public Rectangle GetInputPortBounds(int index)
        {
            // Port coordinate fix: X matches OnPaint (client PortRadius from left)
            float ratio = (index + 1f) / (Descriptor.InputPorts.Length + 1);
            int yPos = (int)(Height * ratio);
            return new Rectangle(Left + PortRadius - 5, Top + yPos - 5, 10, 10);
        }

        public Rectangle GetOutputPortBounds(int index)
        {
            // Port coordinate fix: X matches OnPaint (client Width - PortRadius from right)
            float ratio = (index + 1f) / (Descriptor.OutputPorts.Length + 1);
            int yPos = (int)(Height * ratio);
            return new Rectangle(Left + Width - PortRadius - 5, Top + yPos - 5, 10, 10);
        }

        /// <summary>
        /// ポート初期化（ダミー）
        /// 注意点: デザイナー互換用
        /// </summary>
        private void InitializeParameters()
        {
            foreach (var param in Descriptor.Parameters)
            {
                Parameters[param.Name] = param.DefaultValue;
            }
        }

        /// <summary>
        /// イベントハンドラを初期化
        /// 注意点: マウスイベントを登録
        /// </summary>
        private void InitializeEvents()
        {
            this.MouseDown += ComponentControl_MouseDown;
        }

        /// <summary>
        /// パラメータを初期化
        /// 注意点: ディスクリプタのデフォルト値を設定
        /// </summary>
        public void InitializePortsDummy()
        {
            // 座標非依存アーキテクチャでは不要
        }

        /// <summary>
        /// 最適なサイズを計算
        /// </summary>
        private void CalculateOptimalSize()
        {
            // レイアウトを一時停止
            this.SuspendLayout();

            try
            {
                int maxPorts = Math.Max(
                    Descriptor.InputPorts?.Length ?? 0,
                    Descriptor.OutputPorts?.Length ?? 0
                );

                // 基本高さ + ポート数に応じた追加高さ
                int calculatedHeight = BaseHeight + (maxPorts * PortSpacing);

                // 最小高さを確保（少なくとも100ピクセル）
                this.Size = new Size(BaseWidth, Math.Max(calculatedHeight, 100));
            }
            finally
            {
                // レイアウトを再開
                this.ResumeLayout(true);
                this.PerformLayout();
            }
        }
        /// <summary>
        /// 円内にポイントがあるか判定
        /// </summary>
        private bool IsPointInCircle(Point point, Point center, int radius)
        {
            int dx = point.X - center.X;
            int dy = point.Y - center.Y;
            return (dx * dx + dy * dy) <= (radius * radius);
        }

        /// <summary>
        /// マウスダウンイベント処理
        /// </summary>
        private void ComponentControl_MouseDown(object sender, MouseEventArgs e)
        {
            this.Focus();
            // Right-click: only update selection, never start port/connection operations.
            // Ensures IsSelected is set even when the click lands on a port, so the
            // context menu Delete always targets the correct component.
            if (e.Button != MouseButtons.Left)
            {
                IsSelected = true;
                BringToFront();
                return;
            }
            bool portClicked = false;

            // 入力ポートチェック
            if (Descriptor.InputPorts != null)
            {
                for (int i = 0; i < Descriptor.InputPorts.Length; i++)
                {
                    // Port coordinate fix
                    float ratio = (i + 1f) / (Descriptor.InputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);
                    var portCenter = new Point(PortRadius, yPos);

                    if (IsPointInCircle(e.Location, portCenter, HitTestRadius))
                    {
                        PortClicked?.Invoke(this, new PortEventArgs(Descriptor.InputPorts[i]));
                        portClicked = true;
                        break;
                    }
                }
            }

            // 出力ポートチェック
            if (!portClicked && Descriptor.OutputPorts != null)
            {
                for (int i = 0; i < Descriptor.OutputPorts.Length; i++)
                {
                    // Port coordinate fix
                    float ratio = (i + 1f) / (Descriptor.OutputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);
                    var portCenter = new Point(Width - PortRadius * 2, yPos);

                    if (IsPointInCircle(e.Location, portCenter, HitTestRadius))
                    {
                        PortClicked?.Invoke(this, new PortEventArgs(Descriptor.OutputPorts[i]));
                        portClicked = true;
                        break;
                    }
                }
            }

            // ポートがクリックされなかった場合のみ選択状態を更新
            if (!portClicked)
            {
                IsSelected = true;
                BringToFront();
            }
        }

        /// <summary>
        /// マウス移動イベント処理
        /// </summary>
        public void HandleMouseMove(MouseEventArgs e)
        {
            bool portHovered = false;

            // 入力ポートホバーチェック
            if (Descriptor.InputPorts != null)
            {
                for (int i = 0; i < Descriptor.InputPorts.Length; i++)
                {
                    // Port coordinate fix
                    float ratio = (i + 1f) / (Descriptor.InputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);
                    var portCenter = new Point(PortRadius, yPos);

                    if (IsPointInCircle(e.Location, portCenter, HitTestRadius))
                    {
                        _hoveredPort = Descriptor.InputPorts[i];
                        _isInputPortHovered = true;
                        portHovered = true;
                        Cursor = Cursors.Hand;
                        break;
                    }
                }
            }

            // 出力ポートホバーチェック
            if (!portHovered && Descriptor.OutputPorts != null)
            {
                for (int i = 0; i < Descriptor.OutputPorts.Length; i++)
                {
                    // Port coordinate fix
                    float ratio = (i + 1f) / (Descriptor.OutputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);
                    var portCenter = new Point(Width - PortRadius * 2, yPos);

                    if (IsPointInCircle(e.Location, portCenter, HitTestRadius))
                    {
                        _hoveredPort = Descriptor.OutputPorts[i];
                        _isInputPortHovered = false;
                        portHovered = true;
                        Cursor = Cursors.Hand;
                        break;
                    }
                }
            }

            // ポート上でない場合
            if (!portHovered)
            {
                _hoveredPort = null;
                Cursor = Cursors.Default;
            }

            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            HandleMouseMove(e);
        }

        /// <summary>
        /// 描画処理
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.FillRectangle(Brushes.LightGray, ClientRectangle);

            // インスタンス名を描画
            using (var font = new Font("Arial", 8))
            {
                e.Graphics.DrawString(InstanceName, font, Brushes.Black, 10, 10);
            }

            // 選択状態を描画
            if (IsSelected)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }

            // 入力ポート描画
            if (Descriptor.InputPorts != null)
            {
                for (int i = 0; i < Descriptor.InputPorts.Length; i++)
                {
                    // Port coordinate fix: ratio formula (consistent with CheckPortClick)
                    float ratio = (i + 1f) / (Descriptor.InputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);
                    var portCenter = new Point(PortRadius, yPos);

                    bool isHovered = (_hoveredPort == Descriptor.InputPorts[i] && _isInputPortHovered);
                    using (var brush = new SolidBrush(isHovered ? Color.Cyan : Color.Blue))
                    {
                        e.Graphics.FillEllipse(brush,
                            portCenter.X - PortRadius,
                            portCenter.Y - PortRadius,
                            PortRadius * 2,
                            PortRadius * 2);
                    }
                    e.Graphics.DrawEllipse(Pens.Black,
                        portCenter.X - PortRadius,
                        portCenter.Y - PortRadius,
                        PortRadius * 2,
                        PortRadius * 2);

                    // ポート名を描画（省略表示）
                    string displayName = Descriptor.InputPorts[i];
                    if (displayName.Length > 12)
                    {
                        displayName = displayName.Substring(0, 10) + "..";
                    }
                    e.Graphics.DrawString(displayName, Font, Brushes.Black, 20, yPos - 7);
                }
            }

            // 出力ポート描画
            if (Descriptor.OutputPorts != null)
            {
                for (int i = 0; i < Descriptor.OutputPorts.Length; i++)
                {
                    // Port coordinate fix
                    float ratio = (i + 1f) / (Descriptor.OutputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);
                    var portCenter = new Point(Width - PortRadius * 2, yPos);

                    bool isHovered = (_hoveredPort == Descriptor.OutputPorts[i] && !_isInputPortHovered);
                    using (var brush = new SolidBrush(isHovered ? Color.Cyan : Color.Green))
                    {
                        e.Graphics.FillEllipse(brush,
                            portCenter.X - PortRadius,
                            portCenter.Y - PortRadius,
                            PortRadius * 2,
                            PortRadius * 2);
                    }
                    e.Graphics.DrawEllipse(Pens.Black,
                        portCenter.X - PortRadius,
                        portCenter.Y - PortRadius,
                        PortRadius * 2,
                        PortRadius * 2);

                    // ポート名を描画（省略表示）
                    string displayName = Descriptor.OutputPorts[i];
                    if (displayName.Length > 12)
                    {
                        displayName = displayName.Substring(0, 10) + "..";
                    }

                    // テキストの幅を計算して右側に配置
                    SizeF textSize = e.Graphics.MeasureString(displayName, Font);
                    e.Graphics.DrawString(displayName, Font, Brushes.Black,
                        Width - textSize.Width - 30, yPos - 7);
                }
            }
        }
    }
}