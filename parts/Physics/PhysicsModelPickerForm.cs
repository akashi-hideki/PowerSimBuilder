#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PowerSimBuilder.Physics;

namespace PowerSimBuilder;

public sealed class PhysicsModelPickerForm : Form
{
    private readonly ListView _list = new();
    private readonly Button _btnInsert = new();
    private readonly Button _btnCancel = new();
    private readonly Button _btnOpenFolder = new();
    private readonly Label _lblStatus = new();
    private readonly TextBox _txtDetail = new();
    private readonly List<PhysicsModelEntry> _entries;

    public PhysicsModelEntry? SelectedModel { get; private set; }

    public PhysicsModelPickerForm() : this(PhysicsModelCatalog.DefaultRoot()) { }

    public PhysicsModelPickerForm(string userPhysicsRoot)
        : this(PhysicsModelCatalog.Scan(userPhysicsRoot)) { }

    public PhysicsModelPickerForm(IReadOnlyList<PhysicsModelEntry> entries)
    {
        _entries = new List<PhysicsModelEntry>(entries ?? Array.Empty<PhysicsModelEntry>());
        InitializeUi();
        Populate();
    }

    private void InitializeUi()
    {
        Text = "Add Physics Model";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(820, 520);
        MinimumSize = new Size(700, 400);
        Font = new Font("Segoe UI", 9F);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 500,
            SplitterWidth = 6,
        };

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.MultiSelect = false;
        _list.HideSelection = false;
        _list.Columns.Add("Model", 220);
        _list.Columns.Add("In", 40, HorizontalAlignment.Right);
        _list.Columns.Add("Out", 40, HorizontalAlignment.Right);
        _list.Columns.Add("FMI", 120);
        _list.DoubleClick += (s, e) => AcceptIfSelected();
        _list.SelectedIndexChanged += (s, e) => UpdateDetail();

        _txtDetail.Dock = DockStyle.Fill;
        _txtDetail.Multiline = true;
        _txtDetail.ReadOnly = true;
        _txtDetail.ScrollBars = ScrollBars.Vertical;
        _txtDetail.Font = new Font("Consolas", 9F);
        _txtDetail.BackColor = Color.White;

        split.Panel1.Controls.Add(_list);
        split.Panel2.Controls.Add(_txtDetail);

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 56 };
        _lblStatus.AutoSize = false;
        _lblStatus.Dock = DockStyle.Fill;
        _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        _lblStatus.Padding = new Padding(8, 0, 8, 0);

        _btnInsert.Text = "Insert";
        _btnInsert.Dock = DockStyle.Right;
        _btnInsert.Width = 100;
        _btnInsert.Enabled = false;
        _btnInsert.Click += (s, e) => AcceptIfSelected();

        _btnOpenFolder.Text = "Open Folder";
        _btnOpenFolder.Dock = DockStyle.Right;
        _btnOpenFolder.Width = 110;
        _btnOpenFolder.Enabled = false;
        _btnOpenFolder.Click += (s, e) => OpenSelectedFolder();

        _btnCancel.Text = "Cancel";
        _btnCancel.Dock = DockStyle.Right;
        _btnCancel.Width = 100;
        _btnCancel.DialogResult = DialogResult.Cancel;

        var rightPanel = new Panel { Dock = DockStyle.Right, Width = 330 };
        rightPanel.Controls.Add(_btnInsert);
        rightPanel.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 6 });
        rightPanel.Controls.Add(_btnOpenFolder);
        rightPanel.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 6 });
        rightPanel.Controls.Add(_btnCancel);

        bottom.Controls.Add(_lblStatus);
        bottom.Controls.Add(rightPanel);

        _list.SelectedIndexChanged += (s, e) =>
        {
            var hasSelection = _list.SelectedItems.Count > 0;
            _btnInsert.Enabled = hasSelection;
            _btnOpenFolder.Enabled = hasSelection;
        };

        Controls.Add(split);
        Controls.Add(bottom);
        AcceptButton = _btnInsert;
        CancelButton = _btnCancel;
    }

    private void Populate()
    {
        _list.Items.Clear();
        foreach (var e in _entries)
        {
            var item = new ListViewItem(e.DisplayName);
            item.SubItems.Add(e.Info.Inputs.Count.ToString());
            item.SubItems.Add(e.Info.Outputs.Count.ToString());
            item.SubItems.Add(e.Info.FmiVersion + " / " + e.Info.FmiType);
            item.Tag = e;
            _list.Items.Add(item);
        }

        if (_entries.Count == 0)
        {
            _lblStatus.Text = "No physics models found under user_physics_models.";
            _txtDetail.Text = "(no models)";
        }
        else
        {
            _lblStatus.Text = _entries.Count + " model(s) available.";
            _list.Items[0].Selected = true;
        }
    }

    private void UpdateDetail()
    {
        if (_list.SelectedItems.Count == 0) { _txtDetail.Text = ""; return; }
        if (_list.SelectedItems[0].Tag is not PhysicsModelEntry entry) { _txtDetail.Text = ""; return; }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Model     : " + entry.Info.ModelName);
        sb.AppendLine("FMI       : " + entry.Info.FmiVersion + " (" + entry.Info.FmiType + ")");
        sb.AppendLine("Directory : " + entry.ModelDirectory);
        sb.AppendLine("FMU       : " + entry.FmuPath);
        sb.AppendLine();

        sb.AppendLine("Inputs (" + entry.Info.Inputs.Count + "):");
        foreach (var name in entry.Info.Inputs)
        {
            var t = entry.Info.InputTypes != null && entry.Info.InputTypes.TryGetValue(name, out var tt) ? tt : "Real";
            sb.AppendLine("  " + name + " : " + t);
        }
        sb.AppendLine();

        sb.AppendLine("Outputs (" + entry.Info.Outputs.Count + "):");
        foreach (var name in entry.Info.Outputs)
        {
            var t = entry.Info.OutputTypes != null && entry.Info.OutputTypes.TryGetValue(name, out var tt) ? tt : "Real";
            sb.AppendLine("  " + name + " : " + t);
        }
        sb.AppendLine();

        sb.AppendLine("Parameters (" + entry.Info.Parameters.Count + "):");
        foreach (var p in entry.Info.Parameters)
        {
            var startText = p.StartValue == "# TODO" ? "?" : p.StartValue;
            sb.AppendLine("  " + p.Name + " : " + p.Type + " = " + startText);
        }
        sb.AppendLine();

        if (entry.Info.Timing is { } t2)
        {
            sb.AppendLine("Timing:");
            if (t2.StartTime.HasValue) sb.AppendLine("  startTime : " + t2.StartTime.Value);
            if (t2.StopTime.HasValue)  sb.AppendLine("  stopTime  : " + t2.StopTime.Value);
            if (t2.StepSize.HasValue)  sb.AppendLine("  stepSize  : " + t2.StepSize.Value);
        }
        else
        {
            sb.AppendLine("Timing: (none)");
        }

        _txtDetail.Text = sb.ToString();
    }

    private void OpenSelectedFolder()
    {
        if (_list.SelectedItems.Count == 0) return;
        if (_list.SelectedItems[0].Tag is not PhysicsModelEntry entry) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = entry.ModelDirectory,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Open folder failed",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void AcceptIfSelected()
    {
        if (_list.SelectedItems.Count == 0) return;
        if (_list.SelectedItems[0].Tag is PhysicsModelEntry entry)
        {
            SelectedModel = entry;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}