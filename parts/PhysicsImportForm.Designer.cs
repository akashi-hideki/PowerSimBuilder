#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
namespace PowerSimBuilder;

partial class PhysicsImportForm
{
    private System.ComponentModel.IContainer components = null!;
    private TableLayoutPanel _root = null!;
    private Label _lblFile = null!;
    private TextBox _txtFile = null!;
    private Button _btnBrowse = null!;
    private Label _lblName = null!;
    private TextBox _txtName = null!;
    private Label _lblFmiType = null!;
    private Label _txtFmiType = null!;
    private Label _lblVariables = null!;
    private DataGridView _gridVars = null!;
    private DataGridViewCheckBoxColumn _colUse = null!;
    private DataGridViewTextBoxColumn _colPort = null!;
    private DataGridViewTextBoxColumn _colVar = null!;
    private DataGridViewTextBoxColumn _colDir = null!;
    private DataGridViewTextBoxColumn _colType = null!;
    private FlowLayoutPanel _buttons = null!;
    private Button _btnImport = null!;
    private Button _btnCancel = null!;
    private Label _lblStatus = null!;
    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 8, Padding = new Padding(8) };
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        for (int i = 0; i < 6; i++) _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _lblFile = new Label { Text = "FMU file:", AutoSize = true, Anchor = AnchorStyles.Left };
        _txtFile = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
        _btnBrowse = new Button { Text = "Browse...", Dock = DockStyle.Fill };
        _btnBrowse.Click += OnBrowseClick;
        _root.Controls.Add(_lblFile, 0, 0);
        _root.Controls.Add(_txtFile, 1, 0);
        _root.Controls.Add(_btnBrowse, 2, 0);

        _lblName = new Label { Text = "Model name:", AutoSize = true, Anchor = AnchorStyles.Left };
        _txtName = new TextBox { Dock = DockStyle.Fill };
        _root.Controls.Add(_lblName, 0, 1);
        _root.Controls.Add(_txtName, 1, 1);
        _root.SetColumnSpan(_txtName, 2);

        _lblFmiType = new Label { Text = "FMI type:", AutoSize = true, Anchor = AnchorStyles.Left };
        _txtFmiType = new Label { Text = "-", AutoSize = true, Anchor = AnchorStyles.Left };
        _root.Controls.Add(_lblFmiType, 0, 2);
        _root.Controls.Add(_txtFmiType, 1, 2);

        _lblVariables = new Label { Text = "Variables (check to expose):", AutoSize = true };
        _root.Controls.Add(_lblVariables, 0, 3);
        _root.SetColumnSpan(_lblVariables, 3);

        _gridVars = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
        };
        _colUse  = new DataGridViewCheckBoxColumn { HeaderText = "Use",     Width = 40,  Name = "Use" };
        _colPort = new DataGridViewTextBoxColumn    { HeaderText = "Port",    Width = 140, Name = "Port" };
        _colVar  = new DataGridViewTextBoxColumn    { HeaderText = "FMU var", Width = 160, ReadOnly = true, Name = "Var" };
        _colDir  = new DataGridViewTextBoxColumn    { HeaderText = "Dir",     Width = 90,  ReadOnly = true, Name = "Dir" };
        _colType = new DataGridViewTextBoxColumn    { HeaderText = "Type",    Width = 80,  ReadOnly = true, Name = "Type" };
        _gridVars.Columns.AddRange(_colUse, _colPort, _colVar, _colDir, _colType);
        _root.Controls.Add(_gridVars, 0, 4);
        _root.SetColumnSpan(_gridVars, 3);

        _lblStatus = new Label { AutoSize = true, ForeColor = SystemColors.GrayText };
        _root.Controls.Add(_lblStatus, 0, 5);
        _root.SetColumnSpan(_lblStatus, 3);

        _buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true };
        _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        _btnImport = new Button { Text = "Import", AutoSize = true };
        _btnImport.Click += OnImportClick;
        _buttons.Controls.Add(_btnCancel);
        _buttons.Controls.Add(_btnImport);
        _root.Controls.Add(_buttons, 0, 6);
        _root.SetColumnSpan(_buttons, 3);

        AcceptButton = _btnImport;
        CancelButton = _btnCancel;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(720, 520);
        Controls.Add(_root);
        MinimumSize = new Size(640, 420);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Import Physics Model (FMU)";
    }
}