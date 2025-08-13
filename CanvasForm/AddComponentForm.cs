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
    /// 注意点: 
    ///   - ユーザー定義コンポーネントを作成しXMLに保存
    ///   - カテゴリ、パラメータ、ポート情報を入力
    ///   - コンポーネント定義マネージャーと連携
    /// </summary>
    public partial class AddComponentForm : Form
    {
        /// <summary>
        /// パラメータ定義リスト
        /// 作成日: 2025-08-15
        /// 注意点: ユーザーが追加したパラメータを保持
        /// </summary>
        private List<ParameterDefinition> _parameters = new List<ParameterDefinition>();
        
        /// <summary>
        /// コンポーネント定義マネージャー
        /// 作成日: 2025-08-15
        /// 注意点: コンポーネント定義の読み込み/保存を担当
        /// </summary>
        private readonly ComponentDefinitionManager _definitionManager;
        
        /// <summary>
        /// ツールチップコンポーネント
        /// 作成日: 2025-08-15
        /// 注意点: 入力フィールドのヒント表示に使用
        /// </summary>
        private ToolTip toolTip = new ToolTip();

        /// <summary>
        /// 新規コンポーネント定義プロパティ
        /// 作成日: 2025-08-15
        /// 注意点: 
        ///   - フォーム完了時に設定される
        ///   - 呼び出し元でこのプロパティを使用
        /// </summary>
        public ComponentDefinition NewComponentDefinition { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// 作成日: 2025-08-15
        /// 処理内容: 
        ///   - 定義マネージャーを初期化
        ///   - UIコンポーネントを初期化
        /// 注意点: 
        ///   - 引数でコンポーネント定義マネージャーを受け取る
        ///   - フォームのライフサイクルを開始
        /// </summary>
        /// <param name="definitionManager">コンポーネント定義マネージャーインスタンス</param>
        public AddComponentForm(ComponentDefinitionManager definitionManager)
        {
            _definitionManager = definitionManager;
            InitializeComponent();
            InitializeCustomComponents();
        }

        /// <summary>
        /// カスタムコンポーネント初期化
        /// 作成日: 2025-08-15
        /// 処理内容:
        ///   - コンボボックスにアイテムを追加
        ///   - ツールチップを設定
        /// 注意点: 
        ///   - 基本初期化後に呼び出される
        ///   - UI要素の動的設定を担当
        /// </summary>
        private void InitializeCustomComponents()
        {
            // カテゴリコンボボックス初期化
            cmbCategory.Items.AddRange(_definitionManager.GetCategories().ToArray());
            cmbCategory.Items.Add("<< 新規カテゴリ >>");
            cmbCategory.SelectedIndex = 0;
            
            // 色コンボボックス初期化
            cmbColor.Items.AddRange(new[] { "Red", "Blue", "Green", "Orange", "Purple", "Yellow" });
            cmbColor.SelectedIndex = 0;

            // ツールチップ設定
            toolTip.SetToolTip(txtInputPorts, "カンマ区切りで入力 (例: Input1,Input2)");
            toolTip.SetToolTip(txtOutputPorts, "カンマ区切りで入力 (例: Output1,Output2)");
        }

        /// <summary>
        /// パラメータ追加ボタンクリックイベント
        /// 作成日: 2025-08-15
        /// 処理内容:
        ///   - パラメータ追加フォームを表示
        ///   - 入力されたパラメータをリストに追加
        /// 注意点:
        ///   - パラメータ追加フォームはモーダルダイアログ
        ///   - キャンセル時は処理を中断
        /// </summary>
        /// <param name="sender">イベント発生元オブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void btnAddParam_Click(object sender, EventArgs e)
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
        }

        /// <summary>
        /// OKボタンクリックイベント
        /// 作成日: 2025-08-15
        /// 処理内容:
        ///   - 入力値の検証
        ///   - 重複チェック
        ///   - カテゴリ処理
        ///   - ポート情報変換
        ///   - 新しいコンポーネント定義を作成
        /// 注意点:
        ///   - 必須フィールドチェックを実施
        ///   - カテゴリが"新規カテゴリ"の場合は追加ダイアログ表示
        ///   - フォームを閉じて結果を返す
        /// </summary>
        /// <param name="sender">イベント発生元オブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void btnOK_Click(object sender, EventArgs e)
        {
            // 必須フィールドチェック
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtType.Text))
            {
                MessageBox.Show("名前とタイプは必須です", "入力エラー", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            // タイプ重複チェック
            if (_definitionManager.GetDefinition(txtType.Text) != null)
            {
                MessageBox.Show("このタイプは既に存在します", "重複エラー", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            // カテゴリ処理
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

            // ポート情報変換
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

            // コンポーネント定義作成
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
        }

        /// <summary>
        /// キャンセルボタンクリックイベント
        /// 作成日: 2025-08-15
        /// 処理内容:
        ///   - フォームを閉じる
        /// 注意点:
        ///   - DialogResult.Cancelが自動で設定される
        /// </summary>
        /// <param name="sender">イベント発生元オブジェクト</param>
        /// <param name="e">イベント引数</param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            // キャンセル処理（DialogResult.Cancelが自動設定）
        }

        /// <summary>
        /// パラメータ入力ダイアログクラス
        /// 作成日: 2025-08-15
        /// 注意点: 
        ///   - 新規パラメータの詳細を入力
        ///   - AddComponentFormの内部クラス
        /// </summary>
        private class AddParameterForm : Form
        {
            /// <summary>
            /// パラメータ名プロパティ
            /// 作成日: 2025-08-15
            /// 注意点: ユーザー入力値
            /// </summary>
            public string ParamName { get; private set; }
            
            /// <summary>
            /// パラメータ型プロパティ
            /// 作成日: 2025-08-15
            /// 注意点: コンボボックスから選択
            /// </summary>
            public string ParamType { get; private set; }
            
            /// <summary>
            /// デフォルト値プロパティ
            /// 作成日: 2025-08-15
            /// 注意点: 型に応じた変換が必要
            /// </summary>
            public object DefaultValue { get; private set; }
            
            /// <summary>
            /// 単位プロパティ
            /// 作成日: 2025-08-15
            /// 注意点: オプション入力
            /// </summary>
            public string Unit { get; private set; }

            // UIコントロール
            private TextBox txtName;
            private ComboBox cmbType;
            private TextBox txtValue;
            private TextBox txtUnit;

            /// <summary>
            /// コンストラクタ
            /// 作成日: 2025-08-15
            /// 処理内容: フォームを初期化
            /// </summary>
            public AddParameterForm()
            {
                InitializeForm();
            }

            /// <summary>
            /// フォーム初期化
            /// 作成日: 2025-08-15
            /// 処理内容:
            ///   - UIコンポーネントを作成・配置
            ///   - イベントハンドラを登録
            /// 注意点: コードによる動的生成
            /// </summary>
            private void InitializeForm()
            {
                // 基本設定
                this.Text = "パラメータ追加";
                this.Size = new Size(300, 250);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.StartPosition = FormStartPosition.CenterParent;

                // パラメータ名入力
                var lblName = new Label { Text = "パラメータ名:", Location = new Point(20, 20) };
                txtName = new TextBox { Location = new Point(120, 20), Width = 150 };

                // データ型選択
                var lblType = new Label { Text = "データ型:", Location = new Point(20, 50) };
                cmbType = new ComboBox
                {
                    Location = new Point(120, 50),
                    Width = 150,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cmbType.Items.AddRange(new[] { "double", "int", "string", "bool" });
                cmbType.SelectedIndex = 0;

                // デフォルト値入力
                var lblValue = new Label { Text = "デフォルト値:", Location = new Point(20, 80) };
                txtValue = new TextBox { Location = new Point(120, 80), Width = 150 };

                // 単位入力
                var lblUnit = new Label { Text = "単位:", Location = new Point(20, 110) };
                txtUnit = new TextBox { Location = new Point(120, 110), Width = 150 };

                // 操作ボタン
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

                // イベントハンドラ登録
                btnOK.Click += BtnOK_Click;
            }

            /// <summary>
            /// OKボタンクリックイベント
            /// 作成日: 2025-08-15
            /// 処理内容:
            ///   - 入力値検証
            ///   - 型変換処理
            ///   - プロパティに値を設定
            /// 注意点:
            ///   - 変換エラー時はダイアログを閉じない
            /// </summary>
            /// <param name="sender">イベント発生元オブジェクト</param>
            /// <param name="e">イベント引数</param>
            private void BtnOK_Click(object sender, EventArgs e)
            {
                // 必須入力チェック
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("パラメータ名は必須です", "入力エラー", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                // 入力値取得
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
            }
        }

        /// <summary>
        /// カテゴリ入力ダイアログクラス
        /// 作成日: 2025-08-15
        /// 注意点: 
        ///   - 新規カテゴリ名を入力
        ///   - AddComponentFormの内部クラス
        /// </summary>
        private class InputDialog : Form
        {
            /// <summary>
            /// 入力値プロパティ
            /// 作成日: 2025-08-15
            /// 注意点: ユーザーが入力したカテゴリ名
            /// </summary>
            public string InputValue { get; private set; }
            
            private TextBox txtInput;

            /// <summary>
            /// コンストラクタ
            /// 作成日: 2025-08-15
            /// 処理内容: プロンプトメッセージを受け取りフォームを初期化
            /// </summary>
            /// <param name="prompt">ユーザーへの指示メッセージ</param>
            public InputDialog(string prompt)
            {
                InitializeForm(prompt);
            }

            /// <summary>
            /// フォーム初期化
            /// 作成日: 2025-08-15
            /// 処理内容:
            ///   - UIコンポーネントを作成・配置
            ///   - イベントハンドラを登録
            /// 注意点: コードによる動的生成
            /// </summary>
            /// <param name="prompt">ユーザーへの指示メッセージ</param>
            private void InitializeForm(string prompt)
            {
                // 基本設定
                this.Text = "新規カテゴリ";
                this.Size = new Size(300, 150);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.StartPosition = FormStartPosition.CenterParent;

                // プロンプト表示
                var lblPrompt = new Label
                {
                    Text = prompt,
                    Location = new Point(10, 20),
                    Width = 280
                };

                // 入力フィールド
                txtInput = new TextBox
                {
                    Location = new Point(10, 50),
                    Width = 260
                };

                // 操作ボタン
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

                // コントロール追加
                this.Controls.AddRange(new Control[] { lblPrompt, txtInput, btnOK, btnCancel });

                // イベントハンドラ登録
                btnOK.Click += BtnOK_Click;
            }

            /// <summary>
            /// OKボタンクリックイベント
            /// 作成日: 2025-08-15
            /// 処理内容:
            ///   - 入力値検証
            ///   - プロパティに値を設定
            /// 注意点:
            ///   - 空入力時はダイアログを閉じない
            /// </summary>
            /// <param name="sender">イベント発生元オブジェクト</param>
            /// <param name="e">イベント引数</param>
            private void BtnOK_Click(object sender, EventArgs e)
            {
                if (string.IsNullOrWhiteSpace(txtInput.Text))
                {
                    MessageBox.Show("カテゴリ名を入力してください", "入力エラー", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }
                InputValue = txtInput.Text;
            }
        }
    }
}