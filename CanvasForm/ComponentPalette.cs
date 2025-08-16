using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class ComponentPalette : FlowLayoutPanel
    {
        
        /// <summary>
        /// 利用可能なコンポーネントリスト
        /// </summary>
        public List<ComponentDescriptor> AvailableComponents
        {
            get => _availableComponents;
            set
            {
                _availableComponents = value;
                ValidateAndInitializeComponents();
            }
        }
        private List<ComponentDescriptor> _availableComponents = new List<ComponentDescriptor>();
        // 追加: 重複チェック用のコンポーネント名セット
        private HashSet<string> _componentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// データが有効かどうかを示すプロパティ
        /// </summary>
        public bool IsDataValid => _componentNames.Count > 0; // 改善: 計算プロパティに変更      
 
        /// <summary>
        /// 選択中のコンポーネント
        /// </summary>
        public ComponentDescriptor SelectedComponent { get; private set; }

        /// <summary>
        /// コンポーネントパレットのコンストラクタ
        /// </summary>
        public ComponentPalette()
        {
            InitializeComponent();
        }

        /// <summary>
        /// コンポーネントパレットの初期化
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
        /// データを検証しコンポーネントを初期化
        /// </summary>
        /// <summary>
        /// データを検証しコンポーネントを初期化
        /// </summary>
        private void ValidateAndInitializeComponents()
        {
            // 既存のコントロールをクリア
            this.Controls.Clear();
            _componentNames.Clear(); // 名前セットをクリア

            if (AvailableComponents == null || AvailableComponents.Count == 0)
            {
                MessageBox.Show("利用可能なコンポーネントがありません", "警告",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 各コンポーネントを検証しながら追加
            foreach (var component in AvailableComponents)
            {
                if (IsValidComponent(component))
                {
                    // 重複チェックを追加
                    if (!_componentNames.Contains(component.Name))
                    {
                        _componentNames.Add(component.Name);
                        CreateComponentButton(component);
                    }
                    else
                    {
                        LogcatTraceHelper.Instance.LogError(component.Name, $"重複コンポーネントがスキップされました: {component.Name}");
                    }
                }
                else
                {
                    LogcatTraceHelper.Instance.LogError(component.Name, $"無効なコンポーネント: {component.Name}");
                }
            }
        }

        /// <summary>
        /// コンポーネントが有効かどうかを検証
        /// </summary>
        private bool IsValidComponent(ComponentDescriptor component)
        {
            // 基本的な検証ルール
            return !string.IsNullOrEmpty(component.Name) &&
                   !string.IsNullOrEmpty(component.Type) &&
                   component.Parameters != null &&
                   component.InputPorts != null &&
                   component.OutputPorts != null;
        }


        /// <summary>
        /// コンポーネントを追加する
        /// </summary>
        public void AddComponent(ComponentDescriptor component)
        {
            if (component == null)
            {
                LogcatTraceHelper.Instance.LogError("AddComponent", "Null component");
                return;
            }

            if (InvokeRequired)
            {
                Invoke(new Action(() => AddComponent(component)));
                return;
            }

            if (!IsValidComponent(component))
            {
                LogcatTraceHelper.Instance.LogError(component.Name, $"追加をスキップ: 無効なコンポーネント - {component.Name}");
                MessageBox.Show("コンポーネント情報が不完全です", "追加エラー",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_componentNames.Contains(component.Name))
            {
                LogcatTraceHelper.Instance.LogError(component.Name, $"追加をスキップ: 重複コンポーネント - {component.Name}");
                MessageBox.Show("同じ名前のコンポーネントが既に存在します", "追加エラー",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SuspendLayout();

                AvailableComponents.Add(component);
                _componentNames.Add(component.Name);
                CreateComponentButton(component);

                ResumeLayout(true);
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.LogError("AddComponent", ex.Message);
                MessageBox.Show($"コンポーネント追加中にエラーが発生しました: {ex.Message}",
                               "システムエラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// コンポーネントボタンを作成する（以前の実装を保持）
        /// </summary>
        private void CreateComponentButton(ComponentDescriptor component)
        {
            var button = new Button
            {
                Text = component.Name,
                Image = CreateIcon(component, GetColorByType(component.Type)),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleRight,
                Tag = component,
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

            this.Controls.Add(button);
        }

        /// <summary>
        /// コンポーネントタイプに基づいた色を取得（以前の実装を保持）
        /// </summary>
        private Color GetColorByType(string type)
        {
            switch (type)
            {
                case "VoltageSourceInverter": return Color.Red;
                case "InductionMotor": return Color.Blue;
                case "MechanicalLoad": return Color.Green;
                case "FaultMonitor": return Color.Orange;
                default: return Color.Gray;
            }
        }

        /// <summary>
        /// アイコン画像を作成する（以前の実装を保持）
        /// </summary>
        private Bitmap CreateIcon(ComponentDescriptor component, Color color)
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(new SolidBrush(color), 0, 0, 32, 32);
                g.DrawRectangle(Pens.Black, 0, 0, 31, 31);

                string symbol;
                switch (component.Type)
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
                        symbol = "C";
                        break;
                }

                g.DrawString(symbol, new Font("Arial", 10, FontStyle.Bold),
                            Brushes.White, new PointF(8, 8));
            }
            return bmp;
        }

        /// <summary>
        /// コンポーネントボタンマウスダウンイベント（以前の実装を保持）
        /// </summary>
        private void ComponentButton_MouseDown(object sender, MouseEventArgs e)
        {
            var button = (Button)sender;
            var component = (ComponentDescriptor)button.Tag;

            var data = new DataObject();
            data.SetData("ComponentDescriptor", component);

            button.DoDragDrop(data, DragDropEffects.Copy);
        }

        /// <summary>
        /// コンポーネントボタンクリックイベント（以前の実装を保持）
        /// </summary>
        private void ComponentButton_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            SelectedComponent = (ComponentDescriptor)button.Tag;

            // 選択状態を通知
            OnComponentSelected(EventArgs.Empty);

            // 選択されたボタンを強調表示
            foreach (Control control in Controls)
            {
                if (control is Button btn)
                {
                    btn.BackColor = (btn == button) ? Color.LightBlue : Color.WhiteSmoke;
                }
            }
        }
        /// <summary>
        /// コンポーネントを更新する
        /// </summary>
        public void UpdateComponent(ComponentDescriptor original, ComponentDescriptor updated)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateComponent(original, updated)));
                return;
            }

            if (original == null || updated == null) return;

            // 名前が変更された場合の重複チェック
            if (original.Name != updated.Name && _componentNames.Contains(updated.Name))
            {
                MessageBox.Show("この名前は既に使用されています", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // コンポーネントリストを更新
            int index = AvailableComponents.IndexOf(original);
            if (index >= 0)
            {
                AvailableComponents[index] = updated;

                // 名前セットを更新
                _componentNames.Remove(original.Name);
                _componentNames.Add(updated.Name);

                // 対応するボタンを更新
                var button = Controls.OfType<Button>().FirstOrDefault(b => b.Tag == original);
                if (button != null)
                {
                    button.Text = updated.Name;
                    button.Tag = updated;
                    button.Image = CreateIcon(updated, GetColorByType(updated.Type));
                }

                // 選択中のコンポーネントを更新
                if (SelectedComponent == original)
                {
                    SelectedComponent = updated;
                }
            }
        }

        /// <summary>
        /// コンポーネントを削除する
        /// </summary>
        public void RemoveComponent(ComponentDescriptor component)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => RemoveComponent(component)));
                return;
            }

            if (component == null) return;

            // 名前セットから削除
            _componentNames.Remove(component.Name);

            // リストから削除
            AvailableComponents.Remove(component);

            // 対応するボタンを削除
            var button = Controls.OfType<Button>().FirstOrDefault(b => b.Tag == component);
            if (button != null)
            {
                Controls.Remove(button);
                button.Dispose();
            }

            // 選択中のコンポーネントをクリア
            if (SelectedComponent == component)
            {
                SelectedComponent = null;
            }

            // 状態フラグ更新
            //IsDataValid = AvailableComponents.Count > 0;
        }
        /// <summary>
        /// コンポーネント選択イベント（以前の実装を保持）
        /// </summary>
        public event EventHandler ComponentSelected;

        /// <summary>
        /// コンポーネント選択イベントを発行する（以前の実装を保持）
        /// </summary>
        protected virtual void OnComponentSelected(EventArgs e)
        {
            ComponentSelected?.Invoke(this, e);
        }
    }


}