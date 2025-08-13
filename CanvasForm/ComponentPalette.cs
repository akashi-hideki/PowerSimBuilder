using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class ComponentPalette : Panel
    {
        private ComponentDefinitionManager _definitionManager;
        private TabControl _categoryTabControl;

        public ComponentDefinition SelectedDefinition { get; private set; }
        public event EventHandler ComponentSelected;

        public ComponentPalette()
        {
            InitializeComponent();
            ShowInitializingMessage(); // 初期化中メッセージを表示
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.White;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ResumeLayout(false);
        }

        /// <summary>
        /// 初期化中メッセージを表示
        /// </summary>
        public void ShowInitializingMessage()
        {
            Controls.Clear();
            Controls.Add(new Label
            {
                Text = "コンポーネントを読み込み中...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            });
        }

        /// <summary>
        /// 定義マネージャーを設定し、パレットを初期化
        /// </summary>
        public void InitializePalette(ComponentDefinitionManager manager)
        {
            _definitionManager = manager;
            RefreshPalette();
        }

        /// <summary>
        /// パレットを再読み込み
        /// </summary>
        public void RefreshPalette()
        {
            Controls.Clear();

            if (_definitionManager == null)
            {
                // マネージャーが初期化されていない場合はデフォルト定義を直接使用
                ShowComponents(DefaultComponentDefinitions.Definitions);
                return;
            }

            try
            {
                // マネージャーから定義を取得
                ShowComponents(_definitionManager.Definitions);
            }
            catch (Exception ex)
            {
                // エラー時はデフォルト定義を表示
                ShowComponents(DefaultComponentDefinitions.Definitions);
                ShowErrorMessage($"コンポーネントの読み込みに失敗しました: {ex.Message}");
            }
        }
        public void ShowComponents(IEnumerable<ComponentDefinition> definitions)
        {
            // カテゴリ別にグループ化
            var categories = new Dictionary<string, List<ComponentDefinition>>();
            foreach (var definition in definitions)
            {
                if (!categories.ContainsKey(definition.Category))
                {
                    categories[definition.Category] = new List<ComponentDefinition>();
                }
                categories[definition.Category].Add(definition);
            }

            CreateCategoryTabs(categories);
        }

        private void CreateCategoryTabs(Dictionary<string, List<ComponentDefinition>> categories)
        {
            if (categories == null || categories.Count == 0)
            {
                ShowInfoMessage("利用可能なコンポーネントがありません");
                return;
            }

            _categoryTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Alignment = TabAlignment.Left,
                SizeMode = TabSizeMode.Fixed,
                Multiline = true,
                ItemSize = new Size(25, 100),
                Padding = new Point(0, 0)
            };

            foreach (var category in categories)
            {
                var tabPage = new TabPage(category.Key)
                {
                    BackColor = Color.White,
                    Padding = new Padding(5)
                };

                CreateComponentButtons(tabPage, category.Value);
                _categoryTabControl.TabPages.Add(tabPage);
            }

            Controls.Add(_categoryTabControl);
        }

        private void CreateComponentButtons(TabPage tabPage, List<ComponentDefinition> definitions)
        {
            var flowLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown
            };

            foreach (var definition in definitions)
            {
                var button = new Button
                {
                    Text = definition.Name,
                    Image = CreateIcon(definition),
                    ImageAlign = ContentAlignment.MiddleLeft,
                    TextAlign = ContentAlignment.MiddleRight,
                    Tag = definition,
                    Height = 40,
                    Width = 180,
                    Margin = new Padding(5),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.WhiteSmoke
                };

                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Color.LightGray;

                button.MouseDown += ComponentButton_MouseDown;
                button.Click += ComponentButton_Click;

                flowLayout.Controls.Add(button);
            }

            tabPage.Controls.Add(flowLayout);
        }

        private Bitmap CreateIcon(ComponentDefinition definition)
        {
            Color color = Color.Gray;
            try
            {
                color = Color.FromName(definition.IconColor);
            }
            catch
            {
                // 無効な色名の場合はデフォルト色を使用
            }

            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(new SolidBrush(color), 0, 0, 32, 32);
                g.DrawRectangle(Pens.Black, 0, 0, 31, 31);

                string symbol;
                switch (definition.Type)
                {
                    case "VoltageSourceInverter":
                        symbol = "V";
                        break;
                    case "InductionMotor":
                        symbol = "M";
                        break;
                    case "MechanicalLoad":
                        symbol = "L";
                        break;
                    case "FaultMonitor":
                        symbol = "F";
                        break;
                    default:
                        symbol = !string.IsNullOrEmpty(definition.Type) ?
                            definition.Type[0].ToString() : "C";
                        break;
                }

                g.DrawString(symbol, new Font("Arial", 10, FontStyle.Bold),
                            Brushes.White, new PointF(8, 8));
            }
            return bmp;
        }

        private void ComponentButton_MouseDown(object sender, MouseEventArgs e)
        {
            var button = (Button)sender;
            var definition = (ComponentDefinition)button.Tag;

            var data = new DataObject();
            data.SetData("ComponentDefinition", definition);

            button.DoDragDrop(data, DragDropEffects.Copy);
        }

        private void ComponentButton_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            SelectedDefinition = (ComponentDefinition)button.Tag;

            OnComponentSelected(EventArgs.Empty);
            UpdateButtonSelection(button);
        }

        private void UpdateButtonSelection(Button selectedButton)
        {
            if (_categoryTabControl == null) return;

            foreach (TabPage tabPage in _categoryTabControl.TabPages)
            {
                foreach (Control control in tabPage.Controls)
                {
                    if (control is FlowLayoutPanel flowLayout)
                    {
                        foreach (Control btnControl in flowLayout.Controls)
                        {
                            if (btnControl is Button btn)
                            {
                                btn.BackColor = (btn == selectedButton) ?
                                    Color.LightBlue : Color.WhiteSmoke;
                            }
                        }
                    }
                }
            }
        }

        protected virtual void OnComponentSelected(EventArgs e)
        {
            ComponentSelected?.Invoke(this, e);
        }

        public void AddComponent(ComponentDefinition definition)
        {
            if (_definitionManager == null ||
                _definitionManager.GetDefinition(definition.Type) != null)
            {
                return;
            }

            _definitionManager.AddDefinition(definition);
            _definitionManager.SaveDefinitions();
            RefreshPalette();
        }

        private void ShowErrorMessage(string message)
        {
            Controls.Clear();
            Controls.Add(new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Red
            });
        }

        private void ShowInfoMessage(string message)
        {
            Controls.Clear();
            Controls.Add(new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Blue
            });
        }
    }

    /// <summary>
    /// コンポーネント記述子クラス
    /// </summary>
    public class ComponentDescriptor
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Image Icon { get; set; }
        public string Category { get; set; }
        public List<ParameterDescriptor> Parameters { get; set; } = new List<ParameterDescriptor>();
        public string[] InputPorts { get; set; } = Array.Empty<string>();
        public string[] OutputPorts { get; set; } = Array.Empty<string>();

        public override string ToString()
        {
            return $"{Name} ({Type})";
        }
    }

    /// <summary>
    /// パラメータ記述子クラス
    /// </summary>
    public class ParameterDescriptor
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public object DefaultValue { get; set; }

        public override string ToString()
        {
            return $"{Name}: {DefaultValue} ({Type})";
        }
    }

    /// <summary>
    /// 故障条件クラス
    /// </summary>
    public class FaultCondition
    {
        public string ComponentName { get; set; }
        public string StateName { get; set; }
        public string ConditionType { get; set; }
        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();
    }

}