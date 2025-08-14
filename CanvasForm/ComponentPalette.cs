/* 
 * ComponentPalette.cs
 * 
 * シミュレーションビルダーアプリケーションのコンポーネントパレットクラス
 * コンポーネント定義を表示・選択するためのパネルを提供します。
 * 
 * 変更履歴:
 * 2025-08-20 - パレット表示問題の修正
 *   - レイアウトをFlowLayoutPanelに変更
 *   - カテゴリタブを廃止しシンプルなリスト表示に変更
 *   - 選択状態の視覚的フィードバックを強化
 *   - ドラッグ&ドロップ処理の互換性を確保
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// コンポーネントパレットクラス
    /// 利用可能なコンポーネントを表示し、選択操作を提供します
    /// </summary>
    public partial class ComponentPalette : FlowLayoutPanel
    {
        private ComponentDefinitionManager _definitionManager;

        /// <summary>
        /// 選択中のコンポーネント定義
        /// </summary>
        public ComponentDefinition SelectedDefinition { get; private set; }

        /// <summary>
        /// コンポーネント選択イベント
        /// </summary>
        public event EventHandler ComponentSelected;

        /// <summary>
        /// コンポーネントパレットのコンストラクタ
        /// </summary>
        public ComponentPalette()
        {
            InitializeComponent();
            ShowInitializingMessage();
        }

        /// <summary>
        /// コンポーネントパレットのUI初期化
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
            this.FlowDirection = FlowDirection.TopDown;
            this.WrapContents = false;
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
        /// <param name="manager">コンポーネント定義マネージャー</param>
        public void InitializePalette(ComponentDefinitionManager manager)
        {
            _definitionManager = manager;
            RefreshPalette();
        }

        /// <summary>
        /// データバインディングを設定
        /// </summary>
        /// <param name="manager">コンポーネント定義マネージャー</param>
        public void BindData(ComponentDefinitionManager manager)
        {
            _definitionManager = manager;
            _definitionManager.DefinitionsChanged += (s, e) => 
            {
                ShowComponents(_definitionManager.Definitions);
            };
            ShowComponents(_definitionManager.Definitions);
        }

        /// <summary>
        /// パレットを再読み込み
        /// </summary>
        public void RefreshPalette()
        {
            Controls.Clear();

            if (_definitionManager == null)
            {
                ShowComponents(DefaultComponentDefinitions.Definitions);
                return;
            }

            try
            {
                ShowComponents(_definitionManager.Definitions);
            }
            catch (Exception ex)
            {
                ShowComponents(DefaultComponentDefinitions.Definitions);
                ShowErrorMessage($"コンポーネントの読み込みに失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// コンポーネントを表示
        /// </summary>
        /// <param name="definitions">表示するコンポーネント定義のリスト</param>
        public void ShowComponents(IEnumerable<ComponentDefinition> definitions)
        {
            SuspendLayout();
            Controls.Clear();

            foreach (var definition in definitions)
            {
                CreateComponentButton(definition);
            }

            ResumeLayout(true);
            PerformLayout();
            Invalidate();
        }

        /// <summary>
        /// コンポーネントボタンを作成
        /// </summary>
        /// <param name="definition">コンポーネント定義</param>
        private void CreateComponentButton(ComponentDefinition definition)
        {
            var button = new Button
            {
                Text = definition.Name,
                Image = CreateIcon(definition),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleRight,
                Tag = definition, // 定義をTagに保存
                Height = 40,
                Width = 180,
                Margin = new Padding(5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.WhiteSmoke
            };

            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.LightGray;

            // イベントハンドラをメソッドで登録
            button.MouseDown += ComponentButton_MouseDown;
            button.Click += ComponentButton_Click;

            Controls.Add(button);
        }
        // マウスダウンイベントハンドラ
        private void ComponentButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is Button button && button.Tag is ComponentDefinition definition)
            {
                // 定義からディスクリプタに変換
                var descriptor = new ComponentDescriptor
                {
                    Name = definition.Name,
                    Type = definition.Type,
                    Category = definition.Category,
                    Parameters = definition.Parameters.Select(p => new ParameterDescriptor
                    {
                        Name = p.Name,
                        Type = p.Type,
                        DefaultValue = p.DefaultValue
                    }).ToList(),
                    InputPorts = definition.InputPorts?.ToArray() ?? Array.Empty<string>(),
                    OutputPorts = definition.OutputPorts?.ToArray() ?? Array.Empty<string>()
                };

                var data = new DataObject();
                data.SetData("ComponentDescriptor", descriptor);
                button.DoDragDrop(data, DragDropEffects.Copy);
            }
        }

        // クリックイベントハンドラ
        private void ComponentButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is ComponentDefinition definition)
            {
                SelectedDefinition = definition;
                UpdateButtonSelection(button);
                OnComponentSelected(EventArgs.Empty);
            }
        }
        /// <summary>
        /// コンポーネントアイコンを作成
        /// </summary>
        /// <param name="definition">コンポーネント定義</param>
        /// <returns>生成されたアイコン画像</returns>
        private Bitmap CreateIcon(ComponentDefinition definition)
        {
            // タイプとシンボルのマッピング
            var typeSymbols = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"VoltageSourceInverter", "V"},
                {"InductionMotor", "M"},
                {"MechanicalLoad", "L"},
                {"FaultMonitor", "F"}
            };

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

                // タイプに応じたシンボルを取得
                string symbol;
                if (!string.IsNullOrEmpty(definition.Type) && 
                    typeSymbols.TryGetValue(definition.Type, out var foundSymbol))
                {
                    symbol = foundSymbol;
                }
                else
                {
                    // マッピングがない場合はタイプの最初の文字を使用
                    symbol = !string.IsNullOrEmpty(definition.Type) ? 
                        definition.Type[0].ToString() : "C";
                }

                g.DrawString(symbol, new Font("Arial", 10, FontStyle.Bold),
                            Brushes.White, new PointF(8, 8));
            }
            return bmp;
        }

        /// <summary>
        /// ボタンの選択状態を更新
        /// </summary>
        /// <param name="selectedButton">選択されたボタン</param>
        private void UpdateButtonSelection(Button selectedButton)
        {
            foreach (Control control in Controls)
            {
                if (control is Button btn)
                {
                    if (btn == selectedButton)
                    {
                        btn.BackColor = Color.LightBlue;
                        btn.Font = new Font(btn.Font, FontStyle.Bold);
                    }
                    else
                    {
                        btn.BackColor = Color.WhiteSmoke;
                        btn.Font = new Font(btn.Font, FontStyle.Regular);
                    }
                }
            }
        }

        /// <summary>
        /// コンポーネント選択イベントを発行
        /// </summary>
        /// <param name="e">イベント引数</param>
        protected virtual void OnComponentSelected(EventArgs e)
        {
            ComponentSelected?.Invoke(this, e);
        }

        /// <summary>
        /// コンポーネントを追加
        /// </summary>
        /// <param name="definition">追加するコンポーネント定義</param>
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

        /// <summary>
        /// エラーメッセージを表示
        /// </summary>
        /// <param name="message">表示するエラーメッセージ</param>
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

        /// <summary>
        /// 情報メッセージを表示
        /// </summary>
        /// <param name="message">表示する情報メッセージ</param>
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
}

