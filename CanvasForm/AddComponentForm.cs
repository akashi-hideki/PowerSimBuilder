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
        private ListBox paramListBox;
        
        // フィールドとして宣言
        private TextBox txtName;
        private TextBox txtType;
        private TextBox txtCategory;
        
        public AddComponentForm()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(400, 350);
            this.Text = "コンポーネントの追加";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // コントロールの作成
            var lblName = new Label { Text = "名前:", Location = new Point(20, 20), Size = new Size(80, 20) };
            txtName = new TextBox { Location = new Point(110, 20), Size = new Size(250, 20) }; // フィールドに代入
            
            var lblType = new Label { Text = "タイプ:", Location = new Point(20, 50), Size = new Size(80, 20) };
            txtType = new TextBox { Location = new Point(110, 50), Size = new Size(250, 20) }; // フィールドに代入
            
            var lblCategory = new Label { Text = "カテゴリ:", Location = new Point(20, 80), Size = new Size(80, 20) };
            txtCategory = new TextBox { Location = new Point(110, 80), Size = new Size(250, 20) }; // フィールドに代入
            
            var btnAddParam = new Button { 
                Text = "パラメータを追加", 
                Location = new Point(20, 110), 
                Size = new Size(150, 30) 
            };
            
            paramListBox = new ListBox { 
                Location = new Point(20, 150), 
                Size = new Size(340, 120) 
            };
            
            var btnOK = new Button { 
                Text = "OK", 
                DialogResult = DialogResult.OK, 
                Location = new Point(150, 280),
                Size = new Size(80, 30)
            };
            
            var btnCancel = new Button { 
                Text = "キャンセル", 
                DialogResult = DialogResult.Cancel, 
                Location = new Point(250, 280),
                Size = new Size(80, 30)
            };
            
            // イベントハンドラ
            btnAddParam.Click += BtnAddParam_Click;
            btnOK.Click += BtnOK_Click;
            
            // コントロール追加
            this.Controls.AddRange(new Control[] {
                lblName, txtName,
                lblType, txtType,
                lblCategory, txtCategory,
                btnAddParam, paramListBox,
                btnOK, btnCancel
            });
            
            this.ResumeLayout(true);
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
                this.DialogResult = DialogResult.None;
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