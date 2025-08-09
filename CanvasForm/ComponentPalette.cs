using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class ComponentPalette : FlowLayoutPanel
    {
        public List<ComponentDescriptor> AvailableComponents { get; } = new List<ComponentDescriptor>();
        public ComponentDescriptor SelectedComponent { get; private set; }
        
        public ComponentPalette()
        {
            InitializeComponent();
            InitializeComponentPalette();
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScroll = true;
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.ResumeLayout(false);
        }
        
        private void InitializeComponentPalette()
        {
            // 初期コンポーネントの追加
            AddDefaultComponents();
        }
        
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
        
        public void AddComponent(ComponentDescriptor component)
        {
            if (!AvailableComponents.Contains(component))
            {
                AvailableComponents.Add(component);
                CreateComponentButton(component);
            }
        }
        
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