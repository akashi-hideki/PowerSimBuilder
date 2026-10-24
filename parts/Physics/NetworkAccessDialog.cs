#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder.Physics;

/// <summary>
/// Pre-download consent dialog. Shows URL, filename, optional size/license,
/// and an "Always allow" checkbox. All texts via I18n.T().
/// </summary>
public sealed class NetworkAccessDialog : Form
{
    public string Url { get; }
    public bool AlwaysAllow { get; private set; }

    private readonly CheckBox _chkAlwaysAllow = new();

    public NetworkAccessDialog(string url, string fileName, string? sizeText = null, string? license = null)
    {
        Url = url ?? "";
        InitializeUi(fileName ?? "", sizeText, license);
    }

    private void InitializeUi(string fileName, string? sizeText, string? license)
    {
        Text = I18n.T("NetworkAccess.Title");
        Name = "NetworkAccessDialog";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(560, 320);
        Font = new Font("Segoe UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(12)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var lblPrompt = new Label
        {
            Text = I18n.T("NetworkAccess.Prompt"),
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font(Font, FontStyle.Bold)
        };
        root.Controls.Add(lblPrompt, 0, 0);
        root.SetColumnSpan(lblPrompt, 2);

        var lblUrlCap = new Label { Text = I18n.T("NetworkAccess.LblUrl"), AutoSize = true, Anchor = AnchorStyles.Left };
        var lblUrl = new TextBox { Text = Url, ReadOnly = true, Dock = DockStyle.Fill, Name = "TxtUrl" };
        root.Controls.Add(lblUrlCap, 0, 1);
        root.Controls.Add(lblUrl, 1, 1);

        var lblFileCap = new Label { Text = I18n.T("NetworkAccess.LblFile"), AutoSize = true, Anchor = AnchorStyles.Left };
        var lblFile = new Label { Text = string.IsNullOrEmpty(fileName) ? I18n.T("NetworkAccess.LblUnknown") : fileName, AutoSize = true, Anchor = AnchorStyles.Left };
        root.Controls.Add(lblFileCap, 0, 2);
        root.Controls.Add(lblFile, 1, 2);

        var lblSizeCap = new Label { Text = I18n.T("NetworkAccess.LblSize"), AutoSize = true, Anchor = AnchorStyles.Left };
        var lblSize = new Label { Text = sizeText ?? I18n.T("NetworkAccess.LblUnknown"), AutoSize = true, Anchor = AnchorStyles.Left };
        root.Controls.Add(lblSizeCap, 0, 3);
        root.Controls.Add(lblSize, 1, 3);

        var lblLicenseCap = new Label { Text = I18n.T("NetworkAccess.LblLicense"), AutoSize = true, Anchor = AnchorStyles.Left };
        var lblLicense = new Label { Text = license ?? I18n.T("NetworkAccess.LblUnknown"), AutoSize = true, Anchor = AnchorStyles.Left };
        root.Controls.Add(lblLicenseCap, 0, 4);
        root.Controls.Add(lblLicense, 1, 4);

        var lblWarning = new Label
        {
            Text = I18n.T("NetworkAccess.WarningProxy"),
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Dock = DockStyle.Fill
        };
        root.Controls.Add(lblWarning, 0, 5);
        root.SetColumnSpan(lblWarning, 2);

        _chkAlwaysAllow.Text = I18n.T("NetworkAccess.ChkAlwaysAllow");
        _chkAlwaysAllow.AutoSize = true;
        _chkAlwaysAllow.Name = "ChkAlwaysAllow";
        root.Controls.Add(_chkAlwaysAllow, 0, 6);
        root.SetColumnSpan(_chkAlwaysAllow, 2);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        var btnCancel = new Button
        {
            Text = I18n.T("NetworkAccess.BtnCancel"),
            DialogResult = DialogResult.Cancel,
            AutoSize = true,
            Name = "BtnCancel"
        };
        var btnAllow = new Button
        {
            Text = I18n.T("NetworkAccess.BtnAllow"),
            DialogResult = DialogResult.OK,
            AutoSize = true,
            Name = "BtnAllow"
        };
        buttons.Controls.Add(btnCancel);
        buttons.Controls.Add(btnAllow);
        root.Controls.Add(buttons, 0, 7);
        root.SetColumnSpan(buttons, 2);

        Controls.Add(root);
        AcceptButton = btnAllow;
        CancelButton = btnCancel;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        AlwaysAllow = _chkAlwaysAllow.Checked;
        base.OnFormClosing(e);
    }

    public CheckBox Test_AlwaysAllowCheckbox => _chkAlwaysAllow;
}