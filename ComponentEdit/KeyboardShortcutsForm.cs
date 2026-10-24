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
            Text = "Keyboard Shortcuts";
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
            list.Columns.Add("Key", 220);
            list.Columns.Add("Action", 320);

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
                Height = 32,
                Name = "BtnOk"
            };

            Controls.Add(list);
            Controls.Add(btnOk);
            AcceptButton = btnOk;
            CancelButton = btnOk;

            AddSection(list, "Line Operations");
            AddRow(list, "Ctrl+C (no selection)", "Copy current line");
            AddRow(list, "Ctrl+X (no selection)", "Cut current line");
            AddRow(list, "Ctrl+D", "Duplicate current line");
            AddRow(list, "Ctrl+Shift+K", "Delete current line(s)");
            AddRow(list, "Alt+Up", "Move line up");
            AddRow(list, "Alt+Down", "Move line down");
            AddRow(list, "Ctrl+/", "Toggle line comment");
            AddRow(list, "Ctrl+L", "Select current line");
            AddSection(list, "Help");
            AddRow(list, "F1", "Show this dialog");

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