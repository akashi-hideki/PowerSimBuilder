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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

/// <summary>
/// Namespace Name - CSgen.
/// </summary>
namespace PowerSimBuilder
{
    /// <summary>
    /// MouseUpEvent delegate
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the
    ///     event data.</param>
    public delegate void MouseUpEvent(object sender, MouseEventArgs e);

    /// <summary>
    /// TabControlManager
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TabControlManager<T> where T : new()
    {
        private GroupBox _userGroup;
        private readonly TabControl tabControl;
        private multiFileManager<T> _viewFileManager;

        private readonly InputBox<TabAttr> tabnameBox;
        private readonly InputBox<TabNameType> renameMenu;
        private TabNameType tabName;
        private ContextMenuManager toolMenus;

        private Dictionary<string, BindingList<T>> tabDataDictionary;
        private CelEditEndProcess<T> cellEndEditProcess;
        private Dictionary<string, List<string>> _ordermap;

        private string _strAddTabBoxTitle = "New";

        private List<string> toolMenuName = new List<string>
        {
            "Add Tab",
            "Rename Tab",
            "Delete Tab"
        };

        private Dictionary<string, tableParam> _tableSetting;
        private Dictionary<string, List<string>> _comboboxdata = new Dictionary<string, List<string>>();

        /// <summary>
        /// The table name.: TabAttr テーブル名: TabAttr
        /// </summary>
        public Dictionary<string, tableParam> TabAttrTable = new Dictionary<string, tableParam>
        {
            { "TabName", new tableParam { KeyName = "TabName", TitleName = "Name", Width = 11, ComboxStringName = "", ReadOnly = false } },
            { "ColumnCont", new tableParam { KeyName = "ColumnCont", TitleName = "Item Count", Width = 8, ComboxStringName = "", ReadOnly = false } },
        };

        /// <summary>
        /// The table name.: TabNameType テーブル名: TabNameType
        /// </summary>
        public Dictionary<string, tableParam> TabNameTypeTable = new Dictionary<string, tableParam>
        {
            { "TabName", new tableParam { KeyName = "TabName", TitleName = "Name", Width = 11, ComboxStringName = "", ReadOnly = false } },
        };

        /// <summary>
        /// Property Name - userMethodView.
        /// </summary>
        public UserCellEndEditMethod<T> userMethodView { get; set; } = null;

        /// <summary>
        /// Property Name - userClipMethodView.
        /// </summary>
        public UserClipMethod userClipMethodView { get; set; } = null;

        /// <summary>
        /// Property Name - userMouseUpUpEvent.
        /// </summary>
        public MouseUpEvent userMouseUpUpEvent { get; set; } = null;

        /// <summary>
        /// Property Name - enableDoubleClickEvent.
        /// </summary>
        public bool enableDoubleClickEvent { get; set; } = false;

        /// <summary>
        /// Property Name - enableAddLineInputBox.
        /// </summary>
        public bool enableAddLineInputBox { get; set; } = true;

        /// <summary>
        /// Property Name - Folder.
        /// </summary>
        public string Folder { get; set; } = "Tabs";

        /// <summary>
        /// Property Name - enableAddLine.
        /// </summary>
        public bool enableAddLine { get; set; } = true;

        /// <summary>
        /// Property Name - TitleName.
        /// </summary>
        public string TitleName { get; set; } = "DefaultTabControlKey";

        /// <summary>
        /// Property Name - ComboBoxData.
        /// </summary>
        public Dictionary<string, List<string>> ComboBoxData
        {
            get => _comboboxdata;
            set
            {
                _comboboxdata = value;
                SetComboBoxDataToAllTabs(_comboboxdata);
            }
        }

        /// <summary>
        /// Property Name - TableSetting.
        /// </summary>
        public Dictionary<string, tableParam> TableSetting
        {
            set => _tableSetting = value;
        }

        /// <summary>
        /// Property Name - ToolMenuList.
        /// </summary>
        public List<string> ToolMenuList
        {
            get => toolMenuName;
            set => toolMenuName = value;
        }

        /// <summary>
        /// Property Name - OrderMap.
        /// </summary>
        public Dictionary<string, List<string>> OrderMap
        {
            get => _ordermap ?? new Dictionary<string, List<string>>();
            set
            {
                _ordermap = value;
            }
        }

        /// <summary>
        /// Property Name - InputBoxTitle.
        /// </summary>
        public string InputBoxTitle
        {
            get => _strAddTabBoxTitle;
            set
            {
                _strAddTabBoxTitle = value;
                tabnameBox.TitleName = _strAddTabBoxTitle;
                renameMenu.TitleName = _strAddTabBoxTitle;
                toolMenuName[0] = $"Add {_strAddTabBoxTitle}";
                toolMenuName[1] = $"Rename {_strAddTabBoxTitle}";
                toolMenuName[2] = $"Delete {_strAddTabBoxTitle}";
                InitilizeContextMenuTab();
            }
        }

        /// <summary>
        /// Property Name - cellEndEdit.
        /// </summary>
        public CelEditEndProcess<T> cellEndEdit
        {
            set => cellEndEditProcess = value;
        }

        /// <summary>
        /// Property Name - CurrentTab.
        /// </summary>
        public string CurrentTab => tabControl.SelectedTab.Name;

        /// <summary>
        /// Property Name - CurrentIndex.
        /// </summary>
        public int CurrentIndex
        {
            get
            {
                DataGridViewGeneric<T> temp = tabControl.SelectedTab.Tag as DataGridViewGeneric<T>;
                return temp.CurrentCell.RowIndex;
            }
        }

        /// <summary>
        /// Property Name - UserGroup.
        /// </summary>
        public GroupBox UserGroup
        {
            set
            {
                _userGroup = value; ;
                _userGroup.Controls.Add(tabControl);
            }
        }

        /// <summary>
        /// Property Name - isFileDataExist.
        /// </summary>
        public bool isFileDataExist => _viewFileManager.isExistFile();

        /// <summary>
        /// Property Name - SelectedTab.
        /// </summary>
        public TabPage SelectedTab => tabControl.SelectedTab;

        /// <summary>
        /// Property Name - viewData.
        /// </summary>
        public Dictionary<string, BindingList<T>> viewData
        {
            get
            {
                // 現在のすべてのタブからデータを収集
                foreach (TabPage tabPage in tabControl.TabPages)
                {
                    // 各タブに対応するデータを取得し、tabDataDictionary に保存
                    string tabName = tabPage.Name;
                    tabDataDictionary[tabName] = GetDataForTab(tabPage);
                }

                return tabDataDictionary;
            }
            set
            {
                if (!ValidateTabDataDictionary(value)) return;

                // 新しいデータを直接設定
                tabDataDictionary = value;
                // 最後に選択されたタブを取得 (無ければnull)
                TabPage lastTab = GetTabPageFromName(tabControl.TabPages.Cast<TabPage>().LastOrDefault()?.Name);

                // OrderMapに基づいてタブを並べ替えて再追加し、最後のタブを選択
                ClearAndAddTabsByOrderMap(lastTab);
                RebuildOrderMapFromTabDataDictionary();

                ResetTabs();
            }
        }

        /// <summary>
        /// Function Name :  ValidateTabDataDictionary.
        /// </summary>
        /// <param name="data">This data's Datatype is :
        ///     System.Collections.Generic.Dictionary string,
        ///     System.ComponentModel.BindingList T&gt;&gt;.</param>
        /// <returns>bool.</returns>
        private bool ValidateTabDataDictionary(Dictionary<string, BindingList<T>> data)
        {
            // データが null の場合は無効とする
            if (data == null)
            {
                MessageBox.Show("The data dictionary cannot be null.");
                return false;
            }

            // 各キーとリストが有効かを確認
            foreach (var entry in data)
            {
                if (string.IsNullOrEmpty(entry.Key))
                {
                    MessageBox.Show("Tab name cannot be null or empty.");
                    return false;
                }

                if (entry.Value == null)
                {
                    MessageBox.Show($"The data for tab '{entry.Key}' is null.");
                    return false;
                }
            }

            // すべてのチェックに合格した場合、true を返す
            return true;
        }

        /// <summary>
        ///  Function Name :  GetTabPageFromName.
        /// </summary>
        /// <param name="tabName">This tabName's Datatype is : string.</param>
        /// <returns>System.Windows.Forms.TabPage.</returns>
        private TabPage GetTabPageFromName(string tabName)
        {
            if (string.IsNullOrEmpty(tabName))
                return null;

            return tabControl.TabPages.Cast<TabPage>().FirstOrDefault(tab => tab.Name == tabName);
        }

        /// <summary>
        /// Gets the data for tab.
        /// </summary>
        /// <param name="tabPage">The tab page.</param>
        /// <returns></returns>
        private BindingList<T> GetDataForTab(TabPage tabPage)
        {
            if (tabPage.Tag is DataGridViewGeneric<T> dataGridView)
            {
                // タブに対応する DataGridView のデータを取得
                return dataGridView.viewData;
            }

            return new BindingList<T>();  // タブが見つからない場合やデータがない場合は空のリストを返す
        }

        /// <summary>
        ///      Property Name - AddDataSelectedTab.
        /// </summary>
        public T AddDataSelectedTab
        {
            set
            {
                TabPage selectedTabPage = tabControl.SelectedTab;
                if (selectedTabPage.Tag is DataGridViewGeneric<T> dataGridView)
                {
                    dataGridView.AddData = value;
                }
                else
                {
                    LogcatTraceHelper.Instance.LogError("Info", "Tab Page has no dataGridView {CurrentTab}");
                }
            }
        }

        /// <summary>
        ///      Property Name - UserFileManager.
        /// </summary>
        public multiFileManager<T> UserFileManager
        {
            get => _viewFileManager;
            set
            {
                _viewFileManager = value;
                if (_viewFileManager != null)
                {
                    CheckAndSetFolder();
                }
            }
        }

        /// <summary>
        ///  Function Name :  CheckAndSetFolder.
        /// </summary>
        /// <returns>void.</returns>
        private void CheckAndSetFolder()
        {
            if (string.IsNullOrEmpty(_viewFileManager.Folder))
            {
                if (!string.IsNullOrEmpty(Folder))
                {
                    _viewFileManager.Folder = Folder;
                }
                else
                {
                    LogcatTraceHelper.Instance.LogError("Warning", "Both UserFileManager's Folder and TabControlManager's Folder are not set.");
                }
            }
            else
            {
                Folder = _viewFileManager.Folder;
            }
        }

        /// <summary>
        ///  Function Name :  FileLoad.
        /// </summary>
        /// <returns>void.</returns>
        public void FileLoad()
        {
            InitilizeContextMenuTab();
            ResetTabs();

            if (_viewFileManager == null)
            {
                _viewFileManager = new multiFileManager<T>();
            }
            _viewFileManager.Folder = Folder;
            InitializeTabsFromFiles();
        }

        /// <summary>
        ///  Function Name :  TabControlManager.
        /// </summary>
        /// <returns>void.</returns>
        public TabControlManager()
        {
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill, // GroupBoxのサイズに合わせる
            };
            tabDataDictionary = new Dictionary<string, BindingList<T>>();
            // _viewFileManager = new multiFileManager<T>();
            tabnameBox = new InputBox<TabAttr>() { TitleName = _strAddTabBoxTitle, TextBoxWidthInChars = 15, TableSetting = TabAttrTable };
            renameMenu = new InputBox<TabNameType>() { TitleName = _strAddTabBoxTitle, TextBoxWidthInChars = 15, TableSetting = TabNameTypeTable };
            tabName = new TabNameType();
            InitilizeContextMenuTab();
        }

        /// <summary>
        ///  Function Name :  ResetTabs.
        /// </summary>
        /// <returns>void.</returns>
        private void ResetTabs()
        {
            InitilizeContextMenuTab();
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += new DrawItemEventHandler(tabControl_DrawItem);
            tabControl.MouseUp += new System.Windows.Forms.MouseEventHandler(TabMouseUpEvent);
        }

        /// <summary>
        ///  Function Name :  InitilizeContextMenuTab.
        /// </summary>
        /// <returns>void.</returns>
        private void InitilizeContextMenuTab()
        {
            toolMenus = new ContextMenuManager();
            List<MenuItemDefinition> tabMenus = new List<MenuItemDefinition>
            {
                new MenuItemDefinition(toolMenuName[0],ContextMenuAdd),
                new MenuItemDefinition(toolMenuName[1],ContextMenuRenameTab),
                new MenuItemDefinition(toolMenuName[2],ContextMenuRemove)
            };
            toolMenus.AddMenuItems("TAB_MENU", tabMenus);
        }

        /// <summary>
        ///  Function Name :  ContextMenuAdd.
        /// </summary>
        /// <param name="sender">This sender's Datatype is : object.</param>
        /// <param name="e">This e's Datatype is : System.EventArgs.</param>
        /// <returns>void.</returns>
        private void ContextMenuAdd(object sender, EventArgs e)
        {
            int tabCount = tabControl.TabCount + 1;
            AddTabMenu($"{_strAddTabBoxTitle.Replace(" ", string.Empty)}{tabCount.ToString()}");
        }

        /// <summary>
        ///  Function Name :  ContextMenuRemove.
        /// </summary>
        /// <param name="sender">This sender's Datatype is : object.</param>
        /// <param name="e">This e's Datatype is : System.EventArgs.</param>
        /// <returns>void.</returns>
        private void ContextMenuRemove(object sender, EventArgs e)
        {
            string temp = SelectedTab.Text;
            if (tabControl.TabCount > 1)
            {
                if (DialogResult.Yes == MessageBox.Show("Delete tab? Confirm?", "Confirmation", MessageBoxButtons.YesNo))
                {
                	RemoveTab(temp);
            	}
            }
            else
            {
                MessageBox.Show("this tab is disable to delete");
            }
        }

        /// <summary>
        ///  Function Name :  ContextMenuRenameTab.
        /// </summary>
        /// <param name="sender">This sender's Datatype is : object.</param>
        /// <param name="e">This e's Datatype is : System.EventArgs.</param>
        /// <returns>void.</returns>
        private void ContextMenuRenameTab(object sender, EventArgs e)
        {
            tabName.TabName = SelectedTab.Name;
            string old_name = SelectedTab.Name;
            if (renameMenu.ShowDialogValues(tabName) == DialogResult.OK)
            {
                tabName = renameMenu.Result;
                RenameSelectedTab(tabName.TabName);
            }
        }

        /// <summary>
        ///  Function Name :  TabMouseUpEvent.
        /// </summary>
        /// <param name="sender">This sender's Datatype is : object.</param>
        /// <param name="e">This e's Datatype is :
        ///     System.Windows.Forms.MouseEventArgs.</param>
        /// <returns>void.</returns>
        private void TabMouseUpEvent(object sender, MouseEventArgs e)
        {
            if (userMouseUpUpEvent != null)
            {
                userMouseUpUpEvent(sender, e);
            }
            else
            {
                if (e.Button == MouseButtons.Right) // 右クリックの場合
                {
                    // タブ部分がクリックされたかどうか確認
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
        ///  Function Name :  tabControl_DrawItem.
        /// </summary>
        /// <param name="sender">This sender's Datatype is : object.</param>
        /// <param name="e">This e's Datatype is :
        ///     System.Windows.Forms.DrawItemEventArgs.</param>
        /// <returns>void.</returns>
        private void tabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabPage tabPage = tabControl.TabPages[e.Index];
            Rectangle tabRect = tabControl.GetTabRect(e.Index);

            if (e.Index == tabControl.SelectedIndex)
            {
                // 選択中
                e.Graphics.FillRectangle(Brushes.LightBlue, tabRect);
                Font tabFont = new Font(tabControl.Font, FontStyle.Regular);
                // テキストを描画 (テキストがはみ出さないようにする)
                TextRenderer.DrawText(e.Graphics, tabControl.TabPages[e.Index].Text, tabFont, tabRect, tabControl.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            else
            {
                // 非選択
                e.Graphics.FillRectangle(Brushes.LightGray, tabRect);
                // テキスト描画
                TextRenderer.DrawText(e.Graphics, tabControl.TabPages[e.Index].Text, tabControl.Font, tabRect, tabControl.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        /// <summary>
        ///  Function Name :  AddDataSpecifiedTab.
        /// </summary>
        /// <param name="tabPageName">This tabPageName's Datatype is : string.
        ///     </param>
        /// <param name="value">This value's Datatype is : T.</param>
        /// <returns>void.</returns>
        public void AddDataSpecifiedTab(string tabPageName, T value)
        {
            TabPage tabPage = tabControl.TabPages[tabPageName];

            if (tabPage != null)
            {
                if (tabPage.Tag is DataGridViewGeneric<T> dataGridView)
                {
                    dataGridView.AddData = value;
                }
                else
                {
                    LogcatTraceHelper.Instance.LogError("Info", "Tab Page has no dataGridView");
                }
            }
            else
            {
                BindingList<T> values = new BindingList<T>
                {
                    value
                };
                AddTab(tabPageName, values);
                _viewFileManager.FileData = ConvertBindingDictinary(viewData);
            }
        }

        /// <summary>
        /// Converts the binding dictinary.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        private Dictionary<string, List<T>> ConvertBindingDictinary(Dictionary<string, BindingList<T>> data)
        {
            Dictionary<string, List<T>> ret = new Dictionary<string, List<T>>();
            foreach (KeyValuePair<string, BindingList<T>> entry in data)
            {
                ret.Add(entry.Key, new List<T>(entry.Value));
            }
            return ret;
        }

        /// <summary>
        ///  Function Name :  GetSymbolsStartingWith.
        /// 前方一致検索でコンボボックス用のシンボルリストを生成するメソッド
        /// </summary>
        /// <param name="prefix">This prefix's Datatype is : string.</param>
        /// <param name="tagname">This tagname's Datatype is : string.</param>
        /// <returns>System.Collections.Generic.List string>.</returns>
        public List<string> GetSymbolsStartingWith(string prefix, string tagname)
        {
            List<string> symbols = new List<string>();
            try
            {
                foreach (KeyValuePair<string, BindingList<T>> kvp in viewData)
                {
                    symbols.AddRange(kvp.Value
                        .Where(item => GetSymbol(item, tagname).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        .Select(item => GetSymbol(item, tagname))
                        .Distinct());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return symbols.Distinct().ToList();
        }

        /// <summary>
        ///  Function Name :  MoveToSymbol.
        /// </summary>
        /// <param name="symbol">This symbol's Datatype is : string.</param>
        /// <param name="tagname">This tagname's Datatype is : string.</param>
        /// <returns>void.</returns>
        public void MoveToSymbol(string symbol, string tagname)
        {
            string tabname = string.Empty;
            int index = GetTabNameAndRowIndexBySymbol(symbol, tagname, ref tabname);
            SelectTabAndSetCursor(tabname, index);
        }

        /// <summary>
        ///  Function Name :  GetTabNameAndRowIndexBySymbol.
        /// シンボルからタブ名と行番号を取得するメソッド
        /// </summary>
        /// <param name="symbol">This symbol's Datatype is : string.</param>
        /// <param name="tagname">This tagname's Datatype is : string.</param>
        /// <param name="tabname">This tabname's Datatype is : string.</param>
        /// <returns>int.</returns>
        private int GetTabNameAndRowIndexBySymbol(string symbol, string tagname, ref string tabname)
        {
            foreach (KeyValuePair<string, BindingList<T>> kvp in viewData)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    if (GetSymbol(kvp.Value[i], tagname).Equals(symbol, StringComparison.OrdinalIgnoreCase))
                    {
                        tabname = kvp.Key;
                        return (i);
                    }
                }
            }
            tabname = null;
            return (-1); // 見つからない場合
        }

        /// <summary>
        ///  Function Name :  SelectTabAndSetCursor.
        /// </summary>
        /// <param name="tabName">This tabName's Datatype is : string.</param>
        /// <param name="index">This index's Datatype is : int.</param>
        /// <returns>void.</returns>
        public void SelectTabAndSetCursor(string tabName, int index)
        {
            TabPage tabPage = tabControl.TabPages.Cast<TabPage>().FirstOrDefault(page => page.Text == tabName);
            if (tabPage != null)
            {
                tabControl.SelectedTab = tabPage;

                DataGridViewGeneric<T> dataGridView = tabPage.Tag as DataGridViewGeneric<T>;
                if (dataGridView != null && index >= 0 && index < dataGridView.Rows.Count)
                {
                    dataGridView.ClearSelection();
                    dataGridView.Rows[index].Selected = true;
                    dataGridView.CurrentCell = dataGridView.Rows[index].Cells[0]; // カーソルを最初のセルに置く
                }
            }
        }

        /// <summary>
        /// Sets the ComboBox data to all tabs.
        /// </summary>
        /// <param name="comboBoxData">The combo box data.</param>
        private void SetComboBoxDataToAllTabs(Dictionary<string, List<string>> comboBoxData)
        {
            foreach (TabPage tabPage in tabControl.TabPages)
            {
                DataGridViewGeneric<T> dataGridView = tabPage.Controls.OfType<DataGridViewGeneric<T>>().FirstOrDefault();
                if (dataGridView != null)
                {
                    dataGridView.ComboBoxData = comboBoxData;
                }
            }
        }

        /// <summary>
        ///  Function Name :  GetSymbol.
        /// </summary>
        /// <param name="item">This item's Datatype is : T.</param>
        /// <param name="tagname">This tagname's Datatype is : string.</param>
        /// <returns>string.</returns>
        private string GetSymbol(T item, string tagname)
        {
            System.Reflection.PropertyInfo property = typeof(T).GetProperty(tagname);
            if (property != null)
            {
                return property.GetValue(item)?.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        ///  Function Name :  InitializeTabsFromFiles.
        /// </summary>
        /// <returns>void.</returns>
        private void InitializeTabsFromFiles()
        {
            if (_viewFileManager == null)
            {
                return; // ViewFileManagerが設定されていない場合は終了
            }
            tabControl.TabPages.Clear();

            // _viewFileManager のデータをフォルダからロード
            _viewFileManager.Folder = Folder;
            _viewFileManager.LoadAllData();
            tabDataDictionary = DeepCopy(ConvertListDictinary(_viewFileManager.FileData));
            // tabDataDictionary が空の場合に "anonymous" キーを追加
            if (tabDataDictionary.Count == 0)
            {
                tabDataDictionary["anonymous"] = new BindingList<T>();
            }

            // viewData に tabDataDictionary を代入
            viewData = tabDataDictionary;
        }

        /// <summary>
        ///  Function Name :  AddTab.
        /// </summary>
        /// <param name="tabName">This tabName's Datatype is : string.</param>
        /// <param name="data">This data's Datatype is :
        ///     System.ComponentModel.BindingList T>.</param>
        /// <returns>void.</returns>
        private void AddTab(string tabName, BindingList<T> data = null)
        {
            // タブの追加
            CreateAndAddTabPage(tabName, data);

            // tabDataDictionary にデータを追加
            if (data != null)
            {
                tabDataDictionary[tabName] = data;
            }
            else
            {
                tabDataDictionary[tabName] = new BindingList<T>();
            }

            // OrderMapをリフレッシュ
            RefreshOrderMap();
        }

        /// <summary>
        ///  Function Name :  CreateAndAddTabPage.
        /// </summary>
        /// <param name="tabName">This tabName's Datatype is : string.</param>
        /// <param name="data">This data's Datatype is :
        ///     System.ComponentModel.BindingList T>.</param>
        /// <returns>void.</returns>
        private void CreateAndAddTabPage(string tabName, BindingList<T> data)
        {
            DataGridViewGeneric<T> dataGridView = new DataGridViewGeneric<T>()
            {
                enableDoubleClickEvent = enableDoubleClickEvent,
                enableAddLineInputBox = enableAddLineInputBox,
                userCellEditEndMethod = userMethodView,
                userClipMethod = userClipMethodView,
                enableAddLine = enableAddLine,
                TableSetting = _tableSetting,
                ComboBoxData = ComboBoxData
            };
            dataGridView.cellEditEnd = cellEndEditProcess;

            if (data != null && data.Count > 0)
            {
                dataGridView.viewData = data;
            }

            TabPage tabPage = new TabPage(tabName) { Tag = dataGridView, Name = tabName, Text = tabName };
            dataGridView.Dock = DockStyle.Fill;
            tabPage.Controls.Add(dataGridView);
            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage;
        }

        /// <summary>
        ///  Function Name :  AddTabMenu.
        /// </summary>
        /// <param name="tabName">This tabName's Datatype is : string.</param>
        /// <returns>void.</returns>
        public void AddTabMenu(string tabName)
        {
            int count, countMax;
            countMax = 3;
            BindingList<T> data = new BindingList<T>();

            if (tabnameBox.ShowDialogValues(new TabAttr() { TabName = tabName, ColumnCont = countMax }) == DialogResult.OK)
            {
                tabName = tabnameBox.Result.TabName;
                int temp = tabnameBox.Result.ColumnCont;
                if (temp > 0)
                {
                    countMax = temp;
                }
                for (count = 0; count < countMax; count++)
                {
                    data.Add(new T());
                }
                AddTab(tabName, data);
            }
        }

        /// <summary>
        ///  Function Name :  AddTabMenuInit.
        /// </summary>
        /// <param name="tabName">This tabName's Datatype is : string.</param>
        /// <returns>void.</returns>
        public void AddTabMenuInit(string tabName)
        {
            int count, countMax;
            countMax = 3;
            BindingList<T> data = new BindingList<T>();
            for (count = 0; count < countMax; count++)
            {
                data.Add(new T());
            }
            AddTab(tabName, data);
        }
        
        /// <summary>
        ///  Function Name :  RemoveTab.
        /// </summary>
        /// <param name="tabName">This tabName's Datatype is : string.</param>
        /// <returns>void.</returns>
        public void RemoveTab(string tabName)
        {
            TabPage tabPage = tabControl.TabPages.Cast<TabPage>().FirstOrDefault(page => page.Text == tabName);
            if (tabPage != null)
            {
                // タブページを削除
                tabControl.TabPages.Remove(tabPage);

                // tabDataDictionary からデータを削除
                tabDataDictionary.Remove(tabName);

                // ファイルマネージャーを更新
                if (_viewFileManager != null)
                {
                    _viewFileManager.FileData = ConvertBindingDictinary(tabDataDictionary);
                    _viewFileManager.RemoveData(tabName);
                    SaveTabsData();
                }

                // OrderMapをリフレッシュ
                RefreshOrderMap();
            }
        }

        /// <summary>
        ///  Function Name :  RemoveFiles.
        /// </summary>
        /// <param name="name">This name's Datatype is : string.</param>
        /// <returns>void.</returns>
        public void RemoveFiles(string name)
        {
            _viewFileManager.FileData.Remove(name);
        }

        /// <summary>
        ///  Function Name :  SaveTabsData.
        /// </summary>
        /// <returns>void.</returns>
        public void SaveTabsData()
        {
            if (_viewFileManager == null)
            {
                return; // ViewFileManagerが設定されていない場合は終了
            }

            _viewFileManager.Folder = Folder;
            _viewFileManager.FileData = ConvertBindingDictinary(viewData);
            _viewFileManager.SaveAllData();
        }

        /// <summary>
        ///  Function Name :  ChangeAndReloadDataFolder.
        /// </summary>
        /// <returns>void.</returns>
        public void ChangeAndReloadDataFolder()
        {
            if (_viewFileManager != null && _viewFileManager.SetFolderPathWithDialog())
            {
                _viewFileManager.Folder = Folder;
                _viewFileManager.LoadAllData();
                viewData = ConvertListDictinary(_viewFileManager.FileData);
                UpdateTabsWithData();
            }
        }

        /// <summary>
        ///  Function Name :  UpdateTabsWithData.
        /// </summary>
        /// <returns>void.</returns>
        private void UpdateTabsWithData()
        {
            if (_viewFileManager != null)
            {
                tabControl.TabPages.Clear();
                foreach (KeyValuePair<string, List<T>> pair in _viewFileManager.FileData)
                {
                    BindingList<T> data = new BindingList<T>(pair.Value);
                    AddTab(pair.Key, data);
                }
            }
        }

        /// <summary>
        ///  Function Name :  ChangeFolderAndSaveData.
        /// </summary>
        /// <returns>void.</returns>
        public void ChangeFolderAndSaveData()
        {
            if (_viewFileManager != null && _viewFileManager.SetFolderPathWithDialog())
            {
                SaveAllDataToNewLocation();
            }
        }

        /// <summary>
        ///  Function Name :  SaveAllDataToNewLocation.
        /// </summary>
        /// <returns>void.</returns>
        private void SaveAllDataToNewLocation()
        {
            if (_viewFileManager != null)
            {
                _viewFileManager.Folder = Folder;
                _viewFileManager.FileData = ConvertBindingDictinary(viewData);
                _viewFileManager.SaveAllData();
            }
        }

        /// <summary>
        ///  Function Name :  TabControl_SelectedIndexChanged.
        /// </summary>
        /// <param name="sender">This sender's Datatype is : object.</param>
        /// <param name="e">This e's Datatype is : System.EventArgs.</param>
        /// <returns>void.</returns>
        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null)
            {
                DataGridViewGeneric<T> dataGridView = tabControl.SelectedTab.Tag as DataGridViewGeneric<T>;
                dataGridView?.Refresh();
            }
        }

        /// <summary>
        ///  Function Name :  RenameSelectedTab.
        /// </summary>
        /// <param name="newTabName">This newTabName's Datatype is : string.
        ///     </param>
        /// <returns>void.</returns>
        public void RenameSelectedTab(string newTabName)
        {
            if (tabControl.SelectedTab == null || string.IsNullOrEmpty(newTabName))
            {
                return; // 選択されたタブがないか、新しい名前が無効な場合は終了
            }

            string oldTabName = tabControl.SelectedTab.Text;

            // 新しいタブ名が既に存在する場合、ユニークな名前を生成
            newTabName = GenerateUniqueTabName(newTabName);

            // タブページの名前を変更
            UpdateTabPageName(oldTabName, newTabName);

            // tabDataDictionary を更新
            UpdateTabDataDictionaryAfterRename(oldTabName, newTabName);

            // ファイルマネージャーのデータを更新
            UpdateFileManagerDataAfterRename(oldTabName, newTabName);

            // OrderMapをリフレッシュ
            RefreshOrderMap();
        }
        // Dictionary内容からOrderMapを再構築するメソッドの例
        private void RebuildOrderMapFromTabDataDictionary()
        {
            // uniqueKeyを生成
            string uniqueKey = GenerateUniqueKeyForTab();

            // tabDataDictionaryのキー順でタブを並べる
            // Dictionaryのキー順が保証されない場合は、ここでソートまたは別の並び順取得法を用意
            _ordermap[uniqueKey] = tabDataDictionary.Keys.ToList();
        }

        /// <summary>
        ///  Function Name :  ClearAndAddTabsByOrderMap.
        /// </summary>
        /// <param name="selectedTab">This selectedTab's Datatype is :
        ///     System.Windows.Forms.TabPage.</param>
        /// <returns>void.</returns>
        private void ClearAndAddTabsByOrderMap(TabPage selectedTab = null)
        {
            // 一旦すべてのタブをクリア
            tabControl.TabPages.Clear();

            // OrderMap に基づいてタブを追加
            if (_ordermap != null && _ordermap.Count > 0)
            {
                string uniqueKey = GenerateUniqueKeyForTab(); // タブコントロールに対する一意なキーを生成
                if (_ordermap.ContainsKey(uniqueKey))
                {
                    List<string> tabOrder = _ordermap[uniqueKey];

                    // OrderMap に基づいてタブを追加
                    foreach (string tabName in tabOrder)
                    {
                        // 既存のデータを使用してタブを追加
                        if (tabDataDictionary.ContainsKey(tabName))
                        {
                            CreateAndAddTabPage(tabName, tabDataDictionary[tabName]);
                        }
                    }
                }
            }

            // OrderMap に存在しないタブも追加
            var remainingTabs = tabDataDictionary.Keys
                .Where(tabName => !tabControl.TabPages.Cast<TabPage>().Any(tab => tab.Name == tabName))
                .ToList();

            foreach (var tabName in remainingTabs)
            {
                CreateAndAddTabPage(tabName, tabDataDictionary[tabName]);
            }

            // 指定されたタブを選択、または最初のタブを選択
            if (selectedTab != null && tabControl.TabPages.Contains(selectedTab))
            {
                tabControl.SelectedTab = selectedTab;
            }
            else if (tabControl.TabPages.Count > 0)
            {
                tabControl.SelectedTab = tabControl.TabPages[0]; // 最初のタブを選択
            }
        }


        /// <summary>
        ///  Function Name :  AddDefaultTab.
        /// </summary>
        /// <returns>void.</returns>
        private void AddDefaultTab()
        {
            // デフォルトのタブページを追加する処理
            TabPage defaultTab = new TabPage
            {
                Name = "DefaultTab",
                Text = "Default Tab"
            };
            tabControl.TabPages.Add(defaultTab);
        }

        /// <summary>
        ///  Function Name :  ResetTabsToDefaultOrder.
        /// </summary>
        /// <returns>void.</returns>
        private void ResetTabsToDefaultOrder()
        {
            // タブのデフォルトの順序で再追加
            var defaultTabs = tabDataDictionary.Keys.ToList();
            tabControl.TabPages.Clear();
            foreach (string tabName in defaultTabs)
            {
                TabPage tabPage = GetTabPageFromName(tabName);
                if (tabPage != null)
                {
                    tabControl.TabPages.Add(tabPage);
                }
            }

            // 最初のタブを選択
            if (tabControl.TabPages.Count > 0)
            {
                tabControl.SelectedTab = tabControl.TabPages[0];
            }
        }

        /// <summary>
        ///  Function Name :  GenerateUniqueTabName.
        /// </summary>
        /// <param name="baseName">This baseName's Datatype is : string.</param>
        /// <returns>string.</returns>
        private string GenerateUniqueTabName(string baseName)
        {
            string uniqueName = baseName;
            while (tabDataDictionary.ContainsKey(uniqueName))
            {
                uniqueName += "_"; // 名前が衝突する場合は、アンダースコアを追加してユニークにする
            }
            return uniqueName;
        }

        /// <summary>
        ///  Function Name :  UpdateTabPageName.
        /// </summary>
        /// <param name="oldTabName">This oldTabName's Datatype is : string.
        ///     </param>
        /// <param name="newTabName">This newTabName's Datatype is : string.
        ///     </param>
        /// <returns>void.</returns>
        private void UpdateTabPageName(string oldTabName, string newTabName)
        {
            tabControl.SelectedTab.Text = newTabName;
            tabControl.SelectedTab.Name = newTabName;
        }

        /// <summary>
        ///  Function Name :  UpdateTabDataDictionaryAfterRename.
        /// </summary>
        /// <param name="oldTabName">This oldTabName's Datatype is : string.
        ///     </param>
        /// <param name="newTabName">This newTabName's Datatype is : string.
        ///     </param>
        /// <returns>void.</returns>
        private void UpdateTabDataDictionaryAfterRename(string oldTabName, string newTabName)
        {
            if (tabDataDictionary.ContainsKey(oldTabName))
            {
                BindingList<T> data = tabDataDictionary[oldTabName];
                tabDataDictionary.Remove(oldTabName);
                tabDataDictionary[newTabName] = data;
            }
        }

        /// <summary>
        ///  Function Name :  UpdateFileManagerDataAfterRename.
        /// </summary>
        /// <param name="oldTabName">This oldTabName's Datatype is : string.
        ///     </param>
        /// <param name="newTabName">This newTabName's Datatype is : string.
        ///     </param>
        /// <returns>void.</returns>
        private void UpdateFileManagerDataAfterRename(string oldTabName, string newTabName)
        {
            if (_viewFileManager == null)
            {
                return; // multiFileManager が設定されていない場合は終了
            }

            _viewFileManager.Folder = Folder;
            _viewFileManager.FileData = ConvertBindingDictinary(tabDataDictionary);
            _viewFileManager.RenameData(oldTabName, newTabName);
        }

        /// <summary>
        ///  Function Name :  UpdateTabDataDictionary.
        /// </summary>
        /// <returns>void.</returns>
        private void UpdateTabDataDictionary()
        {
            // tabDataDictionary が null の場合、新しい辞書を初期化
            if (tabDataDictionary == null)
            {
                tabDataDictionary = new Dictionary<string, BindingList<T>>();
            }

            // 現在のタブ情報を基に tabDataDictionary を更新
            foreach (TabPage tabPage in tabControl.TabPages)
            {
                string tabName = tabPage.Text;
                DataGridViewGeneric<T> dataGridView = tabPage.Tag as DataGridViewGeneric<T>;
                if (dataGridView != null)
                {
                    tabDataDictionary[tabName] = dataGridView.viewData;
                }
            }
        }

        /// <summary>
        ///  Function Name :  RefreshOrderMap.
        /// </summary>
        /// <returns>void.</returns>
        private void RefreshOrderMap()
        {
            List<string> tabNames = tabControl.TabPages.Cast<TabPage>().Select(tab => tab.Name).ToList();
            string uniqueKey = GenerateUniqueKeyForTab();
            _ordermap[uniqueKey] = tabNames;
        }

        /// <summary>
        ///  Function Name :  ConvertListDictinary.
        /// </summary>
        /// <param name="data">This data's Datatype is :
        ///     System.Collections.Generic.Dictionary string,
        ///     System.Collections.Generic.List T>>.</param>
        /// <returns>System.Collections.Generic.Dictionary string,
        ///     System.ComponentModel.BindingList T>>.</returns>
        private Dictionary<string, BindingList<T>> ConvertListDictinary(Dictionary<string, List<T>> data)
        {
            Dictionary<string, BindingList<T>> ret = new Dictionary<string, BindingList<T>>();
            foreach (KeyValuePair<string, List<T>> entry in data)
            {
                ret.Add(entry.Key, new BindingList<T>(entry.Value));
            }
            return ret;
        }

        /// <summary>
        ///  Function Name :  GenerateUniqueKeyForTab.
        /// </summary>
        /// <returns>string.</returns>
        private string GenerateUniqueKeyForTab()
        {
            // TitleName プロパティを使用して一意のキーを生成
            if (!string.IsNullOrEmpty(TitleName))
            {
                return TitleName;
            }

            // TitleName がない場合のフォールバック
            return "DefaultTabControlKey";
        }

        /// <summary>
        ///  Function Name :  DeepCopy.
        /// </summary>
        /// <param name="original">This original's Datatype is : TData.</param>
        /// <returns>TData.</returns>
        private TData DeepCopy<TData>(TData original)
        {
            // オブジェクトをJSON文字列にシリアライズ
            string jsonString = JsonSerializer.Serialize(original);

            // JSON文字列を元にオブジェクトをデシリアライズ（ディープコピー）
            return JsonSerializer.Deserialize<TData>(jsonString);
        }
}
}