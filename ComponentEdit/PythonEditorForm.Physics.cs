#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PowerSimBuilder.Physics;

namespace PowerSimBuilder;

public partial class PythonEditorForm
{
    private bool _physicsWired;

    /// <summary>
    /// Background color used for physics block highlighting.
    /// </summary>
    private static readonly Color PhysicsHighlightColor = Color.FromArgb(220, 235, 255);

    /// <summary>
    /// Component type (e.g., "FirstOrderLag", "ControllerBox").
    /// </summary>
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
        if (editPanel == null) return;
        editPanel.PhysicsRequested += OnPhysicsRequested;
        _physicsWired = true;
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

        textEditor.Text = result.NewCode!;
        RefreshEditPanel();

        var modelName = picker.SelectedModel.Info.ModelName;
        var initBounds   = PhysicsInsertionHighlighter.FindBlock(result.NewCode!, modelName);
        var updateBounds = PhysicsInsertionHighlighter.FindUpdateBlock(result.NewCode!, modelName);

        void ApplyHighlight()
        {
            HighlightBlocks(initBounds, updateBounds);

            // Scroll to init block (top of insertion)
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