using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PowerSimBuilder.ComponentEdit
{
    public partial class PhysicsMappingForm : Form
    {
        public Dictionary<string, string> PortMapping { get; private set; } = new();
        public Dictionary<string, string> Parameters { get; private set; } = new();

        public PhysicsMappingForm(string modelName, List<string> fmuInputs, List<string> parameters)
        {
            this.Text = $"Configure Physics: {modelName}";
            this.Size = new System.Drawing.Size(600, 400);
            
            // TODO: DataGridView をコードまたはデザイナーで追加し、fmuInputs と parameters を表示する
            var lbl = new Label { Text = "Port Mapping & Parameters configuration UI placeholder.", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleCenter };
            this.Controls.Add(lbl);
            
            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom };
            this.Controls.Add(btnOk);
            this.AcceptButton = btnOk;
        }
    }
}
