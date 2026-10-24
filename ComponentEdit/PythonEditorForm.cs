using System;
using System.IO;
using System.Windows.Forms;
using PowerSimBuilder.ComponentEdit;

namespace PowerSimBuilder
{
    public partial class PythonEditorForm : Form
    {
        private string _originalCode;
        private string _filePath;

        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem menuItemForLoop;
        private ToolStripMenuItem menuItemWhileLoop;
        private ToolStripMenuItem menuItemIfStatement;
        private ToolStripMenuItem menuItemFunctionDef;
        private ToolStripMenuItem menuItemClassDef;
        private ToolStripMenuItem menuItemImport;
        private ToolStripMenuItem menuItemTryCatch;
        private ToolStripMenuItem menuItemComment;

        private PythonExecutor _executor;

        public bool IsModified { get; private set; }
        public string PythonCode => textEditor.Text;

        public PythonExecutor Executor
        {
            get { return _executor; }
            set { _executor = value; }
        }

        public string ProjectDir
        {
            get { return editPanel.ProjectDir; }
            set { editPanel.ProjectDir = value; }
        }

        public PythonEditorForm(string filePath, bool enableRunButtons)
            : this(filePath, enableRunButtons, null) { }

        /// <summary>
        /// Lazy-save constructor. When filePath is null, the editor opens as
        /// an unsaved document: no disk write until Save. initialCode (if
        /// non-null) is shown as-is. On first Save, SaveFileDialog is shown
        /// using SuggestedFileName as the default filename.
        /// </summary>
        public PythonEditorForm(string filePath, bool enableRunButtons, string initialCode)
        {
            InitializeComponent();
            InitializeContextMenu();
            WireEditPanel();

            _filePath = filePath;

            btnRun.Visible = enableRunButtons;
            btnRun.Enabled = enableRunButtons;
            btnReload.Visible = enableRunButtons;
            btnReload.Enabled = enableRunButtons;
            btnOpenFolder.Visible = enableRunButtons;
            btnOpenFolder.Enabled = enableRunButtons;
            lblStatus.Visible = enableRunButtons;

            if (filePath != null)
            {
                LoadFile();
            }
            else
            {
                _originalCode = initialCode ?? "";
                textEditor.Text = _originalCode;
                IsModified = !string.IsNullOrEmpty(initialCode);
                UpdateTitle();
                UpdateStatus();
                // Reload is meaningless for an unsaved document
                btnReload.Enabled = false;
            }
            RefreshEditPanel();
        }

        /// <summary>
        /// Default filename proposed in SaveFileDialog on the first save
        /// (e.g. "DigitalInput.py").
        /// </summary>
        public string SuggestedFileName { get; set; }

        /// <summary>
        /// Initial directory proposed in SaveFileDialog on the first save.
        /// </summary>
        public string SuggestedInitialDirectory { get; set; }

        private void WireEditPanel()
        {
            editPanel.JumpRequested += (s, info) => JumpToLine(info.StartLine);
            editPanel.JumpToLine += (s, line) => JumpToLine(line);
            editPanel.OverrideRequested += (s, info) => CreateOverride(info);
            editPanel.AdapterRequested += (s, e) =>
                AppServices.MessageBox.ShowInfo(this, I18n.T("EditPanel.MsgAdapterNotReady"), I18n.T("Common.MsgTitleInfo"));
            editPanel.ImportCRequested += (s, e) =>
                AppServices.MessageBox.ShowInfo(this, I18n.T("EditPanel.MsgImportCNotReady"), I18n.T("Common.MsgTitleInfo"));
        }

        public void RefreshEditPanel()
        {
            try
            {
                var rootDir = FindRepoRoot();
                var projectDir = ProjectDir ?? Directory.GetCurrentDirectory();

                var bundled = rootDir != null
                    ? ComponentScanner.ScanAll(rootDir, projectDir)
                    : (System.Collections.Generic.IReadOnlyList<ComponentInfo>)new ComponentInfo[0];

                var classes = FileStructureParser.Parse(textEditor.Text);
                editPanel.LoadFileStructure(classes, bundled);
                editPanel.LoadRoleFunctions(RoleFunctionParser.Parse(textEditor.Text));
            }
            catch (Exception ex)
            {
                lblStatus.Text = ex.Message;
            }
        }

        private static string FindRepoRoot()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "PowerSimBuilder.sln"))) return dir;
                var parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            return null;
        }

        private void JumpToLine(int oneBasedLine)
        {
            if (oneBasedLine < 1) return;
            var lines = textEditor.Text.Replace("\r\n", "\n").Split('\n');
            if (oneBasedLine > lines.Length) return;
            int offset = 0;
            for (int i = 0; i < oneBasedLine - 1; i++)
                offset += lines[i].Length + 1;
            textEditor.SelectionStart = offset;
            textEditor.SelectionLength = 0;
            textEditor.ScrollToCaret();
            textEditor.Focus();
        }

        private void CreateOverride(ComponentInfo info)
        {
            if (info == null || info.Source != ComponentSourceKind.Bundled) return;
            var projectDir = ProjectDir ?? Directory.GetCurrentDirectory();
            try
            {
                var targetDir = Path.Combine(projectDir, "user_components");
                var createdPath = ComponentOverrideHelper.Scaffold(info.FilePath, targetDir, info.Name);
                AppServices.MessageBox.ShowInfo(this, I18n.T("EditPanel.MsgOverrideSuccess", createdPath), I18n.T("Common.MsgTitleSuccess"));
                RefreshEditPanel();
            }
            catch (Exception ex)
            {
                AppServices.MessageBox.ShowError(this, I18n.T("EditPanel.MsgOverrideFail", ex.Message), I18n.T("Common.MsgTitleError"));
            }
        }

        private void InitializeContextMenu()
        {
            contextMenu = new ContextMenuStrip();

            menuItemForLoop = new ToolStripMenuItem(I18n.T("PythonEditor.MenuInsertForLoop"));
            menuItemWhileLoop = new ToolStripMenuItem(I18n.T("PythonEditor.MenuInsertWhileLoop"));
            menuItemIfStatement = new ToolStripMenuItem(I18n.T("PythonEditor.MenuInsertIf"));
            menuItemFunctionDef = new ToolStripMenuItem(I18n.T("PythonEditor.MenuInsertFunction"));
            menuItemClassDef = new ToolStripMenuItem(I18n.T("PythonEditor.MenuInsertClass"));
            menuItemImport = new ToolStripMenuItem(I18n.T("PythonEditor.MenuInsertImport"));
            menuItemTryCatch = new ToolStripMenuItem(I18n.T("PythonEditor.MenuInsertTryCatch"));
            menuItemComment = new ToolStripMenuItem(I18n.T("PythonEditor.MenuInsertComment"));

            menuItemForLoop.Click += InsertForLoop;
            menuItemWhileLoop.Click += InsertWhileLoop;
            menuItemIfStatement.Click += InsertIfStatement;
            menuItemFunctionDef.Click += InsertFunctionDef;
            menuItemClassDef.Click += InsertClassDef;
            menuItemImport.Click += InsertImport;
            menuItemTryCatch.Click += InsertTryCatch;
            menuItemComment.Click += InsertComment;

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

            AppendLineOpsMenuItems(contextMenu);
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
                MessageBox.Show(I18n.T("PythonEditor.MsgLoadFail", ex.Message), I18n.T("Common.MsgTitleError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
            }
        }

        // ---- Preserved from original (unchanged) ----
        private void UpdateTitle()
                {
                    var displayName = string.IsNullOrEmpty(_filePath)
                        ? "(unsaved)"
                        : Path.GetFileName(_filePath);
                    this.Text = I18n.T("PythonEditor.TitleTemplate", displayName, IsModified ? " *" : "");
                }

                private void UpdateStatus()
                {
                    lblStatus.Text = IsModified ? I18n.T("PythonEditor.LblModified") : I18n.T("PythonEditor.LblUnmodified");
                }

                private void textEditor_TextChanged(object sender, EventArgs e)
                {
            RefreshEditPanel();

                    IsModified = textEditor.Text != _originalCode;
                    UpdateTitle();
                    UpdateStatus();
                }

                private void btnSave_Click(object sender, EventArgs e)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(_filePath))
                        {
                            var savePath = AppServices.FileDialog.PickSavePath(
                                this,
                                I18n.T("Common.FilterPython"),
                                I18n.T("PythonEditor.SaveDialogTitle"),
                                string.IsNullOrEmpty(SuggestedFileName) ? "component.py" : SuggestedFileName);
                            if (string.IsNullOrEmpty(savePath)) return;
                            _filePath = savePath;
                        }

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
                        AppServices.MessageBox.ShowError(this,
                            I18n.T("PythonEditor.MsgSaveFail", ex.Message),
                            I18n.T("Common.MsgTitleError"));
                    }
                }

                private void btnCancel_Click(object sender, EventArgs e)
                {
                    if (IsModified)
                    {
                        if (!AppServices.MessageBox.ShowConfirm(this,
                            I18n.T("PythonEditor.MsgUnsavedClose"),
                            I18n.T("Common.MsgTitleConfirm")))
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
                        var result = MessageBox.Show(I18n.T("PythonEditor.MsgUnsavedDiscard"),
                            I18n.T("Common.MsgTitleConfirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

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
                            MessageBox.Show(I18n.T("PythonEditor.MsgFolderNotFound"), I18n.T("Common.MsgTitleError"),
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(I18n.T("PythonEditor.MsgFolderOpenFail", ex.Message), I18n.T("Common.MsgTitleError"),
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
                        MessageBox.Show(I18n.T("PythonEditor.MsgNoExecutorDetail"),
                            I18n.T("PythonEditor.MsgExecError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    // 変更があれば一時ファイルに保存して実行
                    if (IsModified)
                    {
                        var result = MessageBox.Show(I18n.T("PythonEditor.MsgSaveTempBeforeRun"),
                            I18n.T("Common.MsgTitleConfirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

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
                            MessageBox.Show(I18n.T("PythonEditor.MsgTempFail", ex.Message), I18n.T("Common.MsgTitleError"),
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
