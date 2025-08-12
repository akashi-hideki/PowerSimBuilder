using System;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class ComponentPropertyForm : Form
    {
        private readonly ComponentControl _component;

        public ComponentPropertyForm(ComponentControl component)
        {
            _component = component;
            InitializeComponent();
            InitializeForm();
        }

        private void InitializeForm()
        {
            // 基本レイアウト設定
            this.Text = "コンポーネントプロパティ";
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;

            // 名前入力
            var lblName = new Label { Text = "名前:", Location = new Point(20, 20), Size = new Size(80, 20) };
            var txtName = new TextBox { Text = _component.InstanceName, Location = new Point(100, 20), Size = new Size(200, 20) };

            // 位置設定
            var lblX = new Label { Text = "X位置:", Location = new Point(20, 50), Size = new Size(80, 20) };
            var numX = new NumericUpDown { Value = _component.Location.X, Location = new Point(100, 50), Size = new Size(80, 20) };
            var lblY = new Label { Text = "Y位置:", Location = new Point(190, 50), Size = new Size(80, 20) };
            var numY = new NumericUpDown { Value = _component.Location.Y, Location = new Point(270, 50), Size = new Size(80, 20) };

            // ポート設定
            var lblInputPorts = new Label { Text = "入力ポート:", Location = new Point(20, 80), Size = new Size(80, 20) };
            var txtInputPorts = new TextBox { 
                Text = string.Join(",", _component.Descriptor.InputPorts ?? new string[0]),
                Location = new Point(100, 80), Size = new Size(200, 20)
            };

            var lblOutputPorts = new Label { Text = "出力ポート:", Location = new Point(20, 110), Size = new Size(80, 20) };
            var txtOutputPorts = new TextBox { 
                Text = string.Join(",", _component.Descriptor.OutputPorts ?? new string[0]),
                Location = new Point(100, 110), Size = new Size(200, 20)
            };

            // 特殊設定ボタン
            var btnThermal = new Button { Text = "熱パラメータ設定", Location = new Point(20, 150), Visible = false };
            var btnFault = new Button { Text = "故障条件設定", Location = new Point(150, 150), Visible = false };

            // コンポーネントタイプに応じてボタンを表示
            if (_component.Descriptor.Type == "InductionMotor")
            {
                btnThermal.Visible = true;
            }
            else if (_component.Descriptor.Type == "FaultMonitor")
            {
                btnFault.Visible = true;
            }

            // OK/キャンセルボタン
            var btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(120, 220) };
            var btnCancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Location = new Point(220, 220) };

            // コントロール追加
            this.Controls.AddRange(new Control[] {
                lblName, txtName,
                lblX, numX, lblY, numY,
                lblInputPorts, txtInputPorts,
                lblOutputPorts, txtOutputPorts,
                btnThermal, btnFault,
                btnOK, btnCancel
            });

            // イベントハンドラ
            btnThermal.Click += (s, e) => OpenThermalSettings();
            btnFault.Click += (s, e) => OpenFaultConditions();
            
            btnOK.Click += (s, e) => {
                _component.InstanceName = txtName.Text;
                _component.Location = new Point((int)numX.Value, (int)numY.Value);
                
                // ポート情報を更新
                _component.Descriptor.InputPorts = txtInputPorts.Text.Split(
                    new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                
                _component.Descriptor.OutputPorts = txtOutputPorts.Text.Split(
                    new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                
                // ポートを再初期化
                _component.InitializePortsDummy();
            };
        }

        private void OpenThermalSettings()
        {
            using (var form = new ThermalParameterForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // 熱パラメータをコンポーネントに保存
                    _component.Parameters["ThermalResistance"] = form.ThermalResistance;
                    _component.Parameters["ThermalCapacity"] = form.ThermalCapacity;
                }
            }
        }

        private void OpenFaultConditions()
        {
            // MainFormのインスタンスを取得
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm != null)
            {
                mainForm.OpenFaultConditionEditor();
            }
        }
    }
}