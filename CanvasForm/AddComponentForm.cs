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

        public ComponentDescriptor ResultComponent { get; private set; }
        private List<ParameterDescriptor> _parameters = new List<ParameterDescriptor>();
        
        // 編集モード用プロパティ
        private bool _isEditMode = false;
        private ComponentDescriptor _originalComponent;

        // 新規追加用コンストラクタ
        public AddComponentForm()
        {
            InitializeComponent();
            Text = "新規コンポーネント追加";
            btnOK.Text = "追加";
            btnDeleteParam.Visible = false; // 新規追加時は削除ボタンを非表示
        }

        // 編集用コンストラクタ
        public AddComponentForm(ComponentDescriptor component) : this()
        {
            _isEditMode = true;
            _originalComponent = component;
            Text = "コンポーネント編集";
            btnOK.Text = "更新";
            btnDeleteParam.Visible = true; // 編集時は削除ボタンを表示
            
            // 既存データをフォームに設定
            txtName.Text = component.Name;
            txtType.Text = component.Type;
            txtCategory.Text = component.Category;
            
            // 入力ポート
            txtInputPorts.Text = string.Join(",", component.InputPorts);
            
            // 出力ポート
            txtOutputPorts.Text = string.Join(",", component.OutputPorts);
            
            // パラメータ
            _parameters = new List<ParameterDescriptor>(component.Parameters);
            RefreshParameterList();
        }

        // パラメータリストを更新
        private void RefreshParameterList()
        {
            paramListBox.Items.Clear();
            foreach (var param in _parameters)
            {
                paramListBox.Items.Add($"{param.Name} ({param.Type}) = {param.DefaultValue}");
            }
        }

        // パラメータ追加ボタンクリック
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
                    RefreshParameterList();
                }
            }
        }

        // パラメータ編集ボタンクリック
        private void BtnEditParam_Click(object sender, EventArgs e)
        {
            if (paramListBox.SelectedIndex == -1)
            {
                MessageBox.Show("編集するパラメータを選択してください", "情報",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            int index = paramListBox.SelectedIndex;
            var param = _parameters[index];
            
            using (var paramForm = new AddParameterForm(param))
            {
                if (paramForm.ShowDialog() == DialogResult.OK)
                {
                    param.Name = paramForm.ParamName;
                    param.Type = paramForm.ParamType;
                    param.DefaultValue = paramForm.DefaultValue;
                    
                    RefreshParameterList();
                    paramListBox.SelectedIndex = index;
                }
            }
        }

        // パラメータ削除ボタンクリック
        private void BtnDeleteParam_Click(object sender, EventArgs e)
        {
            if (paramListBox.SelectedIndex == -1)
            {
                MessageBox.Show("削除するパラメータを選択してください", "情報",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            int index = paramListBox.SelectedIndex;
            _parameters.RemoveAt(index);
            RefreshParameterList();
            
            if (paramListBox.Items.Count > 0)
            {
                paramListBox.SelectedIndex = Math.Min(index, paramListBox.Items.Count - 1);
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

            // 入力ポートの処理
            var inputPorts = txtInputPorts.Text.Split(
                new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            
            // 出力ポートの処理
            var outputPorts = txtOutputPorts.Text.Split(
                new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            ResultComponent = new ComponentDescriptor
            {
                Name = txtName.Text,
                Type = txtType.Text,
                Category = txtCategory.Text,
                Parameters = new List<ParameterDescriptor>(_parameters),
                Icon = _isEditMode && _originalComponent?.Icon != null ? 
                    _originalComponent.Icon : GenerateIcon(txtType.Text),
                InputPorts = inputPorts,
                OutputPorts = outputPorts
            };
        }

        /// <summary>
        /// アイコンを生成
        /// 注意点: タイプ名から色を生成
        /// </summary>
        private Bitmap GenerateIcon(string type)
        {
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

                string firstChar = !string.IsNullOrEmpty(type) ? type[0].ToString() : "?";
                graphics.DrawString(firstChar,
                                  new Font("Arial", 10, FontStyle.Bold),
                                  Brushes.White, new PointF(10, 8));
            }
            return bmp;
        }

        // パラメータリストのダブルクリックで編集
        private void paramListBox_DoubleClick(object sender, EventArgs e)
        {
            BtnEditParam_Click(sender, e);
        }
    }
}