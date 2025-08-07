using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class ComponentPalette : FlowLayoutPanel
    {
        public List<ComponentDescriptor> AvailableComponents { get; } = new List<ComponentDescriptor>();
        
        public ComponentPalette()
        {
            InitializeComponent();
            InitializeAvailableComponents();
        }
        
        private void InitializeAvailableComponents()
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
                    Margin = new Padding(5)
                };
                
                button.MouseDown += ComponentButton_MouseDown;
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
    }
    
    public class ComponentDescriptor
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Image Icon { get; set; }
        public string Category { get; set; }
        public List<ParameterDescriptor> Parameters { get; set; }
    }
    
    public class ParameterDescriptor
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public object DefaultValue { get; set; }
    }
}