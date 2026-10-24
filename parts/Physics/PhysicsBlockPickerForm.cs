using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder.Physics;

/// <summary>
/// Lists all physics model blocks in a Python file and lets the user
/// select one to remove. Opened from PythonEditorForm's context menu
/// or Ctrl+Shift+P.
/// </summary>
public sealed class PhysicsBlockPickerForm : Form
{
    private readonly ListView _list = new();
    private readonly Label _lblStatus = new();
    private readonly Button _btnRemove = new();
    private readonly Button _btnCancel = new();
    private readonly List<PhysicsBlockInfo> _blocks;

    public PhysicsBlockInfo? SelectedBlock { get; private set; }

    public PhysicsBlockPickerForm(string pythonCode)
    {
        _blocks = new List<PhysicsBlockInfo>(PhysicsBlockDetector.FindAll(pythonCode));
        InitializeUi();
        Populate();
    }

    private void InitializeUi()
    {
        Text = I18n.T("PhysicsRemove.Title");
        Name = "PhysicsBlockPickerForm";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(600, 380);
        MinimumSize = new Size(480, 300);
        Font = new Font("Segoe UI", 9F);

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.MultiSelect = false;
        _list.HideSelection = false;
        _list.Columns.Add(I18n.T("PhysicsRemove.ColModel"), 260);
        _list.Columns.Add(I18n.T("PhysicsRemove.ColLocation"), 280);
        _list.SelectedIndexChanged += (s, e) => UpdateRemoveEnabled();
        _list.DoubleClick += (s, e) => AcceptIfSelected();

        _lblStatus.Dock = DockStyle.Bottom;
        _lblStatus.Height = 24;
        _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        _lblStatus.Padding = new Padding(8, 0, 8, 0);

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 44 };
        var rightPanel = new Panel { Dock = DockStyle.Right, Width = 220 };

        _btnRemove.Text = I18n.T("PhysicsRemove.BtnRemove");
        _btnRemove.Dock = DockStyle.Right;
        _btnRemove.Width = 100;
        _btnRemove.Enabled = false;
        _btnRemove.Click += (s, e) => AcceptIfSelected();

        _btnCancel.Text = I18n.T("PhysicsRemove.BtnCancel");
        _btnCancel.Dock = DockStyle.Right;
        _btnCancel.Width = 100;
        _btnCancel.DialogResult = DialogResult.Cancel;

        rightPanel.Controls.Add(_btnRemove);
        rightPanel.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 6 });
        rightPanel.Controls.Add(_btnCancel);

        bottom.Controls.Add(_lblStatus);
        bottom.Controls.Add(rightPanel);

        Controls.Add(_list);
        Controls.Add(bottom);

        AcceptButton = _btnRemove;
        CancelButton = _btnCancel;
    }

    private void Populate()
    {
        _list.Items.Clear();
        foreach (var block in _blocks)
        {
            var item = new ListViewItem(block.ModelName);
            item.SubItems.Add(block.Summary());
            item.Tag = block;
            _list.Items.Add(item);
        }

        if (_blocks.Count == 0)
        {
            _lblStatus.Text = I18n.T("PhysicsRemove.StatusNoModels");
        }
        else
        {
            _lblStatus.Text = I18n.T("PhysicsRemove.StatusAvailable", _blocks.Count);
            _list.Items[0].Selected = true;
        }
    }

    private void UpdateRemoveEnabled()
    {
        _btnRemove.Enabled = _list.SelectedItems.Count > 0;
    }

    private void AcceptIfSelected()
    {
        if (_list.SelectedItems.Count == 0) return;
        SelectedBlock = _list.SelectedItems[0].Tag as PhysicsBlockInfo;
        if (SelectedBlock == null) return;
        DialogResult = DialogResult.OK;
        Close();
    }

    // Test hooks
    public ListView Test_ListView => _list;
    public Button Test_RemoveButton => _btnRemove;
    public Button Test_CancelButton => _btnCancel;
    public Label Test_StatusLabel => _lblStatus;

    public void Test_SelectFirst()
    {
        if (_list.Items.Count == 0) return;
        _list.Items[0].Selected = true;
        if (_list.SelectedItems.Count == 0)
            _list.SelectedIndices.Add(0);
        _btnRemove.Enabled = true;
    }

    public void Test_InvokeRemove()
    {
        Test_SelectFirst();
        // Headless fallback: ListView.SelectedItems may not materialize without ShowDialog.
        if (_list.SelectedItems.Count == 0 && _list.Items.Count > 0)
        {
            SelectedBlock = _list.Items[0].Tag as PhysicsBlockInfo;
            if (SelectedBlock != null) DialogResult = DialogResult.OK;
            return;
        }
        AcceptIfSelected();
    }
}
