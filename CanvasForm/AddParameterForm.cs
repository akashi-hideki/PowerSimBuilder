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
        }

        /// <summary>
        /// OKボタンクリック
        /// 注意点: 入力内容を検証しパラメータを設定
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
            ParamType = cmbType.SelectedItem?.ToString();

            // 型選択チェック
            if (string.IsNullOrEmpty(ParamType))
            {
                MessageBox.Show("パラメータ型を選択してください");
                this.DialogResult = DialogResult.None;
                return;
            }

            try
            {
                // 型に応じた値変換
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