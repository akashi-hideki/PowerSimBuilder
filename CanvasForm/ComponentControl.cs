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

        /// <summary>
        /// コンポーネント記述子
        /// </summary>
        public ComponentDescriptor Descriptor { get; }

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
            InitializeEvents();
        }
        public void InitializePortsDummy()
        {
            // 座標非依存アーキテクチャではデザイナーコード
            // 必要に応じてロジックを追加
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
        /// 描画処理
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.FillRectangle(Brushes.LightGray, ClientRectangle);

            // コンポーネント名の描画
            using (var font = new Font("Arial", 8))
            {
                e.Graphics.DrawString(InstanceName, font, Brushes.Black, 10, 10);
            }

            // 選択状態の枠
            if (IsSelected)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }

            // 入力ポートの描画 (左側)
            if (Descriptor.InputPorts != null)
            {
                for (int i = 0; i < Descriptor.InputPorts.Length; i++)
                {
                    // ratio を計算
                    float ratio = (i + 1f) / (Descriptor.InputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);

                    // ポートの円を描画
                    Rectangle portRect = new Rectangle(5, yPos - 5, 10, 10);
                    e.Graphics.FillEllipse(Brushes.Blue, portRect);
                    e.Graphics.DrawEllipse(Pens.Black, portRect);

                    // ポート名を表示 (デバッグ用)
// #if DEBUG
                    e.Graphics.DrawString(Descriptor.InputPorts[i], Font, Brushes.Black, 20, yPos - 7);
// #endif
                }
            }

            // 出力ポートの描画 (右側)
            if (Descriptor.OutputPorts != null)
            {
                for (int i = 0; i < Descriptor.OutputPorts.Length; i++)
                {
                    // ratio を計算
                    float ratio = (i + 1f) / (Descriptor.OutputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);

                    // ポートの円を描画
                    Rectangle portRect = new Rectangle(Width - 15, yPos - 5, 10, 10);
                    e.Graphics.FillEllipse(Brushes.Green, portRect);
                    e.Graphics.DrawEllipse(Pens.Black, portRect);

                    // ポート名を表示 (デバッグ用)
// #if DEBUG
                    e.Graphics.DrawString(Descriptor.OutputPorts[i], Font, Brushes.Black, Width - 50, yPos - 7);
// #endif
                }
            }
        }
        /// <summary>
        /// マウスダウンイベント処理
        /// </summary>
        // ComponentControl.cs
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            this.Focus();

            // 入力ポートチェック（左側）
            if (Descriptor.InputPorts != null)
            {
                for (int i = 0; i < Descriptor.InputPorts.Length; i++)
                {
                    // ratio を計算
                    float ratio = (i + 1f) / (Descriptor.InputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);

                    // ポート周辺の矩形領域 (描画位置と一致)
                    var portRect = new Rectangle(5, yPos - 5, 10, 10);

                    if (portRect.Contains(e.Location))
                    {
                        // portName を取得
                        string portName = Descriptor.InputPorts[i];
                        PortClicked?.Invoke(this, new PortEventArgs(portName));
                        return;
                    }
                }
            }

            // 出力ポートチェック（右側）
            if (Descriptor.OutputPorts != null)
            {
                for (int i = 0; i < Descriptor.OutputPorts.Length; i++)
                {
                    // ratio を計算
                    float ratio = (i + 1f) / (Descriptor.OutputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);

                    // ポート周辺の矩形領域 (描画位置と一致)
                    var portRect = new Rectangle(Width - 15, yPos - 5, 10, 10);

                    if (portRect.Contains(e.Location))
                    {
                        // portName を取得
                        string portName = Descriptor.OutputPorts[i];
                        PortClicked?.Invoke(this, new PortEventArgs(portName));
                        return;
                    }
                }
            }

            // ポート以外をクリック
            IsSelected = true;
            BringToFront();
        }
 
    }
}