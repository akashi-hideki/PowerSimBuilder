using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    [global::System.ComponentModel.DesignerCategory("Code")]
    public partial class ComponentPalette : FlowLayoutPanel
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        /// <summary>
        /// 利用可能なコンポーネントリスト
        /// </summary>
        public List<ComponentDescriptor> AvailableComponents
        {
            get => _availableComponents;
            set
            {
                _availableComponents = value;
                if (!IsDesignMode) // デザイン時は処理しない
                {
                    ValidateAndInitializeComponents();
                }
            }
        }
        private List<ComponentDescriptor> _availableComponents = new List<ComponentDescriptor>();

        // 追加: 重複チェック用のコンポーネント名セット
        private HashSet<string> _componentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // クラスレベル変数を追加
        private Point? _dragStartPoint = null;
        private bool _isDragging = false;

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
            InitializeDesignTimeProperties();

            // デザイン時はここで終了
            if (IsDesignMode) return;

            InitializeRuntimeProperties();
        }

        /// <summary>
        /// デザイン時のプロパティ初期化
        /// </summary>
        private void InitializeDesignTimeProperties()
        {
            this.SuspendLayout();
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.White;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ResumeLayout(false);
        }

        /// <summary>
        /// 実行時のプロパティ初期化
        /// </summary>
        private void InitializeRuntimeProperties()
        {
            // 実行時のみ必要な初期化
            // イベントハンドラの登録など
            this.MouseDown += ComponentPalette_MouseDown;
            this.MouseMove += ComponentPalette_MouseMove;
            this.MouseUp += ComponentPalette_MouseUp;
        }

        private bool IsDesignMode
        {
            get
            {
                return (LicenseManager.UsageMode == LicenseUsageMode.Designtime) ||
                       (Site != null && Site.DesignMode);
            }
        }

        // Loadメソッドを削除し、代わりにOnHandleCreatedをオーバーライドします。
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (IsDesignMode) return;

            // 実行時のみの追加初期化
            if (AvailableComponents != null && AvailableComponents.Count > 0)
            {
                ValidateAndInitializeComponents();
            }
        }

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
                // デザイン時はメッセージボックスを表示しない
                if (!IsDesignMode)
                {
                    MessageBox.Show(I18n.T("Palette.MsgNoComponents"), I18n.T("Common.MsgTitleWarning"),
                                   MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
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
                    else if (!IsDesignMode) // デザイン時はログ出力しない
                    {
                        LogcatTraceHelper.Instance.LogError(component.Name, $"重複コンポーネントがスキップされました: {component.Name}");
                    }
                }
                else if (!IsDesignMode) // デザイン時はログ出力しない
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
            if (IsDesignMode) return; // デザイン時は処理しない

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
                MessageBox.Show(I18n.T("Palette.MsgIncomplete"), I18n.T("Common.MsgTitleError"),
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_componentNames.Contains(component.Name))
            {
                LogcatTraceHelper.Instance.LogError(component.Name, $"追加をスキップ: 重複コンポーネント - {component.Name}");
                MessageBox.Show(I18n.T("Palette.MsgDuplicate"), I18n.T("Common.MsgTitleError"),
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
                MessageBox.Show(I18n.T("Palette.MsgError", ex.Message),
                               I18n.T("Common.MsgTitleSystemError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// コンポーネントボタンを作成する
        /// </summary>
        private void CreateComponentButton(ComponentDescriptor component)
        {
            // ===== Task 3-C-2 fix: ボタン文字を明示的に描画 =====
            // アイコンオーバーレイ問題を回避するため、
            // ControllerBox など特殊コンポーネントにはアイコンなしのテキストのみで表示
            var button = new Button
            {
                Text = component.Name,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Black,
                BackColor = Color.WhiteSmoke,
                Font = new Font("Yu Gothic UI", 9F, FontStyle.Regular),
                Tag = component,
                Height = 40,
                Width = 260,
                Margin = new Padding(5),
                FlatStyle = FlatStyle.Flat,
                AutoEllipsis = true
            };

            // 全コンポーネントにアイコンを付ける（ControllerBox も含む）
            button.Image = CreateIcon(component, GetColorByType(component.Type));
            button.ImageAlign = ContentAlignment.MiddleLeft;
            button.TextAlign = ContentAlignment.MiddleRight;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;

            // ツールチップ（文字が切れてもフル名を確認可能）
            var tooltip = new ToolTip();
            tooltip.SetToolTip(button, component.Name + " (" + component.Type + ")");

            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.LightGray;

            // デザイン時はイベントハンドラを追加しない
            if (!IsDesignMode)
            {
                button.MouseDown += ComponentButton_MouseDown;
                button.MouseMove += ComponentButton_MouseMove;
                button.MouseUp += ComponentButton_MouseUp;
            }

            this.Controls.Add(button);
        }

        /// <summary>
        /// コンポーネントタイプに基づいた色を取得
        /// </summary>
        private Color GetColorByType(string type)
        {
            switch (type)
            {
                case "VoltageSourceInverter": return Color.Red;
                case "InductionMotor": return Color.Blue;
                case "MechanicalLoad": return Color.Green;
                case "FaultMonitor": return Color.Orange;
                case "ControllerBox": return Color.MediumPurple;   // Task 3-C-2: 制御ボックス識別色
                default: return Color.Gray;
            }
        }

        /// <summary>
        /// アイコン画像を作成する
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
                    case "ControllerBox":
                        symbol = "★";   // Task 3-C-2: 制御ボックス専用シンボル
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
        /// コンポーネントボタンマウスダウンイベント
        /// </summary>
        private void ComponentButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (IsDesignMode) return; // デザイン時は処理しない

            var button = (Button)sender;
            var component = (ComponentDescriptor)button.Tag;

            // 選択状態を更新
            SelectedComponent = component;
            OnComponentSelected(EventArgs.Empty);

            // 選択されたボタンを強調表示
            foreach (Control control in Controls)
            {
                if (control is Button btn)
                {
                    btn.BackColor = (btn == button) ? Color.LightBlue : Color.WhiteSmoke;
                }
            }

            // ドラッグ開始位置を記録（左クリックのみ）
            if (e.Button == MouseButtons.Left)
            {
                _dragStartPoint = e.Location;
                _isDragging = false;
            }
        }

        // MouseMoveイベントハンドラを追加
        private void ComponentButton_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsDesignMode) return; // デザイン時は処理しない

            if (e.Button == MouseButtons.Left && _dragStartPoint.HasValue)
            {
                // ドラッグ開始閾値をチェック
                int deltaX = Math.Abs(e.X - _dragStartPoint.Value.X);
                int deltaY = Math.Abs(e.Y - _dragStartPoint.Value.Y);

                if (deltaX > SystemInformation.DragSize.Width / 2 ||
                    deltaY > SystemInformation.DragSize.Height / 2)
                {
                    _isDragging = true;
                    var button = (Button)sender;
                    var component = (ComponentDescriptor)button.Tag;

                    var data = new DataObject();
                    data.SetData("ComponentDescriptor", component);
                    button.DoDragDrop(data, DragDropEffects.Copy);

                    // ドラッグ後は状態をリセット
                    _dragStartPoint = null;
                    _isDragging = false;
                }
            }
        }

        // MouseUpイベントハンドラを追加
        private void ComponentButton_MouseUp(object sender, MouseEventArgs e)
        {
            if (IsDesignMode) return; // デザイン時は処理しない

            if (e.Button == MouseButtons.Left && !_isDragging && _dragStartPoint.HasValue)
            {
                // ドラッグせずにマウスを離した場合、クリック処理を実行
                var button = (Button)sender;
                var component = (ComponentDescriptor)button.Tag;

                // ここで追加のクリック処理があれば実行
                // 例: 詳細表示など
            }

            // 状態をリセット
            _dragStartPoint = null;
            _isDragging = false;
        }

        // パレット自体のマウスイベントハンドラ（デザイン時用）
        private void ComponentPalette_MouseDown(object sender, MouseEventArgs e)
        {
            if (IsDesignMode) return;
            // 必要に応じて実装
        }

        private void ComponentPalette_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsDesignMode) return;
            // 必要に応じて実装
        }

        private void ComponentPalette_MouseUp(object sender, MouseEventArgs e)
        {
            if (IsDesignMode) return;
            // 必要に応じて実装
        }

        /// <summary>
        /// コンポーネントを更新する
        /// </summary>
        public void UpdateComponent(ComponentDescriptor original, ComponentDescriptor updated)
        {
            if (IsDesignMode) return; // デザイン時は処理しない

            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateComponent(original, updated)));
                return;
            }

            if (original == null || updated == null) return;

            // 名前が変更された場合の重複チェック
            if (original.Name != updated.Name && _componentNames.Contains(updated.Name))
            {
                MessageBox.Show(I18n.T("Palette.MsgNameInUse"), I18n.T("Common.MsgTitleError"),
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

                // 対応するボタンを更新（無ければ再作成: Fix 3-C-7b）
                var button = Controls.OfType<Button>().FirstOrDefault(b => b.Tag == original);
                if (button != null)
                {
                    button.Text = updated.Name;
                    button.Tag = updated;
                    button.Image = CreateIcon(updated, GetColorByType(updated.Type));
                }
                else
                {
                    // 削除後に再取り込みされたケース: ボタンを作り直す
                    CreateComponentButton(updated);
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
            if (IsDesignMode) return; // デザイン時は処理しない

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
        }

        /// <summary>
        /// コンポーネント選択イベント
        /// </summary>
        public event EventHandler ComponentSelected;

        /// <summary>
        /// コンポーネント選択イベントを発行する
        /// </summary>
        protected virtual void OnComponentSelected(EventArgs e)
        {
            ComponentSelected?.Invoke(this, e);
        }
    }
}