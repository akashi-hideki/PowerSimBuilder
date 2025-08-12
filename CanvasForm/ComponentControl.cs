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
        /// 描画処理
        /// 注意点: 選択状態、ポート位置を可視化
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.FillRectangle(Brushes.LightGray, ClientRectangle);

            // コンポーネント名表示
            using (var font = new Font("Arial", 8))
            {
                e.Graphics.DrawString(InstanceName, font, Brushes.Black, 10, 10);
            }

            // 選択状態の枠表示
            if (IsSelected)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }

            // 入力ポート描画 (左側)
            if (Descriptor.InputPorts != null)
            {
                for (int i = 0; i < Descriptor.InputPorts.Length; i++)
                {
                    float ratio = (i + 1f) / (Descriptor.InputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);

                    // ポート円描画
                    Rectangle portRect = new Rectangle(5, yPos - 5, 10, 10);
                    e.Graphics.FillEllipse(Brushes.Blue, portRect);
                    e.Graphics.DrawEllipse(Pens.Black, portRect);

                    // ポート名表示 (デバッグ時のみ)
                    e.Graphics.DrawString(Descriptor.InputPorts[i], Font, Brushes.Black, 20, yPos - 7);
                }
            }

            // 出力ポート描画 (右側)
            if (Descriptor.OutputPorts != null)
            {
                for (int i = 0; i < Descriptor.OutputPorts.Length; i++)
                {
                    float ratio = (i + 1f) / (Descriptor.OutputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);

                    // ポート円描画
                    Rectangle portRect = new Rectangle(Width - 15, yPos - 5, 10, 10);
                    e.Graphics.FillEllipse(Brushes.Green, portRect);
                    e.Graphics.DrawEllipse(Pens.Black, portRect);

                    // ポート名表示 (デバッグ時のみ)
                    e.Graphics.DrawString(Descriptor.OutputPorts[i], Font, Brushes.Black, Width - 50, yPos - 7);
                }
            }
        }
        
        /// <summary>
        /// マウスダウンイベント処理
        /// 注意点: ポートクリック検出と選択状態更新
        /// </summary>
        private void ComponentControl_MouseDown(object sender, MouseEventArgs e)
        {
            this.Focus();  // フォーカスを取得
            
            // 入力ポートチェック（左側）
            if (Descriptor.InputPorts != null)
            {
                for (int i = 0; i < Descriptor.InputPorts.Length; i++)
                {
                    float ratio = (i + 1f) / (Descriptor.InputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);
                    var portRect = new Rectangle(5, yPos - 5, 10, 10);

                    if (portRect.Contains(e.Location))
                    {
                        PortClicked?.Invoke(this, new PortEventArgs(Descriptor.InputPorts[i]));
                        return;
                    }
                }
            }

            // 出力ポートチェック（右側）
            if (Descriptor.OutputPorts != null)
            {
                for (int i = 0; i < Descriptor.OutputPorts.Length; i++)
                {
                    float ratio = (i + 1f) / (Descriptor.OutputPorts.Length + 1);
                    int yPos = (int)(Height * ratio);
                    var portRect = new Rectangle(Width - 15, yPos - 5, 10, 10);

                    if (portRect.Contains(e.Location))
                    {
                        PortClicked?.Invoke(this, new PortEventArgs(Descriptor.OutputPorts[i]));
                        return;
                    }
                }
            }

            // ポート以外をクリック: 選択状態更新
            IsSelected = true;
            BringToFront();
        }
    }
}