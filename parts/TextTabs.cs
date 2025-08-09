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
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// TextTabs
    /// </summary>
    /// <seealso cref="System.Windows.Forms.TabControl" />
    public class TextTabs : TabControl
    {
        private GroupBox _userGroup;
        private Dictionary<string, List<string>> tabData = new Dictionary<string, List<string>>();

        private event MouseEventHandler UserMouseUpEvent;

        private ContextMenuManager toolMenus;
        private string _strAddTabBoxTitle = "User Method";
        private InputBox<TabNameType> tabmenu;
        private TabNameType tabName;
        private Dictionary<string, tableParam> _inputBoxSetting;

        private List<string> toolMenuName = new List<string>
        {
            "ADD Method",
            "Rename Method",
            "Delete Method"
        };

        /// <summary>
        /// The table name.: TabNameType テーブル名: TabNameType
        /// </summary>
        public Dictionary<string, tableParam> TabNameTypeTable = new Dictionary<string, tableParam>
        {
            { "TabName", new tableParam { KeyName = "TabName", TitleName = "Name", Width = 15, ComboxStringName = "", ReadOnly = false } },
        };

        /// <summary>
        /// Gets or sets the tab data.
        /// </summary>
        /// <value>The tab data.</value>
        public Dictionary<string, List<string>> TabData
        {
            get
            {
                Dictionary<string, List<string>> currentTabData = new Dictionary<string, List<string>>();
                foreach (TabPage tabPage in TabPages)
                {
                    if (tabPage.Controls[0] is TextBox textBox)
                    {
                        currentTabData[tabPage.Text] = new List<string>(textBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None));
                    }
                }
                return currentTabData;
            }
            set
            {
                tabData = value;
                RebuildTabs();
            }
        }

        /// <summary>
        /// Gets or sets the input box setting.
        /// </summary>
        /// <value>The input box setting.</value>
        public Dictionary<string, tableParam> InputBoxSetting
        {
            get => _inputBoxSetting;
            set
            {
                _inputBoxSetting = value;
                tabmenu.TableSetting = _inputBoxSetting;
            }
        }

        /// <summary>
        /// Gets or sets the tab menus.
        /// </summary>
        /// <value>The tab menus.</value>
        public List<string> TabMenus
        {
            get => toolMenuName;
            set => toolMenuName = value;
        }

        /// <summary>
        /// Gets or sets the name of the tab input box.
        /// </summary>
        /// <value>The name of the tab input box.</value>
        public string TabInputBoxName
        {
            get => _strAddTabBoxTitle;
            set
            {
                _strAddTabBoxTitle = value;
                tabmenu.TitleName = _strAddTabBoxTitle;
                toolMenuName[0] = $"Add {_strAddTabBoxTitle}";
                toolMenuName[1] = $"Rename {_strAddTabBoxTitle}";
                toolMenuName[2] = $"Delete {_strAddTabBoxTitle}";
                InitilizeContextMenuTab();
            }
        }

        /// <summary>
        /// Sets the user group.
        /// </summary>
        /// <value>The user group.</value>
        public GroupBox UserGroup
        {
            set
            {
                _userGroup = value; ;
                _userGroup.Controls.Add(this);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextTabs"/> class.
        /// </summary>
        public TextTabs()
        {
            Dock = DockStyle.Fill;
            _inputBoxSetting = new Dictionary<string, tableParam>();
            InitilizeContextMenuTab();
            MouseUp += new MouseEventHandler(TabMouseUpEvent);
            DrawMode = TabDrawMode.OwnerDrawFixed;
            DrawItem += new DrawItemEventHandler(tabControl_DrawItem);
        }

        /// <summary>
        /// Initilizes the context menu tab.
        /// </summary>
        private void InitilizeContextMenuTab()
        {
            toolMenus = new ContextMenuManager();
            List<MenuItemDefinition> tabMenus = new List<MenuItemDefinition>
            {
                new MenuItemDefinition(toolMenuName[0],ContextMenuAdd),
                new MenuItemDefinition(toolMenuName[1],ContextMenuRenameTab),
                new MenuItemDefinition(toolMenuName[2],ContextMenuRemove)
            };
            tabName = new TabNameType();
            toolMenus.AddMenuItems("TAB_MENU", tabMenus);
            tabmenu = new InputBox<TabNameType>() { TitleName = "Method", TableSetting = TabNameTypeTable };
        }

        /// <summary>
        /// Contexts the menu add.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the
        ///     event data.</param>
        private void ContextMenuAdd(object sender, EventArgs e)
        {
            int tabCount = TabCount + 1;
            AddTabMenu($"{_strAddTabBoxTitle.Replace(" ", string.Empty)}{tabCount.ToString()}");
        }

        /// <summary>
        /// Adds the tab menu.
        /// </summary>
        /// <param name="tabName">Name of the tab.</param>
        public void AddTabMenu(string tabName)
        {
            List<string> data = new List<string>
            {
                string.Empty,
                string.Empty
            };

            if (tabmenu.ShowDialogValues(new TabNameType() { TabName = tabName }) == DialogResult.OK)
            {
                tabName = tabmenu.Result.TabName;
                AddNewTab(tabName, data);
            }
        }

        /// <summary>
        /// Del_tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContextMenuRemove(object sender, EventArgs e)
        {
            string temp = SelectedTab.Name;
            if (TabCount > 1)
            {
                RemoveTab(temp);
            }
            else
            {
                MessageBox.Show("this tab is disable to delete");
            }
        }

        /// <summary>
        /// Rename_tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContextMenuRenameTab(object sender, EventArgs e)
        {
            tabName.TabName = SelectedTab.Text;
            string old_name = SelectedTab.Text;
            if (tabmenu.ShowDialogValues(tabName) == DialogResult.OK)
            {
                tabName = tabmenu.Result;
                RenameTab(old_name, tabName.TabName);
            }
        }

        /// <summary>
        /// Rebuilds the tabs.
        /// </summary>
        private void RebuildTabs()
        {
            TabPages.Clear();

            foreach (KeyValuePair<string, List<string>> tab in tabData)
            {
                AddNewTab(tab.Key, tab.Value);
            }
            if (TabPages.Count > 0)
            {
                SelectedTab = TabPages[0];
            }
        }

        /// <summary>
        /// Adds the new tab.
        /// </summary>
        /// <param name="tabName">Name of the tab.</param>
        /// <param name="textData">The text data.</param>
        public void AddNewTab(string tabName, List<string> textData = null)
        {
            TabPage newTabPage = new TabPage(tabName) { Name = tabName, Text = tabName };
            TextBox textBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both
            };

            if (textData != null)
            {
                textBox.Text = string.Join(Environment.NewLine, textData);
            }

            textBox.TextChanged += (sender, e) => TextBox_TextChanged(sender, e, newTabPage.Text);

            newTabPage.Controls.Add(textBox);
            TabPages.Add(newTabPage);
            SelectedTab = newTabPage;
        }

        /// <summary>
        /// Renames the tab.
        /// </summary>
        /// <param name="oldTabName">Old name of the tab.</param>
        /// <param name="newTabName">New name of the tab.</param>
        public void RenameTab(string oldTabName, string newTabName)
        {
            for (int i = 0; i < TabPages.Count; i++)
            {
                if (TabPages[i].Text == oldTabName)
                {
                    TabPages[i].Text = newTabName;
                    TabPages[i].Name = newTabName;

                    if (tabData.ContainsKey(oldTabName))
                    {
                        List<string> textData = tabData[oldTabName];
                        tabData.Remove(oldTabName);
                        tabData[newTabName] = textData;
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Removes the tab.
        /// </summary>
        /// <param name="tabName">Name of the tab.</param>
        public void RemoveTab(string tabName)
        {
            for (int i = 0; i < TabPages.Count; i++)
            {
                if (TabPages[i].Text == tabName)
                {
                    TabPages.RemoveAt(i);
                    tabData.Remove(tabName);
                    break;
                }
            }
        }

        /// <summary>
        /// Texts the box text changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the
        ///     event data.</param>
        /// <param name="tabName">Name of the tab.</param>
        private void TextBox_TextChanged(object sender, EventArgs e, string tabName)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                tabData[tabName] = new List<string>(textBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None));
            }
        }

        /// <summary>
        /// Tabs the mouse up event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing
        ///     the event data.</param>
        private void TabMouseUpEvent(object sender, MouseEventArgs e)
        {
            if (UserMouseUpEvent != null)
            {
                UserMouseUpEvent(sender, e);
            }
            else
            {
                if (e.Button == MouseButtons.Right)
                {
                    TabControl tabControl = sender as TabControl;
                    for (int i = 0; i < tabControl.TabCount; i++)
                    {
                        Rectangle tabRect = tabControl.GetTabRect(i);
                        if (tabRect.Contains(e.Location))
                        {
                            toolMenus.GetMenu("TAB_MENU").Show(tabControl, e.Location);
                            break;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Handles the DrawItem event of the tabControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DrawItemEventArgs"/> instance
        ///     containing the event data.</param>
        private void tabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabPage tabPage = this.TabPages[e.Index];
            Rectangle tabRect = this.GetTabRect(e.Index);
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            if (e.Index == this.SelectedIndex)
            {
                // Selected Tab
                e.Graphics.FillRectangle(Brushes.LightBlue, tabRect);

                // DrawTab
                Font tabFont = new Font(this.Font, FontStyle.Regular);

                // テキストがはみ出さないように調整
                TextRenderer.DrawText(e.Graphics, this.TabPages[e.Index].Text, tabFont, tabRect, this.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            else
            {
                // 非選択のタブ
                e.Graphics.FillRectangle(Brushes.LightGray, tabRect);

                // 通常のフォントでテキストを描画 (テキストがはみ出さないようにする)
                TextRenderer.DrawText(e.Graphics, this.TabPages[e.Index].Text, this.Font, tabRect, this.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }
    }
}