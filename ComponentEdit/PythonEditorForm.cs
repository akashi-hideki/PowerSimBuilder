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

        /// <summary>Simulation t_end (seconds). Persisted to .psbproj.</summary>
        public double SimTime
        {
            get { return numSimTime != null ? (double)numSimTime.Value : 5.0; }
            set { if (numSimTime != null) numSimTime.Value = (decimal)value; }
        }

        public event EventHandler<double> SimTimeChanged;


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

        /// <summary>
        /// Structured JSON-lines log for Role Snippet insertion diagnostics.
        /// Writes to PSB_EDIT_LOG (default %TEMP%\psb_edit_debug.log).
        /// </summary>
        private static void LogSnippet(string evt, params (string key, object val)[] fields)
        {
            try
            {
                var path = System.Environment.GetEnvironmentVariable("PSB_EDIT_LOG")
                    ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "psb_edit_debug.log");
                var session = System.Environment.GetEnvironmentVariable("PSB_EDIT_SESSION") ?? "?";
                var obj = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["ts"] = System.DateTime.Now.ToString("HH:mm:ss.fff"),
                    ["session"] = session,
                    ["event"] = evt,
                    ["source"] = "PythonEditorForm",
                };
                if (fields != null) foreach (var (k, v) in fields) obj[k] = v;
                var json = System.Text.Json.JsonSerializer.Serialize(obj);
                System.IO.File.AppendAllText(path, json + System.Environment.NewLine);
            }
            catch { }
        }
        public string PythonCode => textEditor.Text;

        public bool SuppressDialogs { get; set; } = false;

        public PythonExecutor Executor
        {
            get { return _executor; }
            set
            {
                if (_executor != null) _executor.Completed -= OnExecutorCompleted;
                _executor = value;
                if (_executor != null) _executor.Completed += OnExecutorCompleted;
            }
        }

        public string ProjectDir
        {
            get { return editPanel.ProjectDir; }
            set { editPanel.ProjectDir = value; }
        }

        public PythonEditorForm(string filePath, bool enableRunButtons, double initialTEnd = 5.0)
            : this(filePath, enableRunButtons, null, initialTEnd) { }

        /// <summary>
        /// Lazy-save constructor. When filePath is null, the editor opens as
        /// an unsaved document: no disk write until Save. initialCode (if
        /// non-null) is shown as-is. On first Save, SaveFileDialog is shown
        /// using SuggestedFileName as the default filename.
        /// </summary>
        public PythonEditorForm(string filePath, bool enableRunButtons, string initialCode, double initialTEnd = 5.0)
        {
            InitializeComponent();
            InitializeContextMenu();
            WireEditPanel();

            _filePath = filePath;
            // t_end persistence (schema v2: metadata.tEnd)
            if (initialTEnd > 0 && numSimTime != null)
                numSimTime.Value = (decimal)initialTEnd;
            if (numSimTime != null)
                numSimTime.ValueChanged += (s, e) => SimTimeChanged?.Invoke(this, SimTime);


            btnRun.Visible = enableRunButtons;
            btnRun.Enabled = enableRunButtons;
            btnReload.Visible = enableRunButtons;
            btnReload.Enabled = enableRunButtons;
            btnOpenFolder.Visible = enableRunButtons;
            btnOpenFolder.Enabled = enableRunButtons;
            lblStatus.Visible = enableRunButtons;
            if (lblSimTime != null) lblSimTime.Visible = enableRunButtons;
            if (numSimTime != null) numSimTime.Visible = enableRunButtons;

            
            // Re-apply button visibility after the form is fully shown.
            // Fixes first-launch layout race (buttons hidden until language switch).
            this.Shown += (s, e) =>
            {
                btnRun.Visible = enableRunButtons;
                btnRun.BringToFront();
                btnReload.Visible = enableRunButtons;
                btnReload.BringToFront();
                btnOpenFolder.Visible = enableRunButtons;
                btnOpenFolder.BringToFront();
                lblStatus.Visible = enableRunButtons;
                if (lblSimTime != null) lblSimTime.Visible = enableRunButtons;
                if (numSimTime != null) numSimTime.Visible = enableRunButtons;
                panelTop.PerformLayout();
                panelTop.Invalidate(true);
                panelTop.Update();
            };

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
            var menuItemRoleSnippets = new ToolStripMenuItem(I18n.T("PythonEditor.MenuRoleSnippets"));
            menuItemRoleSnippets.Click += (s, e) => ShowRoleSnippetsDialog();
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(menuItemRoleSnippets);
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
                private string ApplySimTimeOverride(string code)
                {
                    if (numSimTime == null) return code;
                    return SimTimeOverride.Apply(code, (double)numSimTime.Value);
                }

                private string ResolveRunDirectory()
                {
                    if (!string.IsNullOrEmpty(_filePath))
                    {
                        var dir = Path.GetDirectoryName(_filePath);
                        if (!string.IsNullOrEmpty(dir)) return dir;
                    }
                    if (!string.IsNullOrEmpty(ProjectDir)) return ProjectDir;
                    if (!string.IsNullOrEmpty(SuggestedInitialDirectory)) return SuggestedInitialDirectory;
                    var cwd = Environment.CurrentDirectory;
                    if (!string.IsNullOrEmpty(cwd)) return cwd;
                    return Path.GetTempPath();
                }
                private void BtnRun_Click(object sender, EventArgs e)
                {
                    if (lblStatus != null) { lblStatus.Text = "Running..."; lblStatus.Refresh(); }
                    var ok = ExecutePythonScript();
                    if (!ok && lblStatus != null) { lblStatus.Text = "Failed to start"; }
                }

                // 再読み込みボタンのクリック処理
                private void OnExecutorCompleted(bool success, int exitCode, string stderr)
                {
                    if (lblStatus == null || lblStatus.IsDisposed) return;
                    Action update = () =>
                    {
                        try
                        {
                            lblStatus.Text = success
                                ? I18n.T("PythonExecutor.MsgCompleted")
                                : I18n.T("PythonExecutor.MsgExitedWithCode", exitCode);
                        }
                        catch { }
                    };
                    try
                    {
                        if (lblStatus.InvokeRequired) lblStatus.BeginInvoke(update);
                        else update();
                    }
                    catch { }
                }

                private void ShowRoleSnippetsDialog()
                {
                    using var dlg = new RoleSnippetForm();
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        InsertRoleSnippet(dlg.SelectedLayer, dlg.SelectedFunc, dlg.SelectedCode);
                    }
                }

                private void InsertRoleSnippet(string layer, string func, string code)
                {
                    if (string.IsNullOrEmpty(layer) || string.IsNullOrEmpty(func)) { LogSnippet("SnippetInsertSkipEmpty"); return; }
                    LogSnippet("SnippetInsertStart", ("layer", layer), ("func", func), ("codeLen", (code ?? "").Length));
                    var text = textEditor.Text;
                    var nl = text.Contains("\r\n") ? "\r\n" : "\n";
                    var startMarker = "# STABLE_USER_CODE_START:" + layer + "." + func;
                    var endMarker = "# STABLE_USER_CODE_END:" + layer + "." + func;
                    var startIdx = text.IndexOf(startMarker, System.StringComparison.Ordinal);
                    var endIdx = text.IndexOf(endMarker, System.StringComparison.Ordinal);
                    if (startIdx < 0 || endIdx < 0 || endIdx < startIdx)
                    {
                        MessageBox.Show(this,
                            string.Format(I18n.T("PythonEditor.MsgRoleSnippetNoMarkers"), layer, func),
                            I18n.T("PythonEditor.RoleSnippetsTitle"),
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    var lineEnd = text.IndexOf(nl, startIdx);
                    if (lineEnd < 0) lineEnd = text.Length;
                    else lineEnd += nl.Length;
                    var lineStart = text.LastIndexOf(nl, endIdx);
                    if (lineStart < 0) lineStart = 0;
                    else lineStart += nl.Length;
                    var sb = new System.Text.StringBuilder();
                    var codeLines = (code ?? "").Replace("\r\n", "\n").Split('\n');
                    foreach (var ln in codeLines)
                    {
                        if (string.IsNullOrWhiteSpace(ln)) { sb.Append(nl); continue; }
                        sb.Append("        ");
                        sb.Append(ln.TrimStart());
                        sb.Append(nl);
                    }
                    LogSnippet("SnippetMarkersFound", ("startIdx", startIdx), ("endIdx", endIdx), ("bodyNewLen", sb.Length), ("oldTotalLen", text.Length));
                    var newText = text.Substring(0, lineEnd) + sb.ToString() + text.Substring(lineStart);
                    var beforeLen = textEditor.Text.Length;
                    textEditor.Text = newText;
                    var afterLen = textEditor.Text.Length;
                    LogSnippet("SnippetInsertDone",
                        ("beforeLen", beforeLen),
                        ("afterLen", afterLen),
                        ("delta", afterLen - beforeLen),
                        ("editorType", textEditor.GetType().Name),
                        ("isModified", IsModified));
                    // Move caret + select inserted region so the user sees the change.
                    try
                    {
                        textEditor.SelectionStart = lineEnd;
                        textEditor.SelectionLength = Math.Min(sb.Length, Math.Max(0, textEditor.Text.Length - lineEnd));
                        textEditor.ScrollToCaret();
                        textEditor.Focus();
                    }
                    catch { }
                    // Status feedback.
                    if (lblStatus != null)
                    {
                        lblStatus.Text = string.Format(
                            I18n.T("PythonEditor.MsgRoleSnippetInserted"),
                            layer, func, sb.Length);
                    }
                }
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
                        DialogResult result = SuppressDialogs
                            ? DialogResult.Yes
                            : MessageBox.Show(I18n.T("PythonEditor.MsgSaveTempBeforeRun"),
                                I18n.T("Common.MsgTitleConfirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (result == DialogResult.No)
                            return false;

                        // 一時ファイルを作成
                        string tempFile = Path.Combine(
                            ResolveRunDirectory(),
                            $"temp_{DateTime.Now:yyyyMMdd_HHmmss}.py");

                        try
                        {
                            File.WriteAllText(tempFile, ApplySimTimeOverride(textEditor.Text));
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
                        string originalCode = File.ReadAllText(_filePath);
                        string overrideCode = ApplySimTimeOverride(originalCode);
                        string tmp = Path.Combine(ResolveRunDirectory(), $"temp_{DateTime.Now:yyyyMMdd_HHmmss}.py");
                        File.WriteAllText(tmp, overrideCode);
                        return _executor.Run(tmp);
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
