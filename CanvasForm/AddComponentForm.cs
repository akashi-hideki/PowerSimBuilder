using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class AddComponentForm : Form
    {
        /// <summary>
        /// 新規コンポーネント
        /// 作成日: 2025-08-10
        /// </summary>
        public ComponentDescriptor NewComponent { get; private set; }
        private List<ParameterDescriptor> _parameters = new List<ParameterDescriptor>();

        /// <summary>
        /// コンポーネント追加フォームのコンストラクタ
        /// 作成日: 2025-08-10
        /// </summary>
        public AddComponentForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// パラメータ追加ボタンクリックイベント
        /// 注意事項: AddParameterFormを表示してパラメータを追加する
        /// 作成日: 2025-08-10
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
        /// OKボタンクリックイベント
        /// 注意事項: 入力内容を検証し新規コンポーネントを作成する
        /// 作成日: 2025-08-10
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
                Icon = GenerateIcon(txtType.Text)
            };
        }

        /// <summary>
        /// アイコンを生成する
        /// 注意事項: タイプ名のハッシュから色を生成する
        /// 作成日: 2025-08-10
        /// </summary>
        private Bitmap GenerateIcon(string type)
        {
            // 安全なカラー生成
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

                // 空文字チェック
                string firstChar = !string.IsNullOrEmpty(type) ? type[0].ToString() : "?";
                graphics.DrawString(firstChar,
                                  new Font("Arial", 10, FontStyle.Bold),
                                  Brushes.White, new PointF(10, 8));
            }
            return bmp;
        }
    }
}