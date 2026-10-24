#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using PowerSimBuilder.Physics;

namespace PowerSimBuilder;

/// <summary>
/// MVP-β Phase 3: URL download tab for PhysicsImportForm.
/// Wraps the existing controls in a TabControl (File tab) and adds a URL tab.
/// </summary>
public partial class PhysicsImportForm
{
    private TabControl? _tabs;
    private TabPage? _tabFile;
    private TabPage? _tabUrl;
    private TextBox? _txtUrl;
    private Button? _btnDownload;
    private Label? _lblUrlStatus;
    private ProgressBar? _progressBar;

    /// <summary>Consent provider. Default uses NetworkAccessDialog; overridable in tests.</summary>
    public Func<string, string, bool>? Test_ConsentProvider { get; set; }

    private void WrapInTabs()
    {
        var existing = Controls.Cast<Control>().ToArray();
        if (existing.Length == 0) return;

        Controls.Clear();

        _tabs = new TabControl { Dock = DockStyle.Fill, Name = "TabControl" };
        _tabFile = new TabPage(I18n.T("PhysicsImport.TabFile")) { Name = "TabFile", Padding = new Padding(4) };
        _tabUrl = new TabPage(I18n.T("PhysicsImport.TabUrl")) { Name = "TabUrl", Padding = new Padding(4) };
        _tabCatalog = new TabPage(I18n.T("PhysicsImport.TabCatalog")) { Name = "TabCatalog", Padding = new Padding(4) };

        foreach (var c in existing)
            _tabFile.Controls.Add(c);

        BuildUrlTabUi();
        BuildCatalogTabUi();

        _tabs.TabPages.Add(_tabFile);
        _tabs.TabPages.Add(_tabUrl);
        _tabs.TabPages.Add(_tabCatalog);
        Controls.Add(_tabs);
    }

    private void BuildUrlTabUi()
    {
        if (_tabUrl == null) return;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(12)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 4; i++)
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var lblUrl = new Label { Text = I18n.T("UrlTab.LblUrl"), AutoSize = true, Anchor = AnchorStyles.Left };
        _txtUrl = new TextBox { Dock = DockStyle.Fill, Name = "TxtUrl" };
        root.Controls.Add(lblUrl, 0, 0);
        root.Controls.Add(_txtUrl, 1, 0);

        _btnDownload = new Button { Text = I18n.T("UrlTab.BtnDownload"), AutoSize = true, Name = "BtnDownload" };
        _btnDownload.Click += OnDownloadClick;
        root.Controls.Add(_btnDownload, 1, 1);

        _progressBar = new ProgressBar { Dock = DockStyle.Fill, Height = 20, Name = "ProgressBar", Visible = false };
        root.Controls.Add(_progressBar, 0, 2);
        root.SetColumnSpan(_progressBar, 2);

        _lblUrlStatus = new Label
        {
            Text = I18n.T("UrlTab.StatusReady"),
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Anchor = AnchorStyles.Left,
            Name = "LblUrlStatus"
        };
        root.Controls.Add(_lblUrlStatus, 0, 3);
        root.SetColumnSpan(_lblUrlStatus, 2);

        _tabUrl.Controls.Add(root);
    }

    private async void OnDownloadClick(object? sender, EventArgs e)
    {
        await DownloadFromUrlAsync();
    }

    internal async Task<bool> DownloadFromUrlAsync()
    {
        var url = _txtUrl?.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(url))
        {
            if (_lblUrlStatus != null)
                _lblUrlStatus.Text = I18n.T("UrlTab.StatusFail", "URL is empty");
            return false;
        }

        // Filename from URL
        string fileName = "model.fmu";
        try { fileName = Path.GetFileName(new Uri(url).LocalPath); } catch { }
        if (string.IsNullOrEmpty(fileName)) fileName = "model.fmu";

        // Consent
        bool consent = Test_ConsentProvider != null
            ? Test_ConsentProvider(url, fileName)
            : DefaultConsent(url, fileName);
        if (!consent) return false;

        // Prepare
        if (_btnDownload != null) _btnDownload.Enabled = false;
        if (_progressBar != null) { _progressBar.Visible = true; _progressBar.Value = 0; }
        if (_lblUrlStatus != null) _lblUrlStatus.Text = I18n.T("UrlTab.StatusDownloading", 0);

        var tempDir = Path.Combine(Path.GetTempPath(), "psb_fmu_dl");
        Directory.CreateDirectory(tempDir);
        var destPath = Path.Combine(tempDir, fileName);

        var options = new DownloadOptions
        {
            TimeoutSeconds = AppSettings.DownloadTimeoutSeconds,
            ProxyHost = AppSettings.ProxyHost ?? "",
            ProxyPort = AppSettings.ProxyPort
        };

        var progress = new Progress<int>(p =>
        {
            if (_progressBar != null) _progressBar.Value = Math.Min(100, Math.Max(0, p));
            if (_lblUrlStatus != null) _lblUrlStatus.Text = I18n.T("UrlTab.StatusDownloading", p);
        });

        try
        {
            using var dl = new FmuDownloader(options);
            var result = await dl.DownloadAsync(url, destPath, progress);

            if (!result.Success)
            {
                if (_lblUrlStatus != null)
                    _lblUrlStatus.Text = I18n.T("UrlTab.StatusFail", result.ErrorMessage ?? "Unknown");
                return false;
            }

            if (_lblUrlStatus != null)
                _lblUrlStatus.Text = I18n.T("UrlTab.StatusSuccess", fileName);

            // Switch to File tab and populate
            if (_tabs != null && _tabFile != null)
                _tabs.SelectedTab = _tabFile;

            if (_txtFile != null) _txtFile.Text = destPath;

            try
            {
                var parsed = ParseFmu(destPath);
                if (parsed.FmiType != "CoSimulation")
                {
                    MessageBox.Show(this,
                        I18n.T("PhysicsImport.UnsupportedFmiType"),
                        I18n.T("PhysicsImport.MsgUnsupportedFmiTitle"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                _fmuPath = destPath;
                _fmiType = parsed.FmiType;
                if (_txtFmiType != null) _txtFmiType.Text = parsed.FmiType;
                if (_txtName != null && string.IsNullOrWhiteSpace(_txtName.Text))
                    _txtName.Text = Path.GetFileNameWithoutExtension(destPath);
                if (_gridVars != null)
                {
                    _gridVars.Rows.Clear();
                    foreach (var v in parsed.Variables)
                    {
                        if (v.Causality is not ("input" or "output")) continue;
                        int idx = _gridVars.Rows.Add(v.Causality == "input", v.Name, v.Name, v.Causality, v.Type);
                        _gridVars.Rows[idx].Tag = v;
                    }
                }
                if (_lblStatus != null)
                    _lblStatus.Text = I18n.T("PhysicsImport.StatusParsed", parsed.Variables.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message,
                    I18n.T("PhysicsImport.MsgImportErrorTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }
        finally
        {
            if (_btnDownload != null) _btnDownload.Enabled = true;
            if (_progressBar != null) _progressBar.Visible = false;
        }
    }

    private bool DefaultConsent(string url, string fileName)
    {
        using var dlg = new NetworkAccessDialog(url, fileName);
        if (dlg.ShowDialog(this) != DialogResult.OK) return false;
        if (dlg.AlwaysAllow && !AppSettings.AllowedUrls.Contains(url))
        {
            AppSettings.AllowedUrls.Add(url);
            AppSettings.Save();
        }
        return true;
    }

    // Test hooks
    public TabControl? Test_TabControl => _tabs;
    public TabPage? Test_TabFile => _tabFile;
    public TabPage? Test_TabUrl => _tabUrl;
    public TextBox? Test_TxtUrl => _txtUrl;
    public Button? Test_BtnDownload => _btnDownload;
    public Label? Test_LblUrlStatus => _lblUrlStatus;
    public ProgressBar? Test_ProgressBar => _progressBar;
}
