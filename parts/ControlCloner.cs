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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// The control cloner.
    /// </summary>
    public class ControlCloner
    {
        /// <summary>
        /// The control clone actions.
        /// </summary>
        private readonly Dictionary<Type, Action<Control, Control>> controlCloneActions;

        /// <summary>
        ///  Function Name :  ControlCloner.
        /// </summary>
        /// <returns>void.</returns>
        public ControlCloner()
        {
            controlCloneActions = new Dictionary<Type, Action<Control, Control>>
        {
            { typeof(TextBox), CloneTextBox },
                { typeof(CheckBox), CloneCheckBox },
                { typeof(ComboBox), CloneComboBox } // 追加
        };
        }

        /// <summary>
        ///  Function Name :  CloneTextBox.
        /// </summary>
        /// <param name="original">This original's Datatype is :
        ///     System.Windows.Forms.Control.</param>
        /// <param name="clone">This clone's Datatype is :
        ///     System.Windows.Forms.Control.</param>
        /// <returns>void.</returns>
        private static void CloneTextBox(Control original, Control clone)
        {
            if (original is TextBox originalTextBox && clone is TextBox cloneTextBox)
            {
                cloneTextBox.Text = originalTextBox.Text;
            }
        }

        /// <summary>
        ///  Function Name :  CloneCheckBox.
        /// </summary>
        /// <param name="original">This original's Datatype is :
        ///     System.Windows.Forms.Control.</param>
        /// <param name="clone">This clone's Datatype is :
        ///     System.Windows.Forms.Control.</param>
        /// <returns>void.</returns>
        private static void CloneCheckBox(Control original, Control clone)
        {
            if (original is CheckBox originalCheckBox && clone is CheckBox cloneCheckBox)
            {
                cloneCheckBox.Checked = originalCheckBox.Checked;
            }
        }

        /// <summary>
        ///  Function Name :  CloneComboBox.
        /// </summary>
        /// <param name="original">This original's Datatype is :
        ///     System.Windows.Forms.Control.</param>
        /// <param name="clone">This clone's Datatype is :
        ///     System.Windows.Forms.Control.</param>
        /// <returns>void.</returns>
        private static void CloneComboBox(Control original, Control clone)
        {
            if (original is ComboBox originalComboBox && clone is ComboBox cloneComboBox)
            {
                cloneComboBox.DropDownStyle = originalComboBox.DropDownStyle;
                cloneComboBox.Items.AddRange(originalComboBox.Items.Cast<object>().ToArray());
                cloneComboBox.SelectedIndex = originalComboBox.SelectedIndex;
                cloneComboBox.SelectedItem = originalComboBox.SelectedItem;
                cloneComboBox.AutoCompleteMode = originalComboBox.AutoCompleteMode;
                cloneComboBox.AutoCompleteSource = originalComboBox.AutoCompleteSource;
            }
        }

        /// <summary>
        ///  Function Name :  CloneControl.
        /// </summary>
        /// <param name="original">This original's Datatype is :
        ///     System.Windows.Forms.Control.</param>
        /// <returns>System.Windows.Forms.Control.</returns>
        private Control CloneControl(Control original)
        {
            Control clone = (Control)Activator.CreateInstance(original.GetType());

            clone.Text = original.Text;
            clone.Name = original.Name;
            clone.Size = original.Size;
            clone.Location = original.Location;
            clone.Font = original.Font;
            clone.Margin = original.Margin;
            clone.Padding = original.Padding;
            clone.Dock = original.Dock;
            clone.Anchor = original.Anchor;

            if (controlCloneActions.TryGetValue(original.GetType(), out Action<Control, Control> cloneAction))
            {
                cloneAction(original, clone);
            }

            if (clone is FlowLayoutPanel flowPanel && original is FlowLayoutPanel originalFlowPanel)
            {
                foreach (Control childControl in originalFlowPanel.Controls)
                {
                    Control clonedChild = CloneControl(childControl);
                    flowPanel.Controls.Add(clonedChild);
                    LogcatTraceHelper.Instance.Log("Debug", $"Cloned child control: {clonedChild.GetType().Name}, Text: {clonedChild.Text}");
                }
            }

            return clone;
        }

        /// <summary>
        ///  Function Name :  ClonePanel.
        /// </summary>
        /// <param name="originalPanel">This originalPanel's Datatype is :
        ///     System.Windows.Forms.Panel.</param>
        /// <returns>System.Windows.Forms.Panel.</returns>
        public Panel ClonePanel(Panel originalPanel)
        {
            Panel clonedPanel = new Panel
            {
                Text = originalPanel.Text,
                Name = originalPanel.Name,
                Size = originalPanel.Size,
                Location = originalPanel.Location,
                Font = originalPanel.Font,
                Margin = originalPanel.Margin,
                Padding = originalPanel.Padding,
                Dock = originalPanel.Dock,
                Anchor = originalPanel.Anchor
            };

            foreach (Control originalControl in originalPanel.Controls)
            {
                if (!(originalControl is Button))
                {
                    Control clonedControl = CloneControl(originalControl);
                    clonedPanel.Controls.Add(clonedControl);

                    if (clonedControl is FlowLayoutPanel flowPanel)
                    {
                        LogcatTraceHelper.Instance.Log("Debug", $"Cloned FlowLayoutPanel with {flowPanel.Controls.Count} controls.");
                    }
                    else
                    {
                        LogcatTraceHelper.Instance.Log("Debug", $"Cloned Control: {clonedControl.GetType().Name}, Text: {clonedControl.Text}");
                    }
                }
            }

            return clonedPanel;
        }
    }
}