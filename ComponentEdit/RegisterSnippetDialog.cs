using System;
using System.Drawing;
using System.Windows.Forms;
using PowerSimBuilder.SimPartsManege;

namespace PowerSimBuilder
{
    /// <summary>
    /// Minimal dialog for registering the F_xxx function at the caret as a snippet.
    /// Shows the detected Layer/Func (read-only) and an optional description field.
    /// Overwrite confirmation is handled by the caller (PythonEditorForm).
    /// </summary>
    internal class RegisterSnippetDialog : Form
    {
        public string Layer { get; }
        public string Func { get; }
        public string Description { get; private set; }

        private readonly TextBox _txtDescription;

        public RegisterSnippetDialog(RoleSnippetRegistrar.ExtractedFunction fn)
        {
            Layer = fn?.LayerName ?? "";
            Func = fn?.Name ?? "";
            Description = "";

            Text = I18n.T("RegisterSnippet.Title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(440, 190);
            Name = "RegisterSnippetDialog";

            var lblLayer = new Label
            {
                Text = I18n.T("RegisterSnippet.LabelLayer"),
                Location = new Point(12, 18),
                AutoSize = true,
            };
            var txtLayer = new TextBox
            {
                Text = Layer,
                Location = new Point(130, 15),
                Width = 290,
                ReadOnly = true,
                Name = "TxtLayer",
            };
            var lblFunc = new Label
            {
                Text = I18n.T("RegisterSnippet.LabelFunc"),
                Location = new Point(12, 52),
                AutoSize = true,
            };
            var txtFunc = new TextBox
            {
                Text = Func,
                Location = new Point(130, 49),
                Width = 290,
                ReadOnly = true,
                Name = "TxtFunc",
            };
            var lblDesc = new Label
            {
                Text = I18n.T("RegisterSnippet.LabelDescription"),
                Location = new Point(12, 86),
                AutoSize = true,
            };
            _txtDescription = new TextBox
            {
                Location = new Point(130, 83),
                Width = 290,
                Name = "TxtDescription",
            };

            var btnOK = new Button
            {
                Text = I18n.T("RegisterSnippet.BtnOK"),
                Location = new Point(260, 145),
                Width = 75,
                DialogResult = DialogResult.OK,
                Name = "BtnRegisterSnippetOK",
            };
            var btnCancel = new Button
            {
                Text = I18n.T("RegisterSnippet.BtnCancel"),
                Location = new Point(345, 145),
                Width = 75,
                DialogResult = DialogResult.Cancel,
                Name = "BtnRegisterSnippetCancel",
            };
            btnOK.Click += (s, e) => { Description = _txtDescription.Text; };

            AcceptButton = btnOK;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[] {
                lblLayer, txtLayer, lblFunc, txtFunc, lblDesc, _txtDescription, btnOK, btnCancel
            });
        }
    }
}
