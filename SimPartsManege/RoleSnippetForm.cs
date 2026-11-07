using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public class RoleSnippetForm : Form
    {
        private ListBox lstSnippets;
        private TextBox txtCode;
        private TextBox txtDisplayName;
        private TextBox txtDescription;
        private Button btnInsert;
        private Button btnSave;
        private Button btnDelete;
        private Button btnNew;
        private Button btnCancel;
        private RadioButton rbSnippetOwn;
        private RadioButton rbTargetFunction;
        private TreeView treeTarget;
        private bool _suppressTreeAfterSelect;

        private List<RoleSnippet> _snippets;
        private readonly string _targetLayer;
        private readonly string _targetFunc;
        private readonly List<string> _availableFunctions;

        public string SelectedLayer { get; private set; } = "";
        public string SelectedFunc { get; private set; } = "";
        public string SelectedSnippetLayer { get; private set; } = "";
        public string SelectedSnippetFunc { get; private set; } = "";
        public string SelectedCode { get; private set; } = "";

        private static void LocalLog(string evt, params (string key, object val)[] fields)
        {
            try
            {
                var path = System.Environment.GetEnvironmentVariable("PSB_EDIT_LOG")
                    ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "psb_edit_debug.log");
                var session = System.Environment.GetEnvironmentVariable("PSB_EDIT_SESSION") ?? "?";
                var obj = new Dictionary<string, object>
                {
                    ["ts"] = DateTime.Now.ToString("HH:mm:ss.fff"),
                    ["session"] = session,
                    ["event"] = evt,
                    ["source"] = "RoleSnippetForm",
                };
                if (fields != null) foreach (var (k, v) in fields) obj[k] = v;
                var json = System.Text.Json.JsonSerializer.Serialize(obj);
                System.IO.File.AppendAllText(path, json + System.Environment.NewLine);
            }
            catch { }
        }

        public RoleSnippetForm() : this(null, null, null) { }
        public RoleSnippetForm(string targetLayer, string targetFunc) : this(targetLayer, targetFunc, null) { }
        public RoleSnippetForm(string targetLayer, string targetFunc, List<string> availableFunctions)
        {
            _targetLayer = targetLayer;
            _targetFunc = targetFunc;
            _availableFunctions = availableFunctions ?? new List<string>();
            InitializeComponent();
            LoadSnippets();
            BuildTargetTree();
            UpdateInsertButtonState();
        }

        private void InitializeComponent()
        {
            Text = "Role Snippets";
            Width = 1200;
            Height = 700;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1000, 560);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(6),
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            // ===== Column 1: Target function (both radios in SAME panel) =====
            var targetGroup = new GroupBox
            {
                Text = "Target function",
                Dock = DockStyle.Fill,
                Padding = new Padding(6),
            };
            var targetStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
            };
            targetStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            targetStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            targetStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            rbSnippetOwn = new RadioButton
            {
                Name = "rbSnippetOwn",
                Text = "(snippet's own)",
                Dock = DockStyle.Fill,
                Checked = false,
            };
            rbTargetFunction = new RadioButton
            {
                Name = "rbTargetFunction",
                Text = "Select function:",
                Dock = DockStyle.Fill,
                Checked = false,
            };

            rbSnippetOwn.CheckedChanged += (s, e) =>
            {
                if (rbSnippetOwn.Checked)
                {
                    _suppressTreeAfterSelect = true;
                    try
                    {
                        rbTargetFunction.Checked = false;
                        if (treeTarget != null)
                        {
                            treeTarget.SelectedNode = null;
                            treeTarget.Enabled = false;
                        }
                    }
                    finally { _suppressTreeAfterSelect = false; }
                }
                UpdateInsertButtonState();
            };
            rbTargetFunction.CheckedChanged += (s, e) =>
            {
                if (rbTargetFunction.Checked)
                {
                    rbSnippetOwn.Checked = false;
                    if (treeTarget != null) treeTarget.Enabled = true;
                }
                UpdateInsertButtonState();
            };

            treeTarget = new TreeView
            {
                Name = "treeTarget",
                Dock = DockStyle.Fill,
                HideSelection = false,
                ShowLines = true,
                ShowPlusMinus = true,
                Enabled = false,
                Font = new Font("Consolas", 9),
            };
            treeTarget.AfterSelect += (s, e) =>
            {
                if (_suppressTreeAfterSelect) return;
                if (e.Node != null && e.Node.Tag is string)
                {
                    rbTargetFunction.Checked = true;
                }
                UpdateInsertButtonState();
            };

            targetStack.Controls.Add(rbSnippetOwn, 0, 0);
            targetStack.Controls.Add(rbTargetFunction, 0, 1);
            targetStack.Controls.Add(treeTarget, 0, 2);
            targetGroup.Controls.Add(targetStack);
            mainLayout.Controls.Add(targetGroup, 0, 0);

            // ===== Column 2: Snippet list =====
            var snippetGroup = new GroupBox
            {
                Text = "Snippets",
                Dock = DockStyle.Fill,
                Padding = new Padding(6),
            };
            lstSnippets = new ListBox { Name = "lstSnippets", Dock = DockStyle.Fill, IntegralHeight = false };
            lstSnippets.SelectedIndexChanged += (s, e) => OnSnippetSelected();
            snippetGroup.Controls.Add(lstSnippets);
            mainLayout.Controls.Add(snippetGroup, 1, 0);

            // ===== Column 3: Code + metadata =====
            var codeGroup = new GroupBox
            {
                Text = "Code",
                Dock = DockStyle.Fill,
                Padding = new Padding(6),
            };
            var codeStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
            };
            codeStack.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            codeStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            codeStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            codeStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            codeStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            codeStack.Controls.Add(new Label { Text = "Display name:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            txtDisplayName = new TextBox { Name = "txtDisplayName", Dock = DockStyle.Fill };
            codeStack.Controls.Add(txtDisplayName, 1, 0);
            codeStack.Controls.Add(new Label { Text = "Description:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
            txtDescription = new TextBox { Name = "txtDescription", Dock = DockStyle.Fill };
            codeStack.Controls.Add(txtDescription, 1, 1);
            codeStack.Controls.Add(new Label { Text = "Body:", TextAlign = ContentAlignment.TopLeft, Dock = DockStyle.Fill }, 0, 2);
            txtCode = new TextBox
            {
                Name = "txtCode",
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                AcceptsTab = true,
                Font = new Font("Consolas", 9),
            };
            codeStack.Controls.Add(txtCode, 1, 2);
            codeGroup.Controls.Add(codeStack);
            mainLayout.Controls.Add(codeGroup, 2, 0);

            // ===== Bottom: buttons =====
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 6, 0, 0),
            };
            btnNew = new Button { Name = "btnSnippetNew", Text = "New", AutoSize = true, MinimumSize = new Size(80, 30) };
            btnNew.Click += (s, e) => OnNew();
            btnSave = new Button { Name = "btnSnippetSave", Text = "Save", AutoSize = true, MinimumSize = new Size(80, 30) };
            btnSave.Click += (s, e) => OnSave();
            btnDelete = new Button { Name = "btnSnippetDelete", Text = "Delete", AutoSize = true, MinimumSize = new Size(80, 30) };
            btnDelete.Click += (s, e) => OnDelete();
            btnInsert = new Button { Name = "btnSnippetInsert", Text = "Insert", AutoSize = true, MinimumSize = new Size(90, 30), Enabled = false };
            btnInsert.Click += (s, e) => OnInsert();
            btnCancel = new Button { Name = "btnSnippetCancel", Text = "Cancel", AutoSize = true, MinimumSize = new Size(80, 30) };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            buttonPanel.Controls.Add(btnNew);
            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnDelete);
            buttonPanel.Controls.Add(new Label { Text = "    ", AutoSize = true });
            buttonPanel.Controls.Add(btnInsert);
            buttonPanel.Controls.Add(btnCancel);

            mainLayout.Controls.Add(buttonPanel, 0, 1);
            mainLayout.SetColumnSpan(buttonPanel, 3);

            Controls.Add(mainLayout);
        }

        private void BuildTargetTree()
        {
            treeTarget.BeginUpdate();
            treeTarget.Nodes.Clear();
            var byLayer = _availableFunctions
                .Where(f => !string.IsNullOrEmpty(f))
                .Select(f => new
                {
                    Layer = f.Contains(".") ? f.Substring(0, f.IndexOf('.')) : "",
                    Full = f,
                })
                .GroupBy(x => x.Layer)
                .OrderBy(g => g.Key);
            foreach (var g in byLayer)
            {
                var layerNode = new TreeNode("[" + g.Key + "]");
                layerNode.Tag = null;
                foreach (var x in g.OrderBy(v => v.Full))
                {
                    var child = new TreeNode(x.Full.Substring(x.Full.IndexOf('.') + 1));
                    child.Tag = x.Full;
                    layerNode.Nodes.Add(child);
                }
                treeTarget.Nodes.Add(layerNode);
            }
            treeTarget.EndUpdate();

            if (!string.IsNullOrEmpty(_targetLayer) && !string.IsNullOrEmpty(_targetFunc))
            {
                var cur = _targetLayer + "." + _targetFunc;
                foreach (TreeNode layerNode in treeTarget.Nodes)
                {
                    foreach (TreeNode fn in layerNode.Nodes)
                    {
                        if ((string)fn.Tag == cur)
                        {
                            layerNode.Expand();
                            treeTarget.SelectedNode = fn;
                            rbTargetFunction.Checked = true;
                            return;
                        }
                    }
                }
            }
        }

        private void UpdateInsertButtonState()
        {
            if (btnInsert == null) return;
            bool ok = false;
            if (rbSnippetOwn != null && rbSnippetOwn.Checked) ok = true;
            else if (rbTargetFunction != null && rbTargetFunction.Checked
                     && treeTarget != null && treeTarget.SelectedNode != null
                     && treeTarget.SelectedNode.Tag is string) ok = true;
            btnInsert.Enabled = ok;
        }

        private void LoadSnippets()
        {
            _snippets = RoleSnippetStorage.LoadAll();
            lstSnippets.Items.Clear();
            foreach (var s in _snippets) lstSnippets.Items.Add($"{s.Layer}.{s.Func}");
            if (lstSnippets.Items.Count > 0) lstSnippets.SelectedIndex = 0;
        }

        private void OnSnippetSelected()
        {
            var idx = lstSnippets.SelectedIndex;
            if (idx < 0 || idx >= _snippets.Count) return;
            var s = _snippets[idx];
            txtDisplayName.Text = s.DisplayName ?? "";
            txtDescription.Text = s.Description ?? "";
            txtCode.Text = RoleSnippetStorage.LoadCode(s);
        }

        private void OnNew()
        {
            txtDisplayName.Text = "New snippet";
            txtDescription.Text = "";
            txtCode.Text = "return 0";
            lstSnippets.ClearSelected();
        }

        private void OnSave()
        {
            var idx = lstSnippets.SelectedIndex;
            RoleSnippet snippet;
            if (idx >= 0 && idx < _snippets.Count) snippet = _snippets[idx];
            else
            {
                using var dlg = new SnippetIdDialog();
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                snippet = new RoleSnippet { Layer = dlg.LayerName, Func = dlg.FuncName };
            }
            snippet.DisplayName = txtDisplayName.Text;
            snippet.Description = txtDescription.Text;
            try { RoleSnippetStorage.Save(snippet, txtCode.Text); LoadSnippets(); }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Save failed: " + ex.Message, "Role Snippets",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDelete()
        {
            var idx = lstSnippets.SelectedIndex;
            if (idx < 0 || idx >= _snippets.Count) return;
            var s = _snippets[idx];
            if (MessageBox.Show(this, $"Delete {s.Layer}.{s.Func}?", "Role Snippets",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            RoleSnippetStorage.Delete(s);
            LoadSnippets();
        }

        private void OnInsert()
        {
            var idx = lstSnippets.SelectedIndex;
            if (idx < 0 || idx >= _snippets.Count) return;

            var selNode = treeTarget?.SelectedNode;
            LocalLog("OnInsertEntry",
                ("rbOwnChecked", (object)(rbSnippetOwn?.Checked ?? false)),
                ("rbTargetChecked", (object)(rbTargetFunction?.Checked ?? false)),
                ("treeNodeTag", (object)(selNode?.Tag?.ToString() ?? "(null)")),
                ("snippetIndex", (object)idx));

            string targetLayer = null;
            string targetFunc = null;

            if (rbSnippetOwn != null && rbSnippetOwn.Checked)
            {
                // OWN: snippet's own name
            }
            else if (rbTargetFunction != null && rbTargetFunction.Checked
                     && selNode != null && selNode.Tag is string full)
            {
                var dot = full.IndexOf('.');
                if (dot > 0 && dot < full.Length - 1)
                {
                    targetLayer = full.Substring(0, dot);
                    targetFunc = full.Substring(dot + 1);
                }
            }

            if (string.IsNullOrEmpty(targetLayer) && !(rbSnippetOwn != null && rbSnippetOwn.Checked))
            {
                MessageBox.Show(this, "Please select a target function or (snippet's own).",
                    "Role Snippets", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var snippet = _snippets[idx];
            SelectedSnippetLayer = snippet.Layer;
            SelectedSnippetFunc = snippet.Func;
            SelectedCode = txtCode.Text;

            if (!string.IsNullOrEmpty(targetLayer))
            {
                SelectedLayer = targetLayer;
                SelectedFunc = targetFunc;
            }
            else
            {
                SelectedLayer = snippet.Layer;
                SelectedFunc = snippet.Func;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal class SnippetIdDialog : Form
    {
        public string LayerName { get; private set; } = "";
        public string FuncName { get; private set; } = "";

        public SnippetIdDialog()
        {
            Text = "New Snippet ID";
            Width = 360; Height = 180;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            var layerBox = new TextBox { Left = 100, Top = 15, Width = 230, Text = "MwOven" };
            var funcBox = new TextBox { Left = 100, Top = 50, Width = 230, Text = "F_NewFunction" };
            var ok = new Button { Text = "OK", Left = 180, Top = 95, Width = 70, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Cancel", Left = 260, Top = 95, Width = 70, DialogResult = DialogResult.Cancel };
            Controls.Add(new Label { Text = "Layer:", Left = 15, Top = 18, Width = 80 });
            Controls.Add(layerBox);
            Controls.Add(new Label { Text = "Function:", Left = 15, Top = 53, Width = 80 });
            Controls.Add(funcBox);
            Controls.Add(ok); Controls.Add(cancel);
            AcceptButton = ok; CancelButton = cancel;
            ok.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(layerBox.Text) || string.IsNullOrWhiteSpace(funcBox.Text))
                { MessageBox.Show(this, "Layer and Function are required."); DialogResult = DialogResult.None; return; }
                LayerName = layerBox.Text.Trim();
                FuncName = funcBox.Text.Trim();
            };
        }
    }
}
