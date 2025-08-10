using System;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class AddParameterForm : Form
    {
        /// <summary>
        /// パラメータ名
        /// 作成日: 2025-08-10
        /// </summary>
        public string ParamName { get; private set; }
        
        /// <summary>
        /// パラメータ型
        /// 作成日: 2025-08-10
        /// </summary>
        public string ParamType { get; private set; }
        
        /// <summary>
        /// デフォルト値
        /// 作成日: 2025-08-10
        /// </summary>
        public object DefaultValue { get; private set; }
        
        /// <summary>
        /// パラメータ追加フォームのコンストラクタ
        /// 作成日: 2025-08-10
        /// </summary>
        public AddParameterForm()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// OKボタンクリックイベント
        /// 注意事項: 入力内容を検証しパラメータを設定する
        /// 作成日: 2025-08-10
        /// </summary>
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