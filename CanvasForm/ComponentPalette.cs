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
        /// </summary>
        public List<ComponentDescriptor> AvailableComponents { get; } = new List<ComponentDescriptor>();

        /// <summary>
        /// 選択中のコンポーネント
        /// </summary>
        public ComponentDescriptor SelectedComponent { get; private set; }

        /// <summary>
        /// コンポーネントパレットのコンストラクタ
        /// </summary>
        public ComponentPalette()
        {
            InitializeComponent();
            InitializeComponentPalette();
        }

        /// <summary>
        /// コンポーネントパレットの初期化
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ComponentPalette
            // 
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.White;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ResumeLayout(false);
        }

        /// <summary>
        /// コンポーネントパレットの初期化
        /// </summary>
        private void InitializeComponentPalette()
        {
            AddDefaultComponents();
        }

        /// <summary>
        /// デフォルトコンポーネントを追加する
        /// </summary>
        private void AddDefaultComponents()
        {
            // 電圧形インバータ
            AddComponent(new ComponentDescriptor
            {
                Name = "電圧形インバータ",
                Type = "VoltageSourceInverter",
                Icon = CreateIcon(new ComponentDescriptor { Type = "VoltageSourceInverter" }, Color.Red),
                Category = "電源",
                Parameters = new List<ParameterDescriptor>
                {
                    new ParameterDescriptor { Name = "V_dc", Type = "double", DefaultValue = 300 },
                    new ParameterDescriptor { Name = "f_rated", Type = "double", DefaultValue = 60 }
                },
                InputPorts = new string[0], // 入力ポートなし
                OutputPorts = new[] { "Output" }
            });

            // 誘導電動機
            AddComponent(new ComponentDescriptor
            {
                Name = "誘導電動機",
                Type = "InductionMotor",
                Icon = CreateIcon(new ComponentDescriptor { Type = "InductionMotor" }, Color.Blue),
                Category = "負荷",
                Parameters = new List<ParameterDescriptor>
                {
                    new ParameterDescriptor { Name = "R1", Type = "double", DefaultValue = 0.15 },
                    new ParameterDescriptor { Name = "R2", Type = "double", DefaultValue = 0.25 },
                    new ParameterDescriptor { Name = "Lm", Type = "double", DefaultValue = 0.008 },
                    new ParameterDescriptor { Name = "J", Type = "double", DefaultValue = 0.02 },
                    new ParameterDescriptor { Name = "B", Type = "double", DefaultValue = 0.005 },
                    new ParameterDescriptor { Name = "poles", Type = "int", DefaultValue = 4 }
                },
                InputPorts = new[] { "VoltageInput" },
                OutputPorts = new[] { "SpeedOutput", "CurrentOutput" }
            });

            // 機械負荷
            AddComponent(new ComponentDescriptor
            {
                Name = "機械負荷",
                Type = "MechanicalLoad",
                Icon = CreateIcon(new ComponentDescriptor { Type = "MechanicalLoad" }, Color.Green),
                Category = "負荷",
                Parameters = new List<ParameterDescriptor>
                {
                    new ParameterDescriptor { Name = "load_type", Type = "string", DefaultValue = "constant" },
                    new ParameterDescriptor { Name = "torque_coeff", Type = "double", DefaultValue = 0.1 }
                },
                InputPorts = new[] { "SpeedInput" },
                OutputPorts = new[] { "TorqueOutput" }
            });

            // 故障監視システム
            AddComponent(new ComponentDescriptor
            {
                Name = "故障監視システム",
                Type = "FaultMonitor",
                Icon = CreateIcon(new ComponentDescriptor { Type = "FaultMonitor" }, Color.Orange),
                Category = "監視",
                Parameters = new List<ParameterDescriptor>
                {
                    new ParameterDescriptor
                    {
                        Name = "Conditions",
                        Type = "List<FaultCondition>",
                        DefaultValue = new List<FaultCondition>()
                    }
                },
                InputPorts = new[] { "Input1", "Input2" },
                OutputPorts = new string[0] // 出力ポートなし
            });
        }

        /// <summary>
        /// コンポーネントを追加する
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
        /// </summary>
        private Bitmap CreateIcon(ComponentDescriptor component, Color color)
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(new SolidBrush(color), 0, 0, 32, 32);
                g.DrawRectangle(Pens.Black, 0, 0, 31, 31);

                string symbol;
                switch (component.Type)
                {
                    case "VoltageSourceInverter":
                        symbol = "V";
                        break;
                    case "InductionMotor":
                        symbol = "M";
                        break;
                    case "MechanicalLoad":
                        symbol = "L";
                        break;
                    case "FaultMonitor":
                        symbol = "F";
                        break;
                    default:
                        symbol = "C";
                        break;
                }

                g.DrawString(symbol, new Font("Arial", 10, FontStyle.Bold),
                            Brushes.White, new PointF(8, 8));
            }
            return bmp;
        }

        /// <summary>
        /// コンポーネントボタンマウスダウンイベント
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
        /// </summary>
        public event EventHandler ComponentSelected;

        /// <summary>
        /// コンポーネント選択イベントを発行する
        /// </summary>
        protected virtual void OnComponentSelected(EventArgs e)
        {
            ComponentSelected?.Invoke(this, e);
        }
    }


}