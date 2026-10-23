#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PowerSimBuilder.Physics;

namespace PowerSimBuilder;

/// <summary>
/// Picks a physics model from user_physics_models and returns it via SelectedModel.
/// </summary>
public sealed class PhysicsModelPickerForm : Form
{
    private readonly ListView _list = new();
    private readonly Button _btnInsert = new();
    private readonly Button _btnCancel = new();
    private readonly Label _lblStatus = new();
    private readonly List<PhysicsModelEntry> _entries;

    public PhysicsModelEntry? SelectedModel { get; private set; }

    public PhysicsModelPickerForm() : this(DefaultRoot()) { }

    public PhysicsModelPickerForm(string userPhysicsRoot)
        : this(PhysicsModelCatalog.Scan(userPhysicsRoot)) { }

    public PhysicsModelPickerForm(IReadOnlyList<PhysicsModelEntry> entries)
    {
        _entries = new List<PhysicsModelEntry>(entries ?? Array.Empty<PhysicsModelEntry>());
        InitializeUi();
        Populate();
    }

    public static string DefaultRoot() => PhysicsModelCatalog.DefaultRoot();

    private void InitializeUi()
    {
        Text = "Add Physics Model";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(640, 380);
        MinimumSize = new Size(480, 280);
        Font = new Font("Segoe UI", 9F);

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.MultiSelect = false;
        _list.HideSelection = false;
        _list.Columns.Add("Model", 240);
        _list.Columns.Add("Inputs", 60, HorizontalAlignment.Right);
        _list.Columns.Add("Outputs", 60, HorizontalAlignment.Right);
        _list.Columns.Add("FMI", 100);
        _list.DoubleClick += (s, e) => AcceptIfSelected();

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 48 };
        _lblStatus.AutoSize = false;
        _lblStatus.Dock = DockStyle.Fill;
        _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        _lblStatus.Padding = new Padding(8, 0, 8, 0);

        _btnInsert.Text = "Insert";
        _btnInsert.Dock = DockStyle.Right;
        _btnInsert.Width = 100;
        _btnInsert.Enabled = false;
        _btnInsert.Click += (s, e) => AcceptIfSelected();

        _btnCancel.Text = "Cancel";
        _btnCancel.Dock = DockStyle.Right;
        _btnCancel.Width = 100;
        _btnCancel.DialogResult = DialogResult.Cancel;

        var rightPanel = new Panel { Dock = DockStyle.Right, Width = 210 };
        rightPanel.Controls.Add(_btnInsert);
        rightPanel.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 6 });
        rightPanel.Controls.Add(_btnCancel);

        bottom.Controls.Add(_lblStatus);
        bottom.Controls.Add(rightPanel);

        _list.SelectedIndexChanged += (s, e) =>
            _btnInsert.Enabled = _list.SelectedItems.Count > 0;

        Controls.Add(_list);
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
        }
        else
        {
            _lblStatus.Text = _entries.Count + " model(s) available.";
            _list.Items[0].Selected = true;
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