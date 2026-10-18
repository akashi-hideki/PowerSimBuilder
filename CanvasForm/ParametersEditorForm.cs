using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// ComponentControl.Parameters (Dictionary&lt;string, object&gt;) を
    /// 型に応じた UI で編集するフォーム。
    ///
    /// 背景: PropertyGrid は Dictionary の内容を個別プロパティとして
    /// 表示できないため、パラメータ編集には専用フォームが必要
    /// (Task 3-C-9)。
    /// </summary>
    public class ParametersEditorForm : Form
    {
        private readonly ComponentControl _component;
        private readonly List<ParameterEditRow> _rows = new List<ParameterEditRow>();
        private Button _btnOK;
        private Button _btnCancel;
        private Button _btnReset;

        public ParametersEditorForm(ComponentControl component)
        {
            if (component == null) throw new ArgumentNullException("component");
            _component = component;

            Text = I18n.T("ParametersEditorForm.TitleTemplate", component.InstanceName, component.Descriptor.Name);
            Width = 640;
            Height = 520;
            MinimumSize = new Size(480, 320);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.Sizable;
            ShowInTaskbar = false;

            BuildUI();
        }

        private void BuildUI()
        {
            var lblHeader = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10, 8, 10, 4),
                Text = I18n.T("ParametersEditorForm.ComponentHeader", _component.InstanceName, _component.Descriptor.Type),
                AutoSize = false,
            };

            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10),
                BorderStyle = BorderStyle.FixedSingle,
            };

            var table = new TableLayoutPanel
            {
                ColumnCount = 3,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            table.Controls.Add(CreateHeaderLabel(I18n.T("ParametersEditorForm.ColName")), 0, 0);
            table.Controls.Add(CreateHeaderLabel(I18n.T("ParametersEditorForm.ColType")), 1, 0);
            table.Controls.Add(CreateHeaderLabel(I18n.T("ParametersEditorForm.ColValue")), 2, 0);
            table.RowCount = 1;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

            var descriptors = _component.Descriptor.Parameters
                              ?? new List<ParameterDescriptor>();
            foreach (var kvp in _component.Parameters)
            {
                var desc = descriptors.FirstOrDefault(d => d.Name == kvp.Key);
                var type = (desc != null && !string.IsNullOrEmpty(desc.Type))
                           ? desc.Type : "string";
                AddRow(table, kvp.Key, type, kvp.Value);
            }

            if (_rows.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 60,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text = I18n.T("ParametersEditorForm.MsgNoParams"),
                };
                scroll.Controls.Add(lblEmpty);
            }
            else
            {
                scroll.Controls.Add(table);
            }

            var btnPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                Padding = new Padding(10),
            };
            _btnOK = new Button { Text = "OK", Width = 100, Dock = DockStyle.Right };
            _btnOK.Click += BtnOK_Click;
            _btnCancel = new Button { Text = I18n.T("ParametersEditorForm.BtnCancel"), Width = 100, Dock = DockStyle.Right, DialogResult = DialogResult.Cancel };
            _btnReset = new Button { Text = I18n.T("ParametersEditorForm.BtnReset"), Width = 140, Dock = DockStyle.Left };
            _btnReset.Click += BtnReset_Click;

            btnPanel.Controls.Add(_btnOK);
            btnPanel.Controls.Add(_btnCancel);
            btnPanel.Controls.Add(_btnReset);

            Controls.Add(scroll);
            Controls.Add(btnPanel);
            Controls.Add(lblHeader);

            AcceptButton = _btnOK;
            CancelButton = _btnCancel;
        }

        private static Label CreateHeaderLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font(Control.DefaultFont, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
            };
        }

        private void AddRow(TableLayoutPanel table, string name, string type, object value)
        {
            int rowIndex = table.RowCount;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

            var lblName = new Label
            {
                Text = name,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
            };
            var lblType = new Label
            {
                Text = type,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                ForeColor = Color.Gray,
            };

            var category = ParameterTypeHelper.Classify(type, value);
            Control editor = BuildEditor(category, value);

            table.Controls.Add(lblName, 0, rowIndex);
            table.Controls.Add(lblType, 1, rowIndex);
            table.Controls.Add(editor, 2, rowIndex);
            table.RowCount++;

            _rows.Add(new ParameterEditRow
            {
                Name = name,
                Type = type,
                Category = category,
                Editor = editor,
            });
        }

        private static Control BuildEditor(ParameterTypeCategory category, object value)
        {
            if (category == ParameterTypeCategory.Boolean)
            {
                return new CheckBox
                {
                    Checked = ToBool(value),
                    Dock = DockStyle.Fill,
                };
            }
            return new TextBox
            {
                Text = value != null ? value.ToString() : "",
                Dock = DockStyle.Fill,
            };
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            var newValues = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var row in _rows)
            {
                try
                {
                    newValues[row.Name] = ReadValue(row);
                }
                catch (FormatException fex)
                {
                    MessageBox.Show(this,
                        I18n.T("ParametersEditorForm.MsgInvalidValue", row.Name, fex.Message),
                        I18n.T("ParametersEditorForm.MsgInputError"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            foreach (var kvp in newValues)
            {
                _component.Parameters[kvp.Key] = kvp.Value;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(this,
                I18n.T("ParametersEditorForm.MsgResetConfirm"), I18n.T("Common.MsgTitleConfirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            var descriptors = _component.Descriptor.Parameters
                              ?? new List<ParameterDescriptor>();
            foreach (var row in _rows)
            {
                var desc = descriptors.FirstOrDefault(d => d.Name == row.Name);
                if (desc == null) continue;
                SetEditorValue(row.Editor, desc.DefaultValue);
            }
        }

        private static void SetEditorValue(Control editor, object value)
        {
            if (editor is CheckBox chk)
                chk.Checked = ToBool(value);
            else if (editor is TextBox tb)
                tb.Text = value != null ? value.ToString() : "";
        }

        private static object ReadValue(ParameterEditRow row)
        {
            if (row.Editor is CheckBox chk)
                return chk.Checked;
            if (row.Editor is TextBox tb)
                return ParameterTypeHelper.ParseText(tb.Text ?? "", row.Category);
            return null;
        }

        private static bool ToBool(object value)
        {
            if (value is bool b) return b;
            if (value == null) return false;
            string s = value.ToString();
            return s == "1"
                || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class ParameterEditRow
        {
            public string Name;
            public string Type;
            public ParameterTypeCategory Category;
            public Control Editor;
        }
    }
}