using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// Phase 3 of SPEC_ROLE_SNIPPETS: role function snippet editor.
    /// Lists available snippets from templates/role_snippets/, lets the user
    /// preview/edit them, and (on OK) returns SelectedLayer / SelectedFunc / SelectedCode.
    /// </summary>
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

        private List<RoleSnippet> _snippets;

        public string SelectedLayer { get; private set; } = "";
        public string SelectedFunc { get; private set; } = "";
        public string SelectedCode { get; private set; } = "";

        public RoleSnippetForm()
        {
            InitializeComponent();
            LoadSnippets();
        }

        private void InitializeComponent()
        {
            this.Text = "Role Snippets";
            this.Width = 1000;
            this.Height = 620;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 480);

                        var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
            };

            lstSnippets = new ListBox
            {
                Name = "lstSnippets",
                Dock = DockStyle.Fill,
                IntegralHeight = false,
            };
            lstSnippets.SelectedIndexChanged += (s, e) => OnSnippetSelected();
            split.Panel1.Controls.Add(lstSnippets);

            var rightPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(6),
            };
            rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            rightPanel.Controls.Add(new Label { Text = "Display name:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            txtDisplayName = new TextBox { Name = "txtDisplayName", Dock = DockStyle.Fill };
            rightPanel.Controls.Add(txtDisplayName, 1, 0);

            rightPanel.Controls.Add(new Label { Text = "Description:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
            txtDescription = new TextBox { Name = "txtDescription", Dock = DockStyle.Fill };
            rightPanel.Controls.Add(txtDescription, 1, 1);

            rightPanel.Controls.Add(new Label { Text = "Code:", TextAlign = ContentAlignment.TopLeft, Dock = DockStyle.Fill }, 0, 2);
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
            rightPanel.Controls.Add(txtCode, 1, 2);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
            };
            btnNew = new Button { Text = "New", AutoSize = true, MinimumSize = new Size(80, 30) };
            btnNew.Click += (s, e) => OnNew();
            btnSave = new Button { Text = "Save", AutoSize = true, MinimumSize = new Size(80, 30) };
            btnSave.Click += (s, e) => OnSave();
            btnDelete = new Button { Text = "Delete", AutoSize = true, MinimumSize = new Size(80, 30) };
            btnDelete.Click += (s, e) => OnDelete();
            btnInsert = new Button { Text = "Insert", AutoSize = true, MinimumSize = new Size(90, 30) };
            btnInsert.Click += (s, e) => OnInsert();
            btnCancel = new Button { Text = "Cancel", AutoSize = true, MinimumSize = new Size(80, 30) };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            buttonPanel.Controls.Add(btnNew);
            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnDelete);
            buttonPanel.Controls.Add(btnInsert);
            buttonPanel.Controls.Add(btnCancel);
            rightPanel.Controls.Add(buttonPanel, 0, 3);
            rightPanel.SetColumnSpan(buttonPanel, 2);

            split.Panel2.Controls.Add(rightPanel);
            this.Controls.Add(split);

            // SplitContainer width is not final at init; set SplitterDistance after Load.
            this.Load += (s, e) =>
            {
                try
                {
                    split.Panel1MinSize = 100;
                    split.Panel2MinSize = 300;
                    var target = Math.Max(180, Math.Min(250, split.Width / 4));
                    if (target > split.Width - split.Panel2MinSize - 10)
                        target = Math.Max(split.Panel1MinSize + 10, split.Width - split.Panel2MinSize - 10);
                    split.SplitterDistance = target;
                }
                catch { }
            };
        }

        private void LoadSnippets()
        {
            _snippets = RoleSnippetStorage.LoadAll();
            lstSnippets.Items.Clear();
            foreach (var s in _snippets)
            {
                lstSnippets.Items.Add($"{s.Layer}.{s.Func}");
            }
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
            if (idx >= 0 && idx < _snippets.Count)
            {
                snippet = _snippets[idx];
            }
            else
            {
                using var dlg = new SnippetIdDialog();
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                snippet = new RoleSnippet { Layer = dlg.LayerName, Func = dlg.FuncName };
            }
            snippet.DisplayName = txtDisplayName.Text;
            snippet.Description = txtDescription.Text;
            try
            {
                RoleSnippetStorage.Save(snippet, txtCode.Text);
                LoadSnippets();
            }
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
            var s = _snippets[idx];
            SelectedLayer = s.Layer;
            SelectedFunc = s.Func;
            SelectedCode = txtCode.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    /// <summary>
    /// Small dialog for entering Layer.Func when creating a new snippet.
    /// </summary>
    internal class SnippetIdDialog : Form
    {
        public string LayerName { get; private set; } = "";
        public string FuncName { get; private set; } = "";

        public SnippetIdDialog()
        {
            this.Text = "New Snippet ID";
            this.Width = 360;
            this.Height = 180;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var layerBox = new TextBox { Left = 100, Top = 15, Width = 230, Text = "MwOven" };
            var funcBox = new TextBox { Left = 100, Top = 50, Width = 230, Text = "F_NewFunction" };
            var ok = new Button { Text = "OK", Left = 180, Top = 95, Width = 70, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Cancel", Left = 260, Top = 95, Width = 70, DialogResult = DialogResult.Cancel };

            this.Controls.Add(new Label { Text = "Layer:", Left = 15, Top = 18, Width = 80 });
            this.Controls.Add(layerBox);
            this.Controls.Add(new Label { Text = "Function:", Left = 15, Top = 53, Width = 80 });
            this.Controls.Add(funcBox);
            this.Controls.Add(ok);
            this.Controls.Add(cancel);

            this.AcceptButton = ok;
            this.CancelButton = cancel;

            ok.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(layerBox.Text) || string.IsNullOrWhiteSpace(funcBox.Text))
                {
                    MessageBox.Show(this, "Layer and Function are required.");
                    this.DialogResult = DialogResult.None;
                    return;
                }
                LayerName = layerBox.Text.Trim();
                FuncName = funcBox.Text.Trim();
            };
        }
    }
}
