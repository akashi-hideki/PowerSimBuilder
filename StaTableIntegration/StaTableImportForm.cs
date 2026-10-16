using System;
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

        // ==============================================================
        // Browse
        // ==============================================================

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "StaTable XML ファイルを選択";
                dlg.Filter = "StaTable XML|*.xml|すべてのファイル|*.*";
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
                MessageBox.Show(this, "先に XML を読み込んでください。",
                    "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "controller_box.py の保存先";
                dlg.Filter = "Python ファイル|*.py|すべてのファイル|*.*";
                dlg.DefaultExt = ".py";
                dlg.AddExtension = true;
                dlg.FileName = "controller_box.py";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    _generator.GenerateToFile(_currentProject, dlg.FileName);
                    MessageBox.Show(this,
                        "生成しました:\n" + dlg.FileName,
                        "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        "生成に失敗しました:\n" + ex.Message,
                        "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}