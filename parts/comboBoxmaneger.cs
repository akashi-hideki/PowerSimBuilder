// The MIT License (MIT)
// 
// Copyright (c) .NET Foundation and Contributors
// 
// All rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
using System.Collections.Generic;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// ComboBoxManager
    /// </summary>
    public class ComboBoxManager
    {
        private Dictionary<string, List<string>> comboBoxData;

        /// <summary>
        ///  Function Name :  ComboBoxManager.
        /// </summary>
        /// <returns>void.</returns>
        public ComboBoxManager()
        {
            comboBoxData = new Dictionary<string, List<string>>();
        }

        /// <summary>
        ///      Property Name - ComboBoxData.
        /// </summary>
        public Dictionary<string, List<string>> ComboBoxData
        {
            get => comboBoxData;
            set => comboBoxData = value ?? new Dictionary<string, List<string>>();
        }

        /// <summary>
        ///  Function Name :  ContainsKey.
        /// </summary>
        /// <param name="key">This key's Datatype is : string.</param>
        /// <returns>bool.</returns>
        public bool ContainsKey(string key)
        {
            return comboBoxData.ContainsKey(key);
        }

        /// <summary>
        ///  Function Name :  PopulateComboBox.
        /// </summary>
        /// <param name="comboBoxColumn">This comboBoxColumn's Datatype is :
        ///     System.Windows.Forms.DataGridViewComboBoxColumn.</param>
        /// <param name="key">This key's Datatype is : string.</param>
        /// <returns>void.</returns>
        public void PopulateComboBox(DataGridViewComboBoxColumn comboBoxColumn, string key)
        {
            if (comboBoxColumn != null && comboBoxData.ContainsKey(key))
            {
                comboBoxColumn.Items.Clear();
                foreach (string item in comboBoxData[key])
                {
                    comboBoxColumn.Items.Add(item);
                }
            }
        }

        /// <summary>
        ///  Function Name :  HandleComboBoxInput.
        /// </summary>
        /// <param name="sender">This sender's Datatype is : object.</param>
        /// <param name="e">This e's Datatype is :
        ///     System.Windows.Forms.KeyEventArgs.</param>
        /// <returns>void.</returns>
        private void HandleComboBoxInput(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && sender is DataGridViewComboBoxEditingControl comboBox)
            {
                string newItem = comboBox.Text;
                DataGridViewComboBoxColumn column = comboBox.EditingControlDataGridView.Columns[comboBox.EditingControlDataGridView.CurrentCell.ColumnIndex] as DataGridViewComboBoxColumn;
                if (column != null && !comboBoxData[column.DataPropertyName].Contains(newItem))
                {
                    comboBoxData[column.DataPropertyName].Add(newItem);
                    PopulateComboBox(column, column.DataPropertyName);
                    comboBox.Text = newItem;
                    comboBox.SelectedItem = newItem;
                }
            }
        }

        /// <summary>
        ///  Function Name :  RegisterComboBoxEvents.
        /// </summary>
        /// <param name="dataGridView">This dataGridView's Datatype is :
        ///     System.Windows.Forms.DataGridView.</param>
        /// <returns>void.</returns>
        public void RegisterComboBoxEvents(DataGridView dataGridView)
        {
            dataGridView.EditingControlShowing += (sender, e) =>
            {
                if (e.Control is DataGridViewComboBoxEditingControl comboBox)
                {
                    comboBox.KeyDown -= HandleComboBoxInput;
                    comboBox.KeyDown += HandleComboBoxInput;
                }
            };
        }
    }
}