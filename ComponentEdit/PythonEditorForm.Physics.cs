#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PowerSimBuilder.Physics;

namespace PowerSimBuilder;

public partial class PythonEditorForm
{
    private bool _physicsWired;

    private static readonly Color PhysicsHighlightColor = Color.FromArgb(220, 235, 255);

    private readonly Dictionary<string, FmuInfo> _physicsCache =
        new(StringComparer.Ordinal);

    private string? _statusBeforePhysics;

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

        using var picker = new PhysicsModelPickerForm();
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        if (picker.SelectedModel == null) return;

        var result = PhysicsInsertionFlow.TryInsert(textEditor.Text, picker.SelectedModel);

        if (result.Outcome == InsertOutcome.AlreadyExists)
        {
            AppServices.MessageBox.ShowInfo(
                this,
                I18n.T("EditPanel.MsgPhysicsAlreadyExists", picker.SelectedModel.Info.ModelName),
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

        // Cache model info for status bar (D)
        _physicsCache[picker.SelectedModel.Info.ModelName] = picker.SelectedModel.Info;

        textEditor.Text = result.NewCode!;
        RefreshEditPanel();

        var modelName = picker.SelectedModel.Info.ModelName;
        var initBounds   = PhysicsInsertionHighlighter.FindBlock(result.NewCode!, modelName);
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
}