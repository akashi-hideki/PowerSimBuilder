using System;
using System.IO;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class PythonEditorForm : Form
    {
        private string _originalCode;
        private string _filePath;

        // コンテキストメニューとその項目
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem menuItemForLoop;
        private ToolStripMenuItem menuItemWhileLoop;
        private ToolStripMenuItem menuItemIfStatement;
        private ToolStripMenuItem menuItemFunctionDef;
        private ToolStripMenuItem menuItemClassDef;
        private ToolStripMenuItem menuItemImport;
        private ToolStripMenuItem menuItemTryCatch;
        private ToolStripMenuItem menuItemComment;

        // Python実行クラスのインスタンス
        private PythonExecutor _executor;

        public bool IsModified { get; private set; }
        public string PythonCode => textEditor.Text;

        /// <summary>
        /// Python実行クラスを設定するプロパティ
        /// </summary>
        public PythonExecutor Executor
        {
            get { return _executor; }
            set { _executor = value; }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="filePath">編集するファイルのパス</param>
        /// <param name="enableRunButtons">実行ボタンを有効にする場合はtrue</param>
        public PythonEditorForm(string filePath, bool enableRunButtons)
        {
            InitializeComponent();
            InitializeContextMenu();

            _filePath = filePath;

            // 実行関連のボタンの表示/有効状態を設定
            btnRun.Visible = enableRunButtons;
            btnRun.Enabled = enableRunButtons;
            btnReload.Visible = enableRunButtons;
            btnReload.Enabled = enableRunButtons;
            btnOpenFolder.Visible = enableRunButtons;
            btnOpenFolder.Enabled = enableRunButtons;
            lblStatus.Visible = enableRunButtons;

            // 実行ボタンのイベントハンドラーを設定
            if (enableRunButtons)
            {
                btnRun.Click += BtnRun_Click;
                btnReload.Click += BtnReload_Click;
                btnOpenFolder.Click += BtnOpenFolder_Click;
            }

            LoadFile();
        }

        // コンテキストメニューの初期化
        private void InitializeContextMenu()
        {
            contextMenu = new ContextMenuStrip();

            // メニュー項目の作成
            menuItemForLoop = new ToolStripMenuItem("Forループを挿入");
            menuItemWhileLoop = new ToolStripMenuItem("Whileループを挿入");
            menuItemIfStatement = new ToolStripMenuItem("If文を挿入");
            menuItemFunctionDef = new ToolStripMenuItem("関数定義を挿入");
            menuItemClassDef = new ToolStripMenuItem("クラス定義を挿入");
            menuItemImport = new ToolStripMenuItem("Import文を挿入");
            menuItemTryCatch = new ToolStripMenuItem("Try-Catchブロックを挿入");
            menuItemComment = new ToolStripMenuItem("コメントを挿入");

            // イベントハンドラーの設定
            menuItemForLoop.Click += InsertForLoop;
            menuItemWhileLoop.Click += InsertWhileLoop;
            menuItemIfStatement.Click += InsertIfStatement;
            menuItemFunctionDef.Click += InsertFunctionDef;
            menuItemClassDef.Click += InsertClassDef;
            menuItemImport.Click += InsertImport;
            menuItemTryCatch.Click += InsertTryCatch;
            menuItemComment.Click += InsertComment;

            // メニュー項目をコンテキストメニューに追加
            contextMenu.Items.AddRange(new ToolStripItem[] {
                menuItemForLoop,
                menuItemWhileLoop,
                menuItemIfStatement,
                menuItemFunctionDef,
                menuItemClassDef,
                menuItemImport,
                menuItemTryCatch,
                menuItemComment
            });

            // コンテキストメニューをRichTextBoxに設定
            textEditor.ContextMenuStrip = contextMenu;
        }

        private void LoadFile()
        {
            try
            {
                _originalCode = File.ReadAllText(_filePath);
                textEditor.Text = _originalCode;
                IsModified = false;
                UpdateTitle();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイルの読み込みに失敗しました: {ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void UpdateTitle()
        {
            this.Text = $"Pythonコードエディタ - {Path.GetFileName(_filePath)}" +
                       (IsModified ? " *" : "");
        }

        private void UpdateStatus()
        {
            lblStatus.Text = IsModified ? "変更あり" : "変更なし";
        }

        private void textEditor_TextChanged(object sender, EventArgs e)
        {
            IsModified = textEditor.Text != _originalCode;
            UpdateTitle();
            UpdateStatus();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText(_filePath, textEditor.Text);
                _originalCode = textEditor.Text;
                IsModified = false;
                UpdateTitle();
                UpdateStatus();

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイルの保存に失敗しました: {ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (IsModified)
            {
                var result = MessageBox.Show("変更内容が保存されていません。閉じますか？",
                    "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                    return;
            }

            DialogResult = DialogResult.Cancel;
            Close();
        }

        // GetPythonCodeメソッド
        public string GetPythonCode()
        {
            return textEditor.Text;
        }

        // 実行ボタンのクリック処理
        private void BtnRun_Click(object sender, EventArgs e)
        {
            ExecutePythonScript();
        }

        // 再読み込みボタンのクリック処理
        private void BtnReload_Click(object sender, EventArgs e)
        {
            if (IsModified)
            {
                var result = MessageBox.Show("変更内容が失われます。よろしいですか？",
                    "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                    return;
            }

            LoadFile();
        }

        // フォルダを開くボタンのクリック処理
        private void BtnOpenFolder_Click(object sender, EventArgs e)
        {
            try
            {
                string folder = Path.GetDirectoryName(_filePath);
                if (Directory.Exists(folder))
                {
                    System.Diagnostics.Process.Start(folder);
                }
                else
                {
                    MessageBox.Show("フォルダが見つかりません", "エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"フォルダを開けませんでした: {ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Pythonスクリプトを実行する
        /// </summary>
        /// <returns>実行が成功した場合はtrue、失敗した場合はfalse</returns>
        public bool ExecutePythonScript()
        {
            // Python実行環境が設定されていない場合はメッセージを表示
            if (_executor == null)
            {
                MessageBox.Show("Python実行環境が設定されていません。\n\n" +
                               "ExecutorプロパティにPythonExecutorインスタンスを設定してください。\n" +
                               "例: editor.Executor = new PythonExecutor();",
                    "実行エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // 変更があれば一時ファイルに保存して実行
            if (IsModified)
            {
                var result = MessageBox.Show("変更されたコードを一時ファイルに保存して実行しますか？",
                    "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                    return false;

                // 一時ファイルを作成
                string tempFile = Path.Combine(
                    Path.GetDirectoryName(_filePath),
                    $"temp_{DateTime.Now:yyyyMMdd_HHmmss}.py");

                try
                {
                    File.WriteAllText(tempFile, textEditor.Text);
                    return _executor.Run(tempFile);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"一時ファイルの作成に失敗しました: {ex.Message}", "エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            else
            {
                // 元のファイルを実行
                return _executor.Run(_filePath);
            }
        }

        #region コンテキストメニューイベントハンドラー

        // Forループを挿入
        private void InsertForLoop(object sender, EventArgs e)
        {
            string forLoop = "for i in range(10):\n    # ここに処理を記述\n    pass\n";
            InsertTextAtCursor(forLoop);
        }

        // Whileループを挿入
        private void InsertWhileLoop(object sender, EventArgs e)
        {
            string whileLoop = "while condition:\n    # ここに処理を記述\n    pass\n";
            InsertTextAtCursor(whileLoop);
        }

        // If文を挿入
        private void InsertIfStatement(object sender, EventArgs e)
        {
            string ifStatement = "if condition:\n    # 条件が真の時の処理\n    pass\nelse:\n    # 条件が偽の時の処理\n    pass\n";
            InsertTextAtCursor(ifStatement);
        }

        // 関数定義を挿入
        private void InsertFunctionDef(object sender, EventArgs e)
        {
            string functionDef = "def function_name(parameter1, parameter2):\n    \"\"\"\n    関数の説明文\n    \n    Args:\n        parameter1: パラメータ1の説明\n        parameter2: パラメータ2の説明\n    \n    Returns:\n        戻り値の説明\n    \"\"\"\n    # 関数の処理\n    return result\n";
            InsertTextAtCursor(functionDef);
        }

        // クラス定義を挿入
        private void InsertClassDef(object sender, EventArgs e)
        {
            string classDef = "class ClassName:\n    \"\"\"クラスの説明文\"\"\"\n    \n    def __init__(self, parameter1, parameter2):\n        \"\"\"\n        コンストラクタ\n        \n        Args:\n            parameter1: パラメータ1の説明\n            parameter2: パラメータ2の説明\n        \"\"\"\n        self.param1 = parameter1\n        self.param2 = parameter2\n    \n    def method_name(self):\n        \"\"\"メソッドの説明\"\"\"\n        # メソッドの処理\n        pass\n";
            InsertTextAtCursor(classDef);
        }

        // Import文を挿入
        private void InsertImport(object sender, EventArgs e)
        {
            string importStatement = "import numpy as np\nimport pandas as pd\nimport matplotlib.pyplot as plt\n";
            InsertTextAtCursor(importStatement);
        }

        // Try-Catchブロックを挿入
        private void InsertTryCatch(object sender, EventArgs e)
        {
            string tryCatch = "try:\n    # 例外が発生する可能性のある処理\n    pass\nexcept Exception as e:\n    # 例外発生時の処理\n    print(f\"エラーが発生しました: {e}\")\nfinally:\n    # 最終的に実行する処理\n    pass\n";
            InsertTextAtCursor(tryCatch);
        }

        // コメントを挿入
        private void InsertComment(object sender, EventArgs e)
        {
            string comment = "# ここにコメントを記述\n";
            InsertTextAtCursor(comment);
        }

        // カーソル位置にテキストを挿入するヘルパーメソッド
        private void InsertTextAtCursor(string text)
        {
            int selectionStart = textEditor.SelectionStart;
            textEditor.SelectedText = text;
            textEditor.SelectionStart = selectionStart + text.Length;
            textEditor.Focus();
        }

        #endregion
    }
}