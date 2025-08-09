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
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// InputBox
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Windows.Forms.Form"/>
    public class InputBox<T> : Form where T : new()
    {
        private T result;
        private T panel_setting;
        private Button okButton;
        private string titleName;
        private FlowLayoutPanel panel;
        private readonly double _font_adjust = 0.76;
        private Dictionary<string, tableParam> labelWidthDictionary = new Dictionary<string, tableParam>(); // 初期化
        private GroupBox externalGroupBox;
        private readonly Dictionary<string, Control> inputControls;
        private readonly Dictionary<Type, (Func<Control> create, Action<Control, object> set, Func<Control, object> get)> controlHandlers;
        private Panel clonedPanel;  // クローンされたパネルを保持するフィールド
        private readonly ControlCloner controlCloner;  // クローン処理を担当するクラス
        private bool isFormOpened;

        // コントロールの値を取得するハンドラー
        private readonly Dictionary<Type, Func<Control, object>> getValueHandlers = new Dictionary<Type, Func<Control, object>>
        {
            { typeof(TextBox), new Func<Control, object>(GetTextBoxValue) },
            { typeof(CheckBox), new Func<Control, object>(GetCheckBoxValue) },
            { typeof(ComboBox), new Func<Control, object>(GetComboBoxValue) }
        };

        // コントロールに値を設定するハンドラー
        private readonly Dictionary<Type, Action<Control, object>> setValueHandlers = new Dictionary<Type, Action<Control, object>>
        {
            { typeof(TextBox), new Action<Control, object>(SetTextBoxValue) },
            { typeof(CheckBox), new Action<Control, object>(SetCheckBoxValue) },
            { typeof(ComboBox), new Action<Control, object>(SetComboBoxValue) }
        };

        private Dictionary<string, List<string>> comboBoxData = new Dictionary<string, List<string>>();

        /// <summary>
        /// Sets the panel value.
        /// </summary>
        /// <value>The panel value.</value>
        public T PanelValue
        {
            set => SetInitialValues(value);
        }

        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <value>The result.</value>
        public T Result => result;

        /// <summary>
        /// Gets the panel data.
        /// </summary>
        /// <value>The panel data.</value>
        public T PanelData
        {
            get
            {
                panel_setting = new T();
                // フォームが開かれていない場合、パネルのデータを `Result` に反映
                if (clonedPanel != null)
                {
                    foreach (PropertyInfo prop in typeof(T).GetProperties())
                    {
                        Control[] clonedControls = clonedPanel.Controls.Find(prop.Name, true);
                        if (clonedControls.Length > 0)
                        {
                            Control clonedControl = clonedControls[0];
                            object panelValue = GetControlValue(prop, clonedControl);
                            prop.SetValue(panel_setting, panelValue);
                        }
                    }
                }
                return panel_setting;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is form opened.
        /// </summary>
        /// <value><c> true </c> if this instance is form opened; otherwise, <c>
        ///     false </c>.</value>
        public bool IsFormOpened
        {
            get => isFormOpened;
            private set => isFormOpened = value;
        }

        /// <summary>
        /// ComboBox に表示するデータを設定または取得します。
        /// </summary>
        public Dictionary<string, List<string>> ComboBoxData
        {
            get => comboBoxData;
            set
            {
                comboBoxData = value ?? new Dictionary<string, List<string>>();
                AddControls();
            }
        }

        /// <summary>
        /// Gets or sets the name of the title.
        /// </summary>
        /// <value>The name of the title.</value>
        public string TitleName
        {
            get => titleName;
            set
            {
                titleName = value;
                if (externalGroupBox != null)
                {
                    externalGroupBox.Text = value;
                }
                else
                {
                    Text = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the text box width in chars.
        /// </summary>
        /// <value>The text box width in chars.</value>
        public int TextBoxWidthInChars { get; set; } = 20;

        /// <summary>
        /// Sets the table setting.
        /// </summary>
        /// <value>The table setting.</value>
        public Dictionary<string, tableParam> TableSetting
        {
            set
            {
                labelWidthDictionary = value ?? new Dictionary<string, tableParam>();
                AddControls(); // 新しい設定を反映
            }
        }

        /// <summary>
        /// Gets or sets the user group.
        /// </summary>
        // GroupBox プロパティ
        public GroupBox UserGroup
        {
            get => externalGroupBox;
            set
            {
                externalGroupBox = value;
                if (externalGroupBox != null)
                {
                    externalGroupBox.Text = Text;
                    CloneAndAddPanelToGroupBox(); // PanelのクローンをGroupBoxに追加
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputBox{T}"/> class.
        /// </summary>
        /// <param name="groupBox">The group box.</param>
        public InputBox(GroupBox groupBox = null)
        {
            externalGroupBox = groupBox;
            inputControls = new Dictionary<string, Control>();

            controlHandlers = InitializeControlHandlers();
            InitializeComponents();
            TitleName = typeof(T).Name;
            controlCloner = new ControlCloner();  // クローン処理クラスのインスタンスを作成
        }

        /// <summary>
        /// Initializes the control handlers.
        /// </summary>
        /// <returns></returns>
        private Dictionary<Type, (Func<Control> create, Action<Control, object> set, Func<Control, object> get)> InitializeControlHandlers()
        {
            return new Dictionary<Type, (Func<Control> create, Action<Control, object> set, Func<Control, object> get)>
            {
                { typeof(bool), (CreateCheckBox, SetCheckBoxValue, GetCheckBoxValue) },
                { typeof(int), (CreateTextBox, SetTextBoxValue, GetTextBoxValue) },
                { typeof(double), (CreateTextBox, SetTextBoxValue, GetTextBoxValue) },
                { typeof(float), (CreateTextBox, SetTextBoxValue, GetTextBoxValue) },
                { typeof(long), (CreateTextBox, SetTextBoxValue, GetTextBoxValue) }
            };
        }

        /// <summary>
        /// Shows the dialog values.
        /// </summary>
        /// <param name="initialValues">The initial values.</param>
        /// <returns></returns>
        public DialogResult ShowDialogValues(T initialValues = default(T))
        {
            if (EqualityComparer<T>.Default.Equals(initialValues, default(T)) && clonedPanel != null)
            {
                // 初期値が指定されておらず、かつパネルが設定されている場合は、パネルのデータを取得
                initialValues = PanelData;
            }

            SetInitialValues(initialValues);
            return ShowDialog();
        }

        /// <summary>
        /// Sets the initial values.
        /// </summary>
        /// <param name="initialValue">The initial value.</param>
        private void SetInitialValues(T initialValue)
        {
            result = object.Equals(initialValue, default(T)) ? new T() : initialValue;

            // 初期値をFormのコントロールに反映
            UpdateControlValues();

            // クローンされたパネルおよびUserGroupにも初期値を反映
            if (externalGroupBox != null)
            {
                foreach (Control control in externalGroupBox.Controls)
                {
                    if (control is FlowLayoutPanel flowPanel)
                    {
                        foreach (Control innerControl in flowPanel.Controls)
                        {
                            if (innerControl is Label)
                            {
                                continue; // Labelはスキップ
                            }

                            // クローンされたコントロールを inputControls に再登録せずに値を設定
                            PropertyInfo prop = typeof(T).GetProperty(innerControl.Name);
                            if (prop != null)
                            {
                                object value = prop.GetValue(result);
                                SetControlValue(innerControl, value);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds the controls.
        /// </summary>
        private void AddControls()
        {
            panel.Controls.Clear();
            if (IsSimpleType(typeof(T)))
            {
                AddSimpleTypeControl(panel);
            }
            else
            {
                AddComplexTypeControls(panel);
            }
        }

        /// <summary>
        /// Analyzes the maximum label width.
        /// </summary>
        /// <returns></returns>
        private int AnalyzeMaxLabelWidth()
        {
            int maxLabelWidth = 0;
            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                bool hasTableParam = labelWidthDictionary.TryGetValue(prop.Name, out tableParam tableParam);
                int labelWidthInPixels = hasTableParam && tableParam.TitleName != null
                    ? GetTextWidthInPixels(tableParam.TitleName)
                    : GetLabelWidthInPixels(10);

                maxLabelWidth = Math.Max(maxLabelWidth, labelWidthInPixels);
            }

            return maxLabelWidth;
        }

        /// <summary>
        /// Analyzes the maximum text box end position.
        /// </summary>
        /// <returns></returns>
        private int AnalyzeMaxTextBoxEndPosition()
        {
            int maxTextBoxEndPosition = 0;
            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                bool hasTableParam = labelWidthDictionary.TryGetValue(prop.Name, out tableParam tableParam);
                int labelWidthInPixels = hasTableParam && tableParam.TitleName != null
                    ? GetLabelWidthInPixels(prop.Name.Length)
                    : GetLabelWidthInPixels(10);

                int textBoxWidthInPixels = hasTableParam
                    ? GetLabelWidthInPixels(tableParam.Width)
                    : GetLabelWidthInPixels(20);

                int textBoxEndPosition = labelWidthInPixels + textBoxWidthInPixels;

                maxTextBoxEndPosition = Math.Max(maxTextBoxEndPosition, textBoxEndPosition);
            }

            return maxTextBoxEndPosition;
        }

        /// <summary>
        /// CreateCheckBoxControl
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="panel"></param>
        /// <param name="maxLabelWidth"></param>
        private void CreateCheckBoxControl(PropertyInfo prop, FlowLayoutPanel panel, int maxLabelWidth)
        {
            CheckBox checkBox = new CheckBox
            {
                Text = labelWidthDictionary.TryGetValue(prop.Name, out tableParam tableParam) && tableParam.TitleName != null
                    ? tableParam.TitleName
                    : prop.Name,
                Name = prop.Name,
                AutoSize = true,
                Margin = new Padding(10, 0, 0, 0)
            };

            FlowLayoutPanel rowPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0),
                Margin = new Padding(0),
                WrapContents = false
            };

            rowPanel.Controls.Add(checkBox);
            panel.Controls.Add(rowPanel);

            inputControls[prop.Name] = checkBox;
        }

        /// <summary>
        /// CreateDefaultControl
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="panel"></param>
        /// <param name="maxLabelWidth"></param>

        private void CreateDefaultControl(PropertyInfo prop, FlowLayoutPanel panel, int maxLabelWidth)
        {
            Label label = new Label
            {
                Text = GetFormattedLabelText(prop, maxLabelWidth),
                AutoSize = false,
                Width = maxLabelWidth,
                Padding = new Padding(0, 10, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Control inputControl = CreateControlForProperty(prop);
            inputControl.Name = prop.Name;

            int textBoxWidthInPixels = labelWidthDictionary.TryGetValue(prop.Name, out tableParam tableParam)
               ? GetLabelWidthInPixels(tableParam.Width)
               : GetLabelWidthInPixels(20);

            inputControl.Width = textBoxWidthInPixels;
            inputControl.Margin = new Padding(0, 3, 0, 0);

            FlowLayoutPanel rowPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0),
                Margin = new Padding(0),
                WrapContents = false
            };

            rowPanel.Controls.Add(label);
            rowPanel.Controls.Add(inputControl);

            panel.Controls.Add(rowPanel);
            inputControls[prop.Name] = inputControl;
        }

        /// <summary>
        /// Gets the formatted label text.
        /// </summary>
        /// <param name="prop">The property.</param>
        /// <param name="maxLabelWidth">Maximum width of the label.</param>
        /// <returns></returns>
        private string GetFormattedLabelText(PropertyInfo prop, int maxLabelWidth)
        {
            string labelText = labelWidthDictionary.TryGetValue(prop.Name, out tableParam tableParam) && tableParam.TitleName != null
                ? tableParam.TitleName + ":"
                : prop.Name + ":";

            int currentLabelWidth = GetLabelWidthInPixels(labelText.Length);
            while (currentLabelWidth < maxLabelWidth)
            {
                labelText += " ";
                currentLabelWidth = GetLabelWidthInPixels(labelText.Length);
            }

            return labelText;
        }

        /// <summary>
        /// Adds the complex type controls.
        /// </summary>
        /// <param name="panel">The panel.</param>
        private void AddComplexTypeControls(FlowLayoutPanel panel)
        {
            panel.Controls.Clear();

            int maxLabelWidth = AnalyzeMaxLabelWidth();
            int maxTextBoxEndPosition = AnalyzeMaxTextBoxEndPosition();

            CreateControls(panel, maxTextBoxEndPosition, maxLabelWidth);

            Width = maxTextBoxEndPosition + 50;
        }

        /// <summary>
        /// Creates the controls.
        /// </summary>
        /// <param name="panel">The panel.</param>
        /// <param name="maxTextBoxEndPosition">The maximum text box end
        ///     position.</param>
        /// <param name="maxLabelWidth">Maximum width of the label.</param>
        private void CreateControls(FlowLayoutPanel panel, int maxTextBoxEndPosition, int maxLabelWidth)
        {
            PropertyInfo[] properties = typeof(T).GetProperties();

            Dictionary<Type, Action<PropertyInfo, FlowLayoutPanel, int>> controlCreators = new Dictionary<Type, Action<PropertyInfo, FlowLayoutPanel, int>>()
            {
                { typeof(bool), (prop, pnl, _) => CreateCheckBoxControl(prop, pnl, maxLabelWidth) },
                { typeof(string), (prop, pnl, _) => CreateDefaultControl(prop, pnl, maxLabelWidth) },
                { typeof(int), (prop, pnl, _) => CreateDefaultControl(prop, pnl, maxLabelWidth) },
                { typeof(double), (prop, pnl, _) => CreateDefaultControl(prop, pnl, maxLabelWidth) },
                { typeof(float), (prop, pnl, _) => CreateDefaultControl(prop, pnl, maxLabelWidth) },
                { typeof(long), (prop, pnl, _) => CreateDefaultControl(prop, pnl, maxLabelWidth) },
                { typeof(Enum), (prop, pnl, _) => CreateDefaultControl(prop, pnl, maxLabelWidth) }
            };

            foreach (PropertyInfo prop in properties)
            {
                Type propType = prop.PropertyType;
                if (propType.IsEnum)
                {
                    CreateDefaultControl(prop, panel, maxLabelWidth);
                }
                else if (controlCreators.TryGetValue(propType, out var createControl))
                {
                    createControl(prop, panel, maxTextBoxEndPosition);
                }
                else
                {
                    CreateDefaultControl(prop, panel, maxLabelWidth);
                }
            }
        }

        /// <summary>
        /// Adds the simple type control.
        /// </summary>
        /// <param name="panel">The panel.</param>
        private void AddSimpleTypeControl(FlowLayoutPanel panel)
        {
            panel.Controls.Clear();

            Label label = new Label { Text = $"Enter {typeof(T).Name}:", AutoSize = true, Padding = new Padding(0, 5, 0, 5) };
            panel.Controls.Add(label);

            Control inputControl = CreateControlForType(typeof(T));
            inputControl.Name = "inputValue";
            panel.Controls.Add(inputControl);

            inputControls["inputValue"] = inputControl;
        }
        /// <summary>
        /// Initializes the components.
        /// </summary>
        private void InitializeComponents()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            FlowLayoutPanel mainPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown
            };

            if (externalGroupBox == null)
            {
                Controls.Add(mainPanel);
            }
            else
            {
                externalGroupBox.Controls.Add(mainPanel);
            }

            panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown
            };
            mainPanel.Controls.Add(panel);

            AddControls();

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Bottom
            };

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                AutoSize = true
            };
            okButton.Click += OkButton_Click;

            Button cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                AutoSize = true
            };
            cancelButton.Click += CancelButton_Click;
            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(okButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            mainPanel.Controls.Add(buttonPanel);
        }

        /// <summary>
        /// Creates the type of the control for.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private Control CreateControlForType(Type type)
        {
            if (type.IsEnum)
            {
                ComboBox comboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                comboBox.Items.AddRange(Enum.GetNames(type));
                return comboBox;
            }

            if (controlHandlers.TryGetValue(type, out (Func<Control> create, Action<Control, object> set, Func<Control, object> get) handlers))
            {
                return handlers.create();
            }

            return new TextBox { Width = TextBoxWidthInChars * 7 };
        }

        /// <summary>
        /// Gets the text width in pixels.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        private int GetTextWidthInPixels(string text)
        {
            int totalWidth = 0;
            using (Graphics g = CreateGraphics())
            {
                foreach (char c in text)
                {
                    SizeF size = g.MeasureString(c.ToString(), Font);
                    totalWidth += (int)Math.Ceiling(size.Width * _font_adjust);
                }
            }
            return totalWidth;
        }

        /// <summary>
        /// Gets the label width in pixels.
        /// </summary>
        /// <param name="charCount">The character count.</param>
        /// <returns></returns>
        private int GetLabelWidthInPixels(int charCount)
        {
            using (Graphics g = CreateGraphics())
            {
                SizeF size = g.MeasureString(new string('W', charCount), Font);
                return (int)size.Width;
            }
        }

        /// <summary>
        /// Creates the CheckBox.
        /// </summary>
        /// <returns></returns>
        private Control CreateCheckBox()
        {
            return new CheckBox();
        }

        /// <summary>
        /// Creates the ComboBox.
        /// </summary>
        /// <returns></returns>
        private Control CreateComboBox()
        {
            ComboBox comboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBox.Items.AddRange(Enum.GetNames(typeof(T)));
            return comboBox;
        }

        /// <summary>
        /// Creates the text box.
        /// </summary>
        /// <returns></returns>
        private Control CreateTextBox()
        {
            return new TextBox { Width = TextBoxWidthInChars * 7 };
        }

        /// <summary>
        /// Gets the text box value.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns></returns>
        private static object GetTextBoxValue(Control control)
        {
            TextBox textBox = (TextBox)control;
            return textBox.Text;
        }

        /// <summary>
        /// Gets the CheckBox value.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns></returns>
        private static object GetCheckBoxValue(Control control)
        {
            CheckBox checkBox = (CheckBox)control;
            return checkBox.Checked;
        }

        /// <summary>
        /// Gets the ComboBox value.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns></returns>
        private static object GetComboBoxValue(Control control)
        {
            ComboBox comboBox = (ComboBox)control;
            return comboBox.SelectedItem != null && comboBox.Items.Count > 0 ? comboBox.SelectedItem.ToString() : null;
        }

        /// <summary>
        /// Sets the text box value.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value.</param>
        private static void SetTextBoxValue(Control control, object value)
        {
            TextBox textBox = (TextBox)control;
            textBox.Text = value?.ToString();
        }

        /// <summary>
        /// Sets the CheckBox value.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value.</param>
        private static void SetCheckBoxValue(Control control, object value)
        {
            CheckBox checkBox = (CheckBox)control;
            if (value is bool boolValue)
            {
                checkBox.Checked = boolValue;
            }
            else
            {
                checkBox.Checked = Convert.ToBoolean(value);
            }
        }

        /// <summary>
        /// CreateControlForProperty
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        private Control CreateControlForProperty(PropertyInfo prop)
        {
            Type type = prop.PropertyType;

            if (type.IsEnum)
            {
                // Enum 型の場合は既存の処理を維持
                ComboBox comboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                comboBox.Items.AddRange(Enum.GetNames(type));
                return comboBox;
            }
            else if (type == typeof(string) && comboBoxData.ContainsKey(prop.Name))
            {
                // string 型かつ ComboBoxData にデータがある場合は ComboBox を生成
                ComboBox comboBox = new ComboBox { Name = prop.Name };
                comboBox.Items.AddRange(comboBoxData[prop.Name].ToArray());
                // ユーザーの入力をハンドリングする場合はイベントを登録
                RegisterComboBoxEvents(comboBox, prop.Name);
                return comboBox;
            }
            else if (controlHandlers.TryGetValue(type, out var handlers))
            {
                return handlers.create();
            }
            else
            {
                return new TextBox { Width = TextBoxWidthInChars * 7 };
            }
        }

        /// <summary>
        /// Sets the ComboBox value.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value.</param>
        private static void SetComboBoxValue(Control control, object value)
        {
            ComboBox comboBox = (ComboBox)control;
            comboBox.SelectedItem = value?.ToString();
        }

        /// <summary>
        /// Updates the control values.
        /// </summary>
        private void UpdateControlValues()
        {
            if (IsSimpleType(typeof(T)))
            {
                Control control = inputControls["inputValue"];
                SetControlValue(control, result);
            }
            else
            {
                PropertyInfo[] properties = typeof(T).GetProperties();
                foreach (PropertyInfo prop in properties)
                {
                    if (inputControls.TryGetValue(prop.Name, out Control control))
                    {
                        object value = prop.GetValue(result);
                        SetControlValue(control, value);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the control value.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value.</param>
        private void SetControlValue(Control control, object value)
        {
            if (control is ComboBox comboBox && value != null)
            {
                if (value.GetType().IsEnum)
                {
                    comboBox.SelectedItem = value.ToString();
                }
                else
                {
                    comboBox.Text = value.ToString();
                }
            }
            else if (setValueHandlers.TryGetValue(control.GetType(), out var setValueHandler))
            {
                setValueHandler(control, value);
            }
        }

        /// <summary>
        /// Gets the control value.
        /// </summary>
        /// <param name="prop">The property.</param>
        /// <param name="control">The control.</param>
        /// <returns></returns>
        private object GetControlValue(PropertyInfo prop, Control control)
        {
            if (control is ComboBox comboBox)
            {
                if (prop.PropertyType.IsEnum)
                {
                    // Enum 型の場合の処理
                    string selectedItem = comboBox.SelectedItem as string;
                    if (!string.IsNullOrEmpty(selectedItem))
                    {
                        try
                        {
                            object enumValue = Enum.Parse(prop.PropertyType, selectedItem, true);
                            return enumValue;
                        }
                        catch (ArgumentException)
                        {
                            throw new InvalidOperationException($"Invalid enum value '{selectedItem}' for type '{prop.PropertyType.Name}'.");
                        }
                    }
                    else
                    {
                        return Activator.CreateInstance(prop.PropertyType);
                    }
                }
                else if (prop.PropertyType == typeof(string))
                {
                    // string 型の場合の処理
                    return comboBox.Text;
                }
            }
            else if (getValueHandlers.TryGetValue(control.GetType(), out var getValueHandler))
            {
                var value = getValueHandler(control);
                return Convert.ChangeType(value, prop.PropertyType, CultureInfo.InvariantCulture);
            }

            return null;
        }

        /// <summary>
        /// Handles the Click event of the OkButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the
        ///     event data.</param>
        private void OkButton_Click(object sender, EventArgs e)
        {
            try
            {
                ProcessOkButtonClick();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.LogError($"Error{ex.StackTrace}", $"{ex.Message}");
                MessageBox.Show("Error: " + ex.Message);
                DialogResult = DialogResult.None;
            }
        }

        /// <summary>
        /// Processes the ok button click.
        /// </summary>
        private void ProcessOkButtonClick()
        {
            if (IsSimpleType(typeof(T)))
            {
                Control control = inputControls["inputValue"];
                result = (T)Convert.ChangeType(GetControlValue(null, control), typeof(T), CultureInfo.InvariantCulture);

                // クローンされたパネルにもフォームのデータを反映
                ReflectValuesToClonedControls(control, result);
            }
            else
            {
                result = new T();
                foreach (PropertyInfo prop in typeof(T).GetProperties())
                {
                    if (inputControls.TryGetValue(prop.Name, out Control control))
                    {
                        // フォームのデータを取得
                        object formValue = GetControlValue(prop, control);
                        prop.SetValue(result, formValue);

                        // クローンされたパネルにもフォームのデータを反映
                        ReflectValuesToClonedControls(control, formValue);
                    }
                }
            }
        }

        /// <summary>
        /// Reflects the values to cloned controls.
        /// </summary>
        /// <param name="originalControl">The original control.</param>
        /// <param name="value">The value.</param>
        private void ReflectValuesToClonedControls(Control originalControl, object value)
        {
            if (clonedPanel != null)
            {
                Control[] clonedControls = clonedPanel.Controls.Find(originalControl.Name, true);
                if (clonedControls.Length > 0)
                {
                    Control clonedControl = clonedControls[0];
                    SetControlValue(clonedControl, value);
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the CancelButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the
        ///     event data.</param>
        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        /// <summary>
        /// Formats the result.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public string FormatResult(T value)
        {
            if (value == null)
            {
                return "No data provided.";
            }

            if (IsSimpleType(typeof(T)))
            {
                return value.ToString();
            }
            else
            {
                PropertyInfo[] properties = typeof(T).GetProperties();
                System.Text.StringBuilder resultBuilder = new System.Text.StringBuilder();
                foreach (PropertyInfo prop in properties)
                {
                    resultBuilder.AppendLine($"{prop.Name}: {prop.GetValue(value, null)}");
                }
                return resultBuilder.ToString();
            }
        }

        /// <summary>
        /// Clones the and add panel to group box.
        /// </summary>
        private void CloneAndAddPanelToGroupBox()
        {
            if (externalGroupBox != null)
            {
                if (panel != null)
                {
                    clonedPanel = controlCloner.ClonePanel(panel);
                    clonedPanel.Dock = DockStyle.Fill;
                    clonedPanel.Margin = new Padding(0);

                    externalGroupBox.Controls.Clear();
                    externalGroupBox.Controls.Add(clonedPanel);

                    int height = okButton.Height / 4;
                    externalGroupBox.AutoSize = false; // AutoSizeを無効化
                    externalGroupBox.Padding = new Padding(5); // 必要最小限のPadding
                    externalGroupBox.Size = new Size(clonedPanel.Width + 20, clonedPanel.Height - height); // GroupBoxのサイズを設定
                }
                else
                {
                    LogcatTraceHelper.Instance.LogError("input box ","Panel is not created. Unable to clone and add to GroupBox.");
                }
            }
        }

        /// <summary>
        /// Registers the ComboBox events.
        /// </summary>
        /// <param name="comboBox">The combo box.</param>
        /// <param name="propertyName">Name of the property.</param>
        private void RegisterComboBoxEvents(ComboBox comboBox, string propertyName)
        {
            comboBox.KeyDown -= HandleComboBoxInput;
            comboBox.KeyDown += HandleComboBoxInput;
            comboBox.Tag = propertyName;
        }

        /// <summary>
        /// Handles the ComboBox input.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="KeyEventArgs"/> instance containing
        ///     the event data.</param>
        private void HandleComboBoxInput(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && sender is ComboBox comboBox)
            {
                string newItem = comboBox.Text;
                string propertyName = comboBox.Tag as string;
                if (!string.IsNullOrEmpty(propertyName) && !string.IsNullOrEmpty(newItem))
                {
                    if (!comboBoxData.ContainsKey(propertyName))
                    {
                        comboBoxData[propertyName] = new List<string>();
                    }

                    if (!comboBoxData[propertyName].Contains(newItem))
                    {
                        comboBoxData[propertyName].Add(newItem);
                        comboBox.Items.Add(newItem);
                    }
                    comboBox.SelectedItem = newItem;
                }
            }
        }

        /// <summary>
        /// Determines whether [is simple type] [the specified type]. 
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c> true </c> if [is simple type] [the specified type];
        ///     otherwise, <c> false </c>.</returns>
        private bool IsSimpleType(Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime);
        }
    }
}