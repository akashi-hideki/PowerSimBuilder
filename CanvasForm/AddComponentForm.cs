using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
            InitializePortGrids();
            Text = I18n.T("AddComponentForm.TitleNew");
            btnOK.Text = I18n.T("Common.BtnAdd");
            btnDeleteParam.Visible = false; // 新規追加時は削除ボタンを非表示
        }

        // 編集用コンストラクタ
        public AddComponentForm(ComponentDescriptor component) : this()
        {
            _isEditMode = true;
            _originalComponent = component;
            Text = I18n.T("AddComponentForm.TitleEdit");
            btnOK.Text = I18n.T("Common.BtnUpdate");
            btnDeleteParam.Visible = true; // 編集時は削除ボタンを表示

            // 既存データをフォームに設定
            txtName.Text = component.Name;
            txtType.Text = component.Type;
            txtCategory.Text = component.Category;

            // 入力ポート
            foreach (var port in component.InputPorts)
            {
                inputPortsGrid.Rows.Add(port);
            }

            // 出力ポート
            foreach (var port in component.OutputPorts)
            {
                outputPortsGrid.Rows.Add(port);
            }

            // パラメータ
            _parameters = new List<ParameterDescriptor>(component.Parameters);
            RefreshParameterList();
        }

        // ポートグリッドの初期化
        private void InitializePortGrids()
        {
            // 入力ポートグリッド
            inputPortsGrid.Columns.Clear();
            inputPortsGrid.Columns.Add("PortName", I18n.T("Common.PortName"));
            inputPortsGrid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            inputPortsGrid.AllowUserToAddRows = false;
            inputPortsGrid.RowHeadersVisible = false;

            // 出力ポートグリッド
            outputPortsGrid.Columns.Clear();
            outputPortsGrid.Columns.Add("PortName", I18n.T("Common.PortName"));
            outputPortsGrid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            outputPortsGrid.AllowUserToAddRows = false;
            outputPortsGrid.RowHeadersVisible = false;
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
                MessageBox.Show(I18n.T("AddComponentForm.MsgSelectParameterToEdit"), I18n.T("Common.MsgTitleInfo"),
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
                MessageBox.Show(I18n.T("AddComponentForm.MsgSelectParameterToDelete"), I18n.T("Common.MsgTitleInfo"),
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
            GenerateComponentCodeAndOpenEditor();
        }

        /// <summary>
        /// コンポーネントコード生成と編集処理
        /// </summary>
        private void GenerateComponentCodeAndOpenEditor()
        {
            try
            {
                // 1. ResultComponentの生成（既存のBtnOK_Clickから流用）
                if (!ValidateAndCreateComponent())
                {
                    DialogResult = DialogResult.None;
                    return;
                }

                // 2. ファイル出力先の指定
                string filePath = ShowSaveFileDialog();
                if (string.IsNullOrEmpty(filePath))
                {
                    DialogResult = DialogResult.None;
                    return;
                }

                // 3. Pythonコードの生成とファイル保存、エディタ表示（ComponentCodeGeneratorが行う）
                var generator = new ComponentCodeGenerator();
                generator.GeneratePythonCode(ResultComponent, filePath);

                // コード編集フォームの表示
                using (var editorForm = new PythonEditorForm(filePath, false))
                {
                    editorForm.ShowDialog();
                }

                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(I18n.T("AddComponentForm.MsgError", ex.Message), I18n.T("Common.MsgTitleError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
            }
        }

        /// <summary>
        /// コンポーネントの検証と生成
        /// </summary>
        private bool ValidateAndCreateComponent()
        {
            // 既存のBtnOK_Clickの検証ロジックを流用
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtType.Text))
            {
                MessageBox.Show(I18n.T("AddComponentForm.MsgNameAndTypeRequired"));
                return false;
            }

            var inputPorts = new List<string>();
            foreach (DataGridViewRow row in inputPortsGrid.Rows)
            {
                if (row.Cells[0].Value != null && !string.IsNullOrWhiteSpace(row.Cells[0].Value.ToString()))
                {
                    inputPorts.Add(row.Cells[0].Value.ToString().Trim());
                }
            }

            var outputPorts = new List<string>();
            foreach (DataGridViewRow row in outputPortsGrid.Rows)
            {
                if (row.Cells[0].Value != null && !string.IsNullOrWhiteSpace(row.Cells[0].Value.ToString()))
                {
                    outputPorts.Add(row.Cells[0].Value.ToString().Trim());
                }
            }

            if (inputPorts.Count == 0 || outputPorts.Count == 0)
            {
                MessageBox.Show(I18n.T("AddComponentForm.MsgPortsRequired"));
                return false;
            }

            if (inputPorts.Any(string.IsNullOrWhiteSpace) || outputPorts.Any(string.IsNullOrWhiteSpace))
            {
                MessageBox.Show(I18n.T("AddComponentForm.MsgPortNameRequired"));
                return false;
            }

            if (inputPorts.Count != inputPorts.Distinct().Count() ||
                outputPorts.Count != outputPorts.Distinct().Count())
            {
                MessageBox.Show(I18n.T("AddComponentForm.MsgPortNameUnique"));
                return false;
            }

            ResultComponent = new ComponentDescriptor
            {
                Name = txtName.Text,
                Type = txtType.Text,
                Category = txtCategory.Text,
                Parameters = new List<ParameterDescriptor>(_parameters),
                Icon = _isEditMode && _originalComponent?.Icon != null ?
                    _originalComponent.Icon : GenerateIcon(txtType.Text),
                InputPorts = inputPorts.ToArray(),
                OutputPorts = outputPorts.ToArray()
            };

            return true;
        }

        /// <summary>
        /// 保存ファイルダイアログの表示
        /// </summary>
        private string ShowSaveFileDialog()
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Python Files|*.py";
                saveFileDialog.Title = I18n.T("AddComponentForm.SaveFileTitle");
                saveFileDialog.FileName = $"{ResultComponent.Type}.py";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    return saveFileDialog.FileName;
                }
            }
            return null;
        }

        /// <summary>
        /// OKボタンクリック - 新しい処理フロー
        /// </summary>
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

        // 入力ポート追加
        private void btnAddInputPort_Click(object sender, EventArgs e)
        {
            inputPortsGrid.Rows.Add("NewPort");
        }

        // 入力ポート削除
        private void btnRemoveInputPort_Click(object sender, EventArgs e)
        {
            if (inputPortsGrid.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in inputPortsGrid.SelectedRows)
                {
                    if (!row.IsNewRow)
                    {
                        inputPortsGrid.Rows.Remove(row);
                    }
                }
            }
        }

        // 出力ポート追加
        private void btnAddOutputPort_Click(object sender, EventArgs e)
        {
            outputPortsGrid.Rows.Add("NewPort");
        }

        // 出力ポート削除
        private void btnRemoveOutputPort_Click(object sender, EventArgs e)
        {
            if (outputPortsGrid.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in outputPortsGrid.SelectedRows)
                {
                    if (!row.IsNewRow)
                    {
                        outputPortsGrid.Rows.Remove(row);
                    }
                }
            }
        }
    }
}