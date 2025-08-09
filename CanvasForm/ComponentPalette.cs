using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class ComponentPalette : FlowLayoutPanel
    {
        public List<ComponentDescriptor> AvailableComponents { get; } = new List<ComponentDescriptor>();
        public ComponentDescriptor SelectedComponent { get; private set; }  // 選択されたコンポーネント
        
        public ComponentPalette()
        {
            InitializeComponent();
            InitializeComponentPalette();
        }
        
        private void InitializeComponentPalette()
        {
            // 利用可能なコンポーネントを初期化
            AvailableComponents.Add(new ComponentDescriptor
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
            
            AvailableComponents.Add(new ComponentDescriptor
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
            
            // その他のコンポーネント...
            AvailableComponents.Add(new ComponentDescriptor
            {
                Name = "変圧器",
                Type = "Transformer",
                Icon = CreateIcon(Color.Green),
                Category = "変換",
                Parameters = new List<ParameterDescriptor>
                {
                    new ParameterDescriptor { Name = "Ratio", Type = "double", DefaultValue = 1.0 },
                    new ParameterDescriptor { Name = "Impedance", Type = "double", DefaultValue = 0.05 }
                }
            });
            
            AvailableComponents.Add(new ComponentDescriptor
            {
                Name = "太陽光発電",
                Type = "SolarPanel",
                Icon = CreateIcon(Color.Yellow),
                Category = "再生可能",
                Parameters = new List<ParameterDescriptor>
                {
                    new ParameterDescriptor { Name = "Capacity", Type = "double", DefaultValue = 1000 },
                    new ParameterDescriptor { Name = "Efficiency", Type = "double", DefaultValue = 0.18 }
                }
            });
            
            // パレットにボタンを追加
            foreach (var component in AvailableComponents)
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
                Controls.Add(button);
            }
        }
        
        private Bitmap CreateIcon(Color color)
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(new SolidBrush(color), 0, 0, 32, 32);
                g.DrawRectangle(Pens.Black, 0, 0, 31, 31);
                
                // アイコンに識別マークを追加
                g.DrawString("C", new Font("Arial", 10, FontStyle.Bold), 
                             Brushes.White, new PointF(8, 8));
            }
            return bmp;
        }
        
        private void ComponentButton_MouseDown(object sender, MouseEventArgs e)
        {
            var button = (Button)sender;
            var component = (ComponentDescriptor)button.Tag;
            
            var data = new DataObject();
            data.SetData("ComponentDescriptor", component);
            
            button.DoDragDrop(data, DragDropEffects.Copy);
        }
        
        private void ComponentButton_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            SelectedComponent = (ComponentDescriptor)button.Tag;
            
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
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
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
#if false
        // デザイナーメソッド
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScroll = true;
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.ResumeLayout(false);
        }
#endif
        
    }
    
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