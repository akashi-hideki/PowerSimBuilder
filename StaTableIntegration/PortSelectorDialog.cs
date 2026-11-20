using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// PortSelectorControl をモーダル表示する薄いラッパー。
    /// SPEC: docs/SPEC_PORT_SELECTION.md
    /// </summary>
    public class PortSelectorDialog : Form
    {
        private readonly PortSelectorControl _selector;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;

        public IReadOnlyList<string> SelectedInputs  { get; private set; } = Array.Empty<string>();
        public IReadOnlyList<string> SelectedOutputs { get; private set; } = Array.Empty<string>();

        public PortSelectorDialog(
            IEnumerable<string> globals,
            IEnumerable<string> initialInputs  = null,
            IEnumerable<string> initialOutputs = null)
        {
            Text = "Select Ports";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(560, 640);
            MinimumSize = new Size(400, 400);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(8) };
            _btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 96, Height = 28,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
            };
            _btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Width = 96, Height = 28,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
            };
            bottom.Controls.Add(_btnCancel);
            bottom.Controls.Add(_btnOk);
            bottom.Resize += (s, e) =>
            {
                _btnOk.Left     = bottom.ClientSize.Width - _btnOk.Width - 8;
                _btnOk.Top      = 8;
                _btnCancel.Left = _btnOk.Left - _btnCancel.Width - 8;
                _btnCancel.Top  = 8;
            };

            _selector = new PortSelectorControl { Dock = DockStyle.Fill };
            _selector.SetCandidates(globals ?? Enumerable.Empty<string>());
            if (initialInputs != null || initialOutputs != null)
            {
                _selector.SetSelection(
                    initialInputs  ?? Enumerable.Empty<string>(),
                    initialOutputs ?? Enumerable.Empty<string>());
            }

            Controls.Add(_selector);
            Controls.Add(bottom);
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                SelectedInputs  = _selector.GetSelectedInputs();
                SelectedOutputs = _selector.GetSelectedOutputs();
            }
            base.OnFormClosing(e);
        }

        public static bool TrySelect(
            IWin32Window owner,
            IEnumerable<string> globals,
            IEnumerable<string> initialInputs,
            IEnumerable<string> initialOutputs,
            out IReadOnlyList<string> selectedInputs,
            out IReadOnlyList<string> selectedOutputs)
        {
            using (var dlg = new PortSelectorDialog(globals, initialInputs, initialOutputs))
            {
                if (dlg.ShowDialog(owner) == DialogResult.OK)
                {
                    selectedInputs  = dlg.SelectedInputs;
                    selectedOutputs = dlg.SelectedOutputs;
                    return true;
                }
            }
            selectedInputs  = null;
            selectedOutputs = null;
            return false;
        }
    }
}
