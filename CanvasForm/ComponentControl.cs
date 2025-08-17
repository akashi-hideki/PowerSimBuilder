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
        private const int PortRadius = 8; // 描画半径
        private const int HitTestRadius = 12; // ヒットテスト半径

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
        /// 注意点: キー=パラメータ名, 値=パラメータ値
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
                    Invalidate();  // 再描画をトリガー
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

            // ダブルクリックイベント登録
            this.DoubleClick += (s, e) => DoubleClicked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// ポート初期化（ダミー）
        /// 注意点: デザイナー互換用
        /// </summary>
        public void InitializePortsDummy()
        {
            // 座標非依存アーキテクチャでは不要
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
        private void InitializeParameters()
        {
            foreach (var param in Descriptor.Parameters)
            {
                Parameters[param.Name] = param.DefaultValue;
            }
        }


        /// <summary>
        /// マウスダウンイベント処理
        /// 注意点: ポートクリック検出と選択状態更新
        /// </summary>
        // ComponentControl.cs
        private void ComponentControl_MouseDown(object sender, MouseEventArgs e)
        {
            this.Focus();
            bool portClicked = false;

            // 入力ポートチェック
            if (Descriptor.InputPorts != null)
            {
                for (int i = 0; i < Descriptor.InputPorts.Length; i++)
                {
                    float ratio = (i + 1f) / (Descriptor.InputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);
                    var portCenter = new Point(5 + PortRadius, yPos);

                    if (IsPointInCircle(e.Location, portCenter, HitTestRadius))
                    {
                        PortClicked?.Invoke(this, new PortEventArgs(Descriptor.InputPorts[i]));
                        portClicked = true;
                        break; // ループから抜けるが処理は続行
                    }
                }
            }

            // 出力ポートチェック（入力ポート未クリック時のみ）
            if (!portClicked && Descriptor.OutputPorts != null)
            {
                for (int i = 0; i < Descriptor.OutputPorts.Length; i++)
                {
                    float ratio = (i + 1f) / (Descriptor.OutputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);
                    var portCenter = new Point(Width - 15 + PortRadius, yPos);

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
        /// マウス移動
        /// クリック有効時　色変更
        /// </summary>
        /// 有効範囲判定
        /// </summary>
        private bool IsPointInCircle(Point point, Point center, int radius)
        {
            int dx = point.X - center.X;
            int dy = point.Y - center.Y;
            return (dx * dx + dy * dy) <= (radius * radius);
        }
        /// <summary>
        /// マウス移動イベント処理（アクセス修飾子をpublicに変更）
        /// 注意点: DesignCanvasから直接呼び出すためpublicが必要
        /// </summary>
        public void HandleMouseMove(MouseEventArgs e)
        {
            bool portHovered = false;

            // 入力ポートホバーチェック
            if (Descriptor.InputPorts != null)
            {
                for (int i = 0; i < Descriptor.InputPorts.Length; i++)
                {
                    float ratio = (i + 1f) / (Descriptor.InputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);
                    var portCenter = new Point(5 + PortRadius, yPos);

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
                    float ratio = (i + 1f) / (Descriptor.OutputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);
                    var portCenter = new Point(Width - 15 + PortRadius, yPos);

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

        /// <summary>
        /// 標準のマウス移動イベント処理
        /// 注意点: 内部でカスタム処理を呼び出す
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            HandleMouseMove(e);
        }
        /// <summary>
        /// 描画処理
        /// 注意点: 選択状態、ポート位置を可視化
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.FillRectangle(Brushes.LightGray, ClientRectangle);

            using (var font = new Font("Arial", 8))
            {
                e.Graphics.DrawString(InstanceName, font, Brushes.Black, 10, 10);
            }

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
                    float ratio = (i + 1f) / (Descriptor.InputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);
                    var portCenter = new Point(5 + PortRadius, yPos);

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

                    e.Graphics.DrawString(Descriptor.InputPorts[i], Font, Brushes.Black, 20, yPos - 7);
                }
            }

            // 出力ポート描画
            if (Descriptor.OutputPorts != null)
            {
                for (int i = 0; i < Descriptor.OutputPorts.Length; i++)
                {
                    float ratio = (i + 1f) / (Descriptor.OutputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);
                    var portCenter = new Point(Width - 15 + PortRadius, yPos);

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

                    e.Graphics.DrawString(Descriptor.OutputPorts[i], Font, Brushes.Black, Width - 50, yPos - 7);
                }
            }
        }
    }

}