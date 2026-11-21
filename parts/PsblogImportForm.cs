using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>Data passed from <see cref="PsblogImportForm"/> back to MainForm.</summary>
    public sealed class PsblogImportResult
    {
        public string PsblogPath { get; init; } = "";
        public IReadOnlyList<string> SelectedVariables { get; init; } = Array.Empty<string>();
        public bool PlaceInferredComponents { get; init; }
        public bool AddToPalette { get; init; }
        public IReadOnlyDictionary<string, string?> ProposedComponents { get; init; }
            = new Dictionary<string, string?>();
    }

    /// <summary>
    /// Import a .psblog signal log, choose variables, and hand the result
    /// back to MainForm which places RecordedSignal components on the Canvas.
    /// </summary>
    public sealed class PsblogImportForm : Form
    {
        private readonly TextBox _txtPath;
        private readonly Button _btnBrowse;
        private readonly GroupBox _gbSummary;
        private readonly Label _lblSource;
        private readonly Label _lblCaptured;
        private readonly Label _lblDuration;
        private readonly Label _lblWarnings;
        private readonly GroupBox _gbVars;
        private readonly ListView _lvVariables;
        private readonly CheckBox _chkPlaceInferred;
        private readonly CheckBox _chkAddToPalette;
        private readonly Button _btnCancel;
        private readonly Button _btnAddToCanvas;

        private PsblogTraceDto? _dto;
        private string _fullPath = "";
        private readonly Dictionary<string, PsblogClassificationDto> _classifications = new();

        public PsblogImportResult? Result { get; private set; }

        public PsblogImportForm()
        {
            Name = "PsblogImportForm";
            Text = I18n.T("PsblogImport.Title");
            Width = 960;
            Height = 640;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;

            // --- Top: file path ---
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 40 };
            var lblFile = new Label
            {
                Text = I18n.T("PsblogImport.FileLabel"),
                Location = new Point(10, 12),
                AutoSize = true,
            };
            _txtPath = new TextBox
            {
                Location = new Point(100, 10),
                Width = 700,
                ReadOnly = true,
            };
            _btnBrowse = new Button
            {
                Text = I18n.T("PsblogImport.Browse"),
                Location = new Point(810, 8),
                Width = 120,
            };
            _btnBrowse.Click += OnBrowse;
            topPanel.Controls.AddRange(new Control[] { lblFile, _txtPath, _btnBrowse });

            // --- Summary ---
            _gbSummary = new GroupBox
            {
                Text = I18n.T("PsblogImport.Summary"),
                Dock = DockStyle.Top,
                Height = 100,
                Padding = new Padding(10),
            };
            _lblSource = new Label { Location = new Point(10, 25), AutoSize = true };
            _lblCaptured = new Label { Location = new Point(10, 50), AutoSize = true };
            _lblDuration = new Label { Location = new Point(480, 25), AutoSize = true };
            _lblWarnings = new Label { Location = new Point(480, 50), AutoSize = true };
            _gbSummary.Controls.AddRange(new Control[] { _lblSource, _lblCaptured, _lblDuration, _lblWarnings });

            // --- Variables ---
            _gbVars = new GroupBox
            {
                Text = I18n.T("PsblogImport.Variables"),
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
            };
            _lvVariables = new ListView
            {
                Name = "LvVariables",
                Dock = DockStyle.Fill,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                GridLines = true,
            };
            _lvVariables.Columns.Add(I18n.T("PsblogImport.ColName"), 200);
            _lvVariables.Columns.Add(I18n.T("PsblogImport.ColType"), 100);
            _lvVariables.Columns.Add(I18n.T("PsblogImport.ColUnit"), 80);
            _lvVariables.Columns.Add(I18n.T("PsblogImport.ColShape"), 110);
            _lvVariables.Columns.Add(I18n.T("PsblogImport.ColComponent"), 220);
            _gbVars.Controls.Add(_lvVariables);

            // --- Bottom ---
            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            _chkPlaceInferred = new CheckBox
            {
                Text = I18n.T("PsblogImport.PlaceInferred"),
                Location = new Point(10, 18),
                AutoSize = true,
            };
            _chkAddToPalette = new CheckBox
            {
                Text = I18n.T("PsblogImport.AddToPalette"),
                Location = new Point(260, 18),
                AutoSize = true,
                Checked = true,
            };
            _btnCancel = new Button
            {
                Name = "BtnCancel",
                Text = I18n.T("PsblogImport.Cancel"),
                Location = new Point(690, 15),
                Width = 110,
                DialogResult = DialogResult.Cancel,
            };
            _btnAddToCanvas = new Button
            {
                Name = "BtnAddToCanvas",
                Text = I18n.T("PsblogImport.AddToCanvas"),
                Location = new Point(810, 15),
                Width = 130,
                Enabled = false,
                DialogResult = DialogResult.OK,
            };
            _btnAddToCanvas.Click += OnAddToCanvas;
            bottomPanel.Controls.AddRange(new Control[] { _chkPlaceInferred, _chkAddToPalette, _btnCancel, _btnAddToCanvas });
            // Docking order: fill added first, then bottom, then top (last wins for Top).
            Controls.Add(_gbVars);
            Controls.Add(bottomPanel);
            Controls.Add(_gbSummary);
            Controls.Add(topPanel);

            AcceptButton = _btnAddToCanvas;
            CancelButton = _btnCancel;
            this.FormClosing += (s, e) =>
            {
                TraceDiag($"FormClosing: CloseReason={e.CloseReason} Cancel={e.Cancel} DialogResult={DialogResult}");
            };
        }

        /// <summary>Test hook: pre-load a log file (used by FlaUI E2E).</summary>
        public void Test_LoadLog(string path) => LoadLog(path);

        /// <summary>Test hook: number of variables loaded.</summary>
        public int Test_VariableCount => _lvVariables.Items.Count;

        private void OnBrowse(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = I18n.T("PsblogImport.Title"),
                Filter = "PowerSimBuilder Log (*.psblog)|*.psblog|All Files (*.*)|*.*",
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                LoadLog(dlg.FileName);
        }

        private void LoadLog(string path)
        {
            try
            {
                _fullPath = Path.GetFullPath(path);
                _dto = PsblogParser.Parse(_fullPath, includeClassifications: true);
                _classifications.Clear();
                foreach (var c in _dto.Classifications)
                    _classifications[c.VarName] = c;
                _txtPath.Text = _fullPath;
                UpdateSummary();
                PopulateVariables();
                _btnAddToCanvas.Enabled = _lvVariables.Items.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    I18n.T("PsblogImport.ErrorParse", ex.Message),
                    I18n.T("PsblogImport.Title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _dto = null;
                _btnAddToCanvas.Enabled = false;
            }
        }

        private void UpdateSummary()
        {
            if (_dto == null) return;

            _lblSource.Text = I18n.T("PsblogImport.SourceLabel") + " " + _dto.Source;
            _lblCaptured.Text = I18n.T("PsblogImport.CapturedLabel") + " " + _dto.CapturedAt;

            double duration = 0.0;
            if (_dto.Rows.Count >= 2)
            {
                try
                {
                    long t0 = _dto.Rows[0][0].GetInt64();
                    long tN = _dto.Rows[_dto.Rows.Count - 1][0].GetInt64();
                    duration = (tN - t0) / 1000.0;
                }
                catch
                {
                    duration = 0.0;
                }
            }
            _lblDuration.Text = I18n.T("PsblogImport.DurationLabel")
                + " " + duration.ToString("F2") + " s (" + _dto.Rows.Count + " samples)";

            _lblWarnings.Text = I18n.T("PsblogImport.WarningsLabel") + " " + _dto.Warnings
                + " / " + I18n.T("PsblogImport.DroppedLabel") + " " + _dto.Dropped;
        }

        private void PopulateVariables()
        {
            _lvVariables.Items.Clear();
            if (_dto == null) return;

            foreach (var kv in _dto.Vars)
            {
                var varName = kv.Key;
                var meta = kv.Value;
                var hasCls = _classifications.TryGetValue(varName, out var cls);
                var shape = hasCls && cls != null ? cls.Category : "-";
                var component = hasCls && cls != null
                    ? (cls.ProposedComponent ?? "-")
                    : "-";

                var item = new ListViewItem(new[]
                {
                    varName,
                    meta.Type,
                    meta.Unit,
                    shape,
                    component,
                })
                {
                    Tag = varName,
                    Checked = hasCls && cls != null && cls.Confidence >= 0.5,
                };
                _lvVariables.Items.Add(item);
            }
        }

        private void OnAddToCanvas(object? sender, EventArgs e)
        {
            TraceDiag($"OnAddToCanvas: _dto={( _dto == null ? "null" : "set") } _fullPath='{_fullPath}' items={_lvVariables.Items.Count}");
            if (_dto == null || string.IsNullOrEmpty(_fullPath)) { TraceDiag("  early return: _dto null or path empty"); return; }

            var selected = new List<string>();
            var proposed = new Dictionary<string, string?>();
            foreach (ListViewItem item in _lvVariables.Items)
            {
                if (!item.Checked) continue;
                if (item.Tag is not string varName || string.IsNullOrEmpty(varName)) continue;
                selected.Add(varName);
                proposed[varName] = _classifications.TryGetValue(varName, out var c)
                    ? c.ProposedComponent
                    : null;
            }
            TraceDiag($"OnAddToCanvas: checked={selected.Count} vars=[{string.Join(",", selected)}]");
            if (selected.Count == 0) { TraceDiag("  early return: selected.Count == 0"); return; }

            Result = new PsblogImportResult
            {
                PsblogPath = _fullPath,
                SelectedVariables = selected,
                PlaceInferredComponents = _chkPlaceInferred.Checked,
                AddToPalette = _chkAddToPalette.Checked,
                ProposedComponents = proposed,
            };
            TraceDiag($"OnAddToCanvas: Result set (selected={Result.SelectedVariables.Count})");
            TraceDiag($"OnAddToCanvas: about to set DialogResult (current={DialogResult})");
            DialogResult = DialogResult.OK;
            TraceDiag($"OnAddToCanvas: DialogResult now={DialogResult}, IsDisposed={IsDisposed}, Visible={Visible}");
            TraceDiag("OnAddToCanvas: END");
        }
    
        private static void TraceDiag(string msg)
        {
            // [v1.10.1] TraceDiag: no-op in Release
#if DEBUG

            var path = System.Environment.GetEnvironmentVariable("PSB_TRACE_LOG");
            if (string.IsNullOrEmpty(path)) return;
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] [tid={System.Threading.Thread.CurrentThread.ManagedThreadId}] {msg}\n";
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    using var fs = new System.IO.FileStream(
                        path, System.IO.FileMode.Append, System.IO.FileAccess.Write,
                        System.IO.FileShare.ReadWrite);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(line);
                    fs.Write(bytes, 0, bytes.Length);
                    return;
                }
                catch (System.IO.IOException)
                {
                    System.Threading.Thread.Sleep(5);
                }
                catch { return; }
            }
                    #endif
        }

}
}
