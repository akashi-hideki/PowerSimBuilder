using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public static class I18nDumpUi
    {
        private static readonly HashSet<Form> _seen = new HashSet<Form>();
        private static Timer _timer;

        public static void InstallAutoDump()
        {
            if (_timer != null) return;
            _timer = new Timer { Interval = 300 };
            _timer.Tick += (s, e) => DumpNewForms();
            _timer.Start();
        }

        private static void DumpNewForms()
        {
            try
            {
                foreach (Form f in Application.OpenForms)
                {
                    if (_seen.Contains(f)) continue;
                    if (!f.Visible) continue;
                    _seen.Add(f);
                    try { DumpSingleForm(f); } catch { }
                }
            }
            catch { }
        }

        public static void DumpAllForms()
        {
            try
            {
                var count = 0;
                foreach (Form f in Application.OpenForms)
                {
                    if (!f.Visible) continue;
                    try { DumpSingleForm(f); count++; } catch { }
                }
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "i18n_dump.txt");
                MessageBox.Show($"Dumped {count} form(s) to:\n{path}", "i18n dump");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dump failed: {ex.Message}", "i18n dump");
            }
        }

        public static void DumpSingleForm(Form f)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"== Form: {f.GetType().Name} Title=\"{f.Text}\" ==");
            DumpControls(f.Controls, sb, 1);
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "i18n_dump.txt");
            File.AppendAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        private static void DumpControls(Control.ControlCollection controls, StringBuilder sb, int depth)
        {
            foreach (Control c in controls)
            {
                if (!string.IsNullOrEmpty(c.Text))
                    sb.AppendLine($"{new string(' ', depth * 2)}[{c.GetType().Name}] Name={c.Name} Text=\"{c.Text}\"");
                if (c is ToolStrip ts)
                    DumpToolStripItems(ts.Items, sb, depth + 1);
                if (c.HasChildren)
                    DumpControls(c.Controls, sb, depth + 1);
            }
        }

        private static void DumpToolStripItems(ToolStripItemCollection items, StringBuilder sb, int depth)
        {
            foreach (ToolStripItem item in items)
            {
                if (!string.IsNullOrEmpty(item.Text))
                    sb.AppendLine($"{new string(' ', depth * 2)}[{item.GetType().Name}] Name={item.Name} Text=\"{item.Text}\"");
                if (item is ToolStripDropDownItem dd)
                    DumpToolStripItems(dd.DropDownItems, sb, depth + 1);
            }
        }
    }
}