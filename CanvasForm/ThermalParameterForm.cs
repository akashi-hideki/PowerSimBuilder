using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class ThermalParameterForm : Form
    {
        public double ThermalResistance { get; private set; } = 1.0;
        public double ThermalCapacity { get; private set; } = 100.0;

        public ThermalParameterForm()
        {
            InitializeComponent();
            InitializeForm();
        }

        private void InitializeForm()
        {
            // 基本レイアウト設定
            this.Text = "熱パラメータ設定";
            this.Size = new Size(300, 180);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            // 熱抵抗
            var lblResistance = new Label { Text = "熱抵抗 [K/W]:", Location = new Point(20, 20) };
            var numResistance = new NumericUpDown
            {
                Value = (decimal)ThermalResistance,
                Location = new Point(120, 20),
                DecimalPlaces = 2,
                Minimum = 0.01m,
                Maximum = 1000m
            };

            // 熱容量
            var lblCapacity = new Label { Text = "熱容量 [J/K]:", Location = new Point(20, 50) };
            var numCapacity = new NumericUpDown
            {
                Value = (decimal)ThermalCapacity,
                Location = new Point(120, 50),
                DecimalPlaces = 2,
                Minimum = 1m,
                Maximum = 10000m
            };

            // OK/キャンセルボタン
            var btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(80, 90) };
            var btnCancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Location = new Point(160, 90) };

            // コントロール追加
            this.Controls.AddRange(new Control[] {
                lblResistance, numResistance,
                lblCapacity, numCapacity,
                btnOK, btnCancel
            });

            // イベントハンドラ
            btnOK.Click += (s, e) =>
            {
                ThermalResistance = (double)numResistance.Value;
                ThermalCapacity = (double)numCapacity.Value;
            };
        }
    }
}