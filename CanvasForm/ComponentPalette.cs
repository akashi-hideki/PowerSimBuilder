using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class ComponentPalette : FlowLayoutPanel
    {
        /// <summary>
        /// 利用可能なコンポーネントリスト
        /// 作成日: 2025-08-10
        /// </summary>
        public List<ComponentDescriptor> AvailableComponents { get; } = new List<ComponentDescriptor>();

        /// <summary>
        /// 選択中のコンポーネント
        /// 作成日: 2025-08-10
        /// </summary>
        public ComponentDescriptor SelectedComponent { get; private set; }

        /// <summary>
        /// コンポーネントパレットのコンストラクタ
        /// 注意事項: 初期コンポーネントを追加する
        /// 作成日: 2025-08-10
        /// </summary>
        public ComponentPalette()
        {
            InitializeComponent();
            InitializeComponentPalette();
        }

        /// <summary>
        /// コンポーネントパレットの初期化
        /// 作成日: 2025-08-10
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScroll = true;
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.ResumeLayout(false);
        }

        /// <summary>
        /// コンポーネントパレットの初期化
        /// 注意事項: デフォルトコンポーネントを追加する
        /// 作成日: 2025-08-10
        /// </summary>
        private void InitializeComponentPalette()
        {
            // 初期コンポーネントの追加
            AddDefaultComponents();
        }

        /// <summary>
        /// デフォルトコンポーネントを追加する
        /// 作成日: 2025-08-10
        /// </summary>
        private void AddDefaultComponents()
        {
            // デフォルトコンポーネントの追加
            AddComponent(new ComponentDescriptor
            {
                Name = "電圧形インバータ",
                Type = "VoltageSourceInverter",
                Icon = CreateIcon(Color.Red),
                Category = "電源",
                Parameters = new List<ParameterDescriptor>
                {
                    new ParameterDescriptor { Name = "V_dc", Type = "double", DefaultValue = 300 },
                    new ParameterDescriptor { Name = "f_rated", Type = "double", DefaultValue = 60 }
                }
            });

            AddComponent(new ComponentDescriptor
            {
                Name = "誘導電動機",
                Type = "InductionMotor",
                Icon = CreateIcon(Color.Blue),
                Category = "負荷",
                Parameters = new List<ParameterDescriptor>
                {
                    new ParameterDescriptor { Name = "R1", Type = "double", DefaultValue = 0.15 },
                    new ParameterDescriptor { Name = "Lm", Type = "double", DefaultValue = 0.008 }
                }
            });
        }

        /// <summary>
        /// コンポーネントを追加する
        /// 注意事項: 重複追加を防止する
        /// 作成日: 2025-08-10
        /// </summary>
        public void AddComponent(ComponentDescriptor component)
        {
            if (!AvailableComponents.Contains(component))
            {
                AvailableComponents.Add(component);
                CreateComponentButton(component);
            }
        }

        /// <summary>
        /// コンポーネントボタンを作成する
        /// 注意事項: ドラッグ＆ドロップと選択イベントを設定する
        /// 作成日: 2025-08-10
        /// </summary>
        private void CreateComponentButton(ComponentDescriptor component)
        {
            var button = new Button
            {
                Text = component.Name,
                Image = component.Icon,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleRight,
                Tag = component,
                Height = 40,
                Width = 180,
                Margin = new Padding(5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.WhiteSmoke
            };

            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.LightGray;

            button.MouseDown += ComponentButton_MouseDown;
            button.Click += ComponentButton_Click;

            this.Controls.Add(button);
        }

        /// <summary>
        /// アイコン画像を作成する
        /// 作成日: 2025-08-10
        /// </summary>
        private Bitmap CreateIcon(Color color)
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(new SolidBrush(color), 0, 0, 32, 32);
                g.DrawRectangle(Pens.Black, 0, 0, 31, 31);
                g.DrawString("C", new Font("Arial", 10, FontStyle.Bold),
                            Brushes.White, new PointF(8, 8));
            }
            return bmp;
        }

        /// <summary>
        /// コンポーネントボタンマウスダウンイベント
        /// 注意事項: ドラッグ操作を開始する
        /// 作成日: 2025-08-10
        /// </summary>
        private void ComponentButton_MouseDown(object sender, MouseEventArgs e)
        {
            var button = (Button)sender;
            var component = (ComponentDescriptor)button.Tag;

            var data = new DataObject();
            data.SetData("ComponentDescriptor", component);

            button.DoDragDrop(data, DragDropEffects.Copy);
        }

        /// <summary>
        /// コンポーネントボタンクリックイベント
        /// 注意事項: 選択状態を更新しイベントを発行する
        /// 作成日: 2025-08-10
        /// </summary>
        private void ComponentButton_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            SelectedComponent = (ComponentDescriptor)button.Tag;

            // 選択状態を通知
            OnComponentSelected(EventArgs.Empty);

            // 選択されたボタンを強調表示
            foreach (Control control in Controls)
            {
                if (control is Button btn)
                {
                    btn.BackColor = (btn == button) ? Color.LightBlue : Color.WhiteSmoke;
                }
            }
        }

        /// <summary>
        /// コンポーネント選択イベント
        /// 作成日: 2025-08-10
        /// </summary>
        public event EventHandler ComponentSelected;

        /// <summary>
        /// コンポーネント選択イベントを発行する
        /// 作成日: 2025-08-10
        /// </summary>
        protected virtual void OnComponentSelected(EventArgs e)
        {
            ComponentSelected?.Invoke(this, e);
        }
    }

    /// <summary>
    /// コンポーネント記述子クラス
    /// 作成日: 2025-08-10
    /// </summary>
    public class ComponentDescriptor
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Image Icon { get; set; }
        public string Category { get; set; }
        public List<ParameterDescriptor> Parameters { get; set; }

        public override string ToString()
        {
            return $"{Name} ({Type})";
        }
    }

    /// <summary>
    /// パラメータ記述子クラス
    /// 作成日: 2025-08-10
    /// </summary>
    public class ParameterDescriptor
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public object DefaultValue { get; set; }

        public override string ToString()
        {
            return $"{Name}: {DefaultValue} ({Type})";
        }
    }
}