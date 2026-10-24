using System;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// Line-level editing for PythonEditorForm's textEditor (RichTextBox).
    /// Split into a partial class to keep PythonEditorForm.cs focused.
    /// Shortcuts follow VSCode / Visual Studio conventions.
    /// </summary>
    public partial class PythonEditorForm
    {
        // ================================================================
        // ProcessCmdKey: line-level shortcuts
        // ================================================================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool ctrl = (keyData & Keys.Control) == Keys.Control;
            bool shift = (keyData & Keys.Shift) == Keys.Shift;
            bool alt = (keyData & Keys.Alt) == Keys.Alt;
            Keys key = keyData & Keys.KeyCode;

            // Ctrl+C without selection -> copy current line
            if (ctrl && !shift && !alt && key == Keys.C
                && textEditor.Focused && textEditor.SelectionLength == 0)
            {
                CopyCurrentLine();
                return true;
            }
            // Ctrl+X without selection -> cut current line
            if (ctrl && !shift && !alt && key == Keys.X
                && textEditor.Focused && textEditor.SelectionLength == 0)
            {
                CutCurrentLine();
                return true;
            }
            // Ctrl+D -> duplicate current line
            if (ctrl && !shift && !alt && key == Keys.D && textEditor.Focused)
            {
                DuplicateCurrentLine();
                return true;
            }
            // Ctrl+Shift+K -> delete current line (or selected lines)
            if (ctrl && shift && !alt && key == Keys.K && textEditor.Focused)
            {
                DeleteSelectedLines();
                return true;
            }
            // Alt+Up -> move current line up
            if (alt && !ctrl && !shift && key == Keys.Up && textEditor.Focused)
            {
                MoveCurrentLineUp();
                return true;
            }
            // Alt+Down -> move current line down
            if (alt && !ctrl && !shift && key == Keys.Down && textEditor.Focused)
            {
                MoveCurrentLineDown();
                return true;
            }
            // Ctrl+/ -> toggle line comment
            if (ctrl && !shift && !alt && key == Keys.OemQuestion && textEditor.Focused)
            {
                ToggleLineComment();
                return true;
            }
            // Ctrl+L -> select current line
            if (ctrl && !shift && !alt && key == Keys.L && textEditor.Focused)
            {
                SelectCurrentLine(false);
                return true;
            }

            // Ctrl+Shift+P -> open physics removal picker
            if (ctrl && shift && !alt && key == Keys.P && textEditor.Focused)
            {
                ShowPhysicsRemovePicker();
                return true;
            }

            // F1 -> show keyboard shortcuts
            if (!ctrl && !shift && !alt && key == Keys.F1 && textEditor.Focused)
            {
                ShowKeyboardShortcuts();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ================================================================
        // Line range helpers
        // ================================================================
        private void GetCurrentLineRange(out int start, out int length)
        {
            int charIndex = textEditor.SelectionStart;
            int line = textEditor.GetLineFromCharIndex(charIndex);
            if (line < 0) line = 0;
            start = textEditor.GetFirstCharIndexFromLine(line);
            if (start < 0) start = 0;
            int next = textEditor.GetFirstCharIndexFromLine(line + 1);
            if (next < 0) next = textEditor.TextLength;
            length = next - start;
        }

        private int GetLineEnd(int line)
        {
            int next = textEditor.GetFirstCharIndexFromLine(line + 1);
            return next < 0 ? textEditor.TextLength : next;
        }

        // ================================================================
        // Line operations
        // ================================================================
        private void SelectCurrentLine(bool includeLineBreak)
        {
            GetCurrentLineRange(out int start, out int length);
            if (includeLineBreak)
            {
                textEditor.Select(start, length);
                return;
            }
            int end = start + length;
            string text = textEditor.Text;
            while (end > start && (text[end - 1] == '\n' || text[end - 1] == '\r')) end--;
            textEditor.Select(start, end - start);
        }

        private void CopyCurrentLine()
        {
            GetCurrentLineRange(out int start, out int length);
            if (length <= 0) return;
            string line = textEditor.Text.Substring(start, length);
            if (!line.EndsWith("\n") && !line.EndsWith("\r")) line += "\n";
            if (!string.IsNullOrEmpty(line))
            {
                try { Clipboard.SetText(line); } catch { /* ignore */ }
            }
        }

        private void CutCurrentLine()
        {
            CopyCurrentLine();
            DeleteCurrentLine();
        }

        private void DeleteCurrentLine()
        {
            GetCurrentLineRange(out int start, out int length);
            if (length <= 0) return;
            textEditor.Select(start, length);
            textEditor.SelectedText = "";
        }

        private void DeleteSelectedLines()
        {
            if (textEditor.SelectionLength == 0)
            {
                DeleteCurrentLine();
                return;
            }
            int selStart = textEditor.SelectionStart;
            int selEnd = selStart + textEditor.SelectionLength;
            int startLine = textEditor.GetLineFromCharIndex(selStart);
            int endLine = textEditor.GetLineFromCharIndex(selEnd - 1);
            if (startLine < 0) startLine = 0;
            if (endLine < startLine) endLine = startLine;
            int delStart = textEditor.GetFirstCharIndexFromLine(startLine);
            if (delStart < 0) delStart = 0;
            int delEnd = GetLineEnd(endLine);
            textEditor.Select(delStart, delEnd - delStart);
            textEditor.SelectedText = "";
        }

        private void DuplicateCurrentLine()
        {
            int cursorLine = textEditor.GetLineFromCharIndex(textEditor.SelectionStart);
            if (cursorLine < 0) return;
            int lineStart = textEditor.GetFirstCharIndexFromLine(cursorLine);
            if (lineStart < 0) return;
            int lineEnd = GetLineEnd(cursorLine);
            int offsetInLine = textEditor.SelectionStart - lineStart;
            string lineText = textEditor.Text.Substring(lineStart, lineEnd - lineStart);

            textEditor.Select(lineEnd, 0);
            if (lineText.EndsWith("\n") || lineText.EndsWith("\r"))
            {
                textEditor.SelectedText = lineText;
                textEditor.SelectionStart = lineEnd + offsetInLine;
            }
            else
            {
                textEditor.SelectedText = "\n" + lineText;
                textEditor.SelectionStart = lineEnd + 1 + offsetInLine;
            }
            textEditor.SelectionLength = 0;
        }

        private void MoveCurrentLineUp()
        {
            int line = textEditor.GetLineFromCharIndex(textEditor.SelectionStart);
            if (line <= 0) return;
            int lineStart = textEditor.GetFirstCharIndexFromLine(line);
            int lineEnd = GetLineEnd(line);
            int prevStart = textEditor.GetFirstCharIndexFromLine(line - 1);
            if (prevStart < 0 || lineStart < 0) return;
            int offsetInLine = textEditor.SelectionStart - lineStart;
            string prevText = textEditor.Text.Substring(prevStart, lineStart - prevStart);
            string curText = textEditor.Text.Substring(lineStart, lineEnd - lineStart);

            string combined;
            if (curText.EndsWith("\n") || curText.EndsWith("\r"))
            {
                combined = curText + prevText;
            }
            else
            {
                combined = curText + "\n" + prevText.TrimEnd('\r', '\n');
            }

            textEditor.Select(prevStart, lineEnd - prevStart);
            textEditor.SelectedText = combined;
            textEditor.SelectionStart = prevStart + offsetInLine;
            textEditor.SelectionLength = 0;
        }

        private void MoveCurrentLineDown()
        {
            int line = textEditor.GetLineFromCharIndex(textEditor.SelectionStart);
            if (line >= textEditor.Lines.Length - 1) return;
            int lineStart = textEditor.GetFirstCharIndexFromLine(line);
            int lineEnd = GetLineEnd(line);
            int nextEnd = GetLineEnd(line + 1);
            if (lineStart < 0) return;
            int offsetInLine = textEditor.SelectionStart - lineStart;
            string curText = textEditor.Text.Substring(lineStart, lineEnd - lineStart);
            string nextText = textEditor.Text.Substring(lineEnd, nextEnd - lineEnd);

            string combined;
            int newCursor;
            if (nextText.EndsWith("\n") || nextText.EndsWith("\r"))
            {
                combined = nextText + curText;
                newCursor = lineStart + nextText.Length + offsetInLine;
            }
            else
            {
                combined = nextText + "\n" + curText.TrimEnd('\r', '\n');
                newCursor = lineStart + nextText.Length + 1 + offsetInLine;
            }

            textEditor.Select(lineStart, nextEnd - lineStart);
            textEditor.SelectedText = combined;
            textEditor.SelectionStart = newCursor;
            textEditor.SelectionLength = 0;
        }

        private void ToggleLineComment()
        {
            int selStart = textEditor.SelectionStart;
            int selEnd = selStart + textEditor.SelectionLength;
            int startLine = textEditor.GetLineFromCharIndex(selStart);
            int endLine = selEnd > selStart
                ? textEditor.GetLineFromCharIndex(selEnd - 1)
                : startLine;
            if (startLine < 0) startLine = 0;
            if (endLine < startLine) endLine = startLine;

            var lines = textEditor.Lines;
            bool allCommented = true;
            for (int i = startLine; i <= endLine; i++)
            {
                string l = (i >= 0 && i < lines.Length) ? lines[i] : "";
                if (l.Trim().Length == 0) continue;
                if (!l.TrimStart().StartsWith("#")) { allCommented = false; break; }
            }

            textEditor.SuspendLayout();
            try
            {
                for (int i = endLine; i >= startLine; i--)
                {
                    int lineStart = textEditor.GetFirstCharIndexFromLine(i);
                    if (lineStart < 0) continue;
                    string lineText = (i >= 0 && i < textEditor.Lines.Length) ? textEditor.Lines[i] : "";
                    if (lineText.Trim().Length == 0) continue;

                    if (allCommented)
                    {
                        int idx = lineText.IndexOf('#');
                        if (idx >= 0)
                        {
                            int removeLen = 1;
                            if (idx + 1 < lineText.Length && lineText[idx + 1] == ' ') removeLen = 2;
                            textEditor.Select(lineStart + idx, removeLen);
                            textEditor.SelectedText = "";
                        }
                    }
                    else
                    {
                        int indent = 0;
                        while (indent < lineText.Length && (lineText[indent] == ' ' || lineText[indent] == '\t')) indent++;
                        textEditor.Select(lineStart + indent, 0);
                        textEditor.SelectedText = "# ";
                    }
                }
            }
            finally
            {
                textEditor.ResumeLayout();
            }
        }

        // ================================================================
        // Context menu extension (called from InitializeContextMenu)
        // ================================================================
        private void AppendLineOpsMenuItems(ContextMenuStrip menu)
        {
            if (menu == null) return;
            menu.Items.Add(new ToolStripSeparator());

            var dup = new ToolStripMenuItem(I18n.T("PythonEditor.MenuDuplicateLine") + "\tCtrl+D");
            dup.Click += (s, e) => { if (textEditor.Focused) DuplicateCurrentLine(); };
            menu.Items.Add(dup);

            var del = new ToolStripMenuItem(I18n.T("PythonEditor.MenuDeleteLine") + "\tCtrl+Shift+K");
            del.Click += (s, e) => { if (textEditor.Focused) DeleteSelectedLines(); };
            menu.Items.Add(del);

            menu.Items.Add(new ToolStripSeparator());

            var up = new ToolStripMenuItem(I18n.T("PythonEditor.MenuMoveLineUp") + "\tAlt+Up");
            up.Click += (s, e) => { if (textEditor.Focused) MoveCurrentLineUp(); };
            menu.Items.Add(up);

            var down = new ToolStripMenuItem(I18n.T("PythonEditor.MenuMoveLineDown") + "\tAlt+Down");
            down.Click += (s, e) => { if (textEditor.Focused) MoveCurrentLineDown(); };
            menu.Items.Add(down);

            menu.Items.Add(new ToolStripSeparator());

            var comment = new ToolStripMenuItem(I18n.T("PythonEditor.MenuToggleComment") + "\tCtrl+/");
            comment.Click += (s, e) => { if (textEditor.Focused) ToggleLineComment(); };
            menu.Items.Add(comment);

            var selectLine = new ToolStripMenuItem(I18n.T("PythonEditor.MenuSelectLine") + "\tCtrl+L");
            selectLine.Click += (s, e) => { if (textEditor.Focused) SelectCurrentLine(false); };
            menu.Items.Add(selectLine);

            menu.Items.Add(new ToolStripSeparator());
            var shortcuts = new ToolStripMenuItem(I18n.T("PythonEditor.MenuKeyboardShortcuts") + "\tF1");
            shortcuts.Click += (s, e) => ShowKeyboardShortcuts();
            menu.Items.Add(shortcuts);

            AppendPhysicsRemoveMenuItem(menu);
        }

        // ================================================================
        // Test hooks (L2 tests)
        // ================================================================
        public void Test_SetCursor(int oneBasedLine, int zeroBasedCol)
        {
            int lineIdx = oneBasedLine - 1;
            if (lineIdx < 0 || lineIdx >= textEditor.Lines.Length) return;
            int lineStart = textEditor.GetFirstCharIndexFromLine(lineIdx);
            if (lineStart < 0) return;
            int pos = lineStart + zeroBasedCol;
            if (pos > textEditor.TextLength) pos = textEditor.TextLength;
            textEditor.SelectionStart = pos;
            textEditor.SelectionLength = 0;
        }

        public void Test_DuplicateCurrentLine() { DuplicateCurrentLine(); }
        public void Test_DeleteCurrentLine() { DeleteCurrentLine(); }
        public void Test_DeleteSelectedLines() { DeleteSelectedLines(); }
        public void Test_MoveLineUp() { MoveCurrentLineUp(); }
        public void Test_MoveLineDown() { MoveCurrentLineDown(); }
        public void Test_ToggleComment() { ToggleLineComment(); }
        public void Test_SelectCurrentLine() { SelectCurrentLine(false); }
        public string Test_GetSelection() { return textEditor.SelectedText; }
        private void ShowKeyboardShortcuts()
        {
            using (var form = new KeyboardShortcutsForm())
            {
                form.ShowDialog(this);
            }
        }
    }
}
