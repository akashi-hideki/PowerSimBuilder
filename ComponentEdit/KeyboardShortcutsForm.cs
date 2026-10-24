using System;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// Read-only dialog listing keyboard shortcuts for line operations.
    /// Opened via F1 or the context menu.
    /// </summary>
    public class KeyboardShortcutsForm : Form
    {
        public KeyboardShortcutsForm()
        {
            Text = I18n.T("KeyboardShortcutsForm.Title");
            Name = "KeyboardShortcutsForm";
            StartPosition = FormStartPosition.CenterParent;
            Size = new System.Drawing.Size(560, 420);
            MinimumSize = new System.Drawing.Size(420, 300);
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;

            var list = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Name = "ListViewShortcuts"
            };
            list.Columns.Add(I18n.T("KeyboardShortcutsForm.ColKey"), 220);
            list.Columns.Add(I18n.T("KeyboardShortcutsForm.ColAction"), 320);

            var btnOk = new Button
            {
                Text = I18n.T("KeyboardShortcutsForm.OK"),
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
                Height = 32,
                Name = "BtnOk"
            };

            Controls.Add(list);
            Controls.Add(btnOk);
            AcceptButton = btnOk;
            CancelButton = btnOk;

            AddSection(list, I18n.T("KeyboardShortcutsForm.SectionLineOps"));
            AddRow(list, "Ctrl+C (no selection)", I18n.T("KeyboardShortcutsForm.CopyLine"));
            AddRow(list, "Ctrl+X (no selection)", I18n.T("KeyboardShortcutsForm.CutLine"));
            AddRow(list, "Ctrl+D", I18n.T("KeyboardShortcutsForm.DuplicateLine"));
            AddRow(list, "Ctrl+Shift+K", I18n.T("KeyboardShortcutsForm.DeleteLine"));
            AddRow(list, "Alt+Up", I18n.T("KeyboardShortcutsForm.MoveUp"));
            AddRow(list, "Alt+Down", I18n.T("KeyboardShortcutsForm.MoveDown"));
            AddRow(list, "Ctrl+/", I18n.T("KeyboardShortcutsForm.ToggleComment"));
            AddRow(list, "Ctrl+L", I18n.T("KeyboardShortcutsForm.SelectLine"));
            AddSection(list, I18n.T("KeyboardShortcutsForm.SectionHelp"));
            AddRow(list, "F1", I18n.T("KeyboardShortcutsForm.ShowDialog"));

            Test_ListView = list;
        }

        public ListView Test_ListView { get; }

        private static void AddSection(ListView list, string title)
        {
            var item = new ListViewItem(title)
            {
                Font = new System.Drawing.Font(Control.DefaultFont, System.Drawing.FontStyle.Bold),
                BackColor = System.Drawing.SystemColors.ControlLight
            };
            list.Items.Add(item);
        }

        private static void AddRow(ListView list, string key, string action)
        {
            var item = new ListViewItem(key);
            item.SubItems.Add(action);
            list.Items.Add(item);
        }
    }
}
