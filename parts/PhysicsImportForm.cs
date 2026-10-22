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
            Title = "Select FMU",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var parsed = ParseFmu(dlg.FileName);
            if (parsed.FmiType != "CoSimulation")
            {
                MessageBox.Show(this,
                    "This FMU is not Co-Simulation. Please re-export as CoSimulation.",
                    "Unsupported FMI type", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            _lblStatus.Text = "Parsed " + parsed.Variables.Count + " variables.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Import error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnImportClick(object? sender, EventArgs e)
    {
        if (_fmuPath is null)
        {
            MessageBox.Show(this, "Please choose an .fmu file first.",
                "Missing file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var name = _txtName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show(this, "Please enter a model name.",
                "Missing name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            var selected = _gridVars.Rows.Cast<DataGridViewRow>()
                .Where(r => (bool)(r.Cells["Use"].Value ?? false))
                .Select(r => (FmuVariable)r.Tag!)
                .ToList();
            if (selected.Count == 0)
                throw new InvalidOperationException("Select at least one input or output.");
            var yaml = BuildModelYaml(name, Path.GetFileName(_fmuPath), _fmiType, selected);
            var modelDir = Path.Combine(_userPhysicsRoot, Sanitize(name));
            var sourceDir = Path.Combine(modelDir, "source");
            Directory.CreateDirectory(sourceDir);
            File.Copy(_fmuPath, Path.Combine(sourceDir, Path.GetFileName(_fmuPath)), overwrite: true);
            File.WriteAllText(Path.Combine(modelDir, "model.yaml"), yaml, Encoding.UTF8);
            ImportedModelDirectory = modelDir;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Import failed",
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
}

public sealed record FmuVariable(string Name, int ValueReference, string Causality, string Type);
public sealed record FmuParseResult(string FmiType, IReadOnlyList<FmuVariable> Variables);