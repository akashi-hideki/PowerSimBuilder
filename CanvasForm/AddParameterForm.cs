using System;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class AddParameterForm : Form
    {
        public string ParamName { get; private set; }
        public string ParamType { get; private set; }
        public object DefaultValue { get; private set; }
        
        private TextBox txtName;
        private ComboBox cmbType;
        private TextBox txtDefault;
        
        public AddParameterForm()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(300, 200);
            this.Text = "パラメータの追加";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // コントロールの作成
            var lblName = new Label { Text = "名前:", Location = new Point(20, 20), Size = new Size(80, 20) };
            txtName = new TextBox { Location = new Point(110, 20), Size = new Size(150, 20) };
            
            var lblType = new Label { Text = "タイプ:", Location = new Point(20, 50), Size = new Size(80, 20) };
            cmbType = new ComboBox { Location = new Point(110, 50), Size = new Size(150, 20) };
            cmbType.Items.AddRange(new object[] { "double", "int", "string", "bool" });
            cmbType.SelectedIndex = 0;
            
            var lblDefault = new Label { Text = "デフォルト値:", Location = new Point(20, 80), Size = new Size(80, 20) };
            txtDefault = new TextBox { Location = new Point(110, 80), Size = new Size(150, 20) };
            
            var btnOK = new Button { 
                Text = "OK", 
                DialogResult = DialogResult.OK, 
                Location = new Point(80, 120),
                Size = new Size(80, 30)
            };
            
            var btnCancel = new Button { 
                Text = "キャンセル", 
                DialogResult = DialogResult.Cancel, 
                Location = new Point(180, 120),
                Size = new Size(80, 30)
            };
            
            // イベントハンドラ
            btnOK.Click += BtnOK_Click;
            
            // コントロール追加
            this.Controls.AddRange(new Control[] {
                lblName, txtName,
                lblType, cmbType,
                lblDefault, txtDefault,
                btnOK, btnCancel
            });
            
            this.ResumeLayout(true);
        }
        
        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("パラメータ名は必須です");
                this.DialogResult = DialogResult.None;
                return;
            }
            
            ParamName = txtName.Text;
            ParamType = cmbType.SelectedItem.ToString();
            
            try
            {
                switch (ParamType)
                {
                    case "double":
                        DefaultValue = double.Parse(txtDefault.Text);
                        break;
                    case "int":
                        DefaultValue = int.Parse(txtDefault.Text);
                        break;
                    case "bool":
                        DefaultValue = bool.Parse(txtDefault.Text);
                        break;
                    default: // string
                        DefaultValue = txtDefault.Text;
                        break;
                }
            }
            catch
            {
                MessageBox.Show("デフォルト値が型に一致しません");
                this.DialogResult = DialogResult.None;
            }
        }
    }
}