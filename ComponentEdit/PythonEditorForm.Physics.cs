#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PowerSimBuilder.Physics;

namespace PowerSimBuilder;

/// <summary>
/// Picks a physics model. Default implementation opens the WinForms dialog.
/// Overridable for tests.
/// </summary>
public interface IPhysicsPicker
{
    PhysicsModelEntry? Pick(IWin32Window? owner);
}

internal sealed class WinFormsPhysicsPicker : IPhysicsPicker
{
    public PhysicsModelEntry? Pick(IWin32Window? owner)
    {
        using var picker = new PhysicsModelPickerForm();
        return picker.ShowDialog(owner) == DialogResult.OK ? picker.SelectedModel : null;
    }
}

public partial class PythonEditorForm
{
    private bool _physicsWired;
    private static readonly Color PhysicsHighlightColor = Color.FromArgb(220, 235, 255);
    private readonly Dictionary<string, FmuInfo> _physicsCache = new(StringComparer.Ordinal);
    private string? _statusBeforePhysics;

    private IPhysicsPicker _pickerProvider = new WinFormsPhysicsPicker();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IPhysicsPicker PickerProvider
    {
        get => _pickerProvider;
        set => _pickerProvider = value ?? new WinFormsPhysicsPicker();
    }

    public string ComponentType { get; set; } = "";

    public void SetPythonCode(string code)
    {
        if (textEditor == null) return;
        textEditor.Text = code ?? "";
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        EnsurePhysicsWired();
    }

    private void EnsurePhysicsWired()
    {
        if (_physicsWired) return;
        if (editPanel != null)
            editPanel.PhysicsRequested += OnPhysicsRequested;
        if (textEditor != null)
            textEditor.SelectionChanged += OnEditorSelectionChanged;
        _physicsWired = true;
    }

    private void OnEditorSelectionChanged(object? sender, EventArgs e)
    {
        if (textEditor == null || lblStatus == null) return;

        var hit = PhysicsInsertionHighlighter.FindBlockAtPosition(
            textEditor.Text, textEditor.SelectionStart);

        if (hit is null)
        {
            if (_statusBeforePhysics != null)
            {
                lblStatus.Text = _statusBeforePhysics;
                _statusBeforePhysics = null;
            }
            return;
        }

        if (_statusBeforePhysics == null)
            _statusBeforePhysics = lblStatus.Text;

        if (_physicsCache.TryGetValue(hit.ModelName, out var info))
        {
            lblStatus.Text = string.Format(
                "Physics: {0} ({1} in, {2} out, FMI {3}) 窶・{4} L{5}-{6}",
                hit.ModelName, info.Inputs.Count, info.Outputs.Count,
                info.FmiVersion, hit.Kind, hit.StartLine, hit.EndLine);
        }
        else
        {
            lblStatus.Text = string.Format(
                "Physics: {0} 窶・{1} L{2}-{3}",
                hit.ModelName, hit.Kind, hit.StartLine, hit.EndLine);
        }
    }

    private bool IsControllerBox()
    {
        if (string.Equals(ComponentType, "ControllerBox", StringComparison.Ordinal))
            return true;
        var path = _filePath;
        if (!string.IsNullOrEmpty(path))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (name != null &&
                name.EndsWith("ControllerBox", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private void OnPhysicsRequested(object? sender, EventArgs e)
    {
        if (IsControllerBox())
        {
            AppServices.MessageBox.ShowInfo(
                this,
                I18n.T("EditPanel.MsgPhysicsControllerBox"),
                I18n.T("Common.MsgTitleInfo"));
            return;
        }

        var selected = _pickerProvider.Pick(this);
        if (selected == null) return;

        var result = PhysicsInsertionFlow.TryInsert(textEditor.Text, selected);

        if (result.Outcome == InsertOutcome.AlreadyExists)
        {
            AppServices.MessageBox.ShowInfo(
                this,
                I18n.T("EditPanel.MsgPhysicsAlreadyExists", selected.Info.ModelName),
                I18n.T("Common.MsgTitleInfo"));
            return;
        }

        if (!result.Succeeded)
        {
            AppServices.MessageBox.ShowError(
                this,
                result.ErrorMessage ?? I18n.T("EditPanel.MsgPhysicsInsertFail"),
                I18n.T("Common.MsgTitleError"));
            return;
        }

        _physicsCache[selected.Info.ModelName] = selected.Info;

        textEditor.Text = result.NewCode!;
        RefreshEditPanel();

        var modelName = selected.Info.ModelName;
        var initBounds = PhysicsInsertionHighlighter.FindBlock(result.NewCode!, modelName);
        var updateBounds = PhysicsInsertionHighlighter.FindUpdateBlock(result.NewCode!, modelName);

        void ApplyHighlight()
        {
            HighlightBlocks(initBounds, updateBounds);
            if (initBounds is not null)
            {
                textEditor.SelectionStart = initBounds.Start;
                textEditor.SelectionLength = 0;
                textEditor.ScrollToCaret();
            }
            textEditor.Focus();
        }

        if (IsHandleCreated)
            BeginInvoke((Action)ApplyHighlight);
        else
            ApplyHighlight();

        if (lblStatus != null)
        {
            if (initBounds is not null && updateBounds is not null)
                lblStatus.Text = I18n.T("EditPanel.MsgPhysicsInsertedAtLinesBoth",
                    modelName, initBounds.StartLine, initBounds.EndLine,
                    updateBounds.StartLine, updateBounds.EndLine);
            else if (initBounds is not null)
                lblStatus.Text = I18n.T("EditPanel.MsgPhysicsInsertedAtLines",
                    modelName, initBounds.StartLine, initBounds.EndLine);
            else
                lblStatus.Text = I18n.T("EditPanel.MsgPhysicsInserted", modelName);
        }
    }

    private void HighlightBlocks(
        PhysicsBlockBounds? initBounds,
        PhysicsBlockBounds? updateBounds)
    {
        if (textEditor is not RichTextBox rtb) return;
        rtb.SuspendLayout();
        try
        {
            if (initBounds is not null)
            {
                rtb.Select(initBounds.Start, initBounds.Length);
                rtb.SelectionBackColor = PhysicsHighlightColor;
            }
            if (updateBounds is not null)
            {
                rtb.Select(updateBounds.Start, updateBounds.Length);
                rtb.SelectionBackColor = PhysicsHighlightColor;
            }
        }
        finally
        {
            rtb.ResumeLayout();
        }
    }

    // -------- Test hooks --------
    [EditorBrowsable(EditorBrowsableState.Never)]
    public RichTextBox Test_TextEditor => textEditor;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Label Test_StatusLabel => lblStatus;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Dictionary<string, FmuInfo> Test_PhysicsCache => _physicsCache;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Test_RaisePhysicsRequested() => OnPhysicsRequested(this, EventArgs.Empty);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Test_RaiseSelectionChanged() => OnEditorSelectionChanged(this, EventArgs.Empty);
}