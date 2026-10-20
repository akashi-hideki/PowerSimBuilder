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
                AppServices.MessageBox.ShowError(null,
                    I18n.T("Manager.MsgNoSelection"),
                    I18n.T("Common.MsgTitleError"));
                return;
            }

            string filePath = AppServices.FileDialog.PickSavePath(
                null,
                I18n.T("Common.FilterPython"),
                I18n.T("Manager.SaveDialogTitle"),
                $"{descriptor.Type}.py");
            if (string.IsNullOrEmpty(filePath)) return;

            var generator = new ComponentCodeGenerator();
            generator.GeneratePythonCode(descriptor, filePath);

            using (var editorForm = new PythonEditorForm(filePath, false))
            {
                editorForm.Executor = new PythonExecutor();
                if (editorForm.ShowDialog() == DialogResult.OK && editorForm.IsModified)
                {
                    File.WriteAllText(filePath, editorForm.GetPythonCode());
                    AppServices.MessageBox.ShowInfo(null,
                        I18n.T("Manager.MsgGenerated"),
                        I18n.T("Common.MsgTitleSuccess"));
                }
                else
                {
                    AppServices.MessageBox.ShowInfo(null,
                        I18n.T("Manager.MsgNoChange"),
                        I18n.T("Common.MsgTitleInfo"));
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
            this.Text = I18n.T("Manager.Title");
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
                Text = I18n.T("Common.BtnCancel"),
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