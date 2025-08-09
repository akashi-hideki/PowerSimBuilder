using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class AddComponentForm : Form
    {
        public ComponentDescriptor NewComponent { get; private set; }
        private List<ParameterDescriptor> _parameters = new List<ParameterDescriptor>();

        public AddComponentForm()
        {
            InitializeComponent();
        }

        private void BtnAddParam_Click(object sender, EventArgs e)
        {
            using (var paramForm = new AddParameterForm())
            {
                if (paramForm.ShowDialog() == DialogResult.OK)
                {
                    var param = new ParameterDescriptor {
                        Name = paramForm.ParamName,
                        Type = paramForm.ParamType,
                        DefaultValue = paramForm.DefaultValue
                    };
                    
                    _parameters.Add(param);
                    paramListBox.Items.Add($"{param.Name} ({param.Type}) = {param.DefaultValue}");
                }
            }
        }
        
        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtType.Text))
            {
                MessageBox.Show("名前とタイプは必須です");
                DialogResult = DialogResult.None;
                return;
            }
            
            NewComponent = new ComponentDescriptor {
                Name = txtName.Text,
                Type = txtType.Text,
                Category = txtCategory.Text,
                Parameters = new List<ParameterDescriptor>(_parameters),
                Icon = GenerateIcon(txtType.Text)
            };
        }
        
        private Bitmap GenerateIcon(string type)
        {
            var color = Color.FromArgb(
                (type.GetHashCode() * 17) % 256,
                (type.GetHashCode() * 31) % 256,
                (type.GetHashCode() * 53) % 256);
            
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(new SolidBrush(color), 0, 0, 32, 32);
                g.DrawRectangle(Pens.Black, 0, 0, 31, 31);
                g.DrawString(type[0].ToString(), 
                            new Font("Arial", 10, FontStyle.Bold), 
                            Brushes.White, new PointF(10, 8));
            }
            return bmp;
        }
    }
}