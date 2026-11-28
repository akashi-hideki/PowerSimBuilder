using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using PowerSimBuilder.ComponentEdit;

namespace PowerSimBuilder
{
    public partial class PythonEditorForm : Form
    {
        private string _originalCode;
        private string _filePath;
        private bool _registerSnippetAvailable;
        
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
        private ToolStripMenuItem menuItemRegisterSnippet;
        private ToolStripMenuItem menuItemRoleSnippets;
        private PythonExecutor _executor;
        private System.Windows.Forms.Timer _debounceTimer;
        
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
            // Debounce timer for TextChanged (300ms)
            _debounceTimer = new System.Windows.Forms.Timer();
            _debounceTimer.Interval = 300;
            _debounceTimer.Tick += (s, ev) => {
                _debounceTimer.Stop();
                RefreshEditPanel();
                editPanel.RefreshPhysicsModelsList(textEditor.Text);
            };

            ConfigureScintilla();
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
            btnRegisterSnippet.Visible = false; // set by UpdateRegisterSnippetAvailability
            
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
                UpdateRegisterSnippetAvailability();
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
            // ▼▼▼ この1行を追加してください ▼▼▼
            editPanel.RefreshPhysicsModelsList(textEditor.Text);
            // ▲▲▲ ▲▲▲
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
            
            // [PHYSICS_PATCH] 物理モデル削除イベントの購読
            editPanel.PhysicsModelRemoveRequested += OnPhysicsModelRemoveRequested;   
            // ▼▼▼ この1行を追加してください ▼▼▼
            //editPanel.PhysicsRequested += (s, e) => ShowPhysicsManagement();
            // ▲▲▲ ▲▲▲
        }
        
        
        // ================================================================
        // Scintilla configuration: Python lexer, line numbers, indent
        // guides, code folding, auto-indent, syntax colors.
        // ================================================================
        private void ConfigureScintilla()
        {
            if (textEditor == null) return;

            // Python lexer
            textEditor.LexerName = "python";

            // Line numbers (margin 0)
            textEditor.Margins[0].Type = ScintillaNET.MarginType.Number;
            textEditor.Margins[0].Width = 40;

            // Indent guides (vertical lines at each indent level)
            textEditor.IndentationGuides = ScintillaNET.IndentView.LookForward;

            // Code folding (margin 1) — collapsible def/class/if/for/while blocks
            textEditor.SetProperty("fold", "1");
            textEditor.SetProperty("fold.compact", "1");
            textEditor.SetProperty("fold.python.comment", "1");
            textEditor.Margins[1].Type = ScintillaNET.MarginType.Symbol;
            textEditor.Margins[1].Width = 15;
            textEditor.Margins[1].Sensitive = true;
            textEditor.Margins[1].Mask = 0xFE000000u;
            // Fold markers at standard Scintilla indices (25-31).
            // SC_MARKNUM_* -> SC_MARK_* (Scintilla.h):
            //   25=FOLDEREND      -> BoxPlusConnected   (mid-fold start)
            //   26=FOLDEROPENMID  -> BoxMinusConnected  (mid-fold open)
            //   27=FOLDERMIDTAIL  -> TCorner            (mid-fold tail)
            //   28=FOLDERTAIL     -> LCorner            (fold tail)
            //   29=FOLDERSUB      -> VLine              (vertical line)
            //   30=FOLDER         -> BoxPlus            (HEAD: collapsed)
            //   31=FOLDEROPEN     -> BoxMinus           (HEAD: expanded)
            textEditor.Markers[25].Symbol = ScintillaNET.MarkerSymbol.BoxPlusConnected;
            textEditor.Markers[26].Symbol = ScintillaNET.MarkerSymbol.BoxMinusConnected;
            textEditor.Markers[27].Symbol = ScintillaNET.MarkerSymbol.TCorner;
            textEditor.Markers[28].Symbol = ScintillaNET.MarkerSymbol.LCorner;
            textEditor.Markers[29].Symbol = ScintillaNET.MarkerSymbol.VLine;
            textEditor.Markers[30].Symbol = ScintillaNET.MarkerSymbol.BoxPlus;
            textEditor.Markers[31].Symbol = ScintillaNET.MarkerSymbol.BoxMinus;

            // Fold toggle on marker click (ScintillaNET requires this explicitly)
            textEditor.MarginClick += (s, e) =>
            {
                if (e.Margin != 1) return;
                int line = textEditor.LineFromPosition(e.Position);
                if (line < 0 || line >= textEditor.Lines.Count) return;
                textEditor.Lines[line].ToggleFold();
            };

            // Auto-indent + tab settings
            textEditor.SetProperty("indent.automatic", "1");
            textEditor.UseTabs = false;
            textEditor.TabWidth = 4;
            textEditor.IndentWidth = 4;

            // Font + style
            textEditor.StyleResetDefault();
            textEditor.Styles[ScintillaNET.Style.Default].Font = "Consolas";
            textEditor.Styles[ScintillaNET.Style.Default].Size = 10;
            textEditor.StyleClearAll();

            // Python syntax colors
            textEditor.Styles[ScintillaNET.Style.Python.Default].ForeColor = System.Drawing.Color.Black;
            textEditor.Styles[ScintillaNET.Style.Python.CommentLine].ForeColor = System.Drawing.Color.Green;
            textEditor.Styles[ScintillaNET.Style.Python.CommentBlock].ForeColor = System.Drawing.Color.Green;
            textEditor.Styles[ScintillaNET.Style.Python.Number].ForeColor = System.Drawing.Color.DarkBlue;
            textEditor.Styles[ScintillaNET.Style.Python.String].ForeColor = System.Drawing.Color.Brown;
            textEditor.Styles[ScintillaNET.Style.Python.Character].ForeColor = System.Drawing.Color.Brown;
            textEditor.Styles[ScintillaNET.Style.Python.Word].ForeColor = System.Drawing.Color.Blue;
            textEditor.Styles[ScintillaNET.Style.Python.Word2].ForeColor = System.Drawing.Color.DarkViolet;
            textEditor.Styles[ScintillaNET.Style.Python.Operator].ForeColor = System.Drawing.Color.Red;
            textEditor.Styles[ScintillaNET.Style.Python.Decorator].ForeColor = System.Drawing.Color.DarkMagenta;

            // Python keywords (set 0) + builtins (set 1)
            textEditor.SetKeywords(0, "and as assert break class continue def del elif else except False finally for from global if import in is lambda None nonlocal not or pass raise return True try while with yield");
            textEditor.SetKeywords(1, "bool bytearray bytes complex dict float frozenset int list memoryview object set str tuple type");
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
            textEditor.SelectionEnd = textEditor.SelectionStart;
            textEditor.ScrollCaret();
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
            menuItemRoleSnippets = new ToolStripMenuItem(I18n.T("PythonEditor.MenuRoleSnippets")) { Name = "menuItemRoleSnippets" };
            menuItemRoleSnippets.Click += (s, e) => ShowRoleSnippetsDialog();
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(menuItemRoleSnippets);
            menuItemRegisterSnippet = new ToolStripMenuItem(I18n.T("PythonEditor.MenuRegisterSnippet")) { Name = "menuItemRegisterSnippet" };
            menuItemRegisterSnippet.Click += (s, e) => ShowRegisterSnippetDialog();
            contextMenu.Items.Add(menuItemRegisterSnippet);
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
                UpdateRegisterSnippetAvailability();
                // ▼▼▼ この1行を追加してください ▼▼▼
                editPanel.RefreshPhysicsModelsList(textEditor.Text);
                // ▲▲▲ ▲▲▲

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
            // Lightweight: update immediately
            IsModified = textEditor.Text != _originalCode;
            UpdateTitle();
            UpdateStatus();

            // Heavy: debounce the panel refresh (300ms)
            if (_debounceTimer != null)
            {
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
            else
            {
                // Fallback if timer not initialized
                RefreshEditPanel();
                editPanel.RefreshPhysicsModelsList(textEditor.Text);
            }
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
        
        public string GetPythonCode()
        {
            return textEditor.Text;
        }
        
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
        
        private (string layer, string func) DetectRoleFunctionAtCaret()
        {
            if (textEditor == null) { LogSnippet("DetectFail", ("reason", "noEditor")); return (null, null); }
            var caret = textEditor.SelectionStart;
            var text = textEditor.Text ?? "";
            LogSnippet("DetectStart", ("caret", (object)caret), ("textLen", (object)text.Length));
            if (caret < 0 || caret > text.Length) { LogSnippet("DetectFail", ("reason", "caretOutOfRange")); return (null, null); }
            const string SP = "# STABLE_USER_CODE_START:";
            const string EP = "# STABLE_USER_CODE_END:";
            var searchEnd = Math.Min(caret, text.Length - 1);
            if (searchEnd < 0) { LogSnippet("DetectFail", ("reason", "searchEndLt0")); return (null, null); }
            var startIdx = text.LastIndexOf(SP, searchEnd, System.StringComparison.Ordinal);
            LogSnippet("DetectStartFound", ("startIdx", (object)startIdx));
            if (startIdx < 0) { LogSnippet("DetectFail", ("reason", "noStartBeforeCaret")); return (null, null); }
            var endIdx = text.IndexOf(EP, startIdx + SP.Length, System.StringComparison.Ordinal);
            LogSnippet("DetectEndFound", ("endIdx", (object)endIdx));
            if (endIdx < 0) { LogSnippet("DetectFail", ("reason", "noEndAfterStart")); return (null, null); }
            if (caret < startIdx || caret > endIdx)
            {
                LogSnippet("DetectFail", ("reason", "caretNotInside"), ("startIdx", (object)startIdx), ("endIdx", (object)endIdx), ("caret", (object)caret));
                return (null, null);
            }
            var sLine = ReadMarkerLine(text, startIdx + SP.Length);
            var eLine = ReadMarkerLine(text, endIdx + EP.Length);
            LogSnippet("DetectMarkers", ("sLine", (object)(sLine ?? "")), ("eLine", (object)(eLine ?? "")));
            if (sLine == null || eLine == null || sLine != eLine) { LogSnippet("DetectFail", ("reason", "markersMismatch")); return (null, null); }
            var dot = sLine.IndexOf('.');
            if (dot <= 0 || dot >= sLine.Length - 1) { LogSnippet("DetectFail", ("reason", "badFormat")); return (null, null); }
            var layer = sLine.Substring(0, dot);
            var func = sLine.Substring(dot + 1);
            LogSnippet("DetectRoleFunctionOK", ("caret", (object)caret), ("layer", (object)layer), ("func", (object)func));
            return (layer, func);
        }
        
        private static string ReadMarkerLine(string text, int afterMarker)
        {
            var end = text.IndexOf('\n', afterMarker);
            if (end < 0) end = text.Length;
            var s = text.Substring(afterMarker, end - afterMarker).Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }
        
        internal void Test_ShowRoleSnippetsDialog() => ShowRoleSnippetsDialog();
        internal void Test_ShowRegisterSnippetDialog() => ShowRegisterSnippetDialog();
        internal void Test_ShowRegisterSnippetDialogAtFunc(string funcName)
        {
            if (textEditor == null) return;
            var text = textEditor.Text ?? "";
            var needle = "def " + funcName + "(";
            var idx = text.IndexOf(needle, StringComparison.Ordinal);
            if (idx < 0)
            {
                LogSnippet("RegisterSnippetFuncNotFound", ("func", (object)funcName));
                return;
            }
            textEditor.SelectionStart = idx + needle.Length;
            textEditor.SelectionEnd = textEditor.SelectionStart;
            ShowRegisterSnippetDialog();
        }
        internal void Test_UpdateRegisterSnippetAvailability() => UpdateRegisterSnippetAvailability();
        internal bool Test_BtnRegisterSnippetVisible => _registerSnippetAvailable;
        internal bool Test_MenuItemRegisterSnippetVisible => _registerSnippetAvailable;
        internal bool Test_MenuItemRoleSnippetsVisible => _registerSnippetAvailable;
        
        private void ShowRoleSnippetsDialog()
        {
            var (targetLayer, targetFunc) = DetectRoleFunctionAtCaret();
            var available = ParseAvailableRoleFunctions();
            LogSnippet("RoleSnippetsOpen",
                ("caret", (object)(textEditor?.SelectionStart ?? -1)),
                ("targetLayer", (object)(targetLayer ?? "")),
                ("targetFunc", (object)(targetFunc ?? "")),
                ("availableCount", (object)available.Count));
            using var dlg = new RoleSnippetForm(targetLayer, targetFunc, available);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            string actualLayer = dlg.SelectedLayer;
            string actualFunc = dlg.SelectedFunc;
            var msg = string.Format(
                I18n.T("PythonEditor.MsgConfirmCrossInsert"),
                actualLayer, actualFunc,
                dlg.SelectedSnippetLayer, dlg.SelectedSnippetFunc);
            if (MessageBox.Show(this, msg,
                    I18n.T("PythonEditor.RoleSnippetsTitle"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            bool isOwn = (actualLayer == dlg.SelectedSnippetLayer && actualFunc == dlg.SelectedSnippetFunc);
            LogSnippet("SnippetInsertPlan",
                ("snippetLayer", (object)(dlg.SelectedSnippetLayer ?? "")),
                ("snippetFunc", (object)(dlg.SelectedSnippetFunc ?? "")),
                ("targetLayer", (object)(actualLayer ?? "")),
                ("targetFunc", (object)(actualFunc ?? "")),
                ("isSnippetOwn", (object)isOwn),
                ("mode", (object)(isOwn ? "OWN" : "CROSS")));
            InsertRoleSnippet(actualLayer, actualFunc, dlg.SelectedCode);
        }
        
        private System.Collections.Generic.List<string> ParseAvailableRoleFunctions()
        {
            var result = new List<string>();
            var text = textEditor?.Text ?? "";
            const string SP = "STABLE_USER_CODE_START:";
            int idx = 0;
            while ((idx = text.IndexOf(SP, idx, System.StringComparison.Ordinal)) >= 0)
            {
                var end = text.IndexOf('\n', idx);
                if (end < 0) end = text.Length;
                var s = text.Substring(idx + SP.Length, end - idx - SP.Length).Trim();
                if (!string.IsNullOrEmpty(s) && !result.Contains(s)) result.Add(s);
                idx = end;
            }
            return result;
        }
        
        private void UpdateRegisterSnippetAvailability()
        {
            if (textEditor == null) return;
            bool hasMarkers = PowerSimBuilder.SimPartsManege.RoleSnippetRegistrar.ContainsRoleMarkers(textEditor.Text);
            _registerSnippetAvailable = hasMarkers;
            if (btnRegisterSnippet != null)
            {
                btnRegisterSnippet.Visible = hasMarkers;
                if (hasMarkers) btnRegisterSnippet.BringToFront();
            }
            if (menuItemRegisterSnippet != null)
                menuItemRegisterSnippet.Visible = hasMarkers;
            if (menuItemRoleSnippets != null)
                menuItemRoleSnippets.Visible = hasMarkers;
        }
        
        private void ShowRegisterSnippetDialog()
        {
            if (textEditor == null) return;
            var text = textEditor.Text ?? "";
            int line = textEditor.LineFromPosition(textEditor.SelectionStart);
            var fn = PowerSimBuilder.SimPartsManege.RoleSnippetRegistrar
                         .ExtractFunctionAtCursor(text, line);
            if (fn == null || string.IsNullOrEmpty(fn.LayerName))
            {
                LogSnippet("RegisterSnippetNoFunction",
                    ("caret", (object)(textEditor.SelectionStart)));
                MessageBox.Show(this, I18n.T("RegisterSnippet.MsgNoFunction"),
                    I18n.T("RegisterSnippet.Title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            LogSnippet("RegisterSnippetDialogOpen",
                ("layer", (object)fn.LayerName),
                ("func", (object)fn.Name));
            using var dlg = new RegisterSnippetDialog(fn);
            if (dlg.ShowDialog(this) != DialogResult.OK)
            {
                LogSnippet("RegisterSnippetCancelled",
                    ("layer", (object)fn.LayerName),
                    ("func", (object)fn.Name));
                return;
            }
            var registrar = new PowerSimBuilder.SimPartsManege.RoleSnippetRegistrar();
            var source = _filePath ?? "(editor)";
            var result = registrar.Register(fn, source, dlg.Description);
            if (result.RequiresOverwrite)
            {
                LogSnippet("RegisterSnippetOverwritePrompt",
                    ("layer", (object)fn.LayerName),
                    ("func", (object)fn.Name));
                var ask = string.Format(I18n.T("RegisterSnippet.MsgOverwriteConfirm"),
                    fn.LayerName, fn.Name);
                if (MessageBox.Show(this, ask, I18n.T("RegisterSnippet.Title"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    LogSnippet("RegisterSnippetOverwriteCancelled",
                        ("layer", (object)fn.LayerName),
                        ("func", (object)fn.Name));
                    return;
                }
                result = registrar.Register(fn, source, dlg.Description, allowOverwrite: true);
            }
            if (result.Success)
            {
                LogSnippet("RegisterSnippetSuccess",
                    ("layer", (object)fn.LayerName),
                    ("func", (object)fn.Name),
                    ("path", (object)(result.SavedPath ?? "")));
                MessageBox.Show(this,
                    string.Format(I18n.T("RegisterSnippet.MsgSuccess"), fn.LayerName, fn.Name),
                    I18n.T("RegisterSnippet.Title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                LogSnippet("RegisterSnippetFailure",
                    ("layer", (object)fn.LayerName),
                    ("func", (object)fn.Name),
                    ("error", (object)(result.Error ?? "")));
                MessageBox.Show(this,
                    string.Format(I18n.T("RegisterSnippet.MsgFailure"), result.Error ?? ""),
                    I18n.T("RegisterSnippet.Title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            try
            {
                textEditor.SelectionStart = lineEnd;
                textEditor.SelectionEnd = textEditor.SelectionStart + Math.Min(sb.Length, Math.Max(0, textEditor.Text.Length - lineEnd));
                textEditor.ScrollCaret();
                textEditor.Focus();
            }
            catch { }
            if (lblStatus != null)
            {
                lblStatus.Text = string.Format(
                    I18n.T("PythonEditor.MsgRoleSnippetInserted"),
                    layer, func, sb.Length);
            }
        }
        
        private void BtnRegisterSnippet_Click(object sender, EventArgs e)
        {
            ShowRegisterSnippetDialog();
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
        
        public bool ExecutePythonScript()
        {
            if (_executor == null)
            {
                MessageBox.Show(I18n.T("PythonEditor.MsgNoExecutorDetail"),
                    I18n.T("PythonEditor.MsgExecError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (IsModified)
            {
                DialogResult result = SuppressDialogs
                    ? DialogResult.Yes
                    : MessageBox.Show(I18n.T("PythonEditor.MsgSaveTempBeforeRun"),
                        I18n.T("Common.MsgTitleConfirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No)
                    return false;
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
                string originalCode = File.ReadAllText(_filePath);
                string overrideCode = ApplySimTimeOverride(originalCode);
                string tmp = Path.Combine(ResolveRunDirectory(), $"temp_{DateTime.Now:yyyyMMdd_HHmmss}.py");
                File.WriteAllText(tmp, overrideCode);
                return _executor.Run(tmp);
            }
        }
        
        #region コンテキストメニューイベントハンドラー
        private void InsertForLoop(object sender, EventArgs e)
        {
            string forLoop = "for i in range(10):\n    # ここに処理を記述\n    pass\n";
            InsertTextAtCursor(forLoop);
        }
        private void InsertWhileLoop(object sender, EventArgs e)
        {
            string whileLoop = "while condition:\n    # ここに処理を記述\n    pass\n";
            InsertTextAtCursor(whileLoop);
        }
        private void InsertIfStatement(object sender, EventArgs e)
        {
            string ifStatement = "if condition:\n    # 条件が真の時の処理\n    pass\nelse:\n    # 条件が偽の時の処理\n    pass\n";
            InsertTextAtCursor(ifStatement);
        }
        private void InsertFunctionDef(object sender, EventArgs e)
        {
            string functionDef = "def function_name(parameter1, parameter2):\n    \"\"\"\n    関数の説明文\n    \n    Args:\n        parameter1: パラメータ1の説明\n        parameter2: パラメータ2の説明\n    \n    Returns:\n        戻り値の説明\n    \"\"\"\n    # 関数の処理\n    return result\n";
            InsertTextAtCursor(functionDef);
        }
        private void InsertClassDef(object sender, EventArgs e)
        {
            string classDef = "class ClassName:\n    \"\"\"クラスの説明文\"\"\"\n    \n    def __init__(self, parameter1, parameter2):\n        \"\"\"\n        コンストラクタ\n        \n        Args:\n            parameter1: パラメータ1の説明\n            parameter2: パラメータ2の説明\n        \"\"\"\n        self.param1 = parameter1\n        self.param2 = parameter2\n    \n    def method_name(self):\n        \"\"\"メソッドの説明\"\"\"\n        # メソッドの処理\n        pass\n";
            InsertTextAtCursor(classDef);
        }
        private void InsertImport(object sender, EventArgs e)
        {
            string importStatement = "import numpy as np\nimport pandas as pd\nimport matplotlib.pyplot as plt\n";
            InsertTextAtCursor(importStatement);
        }
        private void InsertTryCatch(object sender, EventArgs e)
        {
            string tryCatch = "try:\n    # 例外が発生する可能性のある処理\n    pass\nexcept Exception as e:\n    # 例外発生時の処理\n    print(f\"エラーが発生しました: {e}\")\nfinally:\n    # 最終的に実行する処理\n    pass\n";
            InsertTextAtCursor(tryCatch);
        }
        private void InsertComment(object sender, EventArgs e)
        {
            string comment = "# ここにコメントを記述\n";
            InsertTextAtCursor(comment);
        }
        private void InsertTextAtCursor(string text)
        {
            int selectionStart = textEditor.SelectionStart;
            textEditor.ReplaceSelection(text);
            textEditor.SelectionStart = selectionStart + text.Length;
            textEditor.Focus();
        }
        #endregion

        // [PHYSICS_PATCH] 物理モデル削除イベントハンドラ
        private void OnPhysicsModelRemoveRequested(object sender, string modelName)
        {
            try
            {
                string currentCode = textEditor.Text;
                string newCode = PhysicsBlockManager.RemovePhysicsBlock(currentCode, modelName);
                textEditor.Text = newCode;
                editPanel.RefreshPhysicsModelsList(newCode);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error removing physics model", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
           // [/PHYSICS_PATCH]
    }
}