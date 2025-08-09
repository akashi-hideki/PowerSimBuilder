using System;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class AddParameterForm : Form
    {
        public string ParamName { get; private set; }
        public string ParamType { get; private set; }
        public object DefaultValue { get; private set; }

        public AddParameterForm()
        {
            InitializeComponent();
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