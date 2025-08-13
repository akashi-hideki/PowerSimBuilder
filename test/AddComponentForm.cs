using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// カスタムコンポーネント追加フォーム
    /// 作成日: 2025-08-15
    /// 変更内容: XMLベースのコンポーネント定義システムに対応
    /// 注意点: ユーザー定義コンポーネントを作成しXMLに保存
    /// </summary>
    public partial class AddComponentForm : Form
    {
        private List<ParameterDefinition> _parameters = new List<ParameterDefinition>();
        private readonly ComponentDefinitionManager _definitionManager;

        /// <summary>
        /// 新規コンポーネント定義
        /// 作成日: 2025-08-15
        /// 注意点: フォーム完了時に設定される
        /// </summary>
        public ComponentDefinition NewComponentDefinition { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// 作成日: 2025-08-15
        /// 処理内容: 定義マネージャーを初期化しUIを設定
        /// </summary>
        public AddComponentForm(ComponentDefinitionManager definitionManager)
        {
            _definitionManager = definitionManager;
            InitializeComponent();
            InitializeForm();
        }

        /// <summary>
        /// フォーム初期化
        /// 作成日: 2025-08-15
        /// 処理内容: UIコンポーネントを配置しイベントハンドラを登録
        /// </summary>
        private void InitializeForm()
        {
            // 基本設定
            this.Text = "カスタムコンポーネント追加";
            this.Size = new Size(500, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;

            // カテゴリ選択
            var lblCategory = new Label { Text = "カテゴリ:", Location = new Point(20, 20) };
            var cmbCategory = new ComboBox
            {
                Location = new Point(120, 20),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            // 既存カテゴリと新規カテゴリオプションを追加
            cmbCategory.Items.AddRange(_definitionManager.GetCategories().ToArray());
            cmbCategory.Items.Add("<< 新規カテゴリ >>");
            cmbCategory.SelectedIndex = 0;

            // コンポーネント名
            var lblName = new Label { Text = "名前:", Location = new Point(20, 50) };
            var txtName = new TextBox { Location = new Point(120, 50), Width = 200 };

            // コンポーネントタイプ
            var lblType = new Label { Text = "タイプ:", Location = new Point(20, 80) };
            var txtType = new TextBox { Location = new Point(120, 80), Width = 200 };

            // アイコン色
            var lblColor = new Label { Text = "アイコン色:", Location = new Point(20, 110) };
            var cmbColor = new ComboBox
            {
                Location = new Point(120, 110),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbColor.Items.AddRange(new[] { "Red", "Blue", "Green", "Orange", "Purple", "Yellow" });
            cmbColor.SelectedIndex = 0;

            // パラメータリスト
            var lblParams = new Label { Text = "パラメータ:", Location = new Point(20, 140) };
            var paramListBox = new ListBox
            {
                Location = new Point(120, 140),
                Width = 200,
                Height = 100
            };

            // ポート設定
            var lblInputPorts = new Label { Text = "入力ポート:", Location = new Point(20, 250) };
            var txtInputPorts = new TextBox
            {
                Location = new Point(120, 250),
                Width = 200,
                ToolTipText = "カンマ区切りで入力 (例: Input1,Input2)"
            };

            var lblOutputPorts = new Label { Text = "出力ポート:", Location = new Point(20, 280) };
            var txtOutputPorts = new TextBox
            {
                Location = new Point(120, 280),
                Width = 200,
                ToolTipText = "カンマ区切りで入力 (例: Output1,Output2)"
            };

            // 操作ボタン
            var btnAddParam = new Button
            {
                Text = "パラメータ追加",
                Location = new Point(330, 140),
                Width = 120
            };

            var btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(120, 320)
            };

            var btnCancel = new Button
            {
                Text = "キャンセル",
                DialogResult = DialogResult.Cancel,
                Location = new Point(220, 320)
            };

            // コントロール追加
            this.Controls.AddRange(new Control[] {
                lblCategory, cmbCategory,
                lblName, txtName,
                lblType, txtType,
                lblColor, cmbColor,
                lblParams, paramListBox,
                lblInputPorts, txtInputPorts,
                lblOutputPorts, txtOutputPorts,
                btnAddParam, btnOK, btnCancel
            });

            // イベントハンドラ
            btnAddParam.Click += (s, e) =>
            {
                using (var paramForm = new AddParameterForm())
                {
                    if (paramForm.ShowDialog() == DialogResult.OK)
                    {
                        var param = new ParameterDefinition
                        {
                            Name = paramForm.ParamName,
                            Type = paramForm.ParamType,
                            DefaultValue = paramForm.DefaultValue?.ToString(),
                            Unit = paramForm.Unit
                        };

                        _parameters.Add(param);
                        paramListBox.Items.Add($"{param.Name} ({param.Type}) = {param.DefaultValue} {param.Unit}");
                    }
                }
            };

            btnOK.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtType.Text))
                {
                    MessageBox.Show("名前とタイプは必須です", "入力エラー", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                // タイプの重複チェック
                if (_definitionManager.GetDefinition(txtType.Text) != null)
                {
                    MessageBox.Show("このタイプは既に存在します", "重複エラー", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                // カテゴリ処理 (新規カテゴリ選択時)
                string category = cmbCategory.Text;
                if (category == "<< 新規カテゴリ >>")
                {
                    using (var inputDialog = new InputDialog("新規カテゴリ名を入力:"))
                    {
                        if (inputDialog.ShowDialog() == DialogResult.OK)
                        {
                            category = inputDialog.InputValue;
                        }
                        else
                        {
                            DialogResult = DialogResult.None;
                            return;
                        }
                    }
                }

                // ポート情報をリストに変換
                var inputPorts = new List<string>();
                var outputPorts = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(txtInputPorts.Text))
                {
                    inputPorts.AddRange(txtInputPorts.Text.Split(
                        new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }
                
                if (!string.IsNullOrWhiteSpace(txtOutputPorts.Text))
                {
                    outputPorts.AddRange(txtOutputPorts.Text.Split(
                        new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }

                // 新しいコンポーネント定義を作成
                NewComponentDefinition = new ComponentDefinition
                {
                    Name = txtName.Text,
                    Type = txtType.Text,
                    Category = category,
                    IconColor = cmbColor.SelectedItem?.ToString() ?? "Gray",
                    Parameters = new List<ParameterDefinition>(_parameters),
                    InputPorts = inputPorts,
                    OutputPorts = outputPorts
                };
            };
        }

        /// <summary>
        /// パラメータ入力ダイアログクラス
        /// 作成日: 2025-08-15
        /// 注意点: 新規パラメータの詳細を入力
        /// </summary>
        private class AddParameterForm : Form
        {
            public string ParamName { get; private set; }
            public string ParamType { get; private set; }
            public object DefaultValue { get; private set; }
            public string Unit { get; private set; }

            public AddParameterForm()
            {
                InitializeForm();
            }

            private void InitializeForm()
            {
                this.Text = "パラメータ追加";
                this.Size = new Size(300, 250);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;

                // パラメータ名
                var lblName = new Label { Text = "パラメータ名:", Location = new Point(20, 20) };
                var txtName = new TextBox { Location = new Point(120, 20), Width = 150 };

                // パラメータタイプ
                var lblType = new Label { Text = "データ型:", Location = new Point(20, 50) };
                var cmbType = new ComboBox
                {
                    Location = new Point(120, 50),
                    Width = 150,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cmbType.Items.AddRange(new[] { "double", "int", "string", "bool" });
                cmbType.SelectedIndex = 0;

                // デフォルト値
                var lblValue = new Label { Text = "デフォルト値:", Location = new Point(20, 80) };
                var txtValue = new TextBox { Location = new Point(120, 80), Width = 150 };

                // 単位
                var lblUnit = new Label { Text = "単位:", Location = new Point(20, 110) };
                var txtUnit = new TextBox { Location = new Point(120, 110), Width = 150 };

                // ボタン
                var btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(80, 150) };
                var btnCancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Location = new Point(160, 150) };

                // コントロール追加
                this.Controls.AddRange(new Control[] {
                    lblName, txtName,
                    lblType, cmbType,
                    lblValue, txtValue,
                    lblUnit, txtUnit,
                    btnOK, btnCancel
                });

                // イベントハンドラ
                btnOK.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtName.Text))
                    {
                        MessageBox.Show("パラメータ名は必須です", "入力エラー", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        DialogResult = DialogResult.None;
                        return;
                    }

                    ParamName = txtName.Text;
                    ParamType = cmbType.SelectedItem?.ToString();
                    Unit = txtUnit.Text;

                    // 型に応じた値変換
                    try
                    {
                        switch (ParamType)
                        {
                            case "double":
                                DefaultValue = double.Parse(txtValue.Text);
                                break;
                            case "int":
                                DefaultValue = int.Parse(txtValue.Text);
                                break;
                            case "bool":
                                DefaultValue = bool.Parse(txtValue.Text);
                                break;
                            default: // string
                                DefaultValue = txtValue.Text;
                                break;
                        }
                    }
                    catch
                    {
                        MessageBox.Show("デフォルト値が型に一致しません", "変換エラー", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        DialogResult = DialogResult.None;
                    }
                };
            }
        }

        /// <summary>
        /// カテゴリ入力ダイアログクラス
        /// 作成日: 2025-08-15
        /// 注意点: 新規カテゴリ名を入力
        /// </summary>
        private class InputDialog : Form
        {
            public string InputValue { get; private set; }

            public InputDialog(string prompt)
            {
                InitializeForm(prompt);
            }

            private void InitializeForm(string prompt)
            {
                this.Text = "新規カテゴリ";
                this.Size = new Size(300, 150);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;

                var lblPrompt = new Label
                {
                    Text = prompt,
                    Location = new Point(10, 20),
                    Width = 280
                };

                var txtInput = new TextBox
                {
                    Location = new Point(10, 50),
                    Width = 260
                };

                var btnOK = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(80, 80)
                };

                var btnCancel = new Button
                {
                    Text = "キャンセル",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(160, 80)
                };

                this.Controls.AddRange(new Control[] { lblPrompt, txtInput, btnOK, btnCancel });

                btnOK.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtInput.Text))
                    {
                        MessageBox.Show("カテゴリ名を入力してください", "入力エラー", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        DialogResult = DialogResult.None;
                        return;
                    }
                    InputValue = txtInput.Text;
                };
            }
        }
    }
}