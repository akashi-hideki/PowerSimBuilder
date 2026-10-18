using System;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// パラメータ追加フォーム
    /// 作成日: 2025-08-12
    /// 注意点: コンポーネントパラメータを定義
    /// </summary>
    public partial class AddParameterForm : Form
    {
        /// <summary>
        /// パラメータ名
        /// 注意点: ユーザー入力値
        /// </summary>
        public string ParamName { get; private set; }

        /// <summary>
        /// パラメータ型
        /// 注意点: コンボボックスから選択
        /// </summary>
        public string ParamType { get; private set; }

        /// <summary>
        /// デフォルト値
        /// 注意点: 型に応じた変換が必要
        /// </summary>
        public object DefaultValue { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// 注意点: フォームコントロールを初期化
        /// </summary>
        public AddParameterForm()
        {
            InitializeComponent();
            Text = I18n.T("AddParameterForm.TitleNew");
            cmbType.SelectedIndex = 0; // デフォルトで最初の項目を選択
        }

        /// <summary>
        public AddParameterForm(ParameterDescriptor param) : this()
        {
            Text = I18n.T("AddParameterForm.TitleEdit");
            btnOK.Text = I18n.T("Common.BtnUpdate");

            // 既存データを設定
            txtName.Text = param.Name;
            cmbType.Text = param.Type;
            txtDefaultValue.Text = param.DefaultValue?.ToString();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show(I18n.T("AddParameterForm.MsgNameRequired"));
                this.DialogResult = DialogResult.None;
                return;
            }

            ParamName = txtName.Text;
            ParamType = cmbType.Text;

            // デフォルト値の変換
            try
            {
                DefaultValue = ConvertValue(txtDefaultValue.Text, cmbType.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show(I18n.T("AddParameterForm.MsgDefaultValueTypeMismatch"), I18n.T("Common.MsgTitleError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }

            DialogResult = DialogResult.OK;
        }

        private object ConvertValue(string value, string type)
        {
            if (string.IsNullOrEmpty(value)) return null;

            switch (type.ToLower())
            {
                case "int":
                    return int.Parse(value);
                case "double":
                    return double.Parse(value);
                case "bool":
                    return bool.Parse(value);
                default:
                    return value;
            }
        }
    }
}