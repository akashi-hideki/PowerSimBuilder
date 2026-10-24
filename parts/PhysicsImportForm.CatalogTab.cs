#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using PowerSimBuilder.Physics;

namespace PowerSimBuilder;

/// <summary>
/// MVP-γ Phase 2: Catalog tab. Loads a JSON catalog or GitHub release listing
/// and lets the user pick a model to download.
/// </summary>
public partial class PhysicsImportForm
{
    private TabPage? _tabCatalog;
    private TextBox? _txtCatalogUrl;
    private Button? _btnCatalogLoad;
    private Button? _btnCatalogDownload;
    private ListView? _lstCatalog;
    private Label? _lblCatalogStatus;
    private ComboBox? _cmbCatalogSource;
    private readonly List<CatalogItem> _catalogItems = new();

    private void BuildCatalogTabUi()
    {
        if (_tabCatalog == null) return;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 4,
            Padding = new Padding(12)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _cmbCatalogSource = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Name = "CmbCatalogSource" };
        _cmbCatalogSource.SelectedIndexChanged += (s, e) => OnCatalogSourceChanged();
        _txtCatalogUrl = new TextBox { Dock = DockStyle.Fill, Name = "TxtCatalogUrl" };
        _btnCatalogLoad = new Button { Text = I18n.T("PhysicsCatalog.BtnLoad"), Dock = DockStyle.Fill, Name = "BtnCatalogLoad" };
        _btnCatalogLoad.Click += async (s, e) => await LoadCatalogAsync();
        root.Controls.Add(_cmbCatalogSource, 0, 0);
        root.Controls.Add(_txtCatalogUrl, 1, 0);
        root.Controls.Add(_btnCatalogLoad, 2, 0);

        _lstCatalog = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
            Name = "LstCatalog"
        };
        _lstCatalog.Columns.Add(I18n.T("PhysicsCatalog.ColName"), 180);
        _lstCatalog.Columns.Add(I18n.T("PhysicsCatalog.ColSize"), 80, HorizontalAlignment.Right);
        _lstCatalog.Columns.Add(I18n.T("PhysicsCatalog.ColLicense"), 120);
        _lstCatalog.Columns.Add(I18n.T("PhysicsCatalog.ColDescription"), 300);
        _lstCatalog.SelectedIndexChanged += (s, e) => UpdateDownloadEnabled();
        _lstCatalog.DoubleClick += async (s, e) => await DownloadSelectedAsync();
        root.Controls.Add(_lstCatalog, 0, 1);
        root.SetColumnSpan(_lstCatalog, 3);

        _lblCatalogStatus = new Label
        {
            Text = I18n.T("PhysicsCatalog.StatusReady"),
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Anchor = AnchorStyles.Left,
            Name = "LblCatalogStatus"
        };
        root.Controls.Add(_lblCatalogStatus, 0, 2);
        root.SetColumnSpan(_lblCatalogStatus, 3);

        var bottom = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true
        };
        _btnCatalogDownload = new Button
        {
            Text = I18n.T("PhysicsCatalog.BtnDownload"),
            AutoSize = true,
            Enabled = false,
            Name = "BtnCatalogDownload"
        };
        _btnCatalogDownload.Click += async (s, e) => await DownloadSelectedAsync();
        bottom.Controls.Add(_btnCatalogDownload);
        root.Controls.Add(bottom, 0, 3);
        root.SetColumnSpan(bottom, 3);

        LoadSourceList();
        _tabCatalog.Controls.Add(root);
    }

    private void LoadSourceList()
    {
        if (_cmbCatalogSource == null) return;
        _cmbCatalogSource.Items.Clear();
        _cmbCatalogSource.Items.Add(new CatalogSourceItem(null, I18n.T("PhysicsCatalog.SourceCustom")));
        try
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Resources", "catalog_sources.json");
            if (!System.IO.File.Exists(path))
            {
                var alt = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Resources", "catalog_sources.json");
                if (System.IO.File.Exists(alt)) path = alt;
            }
            if (System.IO.File.Exists(path))
            {
                var json = System.IO.File.ReadAllText(path);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("sources", out var sources))
                {
                    foreach (var s in sources.EnumerateArray())
                    {
                        var label = s.TryGetProperty("label", out var lbl) ? lbl.GetString() : null;
                        var url = s.TryGetProperty("url", out var u) ? u.GetString() : null;
                        var id = s.TryGetProperty("id", out var i) ? i.GetString() : null;
                        if (!string.IsNullOrEmpty(url))
                        {
                            _cmbCatalogSource.Items.Add(new CatalogSourceItem(url, label ?? id ?? url));
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore: fall back to Custom URL only.
        }
        _cmbCatalogSource.SelectedIndex = 0;
    }

    private void OnCatalogSourceChanged()
    {
        if (_cmbCatalogSource?.SelectedItem is CatalogSourceItem item && !string.IsNullOrEmpty(item.Url))
        {
            if (_txtCatalogUrl != null) _txtCatalogUrl.Text = item.Url;
        }
    }

    private sealed class CatalogSourceItem
    {
        public string? Url { get; }
        public string Label { get; }
        public CatalogSourceItem(string? url, string label)
        {
            Url = url;
            Label = label;
        }
        public override string ToString() => Label;
    }

    private async Task LoadCatalogAsync()
    {
        var url = _txtCatalogUrl?.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(url))
        {
            if (_lblCatalogStatus != null)
                _lblCatalogStatus.Text = I18n.T("PhysicsCatalog.StatusFail", "URL is empty");
            return;
        }

        if (_lblCatalogStatus != null) _lblCatalogStatus.Text = I18n.T("PhysicsCatalog.StatusLoading");
        if (_btnCatalogLoad != null) _btnCatalogLoad.Enabled = false;
        if (_btnCatalogDownload != null) _btnCatalogDownload.Enabled = false;
        _catalogItems.Clear();
        if (_lstCatalog != null) _lstCatalog.Items.Clear();

        try
        {
            var source = PhysicsCatalogSource.Detect(url);
            CatalogLoadResult result;
            switch (source.Kind)
            {
                case CatalogSourceKind.GitHub:
                    using (var gh = new GitHubReleaseClient())
                        result = await gh.ListFmuAssetsAsync(url);
                    break;
                case CatalogSourceKind.Json:
                    result = await PhysicsCatalogRegistry.LoadFromUrlAsync(url);
                    break;
                default:
                    result = CatalogLoadResult.Fail(
                        "URL must end with .json or be a GitHub repository URL.");
                    break;
            }

            if (!result.Success)
            {
                if (_lblCatalogStatus != null)
                    _lblCatalogStatus.Text = I18n.T("PhysicsCatalog.StatusFail", result.ErrorMessage ?? "");
                return;
            }

            if (result.Items.Count == 0)
            {
                if (_lblCatalogStatus != null)
                    _lblCatalogStatus.Text = I18n.T("PhysicsCatalog.StatusEmpty");
                return;
            }

            _catalogItems.AddRange(result.Items);
            if (_lstCatalog != null)
            {
                foreach (var item in result.Items)
                {
                    var lvi = new ListViewItem(item.Name);
                    lvi.SubItems.Add(item.SizeBytes.HasValue
                        ? FormatSize(item.SizeBytes.Value)
                        : I18n.T("PhysicsCatalog.SizeUnknown"));
                    lvi.SubItems.Add(item.License ?? I18n.T("PhysicsCatalog.LicenseUnknown"));
                    lvi.SubItems.Add(item.Description ?? I18n.T("PhysicsCatalog.DescUnknown"));
                    lvi.Tag = item;
                    _lstCatalog.Items.Add(lvi);
                }
                if (_lstCatalog.Items.Count > 0)
                {
                    _lstCatalog.Items[0].Selected = true;
                    if (_lstCatalog.SelectedItems.Count == 0)
                        _lstCatalog.SelectedIndices.Add(0);
                    if (_btnCatalogDownload != null) _btnCatalogDownload.Enabled = true;
                }
            }
            if (_lblCatalogStatus != null)
                _lblCatalogStatus.Text = I18n.T("PhysicsCatalog.StatusLoaded", result.Items.Count);
        }
        catch (Exception ex)
        {
            if (_lblCatalogStatus != null)
                _lblCatalogStatus.Text = I18n.T("PhysicsCatalog.StatusFail", ex.Message);
        }
        finally
        {
            if (_btnCatalogLoad != null) _btnCatalogLoad.Enabled = true;
        }
    }

    private async Task DownloadSelectedAsync()
    {
        if (_lstCatalog == null || _lstCatalog.SelectedItems.Count == 0) return;
        if (_lstCatalog.SelectedItems[0].Tag is not CatalogItem item) return;

        if (_txtUrl != null) _txtUrl.Text = item.Url;
        if (_tabs != null && _tabUrl != null) _tabs.SelectedTab = _tabUrl;
        await DownloadFromUrlAsync();
    }

    private void UpdateDownloadEnabled()
    {
        if (_btnCatalogDownload != null)
            _btnCatalogDownload.Enabled = _lstCatalog != null && _lstCatalog.SelectedItems.Count > 0;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.0") + " KB";
        return (bytes / (1024.0 * 1024.0)).ToString("0.0") + " MB";
    }

    // Test hooks
    public TabPage? Test_TabCatalog => _tabCatalog;
    public TextBox? Test_TxtCatalogUrl => _txtCatalogUrl;
    public Button? Test_BtnCatalogLoad => _btnCatalogLoad;
    public Button? Test_BtnCatalogDownload => _btnCatalogDownload;
    public ListView? Test_LstCatalog => _lstCatalog;
    public Label? Test_LblCatalogStatus => _lblCatalogStatus;
    public ComboBox? Test_CmbCatalogSource => _cmbCatalogSource;
    internal IReadOnlyList<CatalogItem> Test_CatalogItems => _catalogItems;
}