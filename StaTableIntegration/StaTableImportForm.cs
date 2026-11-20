using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// StaTable XML を取り込み、controller_box.py を生成するフォーム。
    /// </summary>
    public partial class StaTableImportForm : Form
    {
        private readonly StaTableProjectReader _reader;
        private readonly StaTableCodeGenerator _generator;
        private StaTableProject _currentProject;

        public StaTableImportForm()
        {
            InitializeComponent();
            _reader = new StaTableProjectReader();
            _generator = new StaTableCodeGenerator();
        }

        /// <summary>テスト用: 生成された Python コード (最後のプレビュー内容)</summary>
        public string GeneratedCode { get; private set; }

        /// <summary>取り込み成功時の StaTableProject (Task 3-C-6)</summary>
        public StaTableProject ImportedProject { get; private set; }

        /// <summary>生成された .py ファイルの絶対パス (Task 3-C-6)</summary>
        public string ImportedGeneratedPath { get; private set; }

        /// <summary>取り込み元 XML の絶対パス (Task 3-C-6)</summary>
        public string ImportedSourceXmlPath { get; private set; }

        /// <summary>P2: ユーザーが選択した Input ポート (null = 旧挙動 auto extract)</summary>
        public IReadOnlyList<string> SelectedInputPorts { get; private set; }

        /// <summary>P2: ユーザーが選択した Output ポート (null = 旧挙動 auto extract)</summary>
        public IReadOnlyList<string> SelectedOutputPorts { get; private set; }

        // ==============================================================
        // Browse
        // ==============================================================

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = I18n.T("StaTableImport.BrowseXmlTitle");
                dlg.Filter = I18n.T("Common.FilterXml");
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                txtFilePath.Text = dlg.FileName;
                LoadAndPreview(dlg.FileName);
            }
        }

        // ==============================================================
        // Load + Preview
        // ==============================================================

        private void LoadAndPreview(string path)
        {
            var result = _reader.ParseFile(path);

            if (result.Errors.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("[ERRORS]");
                foreach (var err in result.Errors) sb.AppendLine("  - " + err);
                if (result.Warnings.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("[WARNINGS]");
                    foreach (var w in result.Warnings) sb.AppendLine("  - " + w);
                }
                txtPreview.Text = sb.ToString();
                btnGenerate.Enabled = false;
                _currentProject = null;
                return;
            }

            _currentProject = result.Project;

            // サマリ + プレビュー
            var summary = BuildSummary(result);
            GeneratedCode = _generator.Generate(_currentProject);

            txtPreview.Text = summary
                + Environment.NewLine
                + Environment.NewLine
                + "===== Generated controller_box.py (preview) ====="
                + Environment.NewLine
                + GeneratedCode;

            btnGenerate.Enabled = true;
        }

        private string BuildSummary(StaTableParseResult result)
        {
            var p = result.Project;
            var sb = new StringBuilder();
            sb.AppendLine("===== StaTable XML 読み込み結果 =====");
            sb.AppendLine("Project: " + p.Name);
            sb.AppendLine("Source SHA256: " + p.SourceXmlHash);
            if (p.Settings != null)
            {
                sb.AppendLine("generation_style: " + p.Settings.GenerationStyle);
                sb.AppendLine("os_type: " + p.Settings.OsType);
            }
            sb.AppendLine("Tabs: " + p.Tabs.Count);
            foreach (var tab in p.Tabs)
            {
                sb.AppendLine(string.Format(
                    "  - {0} (priority {1}, {2} states, {3} transitions)",
                    tab.LayerName, tab.LayerPriority,
                    tab.States.Count, tab.Transitions.Count));
            }
            if (result.Warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[WARNINGS]");
                foreach (var w in result.Warnings) sb.AppendLine("  - " + w);
            }
            return sb.ToString();
        }

        // ==============================================================
        // Generate
        // ==============================================================

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            if (_currentProject == null)
            {
                MessageBox.Show(this, I18n.T("StaTableImport.MsgLoadXmlFirst"),
                    I18n.T("Common.MsgTitleInfo"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = I18n.T("StaTableImport.SavePyTitle");
                dlg.Filter = I18n.T("Common.FilterPython");
                dlg.DefaultExt = ".py";
                dlg.AddExtension = true;

                // Task 3-C-7: プロジェクトルートの generated/ を初期ディレクトリに
                string genDir = GetGeneratedDirectory();
                if (!string.IsNullOrEmpty(genDir))
                {
                    dlg.InitialDirectory = genDir;
                }

                // Task 3-C-7: 既存の標準ファイルがあれば優先（STABLE_USER_CODE 保持）
                string standardPath = Path.Combine(
                    string.IsNullOrEmpty(genDir) ? "" : genDir,
                    "controller_box_powersim.py");
                if (File.Exists(standardPath))
                {
                    dlg.FileName = "controller_box_powersim.py";
                }
                else
                {
                    string projectName = _currentProject != null && !string.IsNullOrEmpty(_currentProject.Name)
                        ? _currentProject.Name
                        : "controller";
                    dlg.FileName = SanitizeFileName(projectName) + "_powersim.py";
                }

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    _generator.GenerateToFile(_currentProject, dlg.FileName);

                    // P2a: PortSelectorDialog (docs/SPEC_PORT_SELECTION.md §2.1)
                    // Cancel = null (旧挙動 auto extract) にフォールバック
                    try
                    {
                        var pyText = File.ReadAllText(dlg.FileName, Encoding.UTF8);
                        var globals = ControllerBoxDescriptorBuilder.ExtractGlobalsForSelection(pyText);
                        if (PortSelectorDialog.TrySelect(this, globals, null, null,
                                out var selIn, out var selOut))
                        {
                            SelectedInputPorts  = selIn;
                            SelectedOutputPorts = selOut;
                        }
                        else
                        {
                            SelectedInputPorts  = null;
                            SelectedOutputPorts = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this,
                            "PortSelectorDialog failed (P2a diagnostic):\n\n" + ex.ToString(),
                            "P2a diagnostic", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        SelectedInputPorts  = null;
                        SelectedOutputPorts = null;
                    }
                    // Task 3-C-6: MainForm が Descriptor を生成できるように公開
                    ImportedProject = _currentProject;
                    ImportedGeneratedPath = System.IO.Path.GetFullPath(dlg.FileName);
                    ImportedSourceXmlPath = _currentProject.SourceFilePath;
                    MessageBox.Show(this,
                        I18n.T("StaTableImport.MsgGenerated", dlg.FileName),
                        I18n.T("Common.MsgTitleSuccess"), MessageBoxButtons.OK, MessageBoxIcon.Information);

            // v1.1.0: open the generated file in PythonEditorForm so that
            // STABLE_USER_CODE / Role Functions are immediately visible.
            OpenGeneratedInEditor(dlg.FileName);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        I18n.T("StaTableImport.MsgGenerateFail", ex.Message),
                        I18n.T("Common.MsgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ==============================================================
        // Task 3-C-7: 保存先の自動提案ヘルパー
        // ==============================================================


        /// <summary>
        /// v1.1.0: after generating a StaTable controller, open the .py in
        /// PythonEditorForm so the user can edit STABLE_USER_CODE blocks and
        /// see Role Functions in the EditPanel.
        /// </summary>
        private void OpenGeneratedInEditor(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return;
            try
            {
                using (var editorForm = new PythonEditorForm(filePath, false))
                {
                    editorForm.ComponentType = "ControllerBox"; // suppress Add Physics (StaTable-generated)
                    editorForm.ProjectDir = System.IO.Path.GetDirectoryName(filePath);
                    editorForm.Executor = new PythonExecutor();
                    editorForm.ShowDialog(this);
                }
            }
            catch (System.Exception ex)
            {
                AppServices.MessageBox.ShowError(this, ex.Message, I18n.T("Common.MsgTitleError"));
            }
        }

        /// <summary>
        /// プロジェクトルートの generated/ ディレクトリを探す。
        /// EXE の BaseDirectory から .sln を目印に遡り、見つかった
        /// プロジェクトルートの generated/ を返す。
        /// 見つからなければ EXE フォルダ配下の generated/ を返す。
        /// </summary>
        private static string GetGeneratedDirectory()
        {
            try
            {
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                while (dir != null)
                {
                    if (File.Exists(Path.Combine(dir.FullName, "PowerSimBuilder.sln")))
                    {
                        var gen = Path.Combine(dir.FullName, "generated");
                        if (!Directory.Exists(gen))
                            Directory.CreateDirectory(gen);
                        return gen;
                    }
                    dir = dir.Parent;
                }
            }
            catch
            {
                // 権限等で失敗したらフォールバック
            }

            var fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "generated");
            try
            {
                if (!Directory.Exists(fallback))
                    Directory.CreateDirectory(fallback);
            }
            catch
            {
                // 作成できなくてもパスだけ返す
            }
            return fallback;
        }

        /// <summary>
        /// ファイル名に使えない文字を '_' に置換する。
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "controller";
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }
            return sb.ToString();
        }
    }
}
