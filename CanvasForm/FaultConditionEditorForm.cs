using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class FaultConditionEditorForm : Form
    {
        public List<FaultCondition> Conditions { get; } = new List<FaultCondition>();

        public FaultConditionEditorForm(IEnumerable<ComponentControl> components)
        {
            InitializeComponent();
            InitializeForm(components);
        }

        private void InitializeForm(IEnumerable<ComponentControl> components)
        {
            // 基本レイアウト設定
            this.Text = "故障条件エディタ";
            this.Size = new Size(600, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            // コンポーネント選択
            var lblComponent = new Label { Text = "対象コンポーネント:", Location = new Point(20, 20) };
            var cmbComponent = new ComboBox { Location = new Point(150, 20), Width = 200 };
            cmbComponent.Items.AddRange(components.Select(c => c.InstanceName).ToArray());

            // 状態変数選択
            var lblState = new Label { Text = "監視状態:", Location = new Point(20, 50) };
            var cmbState = new ComboBox { Location = new Point(150, 50), Width = 200 };

            // 条件タイプ
            var lblType = new Label { Text = "条件タイプ:", Location = new Point(20, 80) };
            var cmbType = new ComboBox { Location = new Point(150, 80), Width = 200 };
            cmbType.Items.AddRange(new[] { "Threshold", "RateOfChange", "TimeAboveThreshold", "Oscillation" });

            // パラメータ入力
            var paramPanel = new Panel { Location = new Point(20, 110), Size = new Size(550, 150), BorderStyle = BorderStyle.FixedSingle };

            // 条件リスト
            var conditionList = new ListBox { Location = new Point(20, 270), Size = new Size(550, 100) };

            // 操作ボタン
            var btnAdd = new Button { Text = "追加", Location = new Point(400, 80) };
            var btnRemove = new Button { Text = "削除", Location = new Point(480, 80) };
            var btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(400, 370) };

            // コントロール追加
            this.Controls.AddRange(new Control[] {
                lblComponent, cmbComponent,
                lblState, cmbState,
                lblType, cmbType,
                paramPanel, conditionList,
                btnAdd, btnRemove, btnOK
            });

            // イベントハンドラ
            btnAdd.Click += (s, e) =>
            {
                if (cmbComponent.SelectedItem == null || cmbState.SelectedItem == null || cmbType.SelectedItem == null)
                {
                    MessageBox.Show("すべてのフィールドを選択してください");
                    return;
                }

                var condition = new FaultCondition
                {
                    ComponentName = cmbComponent.SelectedItem.ToString(),
                    StateName = cmbState.SelectedItem.ToString(),
                    ConditionType = cmbType.SelectedItem.ToString()
                };
                Conditions.Add(condition);
                conditionList.Items.Add($"{condition.ComponentName}.{condition.StateName} ({condition.ConditionType})");
            };

            btnRemove.Click += (s, e) =>
            {
                if (conditionList.SelectedIndex != -1)
                {
                    Conditions.RemoveAt(conditionList.SelectedIndex);
                    conditionList.Items.RemoveAt(conditionList.SelectedIndex);
                }
            };

            cmbComponent.SelectedIndexChanged += (s, e) =>
            {
                cmbState.Items.Clear();
                var compName = cmbComponent.SelectedItem?.ToString();
                if (compName != null)
                {
                    // コンポーネントの状態変数を取得（仮実装）
                    cmbState.Items.AddRange(new[] { "Temperature", "Voltage", "Current", "Speed" });
                }
            };

            btnOK.Click += (s, e) => DialogResult = DialogResult.OK;
        }
    }
}