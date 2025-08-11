using System;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class ConnectionEditorForm : Form
    {
        private readonly ComponentControl _source;
        private readonly ComponentControl _target;
        private readonly Connection _connection; // 編集対象の接続（既存の接続を編集する場合）

        public string SourcePort { get; private set; }
        public string TargetPort { get; private set; }
        public string DataType { get; private set; }

        // 新規接続用コンストラクタ
        public ConnectionEditorForm(ComponentControl source, ComponentControl target)
            : this(source, target, null)
        {
        }

        // 既存接続編集用コンストラクタ
        public ConnectionEditorForm(Connection connection)
            : this(connection.StartComponent, connection.EndComponent, connection)
        {
        }

        private ConnectionEditorForm(ComponentControl source, ComponentControl target, Connection connection)
        {
            _source = source;
            _target = target;
            _connection = connection;
            InitializeComponent();
            InitializeForm();
        }

        private void InitializeForm()
        {
            // 基本レイアウト設定
            this.Text = "接続詳細設定";
            this.Size = new Size(400, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;

            // ソースコンポーネント名
            var lblSourceComp = new Label { Text = "ソース:", Location = new Point(20, 20) };
            var txtSourceComp = new TextBox { 
                Text = _source.InstanceName, 
                Location = new Point(100, 20), 
                ReadOnly = true,
                Width = 200
            };

            // ソースポート
            var lblSourcePort = new Label { Text = "ソースポート:", Location = new Point(20, 50) };
            var cmbSourcePort = new ComboBox { Location = new Point(100, 50), Width = 200 };
            
            if (_source.Descriptor.OutputPorts != null)
            {
                cmbSourcePort.Items.AddRange(_source.Descriptor.OutputPorts);
            }

            // ターゲットコンポーネント名
            var lblTargetComp = new Label { Text = "ターゲット:", Location = new Point(20, 80) };
            var txtTargetComp = new TextBox { 
                Text = _target.InstanceName, 
                Location = new Point(100, 80), 
                ReadOnly = true,
                Width = 200
            };

            // ターゲットポート
            var lblTargetPort = new Label { Text = "ターゲットポート:", Location = new Point(20, 110) };
            var cmbTargetPort = new ComboBox { Location = new Point(100, 110), Width = 200 };
            
            if (_target.Descriptor.InputPorts != null)
            {
                cmbTargetPort.Items.AddRange(_target.Descriptor.InputPorts);
            }

            // データタイプ
            var lblDataType = new Label { Text = "データタイプ:", Location = new Point(20, 140) };
            var cmbDataType = new ComboBox { Location = new Point(100, 140), Width = 200 };
            cmbDataType.Items.AddRange(new[] { "double", "vector2", "vector3", "complex" });

            // 既存接続の値を設定
            if (_connection != null)
            {
                cmbSourcePort.SelectedItem = _connection.StartPort;
                cmbTargetPort.SelectedItem = _connection.EndPort;
                cmbDataType.SelectedItem = _connection.DataType ?? "double";
            }
            else
            {
                // デフォルト選択
                if (cmbSourcePort.Items.Count > 0) cmbSourcePort.SelectedIndex = 0;
                if (cmbTargetPort.Items.Count > 0) cmbTargetPort.SelectedIndex = 0;
                cmbDataType.SelectedIndex = 0;
            }

            // OK/キャンセルボタン
            var btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(120, 180) };
            var btnCancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Location = new Point(220, 180) };

            // コントロール追加
            this.Controls.AddRange(new Control[] {
                lblSourceComp, txtSourceComp,
                lblSourcePort, cmbSourcePort,
                lblTargetComp, txtTargetComp,
                lblTargetPort, cmbTargetPort,
                lblDataType, cmbDataType,
                btnOK, btnCancel
            });

            // イベントハンドラ
            btnOK.Click += (s, e) => {
                if (cmbSourcePort.SelectedItem == null || cmbTargetPort.SelectedItem == null)
                {
                    MessageBox.Show("ソースポートとターゲットポートを選択してください");
                    this.DialogResult = DialogResult.None;
                    return;
                }

                SourcePort = cmbSourcePort.SelectedItem.ToString();
                TargetPort = cmbTargetPort.SelectedItem.ToString();
                DataType = cmbDataType.SelectedItem?.ToString() ?? "double";
            };
        }
    }
}