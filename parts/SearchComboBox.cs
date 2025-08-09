// The MIT License (MIT)
//
// Copyright (c) .NET Foundation and Contributors
//
// All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// Search Tool
    /// </summary>
    public class SearchComboBox
    {
        private ToolStripComboBox _comboBox;

        /// <summary>
        /// SearchList Property
        /// </summary>
        /// <param name="searchText">The search text.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public delegate List<string> GetSearchList(string searchText, string type);

        /// <summary>
        /// Gets or sets the search function.
        /// </summary>
        /// <value>The search function.</value>
        public GetSearchList SearchFunction { get; set; }

        /// <summary>
        /// Gets or sets the ComboBox.
        /// </summary>
        /// <value>The ComboBox.</value>
        public ToolStripComboBox ComboBox
        {
            get => _comboBox;
            set
            {
                if (_comboBox != null)
                {
                    _comboBox.TextChanged -= ComboBox_TextChanged;
                }
                _comboBox = value;
                if (_comboBox != null)
                {
                    _comboBox.TextChanged += ComboBox_TextChanged;
                }
            }
        }

        /// <summary>
        /// Gets the selected text.
        /// </summary>
        /// <value>The selected text.</value>
        public string SelectedText => _comboBox?.SelectedIndex >= 0 ? _comboBox.Text : null;

        private void ComboBox_TextChanged(object sender, EventArgs e)
        {
            if (SearchFunction == null)
            {
                return;
            }

            _comboBox.Items.Clear();
            string searchText = _comboBox.Text;
            List<string> dropList = SearchFunction.Invoke(searchText, "SYMBOL");

            if (dropList.Count > 0)
            {
                foreach (string temp in dropList)
                {
                    _comboBox.Items.Add(temp);
                }
                _comboBox.DroppedDown = true; // ドロップダウンを開く
                Cursor.Current = Cursors.Default;
                _comboBox.Select(searchText.Length, 0);
            }
            else
            {
                _comboBox.DroppedDown = false; // ドロップダウンを閉じる
                _comboBox.SelectedIndex = -1; // インデックスをリセット
                _comboBox.Text = string.Empty; // テキストボックスをクリア
            }
        }
    }
}