using System;
using System.IO;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public class ComponentManager
    {
        public static void GeneratePythonComponent(ComponentDescriptor descriptor)
        {
            if (descriptor == null)
            {
                MessageBox.Show("コンポーネントが選択されていません。", "エラー", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ファイル保存ダイアログを表示
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Pythonファイル|*.py|すべてのファイル|*.*";
                saveFileDialog.Title = "Pythonファイルを保存";
                saveFileDialog.FileName = $"{descriptor.Type}.py";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;

                    // Pythonコードを生成
                    var generator = new PythonCodeGenerator();
                    generator.GeneratePythonCode(descriptor, filePath);

                    // 編集フォームを表示
                    using (var editorForm = new PythonCodeEditorForm(filePath))
                    {
                        if (editorForm.ShowDialog() == DialogResult.OK && editorForm.IsModified)
                        {
                            // 変更があればファイルに保存
                            File.WriteAllText(filePath, editorForm.GetPythonCode());
                            MessageBox.Show("Pythonファイルを生成しました。", "成功", 
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            // 変更がなければファイルを削除（オプション）
                            // File.Delete(filePath);
                            MessageBox.Show("変更はありませんでした。", "情報", 
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
        }
    }

    // コンポーネント選択用の簡易フォーム
    public class ComponentSelectorForm : Form
    {
        private ListBox listBox;
        private Button btnOK;
        private Button btnCancel;
        
        public ComponentDescriptor SelectedDescriptor { get; private set; }
        
        public ComponentSelectorForm()
        {
            InitializeComponent();
            LoadComponents();
        }
        
        private void InitializeComponent()
        {
            this.Text = "コンポーネント選択";
            this.Size = new System.Drawing.Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                DisplayMember = "Name"
            };
            
            btnOK = new Button
            {
                Text = "OK",
                Dock = DockStyle.Right,
                Width = 80
            };
            
            btnCancel = new Button
            {
                Text = "キャンセル",
                Dock = DockStyle.Right,
                Width = 80
            };
            
            btnOK.Click += (s, e) => 
            {
                SelectedDescriptor = listBox.SelectedItem as ComponentDescriptor;
                DialogResult = DialogResult.OK;
                Close();
            };
            
            btnCancel.Click += (s, e) => 
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            
            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40
            };
            
            panel.Controls.Add(btnCancel);
            panel.Controls.Add(btnOK);
            
            this.Controls.Add(listBox);
            this.Controls.Add(panel);
        }
        
        private void LoadComponents()
        {
            foreach (var descriptor in ComponentDescriptorDictionary.Descriptors.Values)
            {
                listBox.Items.Add(descriptor);
            }
            
            if (listBox.Items.Count > 0)
            {
                listBox.SelectedIndex = 0;
            }
        }
    }
}