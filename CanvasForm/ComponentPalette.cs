using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Configuration;

namespace PowerSimBuilder
{
    /// <summary>
    /// コンポーネントパレットコントロール
    /// 作成日: 2025-08-15
    /// 変更内容: XMLベースのコンポーネント定義管理に移行
    /// 注意点: カテゴリ別にコンポーネントをグループ化表示
    /// </summary>
    public partial class ComponentPalette : Panel
    {
        private ComponentDefinitionManager _definitionManager;
        private TabControl _categoryTabControl;

        /// <summary>
        /// 選択中のコンポーネント定義
        /// 作成日: 2025-08-15
        /// 注意点: パレットで選択されたコンポーネントの定義
        /// </summary>
        public ComponentDefinition SelectedDefinition { get; private set; }

        /// <summary>
        /// コンポーネント選択イベント
        /// 作成日: 2025-08-15
        /// 注意点: コンポーネントが選択された時に発生
        /// </summary>
        public event EventHandler ComponentSelected;

        /// <summary>
        /// コンストラクタ
        /// 作成日: 2025-08-15
        /// 処理内容: コンポーネント定義をロードしUIを初期化
        /// </summary>
        public ComponentPalette()
        {
            InitializeComponent();
            InitializeComponentPalette();
        }
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ComponentPalette
            // 
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.White;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ResumeLayout(false);
        }
        /// <summary>
        /// 定義マネージャーを設定
        /// </summary>
        public void SetDefinitionManager(ComponentDefinitionManager manager)
        {
            _definitionManager = manager;
            RefreshPalette(); // 設定後にUIを更新
        }
        /// <summary>
        /// コンポーネントパレットの初期化
        /// 作成日: 2025-08-15
        /// 処理内容:
        ///   - 定義マネージャーの初期化
        ///   - カテゴリタブコントロールの作成
        ///   - コンポーネントボタンの生成
        /// </summary>
        private void InitializeComponentPalette()
        {
            // レイアウト設定
            this.AutoScroll = true;
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Dock = DockStyle.Fill;
            
            // 定義マネージャーの初期化
            // _definitionManager = new ComponentDefinitionManager();
            // _definitionManager.LoadDefinitions();
            
            // カテゴリタブコントロールの作成
            CreateCategoryTabs();
        }

        /// <summary>
        /// カテゴリタブを作成
        /// 作成日: 2025-08-15
        /// 処理内容: カテゴリごとにタブページを生成
        /// </summary>
        private void CreateCategoryTabs()
        {
            _categoryTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Alignment = TabAlignment.Left,
                SizeMode = TabSizeMode.Fixed,
                Multiline = true,
                ItemSize = new Size(25, 100),
                Padding = new Point(0, 0)
            };

            // カテゴリごとにタブページを作成
            var categories = _definitionManager.GetDefinitionsByCategory();
            foreach (var category in categories)
        {
                var tabPage = new TabPage(category.Key)
            {
                    BackColor = Color.White,
                    Padding = new Padding(5)
                };

                // カテゴリ内のコンポーネントを追加
                CreateComponentButtons(tabPage, category.Value);
                _categoryTabControl.TabPages.Add(tabPage);
            }

            this.Controls.Add(_categoryTabControl);
        }

        /// <summary>
        /// コンポーネントボタンを作成
        /// 作成日: 2025-08-15
        /// 処理内容: 定義に基づいてコンポーネントボタンを生成
        /// </summary>
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

        /// <summary>
        /// アイコン画像を作成
        /// 作成日: 2025-08-15
        /// 処理内容: コンポーネント定義に基づいたアイコンを生成
        /// </summary>
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

        /// <summary>
        /// コンポーネントボタンマウスダウンイベント
        /// 作成日: 2025-08-15
        /// 処理内容: ドラッグ操作を開始
        /// </summary>
        private void ComponentButton_MouseDown(object sender, MouseEventArgs e)
        {
            var button = (Button)sender;
            var definition = (ComponentDefinition)button.Tag;

            var data = new DataObject();
            data.SetData("ComponentDefinition", definition);

            button.DoDragDrop(data, DragDropEffects.Copy);
        }

        /// <summary>
        /// コンポーネントボタンクリックイベント
        /// 作成日: 2025-08-15
        /// 処理内容: コンポーネント選択状態を更新
        /// </summary>
        private void ComponentButton_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            SelectedDefinition = (ComponentDefinition)button.Tag;

            // 選択状態を通知
            OnComponentSelected(EventArgs.Empty);

            // 選択されたボタンを強調表示
            UpdateButtonSelection(button);
        }

        /// <summary>
        /// ボタン選択状態を更新
        /// 作成日: 2025-08-15
        /// 処理内容: 選択されたボタンのみ強調表示
        /// </summary>
        private void UpdateButtonSelection(Button selectedButton)
        {
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

        /// <summary>
        /// コンポーネント選択イベントを発行
        /// 作成日: 2025-08-15
        /// </summary>
        protected virtual void OnComponentSelected(EventArgs e)
        {
            ComponentSelected?.Invoke(this, e);
        }

    /// <summary>
        /// コンポーネントを追加
        /// 作成日: 2025-08-15
        /// 処理内容: パレットに新しいコンポーネントを追加
    /// </summary>
        public void AddComponent(ComponentDefinition definition)
    {
            if (_definitionManager.GetDefinition(definition.Type) == null)
        {
                _definitionManager.AddDefinition(definition);
                _definitionManager.SaveDefinitions();
                RefreshPalette();
        }
    }

    /// <summary>
        /// パレットを再読み込み
        /// 作成日: 2025-08-15
        /// 処理内容: 定義を再読み込みしてUIを更新
    /// </summary>
        public void RefreshPalette()
    {
            // 既存のコントロールをクリア
            this.Controls.Clear();

            // 定義を再読み込み
            _definitionManager.LoadDefinitions();
            
            // UIを再構築
            CreateCategoryTabs();
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