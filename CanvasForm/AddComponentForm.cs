using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// カスタムコンポーネント追加フォーム
    /// 作成日: 2025-08-12
    /// 注意点: ユーザー定義コンポーネントを作成
    /// </summary>
    public partial class AddComponentForm : Form
    {
        /// <summary>
        /// 新規コンポーネント
        /// 注意点: フォーム完了時に設定される
        /// </summary>
        public ComponentDescriptor NewComponent { get; private set; }
        private List<ParameterDescriptor> _parameters = new List<ParameterDescriptor>();

        /// <summary>
        /// コンストラクタ
        /// 注意点: UIコンポーネントを初期化
        /// </summary>
        public AddComponentForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// パラメータ追加ボタンクリック
        /// 注意点: パラメータ追加フォームを表示
        /// </summary>
        private void BtnAddParam_Click(object sender, EventArgs e)
        {
            using (var paramForm = new AddParameterForm())
            {
                if (paramForm.ShowDialog() == DialogResult.OK)
                {
                    var param = new ParameterDescriptor
                    {
                        Name = paramForm.ParamName,
                        Type = paramForm.ParamType,
                        DefaultValue = paramForm.DefaultValue
                    };

                    _parameters.Add(param);
                    paramListBox.Items.Add($"{param.Name} ({param.Type}) = {param.DefaultValue}");
                }
            }
        }

        /// <summary>
        /// OKボタンクリック
        /// 注意点: 入力内容を検証しコンポーネントを作成
        /// </summary>
        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtType.Text))
            {
                MessageBox.Show("名前とタイプは必須です");
                DialogResult = DialogResult.None;
                return;
            }

            NewComponent = new ComponentDescriptor
            {
                Name = txtName.Text,
                Type = txtType.Text,
                Category = txtCategory.Text,
                Parameters = new List<ParameterDescriptor>(_parameters),
                Icon = GenerateIcon(txtType.Text),

                // ポート情報を初期化
                InputPorts = new[] { "Input" },
                OutputPorts = new[] { "Output" }
            };
        }

        /// <summary>
        /// アイコンを生成
        /// 注意点: タイプ名から色を生成
        /// </summary>
        private Bitmap GenerateIcon(string type)
        {
            // タイプ名のハッシュからRGB値を生成
            uint hash = (uint)Math.Abs(type.GetHashCode());
            byte red = (byte)((hash * 17) % 256);
            byte green = (byte)((hash * 31) % 256);
            byte blue = (byte)((hash * 53) % 256);

            Color color = Color.FromArgb(red, green, blue);

            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics graphics = Graphics.FromImage(bmp))
            {
                graphics.FillRectangle(new SolidBrush(color), 0, 0, 32, 32);
                graphics.DrawRectangle(Pens.Black, 0, 0, 31, 31);

                // 先頭文字を表示
                string firstChar = !string.IsNullOrEmpty(type) ? type[0].ToString() : "?";
                graphics.DrawString(firstChar,
                                  new Font("Arial", 10, FontStyle.Bold),
                                  Brushes.White, new PointF(10, 8));
            }
            return bmp;
        }
    }
}