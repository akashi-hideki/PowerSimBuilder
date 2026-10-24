#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace PowerSimBuilder;

/// <summary>
/// Import dialog for physics models. MVP-1 supports .fmu (Co-Simulation) only.
/// </summary>
public partial class PhysicsImportForm : Form
{
    private readonly string _userPhysicsRoot;
    private string? _fmuPath;
    private string _fmiType = "CoSimulation";

    public string? ImportedModelDirectory { get; private set; }

    public PhysicsImportForm()
        : this(PhysicsImportFormDefaultRoot()) { }

    public PhysicsImportForm(string userPhysicsRoot)
    {
        _userPhysicsRoot = userPhysicsRoot;
        InitializeComponent();
        ApplyI18n();
        Name = "PhysicsImportForm";
    }

    private void ApplyI18n()
    {
        this.Text = I18n.T("PhysicsImport.Title");
        _lblFile.Text = I18n.T("PhysicsImport.File");
        _btnBrowse.Text = I18n.T("PhysicsImport.Browse");
        _lblName.Text = I18n.T("PhysicsImport.Name");
        _lblFmiType.Text = I18n.T("PhysicsImport.FmiType");
        _lblVariables.Text = I18n.T("PhysicsImport.Variables");
        _colUse.HeaderText = I18n.T("PhysicsImport.ColUse");
        _colPort.HeaderText = I18n.T("PhysicsImport.ColPort");
        _colVar.HeaderText = I18n.T("PhysicsImport.ColVar");
        _colDir.HeaderText = I18n.T("PhysicsImport.ColDir");
        _colType.HeaderText = I18n.T("PhysicsImport.ColType");
        _btnImport.Text = I18n.T("PhysicsImport.Import");
        _btnCancel.Text = I18n.T("PhysicsImport.Cancel");
        _btnCopySample.Text = I18n.T("PhysicsImport.BtnCopySample");
        _btnOpenFolder.Text = I18n.T("PhysicsImport.BtnOpenFolder");
        _btnShowReadme.Text = I18n.T("PhysicsImport.BtnShowReadme");
    }
    public static FmuParseResult ParseFmu(string fmuPath)
    {
        if (!File.Exists(fmuPath))
            throw new FileNotFoundException("FMU not found", fmuPath);
        using var zip = ZipFile.OpenRead(fmuPath);
        var entry = zip.GetEntry("modelDescription.xml")
            ?? throw new InvalidDataException(
                "modelDescription.xml not found. Not a valid .fmu archive.");
        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        var root = doc.Root ?? throw new InvalidDataException("Empty modelDescription.xml");
        var fmiType = (string?)root.Attribute("fmiType") ?? GuessFmiType(root);
        var variables = new List<FmuVariable>();
        foreach (var mv in root.Elements().Where(e => e.Name.LocalName == "ModelVariables")
                                   .Elements().Where(e => e.Name.LocalName == "ScalarVariable"))
        {
            var name = (string?)mv.Attribute("name") ?? "";
            var vrStr = (string?)mv.Attribute("valueReference") ?? "0";
            var causality = (string?)mv.Attribute("causality") ?? "local";
            var typeName = mv.Elements().FirstOrDefault()?.Name.LocalName ?? "Real";
            int.TryParse(vrStr, out var vr);
            variables.Add(new FmuVariable(name, vr, causality, typeName));
        }
        return new FmuParseResult(fmiType, variables);
    }

    private static string GuessFmiType(XElement doc)
    {
        if (doc.Descendants().Any(e => e.Name.LocalName == "CoSimulation")) return "CoSimulation";
        if (doc.Descendants().Any(e => e.Name.LocalName == "ModelExchange")) return "ModelExchange";
        return "CoSimulation";
    }

    public static string BuildModelYaml(
        string name, string fmuFileName, string fmiType,
        IReadOnlyList<FmuVariable> selected)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name is required", nameof(name));
        var sb = new StringBuilder();
        sb.Append("name: ").AppendLine(EscapeScalar(name));
        sb.AppendLine("source:");
        sb.AppendLine("  type: fmu");
        sb.Append("  file: source/").AppendLine(EscapeScalar(fmuFileName));
        sb.AppendLine("interface:");
        sb.Append("  fmi_type: ").AppendLine(fmiType);
        sb.AppendLine("  inputs:");
        foreach (var v in selected.Where(v => v.Causality == "input"))
            sb.Append("    ").Append(EscapeScalar(v.Name)).Append(": ").AppendLine(EscapeScalar(v.Name));
        sb.AppendLine("  outputs:");
        foreach (var v in selected.Where(v => v.Causality == "output"))
            sb.Append("    ").Append(EscapeScalar(v.Name)).Append(": ").AppendLine(EscapeScalar(v.Name));
        sb.AppendLine("  parameters: {}");
        sb.AppendLine("units: {}");
        sb.AppendLine("timing:");
        sb.AppendLine("  mode: fixed_step");
        sb.AppendLine("  dt_internal: 0.001");
        return sb.ToString();
    }

    private static string EscapeScalar(string s)
    {
        if (s.Length == 0) return "\"\"";
        bool needsQuotes = s.Any(c => char.IsWhiteSpace(c) || c is ':' or '#' or '&' or '*' or '!' or '|' or '>' or '@' or '`');
        return needsQuotes ? "\"" + s.Replace("\"", "\\\"") + "\"" : s;
    }

    private void OnBrowseClick(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Functional Mock-up Unit (*.fmu)|*.fmu",
            Title = I18n.T("PhysicsImport.SelectFmuTitle"),
            CheckFileExists = true,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var parsed = ParseFmu(dlg.FileName);
            if (parsed.FmiType != "CoSimulation")
            {
                MessageBox.Show(this,
                    I18n.T("PhysicsImport.UnsupportedFmiType"),
                    I18n.T("PhysicsImport.MsgUnsupportedFmiTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _fmuPath = dlg.FileName;
            _fmiType = parsed.FmiType;
            _txtFile.Text = dlg.FileName;
            _txtFmiType.Text = parsed.FmiType;
            if (string.IsNullOrWhiteSpace(_txtName.Text))
                _txtName.Text = Path.GetFileNameWithoutExtension(dlg.FileName);
            _gridVars.Rows.Clear();
            foreach (var v in parsed.Variables)
            {
                if (v.Causality is not ("input" or "output")) continue;
                int idx = _gridVars.Rows.Add(v.Causality == "input", v.Name, v.Name, v.Causality, v.Type);
                _gridVars.Rows[idx].Tag = v;
            }
            _lblStatus.Text = I18n.T("PhysicsImport.StatusParsed", parsed.Variables.Count);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, I18n.T("PhysicsImport.MsgImportErrorTitle"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnImportClick(object? sender, EventArgs e)
    {
        if (_fmuPath is null)
        {
            MessageBox.Show(this, I18n.T("PhysicsImport.MsgSelectFile"),
                I18n.T("PhysicsImport.MsgSelectFileTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var name = _txtName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show(this, I18n.T("PhysicsImport.MsgNameRequired"),
                I18n.T("PhysicsImport.MsgNameRequiredTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            var selected = _gridVars.Rows.Cast<DataGridViewRow>()
                .Where(r => (bool)(r.Cells["Use"].Value ?? false))
                .Select(r => (FmuVariable)r.Tag!)
                .ToList();
            if (selected.Count == 0)
                throw new InvalidOperationException(I18n.T("PhysicsImport.MsgSelectAtLeastOne"));
            var yaml = BuildModelYaml(name, Path.GetFileName(_fmuPath), _fmiType, selected);
            var modelDir = Path.Combine(_userPhysicsRoot, Sanitize(name));
            var sourceDir = Path.Combine(modelDir, "source");
            Directory.CreateDirectory(sourceDir);
            File.Copy(_fmuPath, Path.Combine(sourceDir, Path.GetFileName(_fmuPath)), overwrite: true);
            File.WriteAllText(Path.Combine(modelDir, "model.yaml"), yaml, Encoding.UTF8);
            ImportedModelDirectory = modelDir;
            ShowSuccessPanel(modelDir, selected);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, I18n.T("PhysicsImport.MsgImportFailedTitle"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string Sanitize(string name)
    {
        var bad = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => bad.Contains(c) ? '_' : c).ToArray()).Trim();
    }

    private static string PhysicsImportFormDefaultRoot()
    {
        var cwd = Directory.GetCurrentDirectory();
        if (Directory.Exists(Path.Combine(cwd, ".git")) ||
            File.Exists(Path.Combine(cwd, "PowerSimBuilder.csproj")))
        {
            return Path.Combine(cwd, "user_physics_models");
        }
        return Path.Combine(AppContext.BaseDirectory, "user_physics_models");
    }
    private void ShowSuccessPanel(string modelDir, List<FmuVariable> selected)
    {
        // Generate README.md if not present (Python-side equivalent)
        var readmePath = Path.Combine(modelDir, "README.md");
        if (!File.Exists(readmePath))
        {
            var modelName = Path.GetFileName(modelDir);
            var readme = PythonManifestGenerator.BuildReadme(
                modelName, modelDir, selected, _fmiType);
            File.WriteAllText(readmePath, readme, Encoding.UTF8);
        }

        _lblSuccessPath.Text = I18n.T("PhysicsImport.LblSuccessPath", modelDir);
        _txtPythonSample.Text = PythonManifestGenerator.BuildPythonSample(modelDir, selected);
        _pnlSuccess.Visible = true;
        _btnImport.Enabled = false;
        _btnCancel.Text = I18n.T("PhysicsImport.BtnClose");
        _btnCancel.DialogResult = DialogResult.OK;
        _lblStatus.Text = I18n.T("PhysicsImport.StatusImportSucceeded");
    }

    private void OnCopySampleClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_txtPythonSample.Text)) return;
        Clipboard.SetText(_txtPythonSample.Text);
        _lblStatus.Text = I18n.T("PhysicsImport.StatusSampleCopied");
    }

    private void OnOpenFolderClick(object? sender, EventArgs e)
    {
        var folderPath = ImportedModelDirectory ?? "";
        if (Directory.Exists(folderPath))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true,
            });
        }
    }

    private void OnShowReadmeClick(object? sender, EventArgs e)
    {
        var folderPath = ImportedModelDirectory ?? "";
        var readmePath = Path.Combine(folderPath, "README.md");
        if (File.Exists(readmePath))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = readmePath,
                UseShellExecute = true,
            });
        }
        else
        {
            MessageBox.Show(this, I18n.T("PhysicsImport.MsgReadmeNotFound", folderPath),
                I18n.T("PhysicsImport.MsgReadmeNotFoundTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

public sealed record FmuVariable(string Name, int ValueReference, string Causality, string Type);
public sealed record FmuParseResult(string FmiType, IReadOnlyList<FmuVariable> Variables);
