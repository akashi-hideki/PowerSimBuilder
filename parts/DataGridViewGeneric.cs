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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// delegate UserCellEndEditMethod
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input">The input.</param>
    /// <param name="output">The output.</param>
    /// <returns></returns>
    public delegate bool UserCellEndEditMethod<T>(T input, ref T output);

    /// <summary>
    /// delegate UserClipMethod
    /// </summary>
    /// <param name="select_column">The select column.</param>
    /// <param name="indata">The indata.</param>
    public delegate void UserClipMethod(int select_column, string indata);

    /// <summary>
    /// delegate CelEditEndProcess
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="tabname">The tabname.</param>
    /// <param name="row">The row.</param>
    /// <param name="col">The col.</param>
    /// <param name="input">The input.</param>
    public delegate void CelEditEndProcess<T>(string tabname, int row, int col, T input);

    /// <summary>
    /// dataGridViewGeneric
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Windows.Forms.DataGridView"/>
    public class DataGridViewGeneric<T> : DataGridView where T : new()
    {
        /// <summary>
        /// The debug list
        /// </summary>
        public List<T> debugList;

        private int _charCount = 10; // デフォルトの文字数
        private Font _currentFont = new Font("Microsoft Sans Serif", 8.25F);
        private readonly ContextMenuManager toolMenu;
        private readonly List<MenuItemDefinition> menuItemList;
        private readonly InputBox<T> add_itembox;
        private bool preventRowAddition = false;
        private GroupBox _userGroup;
        private Dictionary<string, tableParam> _tableSetting;
        private static ComboBoxManager _comboBoxManager;

        // 編集禁止の行IDを保持するセット
        private readonly HashSet<string> readOnlyRowIds = new HashSet<string>();

        private readonly Dictionary<Type, Func<PropertyInfo, tableParam, DataGridViewColumn>> columnCreationMap;
        private bool isUpdating = false;
        private object originalValue;

        /// <summary>
        /// Gets a value indicating whether this instance is manual input.
        /// </summary>
        /// <value><c> true </c> if this instance is manual input; otherwise,
        ///     <c> false </c>.</value>
        public bool isManualInput { get; private set; } = false;

        private readonly List<string> _default_menu = new List<string>()
        {
            "Insert Row",
            "Delete Row"
        };

        private readonly Dictionary<string, tableParam> defaultClassViewParam = new Dictionary<string, tableParam>
        {
            { "FunctionType", new tableParam { KeyName = "FunctionType", TitleName = "TypeName", Width = 15, ComboxStringName = "", ReadOnly = false } },
            { "ReturnType", new tableParam { KeyName = "ReturnType", TitleName = "ReturnValue", Width = 10, ComboxStringName = "", ReadOnly = false } },
            { "FunctionName", new tableParam { KeyName = "FunctionName", TitleName = "Name", Width = 30, ComboxStringName = "", ReadOnly = false } },
            { "Arg1Type", new tableParam { KeyName = "Arg1Type", TitleName = "Arg(1)", Width = 10, ComboxStringName = "", ReadOnly = false } },
            { "Arg2Type", new tableParam { KeyName = "Arg2Type", TitleName = "Arg(2)", Width = 10, ComboxStringName = "", ReadOnly = false } },
            { "Arg3Type", new tableParam { KeyName = "Arg3Type", TitleName = "Arg(3)", Width = 10, ComboxStringName = "", ReadOnly = false } },
            { "Arg4Type", new tableParam { KeyName = "Arg4Type", TitleName = "Arg(4)", Width = 10, ComboxStringName = "", ReadOnly = false } },
            { "ArgCount", new tableParam { KeyName = "ArgCount", TitleName = "Number of Args", Width = 15, ComboxStringName = "", ReadOnly = false } },
        };

        /// <summary>
        /// Gets or sets the cell edit end process.
        /// </summary>
        /// <value>The cell edit end process.</value>
        public CelEditEndProcess<T> cellEditEndProcess { get; set; } = null;

        /// <summary>
        /// Gets or sets the user cell edit end method.
        /// </summary>
        /// <value>The user cell edit end method.</value>
        public UserCellEndEditMethod<T> userCellEditEndMethod { get; set; } = null;

        /// <summary>
        /// Gets or sets the user clip method.
        /// </summary>
        /// <value>The user clip method.</value>
        public UserClipMethod userClipMethod { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether [enable double click event].
        /// </summary>
        /// <value><c> true </c> if [enable double click event]; otherwise, <c>
        ///     false </c>.</value>
        public bool enableDoubleClickEvent { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether [enable add line input box].
        /// </summary>
        /// <value><c> true </c> if [enable add line input box]; otherwise, <c>
        ///     false </c>.</value>
        public bool enableAddLineInputBox { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether [enable add line].
        /// </summary>
        /// <value><c> true </c> if [enable add line]; otherwise, <c> false
        ///     </c>.</value>
        public bool enableAddLine { get; set; } = true;

        /// <summary>
        /// rowIdColumnNames
        /// </summary>
        // 行IDを保持する列名を設定するプロパティ
        public List<string> RowIdColumnNames { get; set; } = new List<string>();

        /// <summary>
        /// Sets the cell edit end.
        /// </summary>
        /// <value>The cell edit end.</value>
        public CelEditEndProcess<T> cellEditEnd
        {
            set
            {
                if (value != null)
                {
                    cellEditEndProcess = value;
                }
            }
        }

        /// <summary>
        /// ; Gets or sets the data source list.
        /// </summary>
        /// <value>The data source list.</value>
        public BindingList<T> viewData
        {
            get
            {
                if (DataSource is BindingList<T> bindingList)
                {
                    return bindingList;
                }
                return new BindingList<T>(); // DataSource が null もしくは正しい型でない場合、空のリストを返す
            }
            set
            {
                if (value != null)
                {
                    BindingList<T> temp = (value);
                    temp.ListChanged += dataGridView1_OnListChanged;
                    DataSource = temp;
                }
                else
                {
                    DataSource = new BindingList<T>(); // データがnullの場合に空のリストをセット
                }
            }
        }

        /// <summary>
        /// ListDataの追加
        /// </summary>
        public T AddData
        {
            set
            {
                BindingList<T> temp = (BindingList<T>)DataSource;
                temp.Add(value);
                RefreshData(this, temp);
            }
        }

        /// <summary>
        /// Gets or sets the character count.
        /// </summary>
        /// <value>The character count.</value>
        public int CharCount
        {
            get => _charCount;
            set
            {
                _charCount = value;
                InitializeColumns();
            }
        }

        /// <summary>
        /// Gets or sets the data grid view font.
        /// </summary>
        /// <value>The data grid view font.</value>
        public Font DataGridViewFont
        {
            get => _currentFont;
            set
            {
                _currentFont = value;
                DefaultCellStyle.Font = _currentFont;
                InitializeColumns();
            }
        }

        ///<summary>
        ///Table Setting
        ///</summary>
        // Property to get or set the table settings
        public Dictionary<string, tableParam> TableSetting
        {
            get
            {
                if (_tableSetting == null || _tableSetting.Count == 0)
                {
                    _tableSetting = InitializeDefaultTableSettings();
                }
                return _tableSetting;
            }
            set
            {
                _tableSetting = value ?? InitializeDefaultTableSettings();
                InitializeColumns();
            }
        }

        /// <summary>
        /// グループボックスに登録
        /// </summary>
        public GroupBox UserGroup
        {
            set
            {
                _userGroup = value; ;
                _userGroup.Controls.Add(this);
            }
        }

        /// <summary>
        /// Gets or sets the ComboBox data.
        /// </summary>
        /// <value>The ComboBox data.</value>

        public Dictionary<string, List<string>> ComboBoxData
        {
            get => _comboBoxManager.ComboBoxData;
            set
            {
                _comboBoxManager.ComboBoxData = value;

                // 1. データソースを一時的に保存
                var currentDataSource = this.DataSource;

                // 2. 編集モードを終了
                if (this.IsCurrentCellInEditMode)
                {
                    this.EndEdit();
                }

                // 3. 列を再初期化
                InitializeColumns();

                // 4. データソースを再設定
                this.DataSource = currentDataSource;

                // 5. セルの値を更新
                UpdateCellValues();

                // 6. 再描画
                this.Refresh();
            }
        }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="DataGridViewGeneric{T}"/> class.
        /// </summary>
        public DataGridViewGeneric()
        {
            base.AutoGenerateColumns = true;
            _comboBoxManager = new ComboBoxManager();
            columnCreationMap = new Dictionary<Type, Func<PropertyInfo, tableParam, DataGridViewColumn>>
            {
                { typeof(bool), CreateCheckBoxColumn },
                { typeof(Enum), CreateComboBoxColumn },
                { typeof(long), CreateHexadecimalColumn },
                { typeof(string), CreateTextBoxColumn },
                { typeof(int), CreateTextBoxColumn },
                { typeof(float), CreateTextBoxColumn },
                { typeof(double), CreateTextBoxColumn },
                { typeof(decimal), CreateTextBoxColumn }
            };
            // Register EventHandler
            DoubleClick += DataGridView_DoubleClick;
            CellValueChanged += new DataGridViewCellEventHandler(dataGridView1_CellValueChanged);
            CurrentCellDirtyStateChanged += new EventHandler(dataGridView1_CurrentCellDirtyStateChanged);
            CellBeginEdit += new DataGridViewCellCancelEventHandler(dataGridView1_CellBeginEdit);
            CellEndEdit += new DataGridViewCellEventHandler(dataGridView1_CellEndEdit);
            KeyDown += DataGridView_KeyDown;
            CellClick += DataGridView_CellClick;
            DataError += DataGridView1_DataError;
            CellParsing += DataGridView1_CellParsing;
            MouseUp += DataGridViewGeneric_MouseUp;
            EditingControlShowing += DataGridViewGeneric_EditingControlShowing;
            DataBindingComplete += DataGridView_DataBindingComplete;


            // CellLeave += DataGridView_CellLeave;

            // CellValidated += dataGridView1_RowValidate;

            ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable;
            EditMode = DataGridViewEditMode.EditOnKeystroke;
            SelectionMode = DataGridViewSelectionMode.CellSelect;
            // attributes
            AllowUserToAddRows = false;
            Dock = DockStyle.Fill;
            AllowDrop = false;
            MultiSelect = true;
            ScrollBars = ScrollBars.Both;
            menuItemList = new List<MenuItemDefinition>
            {
                new MenuItemDefinition(_default_menu[0],InsertItem_Click),
                new MenuItemDefinition(_default_menu[1],DeleteItem_Click)
            };
            toolMenu = new ContextMenuManager();
            toolMenu.AddMenuItems("TABLE_MENU", menuItemList);
            add_itembox = new InputBox<T>();
            _tableSetting = defaultClassViewParam;
            InitializeColumns(); // Initilize Column
            _comboBoxManager.RegisterComboBoxEvents(this);
            EnableDoubleBuffering(); // Enable double buffering
            // データソースの初期化と設定
            viewData = new BindingList<T> { new T(), new T(), new T() }; // 初期データを設定
            isManualInput = false;
        }
        // ダブルバッファリング有効化メソッド
        /// <summary>
        /// EnableDoubleBuffering
        /// </summary>
        private void EnableDoubleBuffering()
        {
            typeof(DataGridView).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)
                .SetValue(this, true, null);
        }
        /// <summary>
        /// Initializes the columns.
        /// </summary>
        /// <returns></returns>
        private void InitializeColumns()
        {
            // Clear existing columns before initializing
            base.Columns.Clear();

            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                // Use the tableParam from TableSetting if it exists, otherwise use default settings
                tableParam param = TableSetting.ContainsKey(prop.Name)
                    ? TableSetting[prop.Name]
                    : new tableParam { KeyName = prop.Name, TitleName = prop.Name, Width = _charCount };

                Type propType = prop.PropertyType.IsEnum ? typeof(Enum) : prop.PropertyType;

                // Use the column creation map to create the appropriate column
                DataGridViewColumn column = columnCreationMap.ContainsKey(propType)
                    ? columnCreationMap[propType](prop, param)
                    : CreateTextBoxColumn(prop, param);

                // Set the width of the column
                SetColumnWidth(column, param);

                // Set column read-only if irrevocable is true
                column.ReadOnly = param.ReadOnly;
                // Set Header style
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                column.HeaderCell.Style.Font = new Font(Font, FontStyle.Bold);
                base.Columns.Add(column);
            }
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            EnableHeadersVisualStyles = false;

            // Set read-only rows if applicable
            SetReadOnlyRows();
        }

        /// <summary>
        /// Sets the width of the column based on the parameter or default
        /// measurement.
        /// </summary>
        private void SetColumnWidth(DataGridViewColumn column, tableParam param)
        {
            using (Font fixedFont = new Font("Consolas", 10)) // 等幅フォント "Consolas" を使用
            {
                int width = TextRenderer.MeasureText(new string('W', param.Width), fixedFont).Width;
                column.Width = width;
            }
        }

        /// <summary>
        /// Creates a DataGridViewCheckBoxColumn.
        /// </summary>
        private static DataGridViewColumn CreateCheckBoxColumn(PropertyInfo prop, tableParam param)
        {
            return new DataGridViewCheckBoxColumn
            {
                DataPropertyName = prop.Name,
                HeaderText = param?.TitleName ?? prop.Name,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
        }

        /// <summary>
        /// Creates a DataGridViewComboBoxColumn for enum types.
        /// </summary>
        private static DataGridViewColumn CreateComboBoxColumn(PropertyInfo prop, tableParam param)
        {
            return new DataGridViewComboBoxColumn
            {
                DataPropertyName = prop.Name,
                HeaderText = param?.TitleName ?? prop.Name,
                DataSource = Enum.GetValues(prop.PropertyType),
                SortMode = DataGridViewColumnSortMode.NotSortable,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
        }

        /// <summary>
        /// Creates a DataGridViewTextBoxColumn with hexadecimal formatting for
        /// long types.
        /// </summary>
        private static DataGridViewColumn CreateHexadecimalColumn(PropertyInfo prop, tableParam param)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = prop.Name,
                HeaderText = param?.TitleName ?? prop.Name,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
            column.DefaultCellStyle.FormatProvider = new HexadecimalFormatProvider();
            column.DefaultCellStyle.Format = "X"; // Set format to hexadecimal
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }
        /// <summary>
        /// Handles the CellLeave event of the DataGridView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataGridViewCellEventArgs"/> instance
        ///     containing the event data.</param>
        private void DataGridView_CellLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (this.IsCurrentCellDirty)
            {
                string message = $"CellValueLevave event triggered - Row: {e.RowIndex}, Column: {e.ColumnIndex}";
                LogcatTraceHelper.Instance.LogError("Info", message);
                this.EndEdit();
            }
        }
        /// <summary>
        /// Handles the CellValidating event of the DataGridView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The
        ///     <see cref="DataGridViewCellValidatingEventArgs"/> instance
        ///     containing the event data.</param>
        private void DataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv.IsCurrentCellDirty)
            {
                string message = $"CellValidating event triggered - Row: {e.RowIndex}, Column: {e.ColumnIndex}";
                dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        /// <summary>
        /// Handles the EditingControlShowing event of the DataGridViewGeneric
        /// control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The
        ///     <see cref="DataGridViewEditingControlShowingEventArgs"/>
        ///     instance containing the event data.</param>
        private void DataGridViewGeneric_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv?.CurrentCell == null) return;

            if (e.Control is ComboBox comboBox)
            {
                // 既存のイベントハンドラを削除してから追加
                // コンボボックス時は文字列に入力であるとの認識でIME制御は不要  
                comboBox.SelectionChangeCommitted -= ComboBox_SelectionChangeCommitted;
                comboBox.SelectionChangeCommitted += ComboBox_SelectionChangeCommitted;

            }
            else
            {

                // 現在編集中のセルが属するカラム（OwningColumn）を取得
                DataGridViewColumn col = dgv.CurrentCell.OwningColumn;

                // 列の ValueType が string 以外の場合は IME を OFF にする
                // （数値や日付などの場合は IME を使用しない）
                // 但し元に戻すことはしない
                if (col.ValueType != typeof(string))
                {
                    e.Control.ImeMode = ImeMode.Off;
                }
                else
                {
                    // string の場合は IME を制御しない 
                    e.Control.ImeMode = ImeMode.NoControl;
                }
            }
        }

        /// <summary>
        /// Handles the SelectionChangeCommitted event of the ComboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the
        ///     event data.</param>
        private void ComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            // 編集を確定
            if (this.IsCurrentCellDirty)
            {
                this.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        /// <summary>
        /// Creates a DataGridViewTextBoxColumn.
        /// </summary>
        /// <param name="prop">The property.</param>
        /// <param name="param">The parameter.</param>
        /// <returns></returns>
#if false
        private static DataGridViewColumn CreateTextBoxColumn(PropertyInfo prop, tableParam param)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = prop.Name,
                HeaderText = param?.TitleName ?? prop.Name,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
            if (prop.PropertyType != typeof(string))
            {
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            return column;
        }
#else

        private DataGridViewColumn CreateTextBoxColumn(PropertyInfo prop, tableParam param)
        {
            if (_comboBoxManager.ContainsKey(prop.Name))
            {
                DataGridViewComboBoxColumn comboBoxColumn = new DataGridViewComboBoxColumn
                {
                    DataPropertyName = prop.Name,
                    HeaderText = param.TitleName ?? prop.Name,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                };

                _comboBoxManager.PopulateComboBox(comboBoxColumn, prop.Name);

                return comboBoxColumn;
            }
            else
            {
                DataGridViewTextBoxColumn textBoxColumn = new DataGridViewTextBoxColumn
                {
                    DataPropertyName = prop.Name,
                    HeaderText = param?.TitleName ?? prop.Name,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                };
                if (prop.PropertyType != typeof(string))
                {
                    textBoxColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                return textBoxColumn;
            }
        }

#endif

        /// <summary>
        /// Sets the read-only rows if applicable.
        /// </summary>
        /// <summary>
        /// Sets the read-only rows if applicable.
        /// </summary>
        private void SetReadOnlyRows()
        {
            foreach (DataGridViewRow row in Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    DataGridViewColumn column = Columns[cell.ColumnIndex];
                    string propName = column.DataPropertyName;
                    if (TableSetting.ContainsKey(propName) && (TableSetting[propName].ReadOnly))
                    {
                        cell.ReadOnly = true;
                    }
                }
            }
        }

        /// <summary>
        /// DataGridViewGeneric_MouseUp
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridViewGeneric_MouseUp(object sender, MouseEventArgs e)
        {
            if ((e.Button == MouseButtons.Right) && (true == enableAddLine))
            {
                HitTestInfo hitTestInfo = HitTest(e.X, e.Y);
                if (hitTestInfo.Type == DataGridViewHitTestType.Cell)
                {
                    CurrentCell = Rows[hitTestInfo.RowIndex].Cells[hitTestInfo.ColumnIndex];
                    toolMenu.GetMenu("TABLE_MENU").Show(this, e.Location);
                }
            }
        }

        /// <summary>
        /// Handles the OnListChanged event of the DataGridView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ListChangedEventArgs"/> instance
        ///     containing the event data.</param>
        private void dataGridView1_OnListChanged(object sender, ListChangedEventArgs e)
        {
            // コントロールのハンドルが作成されているか確認
            if (!this.IsHandleCreated)
            {
                // ハンドルが作成されていない場合、処理を行わずに終了
                return;
            }

            // UI スレッドで安全に処理を行うために BeginInvoke を使用
            this.BeginInvoke(new Action(() =>
            {
                if (!isManualInput)
                {
                    // 外部からの変更の場合、表示を更新
                    LogcatTraceHelper.Instance.LogError("Info", "External data change detected. Updating display...");
                    this.Refresh();  // データグリッドビューの表示を更新
                }
                else
                {
                    // 手動編集の場合は、表示更新の処理を無視
                    LogcatTraceHelper.Instance.LogError("Info", "Manual input change detected.");
                    isManualInput = false; // 手動入力の終了を記録
                }
            }));
        }

        /// <summary>
        /// InitializeDefaultTableSettings
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, tableParam> InitializeDefaultTableSettings()
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            Dictionary<string, tableParam> defaultSettings = new Dictionary<string, tableParam>();

            foreach (PropertyInfo prop in properties)
            {
                defaultSettings[prop.Name] = new tableParam
                {
                    KeyName = prop.Name,
                    TitleName = prop.Name,
                    Width = _charCount
                };
            }

            return defaultSettings;
        }

        /// <summary>
        /// cellparse
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridView1_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv == null)
            {
                return;
            }

            Type columnType = dgv.Columns[e.ColumnIndex].ValueType;

            if (string.IsNullOrWhiteSpace(e.Value?.ToString()))
            {
                e.Value = GetDefaultValue(columnType);
                e.ParsingApplied = true;
            }
        }

        /// <summary>
        /// Double click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridView_DoubleClick(object sender, EventArgs e)
        {
            BindingList<T> datalist = DataSource as BindingList<T>;

            if (CurrentCell != null)
            {
                int currentRowIndex = CurrentCell.RowIndex; // 現在選択中のセルの行インデックス
                if (true == enableDoubleClickEvent)
                {
                    T add_data = datalist[currentRowIndex];
                    if (add_itembox.ShowDialogValues(add_data) == DialogResult.OK)
                    {
                        add_data = add_itembox.Result;
                    }
                    datalist[currentRowIndex] = add_data;
                    RefreshData(this, datalist);
                }
            }
        }

        /// <summary>
        /// CellValueChange event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // HandleCellEdit(sender,e);
            string message = $"CellValueChanged event triggered - Row: {e.RowIndex}, Column: {e.ColumnIndex}";
            LogcatTraceHelper.Instance.LogError("Info", message);
        }

        /// <summary>
        /// CellDirtyStateChanged event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv == null)
            {
                return;
            }

            if (isManualInput)
            {
                // チェックボックスセルの場合は即座に編集を確定
                if (dgv.CurrentCell is DataGridViewCheckBoxCell)
                {
                    dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }

                return; // 手動編集中は他の処理をスキップ
            }

            if (dgv.IsCurrentCellDirty)
            {
                dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
                string message = $"CurrentCellDirtyStateChanged event triggered - Cell content changed, commit required - Row: {dgv.CurrentCell.RowIndex}, Column: {dgv.CurrentCell.ColumnIndex}";
                LogcatTraceHelper.Instance.LogError("Info", message);
            }
        }

        /// <summary>
        /// Handles the CellClick event of the DataGridView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataGridViewCellEventArgs"/> instance
        ///     containing the event data.</param>
        private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0) // Ensure the click is not on the header
            {
                if ((userClipMethod != null) && (this[e.ColumnIndex, e.RowIndex].Value != null))
                {
                    string cellValue = this[e.ColumnIndex, e.RowIndex].Value.ToString();
                    userClipMethod(e.ColumnIndex, cellValue);
                }
            }
        }

        /// <summary>
        /// Handles the CellBeginEdit event of the dataGridView1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataGridViewCellCancelEventArgs"/>
        ///     instance containing the event data.</param>
        private void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            isManualInput = true;
            string message = $"CellBeginEdit event triggered - Begin editing cell - Row: {e.RowIndex}, Column: {e.ColumnIndex}";
            LogcatTraceHelper.Instance.LogError("Info", message);
            DataGridView dgv = sender as DataGridView;
            if (dgv == null)
            {
                return;
            }

            originalValue = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
        }

        ///<summary>
        ///</summary>
        ///<param name="sender"></param>
        ///<param name="e"></param>
        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            HandleCellEdit(sender, e);
            isManualInput = false;
        }

        /// <summary>
        /// dataGridView1_RowValidate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_RowValidate(object sender, DataGridViewCellEventArgs e)
        {
            string message = $"Cell Row Validate event triggered - Row Validate editing cell - Row: {e.RowIndex}, Column: {e.ColumnIndex}";
            LogcatTraceHelper.Instance.LogError("Info", message);
            DataGridView dgv = sender as DataGridView;
            BindingList<T> dataList = dgv.DataSource as BindingList<T>;
            TabPage tabPage = Parent as TabPage;
            int rowIndex = e.RowIndex;
            if (rowIndex == 1)
            {
                int rowColumn = e.ColumnIndex;
            }
        }

        ///<summary>
        ///</summary>
        ///<param name="sender"></param>
        ///<param name="e"></param>
        private void DataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.Exception != null && e.Context == DataGridViewDataErrorContexts.Commit)
            {
                MessageBox.Show("Invalid input. Plese enter correct data ");
                e.Cancel = true;
                DataGridView dgv = sender as DataGridView;
                if (dgv != null)
                {
                    dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = originalValue;
                }
            }
        }

        /// <summary>
        /// DataGridView_DataBindingComplete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow row in this.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell is DataGridViewComboBoxCell comboCell)
                    {
                        if (comboCell.Value == null || comboCell.Value == DBNull.Value || !comboCell.Items.Contains(comboCell.Value))
                        {
                            // コンボボックスの最初のアイテムをデフォルト値として設定
                            if (comboCell.Items.Count > 0)
                            {
                                comboCell.Value = comboCell.Items[0];

                                // データソースの値を更新
                                string propertyName = comboCell.OwningColumn.DataPropertyName;
                                var dataBoundItem = row.DataBoundItem;
                                var propInfo = dataBoundItem.GetType().GetProperty(propertyName);
                                if (propInfo != null && propInfo.CanWrite)
                                {
                                    try
                                    {
                                        LogcatTraceHelper.Instance.LogError("Info", $"Updating data source value - Row: {row.Index}, Column: {cell.ColumnIndex}");
                                        propInfo.SetValue(dataBoundItem, comboCell.Value);
                                    }
                                    catch (Exception ex)
                                    {
                                        LogcatTraceHelper.Instance.LogError("Info", $"Updating data source value - Row: {row.Index}, Column: {cell.ColumnIndex}\n{ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the cell edit.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DataGridViewCellEventArgs"/> instance
        ///     containing the event data.</param>
        private void HandleCellEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (isUpdating)
            {
                return;
            }
            // 手動変更開始をマーク
            isManualInput = true;
            bool result_user_method = false;
            string message = $"CellEndEdit event triggered - End editing cell - Row: {e.RowIndex}, Column: {e.ColumnIndex}";
            LogcatTraceHelper.Instance.LogError("Info", message);
            DataGridView dgv = sender as DataGridView;
            BindingList<T> dataList = dgv.DataSource as BindingList<T>;
            TabPage tabPage = Parent as TabPage;
            int rowIndex = e.RowIndex;
            int rowColumn = e.ColumnIndex;
            if (rowIndex < 0 || rowIndex >= dataList.Count)
            {
                LogcatTraceHelper.Instance.LogError("Error", "Invalid row index. Exiting CellEndEdit event handler.");
                return;
            }
            T rowData = dataList[rowIndex];
            if (null != cellEditEndProcess && tabPage != null)
            {
                string tabname = tabPage.Name;
                cellEditEndProcess?.Invoke(tabname, rowIndex, rowColumn, rowData);
                LogcatTraceHelper.Instance.LogError("Info", "CellEndEdit event user handler.");
            }
            else if (null != userCellEditEndMethod)
            {
                T input = dataList[rowIndex];
                T output = new T();
                result_user_method = userCellEditEndMethod(input, ref output);
                LogcatTraceHelper.Instance.LogError("Info", "userCellEditEndMethod event user handler.");
                if (result_user_method)
                {
                    LogcatTraceHelper.Instance.LogError("Info", "userCellEditEndMethod change data.");
                    isUpdating = true;
                    dataList[rowIndex] = output;
                    RefreshData(dgv, dataList);
                    isUpdating = false;
                }
            }
        }

        /// <summary>
        /// Updates the cell values.
        /// </summary>
        private void UpdateCellValues()
        {
            foreach (DataGridViewRow row in this.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell is DataGridViewComboBoxCell comboCell)
                    {
                        string columnName = comboCell.OwningColumn.DataPropertyName;
                        if (_comboBoxManager.ContainsKey(columnName))
                        {
                            var items = _comboBoxManager.ComboBoxData[columnName];

                            // セルの値が新しい項目リストに含まれていない場合、デフォルト値を設定
                            if (!items.Contains((string)comboCell.Value))
                            {
                                comboCell.Value = items.FirstOrDefault();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// GetDefaultValue
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// DataGridView_KeyDown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (CurrentCell != null)
                {
                    int rowIndex = CurrentCell.RowIndex;

                    // 選択された行が最終行であるか確認
                    if ((rowIndex == Rows.Count - 1) && (preventRowAddition == false) && (true == enableAddLine))
                    {
                        BindingList<T> dataList = DataSource as BindingList<T>;
                        if (dataList != null)
                        {
                            dataList.Add(new T());
                            RefreshData(this, dataList);
                        }
                    }
                }

                e.Handled = true; // Enterキーによる次のセルへの移動を防ぐ
            }
        }

        /// <summary>
        /// InsertItem_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InsertItem_Click(object sender, EventArgs e)
        {
            // 選択された行の最後
            BindingList<T> datalist = DataSource as BindingList<T>;

            if (CurrentCell != null)
            {
                int currentRowIndex = CurrentCell.RowIndex; // 現在選択中のセルの行インデックス
                int nextRowIndex = currentRowIndex + 1; // 次の行のインデックス
                T add_data = new T();
                if (enableAddLineInputBox == true)
                {
                    if (add_itembox.ShowDialogValues(add_data) == DialogResult.OK)
                    {
                        add_data = add_itembox.Result;
                    }
                }
                datalist.Insert(nextRowIndex, add_data);
                RefreshData(this, datalist);
                Rows[currentRowIndex].Cells[0].Selected = false;
                Rows[nextRowIndex].Cells[0].Selected = true;
            }
        }

        /// <summary>
        /// DeleteItem_Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteItem_Click(object sender, EventArgs e)
        {
            // 選択された行を削除
            if (CurrentCell != null)
            {
                int currentRowIndex = CurrentCell.RowIndex; // 現在選択中のセルの行インデックス
                BindingList<T> datalist = DataSource as BindingList<T>;
                if (DialogResult.Yes == MessageBox.Show("Delete line? Confirm?", "Confirmation", MessageBoxButtons.YesNo))
                {
                    datalist.RemoveAt(currentRowIndex);
                    RefreshData(this, datalist);
                    if (currentRowIndex != 0)
                    {
                        // Rows[currentRowIndex].Cells[0].Selected = false;
                        Rows[(currentRowIndex - 1)].Cells[0].Selected = true;
                    }
                }
            }
        }

        /// <summary>
        /// RefreshData
        /// </summary>
        /// <param name="view"></param>
        /// <param name="newList"></param>
        private void RefreshData(DataGridView view, BindingList<T> newList)
        {
            preventRowAddition = true;
            view.EndEdit();
            preventRowAddition = false;
            LogcatTraceHelper.Instance.LogError("Info", "RefreshData event triggered.");
            try
            {
                view.DataSource = null;
                InitializeColumns();
                view.DataSource = newList;
            }
            catch (Exception ex)
            {
                string temp = ex.Message;
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// CellFormatting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value is long)
            {
                e.Value = $"0x{((long)e.Value):X}";
                e.FormattingApplied = true;
            }
        }

        /// <summary>
        /// HexadecimalFormatProvider
        /// </summary>
        private class HexadecimalFormatProvider : IFormatProvider, ICustomFormatter
        {
            ///<summary>
            ///</summary>
            ///<param name="formatType"></param>
            ///<returns></returns>
            public object GetFormat(Type formatType)
            {
                if (formatType == typeof(ICustomFormatter))
                {
                    return this;
                }

                return null;
            }

            /// <summary>
            /// Format
            /// </summary>
            /// <param name="format"></param>
            /// <param name="arg"></param>
            /// <param name="formatProvider"></param>
            /// <returns></returns>
            public string Format(string format, object arg, IFormatProvider formatProvider)
            {
                if (arg is long)
                {
                    return $"0x{((long)arg).ToString("X")}";
                }
                return string.Format("{0}", arg);
            }
        }

        /// <summary>
        /// 行IDを指定して編集禁止に設定
        /// </summary>
        /// <param name="rowId"></param>
        public void SetRowReadOnly(string rowId)
        {
            if (!readOnlyRowIds.Contains(rowId))
            {
                readOnlyRowIds.Add(rowId);
            }
        }

        /// <summary>
        /// 複数の行IDを一度に編集禁止に設定するメソッド
        /// </summary>
        /// <param name="rowIds"></param>
        // 複数の行IDを一度に編集禁止に設定するメソッド
        public void SetRowsReadOnly(IEnumerable<string> rowIds)
        {
            foreach (string rowId in rowIds)
            {
                SetRowReadOnly(rowId);
            }
        }

        /// <summary>
        /// 列名（DataPropertyName）を指定して編集禁止に設定するメソッド
        /// </summary>
        /// <param name="columnNames"></param>
        // 列名（DataPropertyName）を指定して編集禁止に設定するメソッド
        public void SetRowsReadOnlyByColumns(IEnumerable<string> columnNames)
        {
            foreach (DataGridViewRow row in Rows)
            {
                foreach (DataGridViewColumn column in Columns)
                {
                    if (columnNames.Contains(column.DataPropertyName))
                    {
                        string cellValue = row.Cells[column.Index].Value?.ToString();
                        if (cellValue != null && !readOnlyRowIds.Contains(cellValue))
                        {
                            readOnlyRowIds.Add(cellValue);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 行IDを指定して編集禁止を解除するメソッド
        /// </summary>
        /// <param name="rowId"></param>
        public void UnsetRowReadOnly(string rowId)
        {
            if (readOnlyRowIds.Contains(rowId))
            {
                readOnlyRowIds.Remove(rowId);
            }
        }

        /// <summary>
        /// 編集禁止の行IDリストを取得するメソッド
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetReadOnlyRowIds()
        {
            return readOnlyRowIds;
        }
    }
}