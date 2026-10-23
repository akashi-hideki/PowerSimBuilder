using System.IO;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public class ComponentManager
    {
        /// <summary>
        /// Edit First flow (always unsaved):
        ///   1) If a file for this component type already exists, read its
        ///      content into memory (do not open it directly).
        ///   2) Otherwise generate code in memory.
        ///   3) Open the editor in an unsaved state. SaveFileDialog is shown
        ///      only when the user presses Save for the first time.
        ///   4) The suggested filename / directory match the existing file
        ///      location when one was found (for easy overwrite).
        /// </summary>
        /// <summary>
        /// Edit First flow (public entry point). Creates the editor and shows it modally.
        /// </summary>
        public static void GeneratePythonComponent(ComponentDescriptor descriptor)
        {
            using (var editorForm = CreateEditorForComponent(descriptor))
            {
                if (editorForm == null) return;
                if (editorForm.ShowDialog() == DialogResult.OK)
                {
                    AppServices.MessageBox.ShowInfo(null,
                        I18n.T("Manager.MsgSaved"),
                        I18n.T("Common.MsgTitleSuccess"));
                }
            }
        }

        /// <summary>
        /// Create the PythonEditorForm for the given component without showing it.
        /// Split out from GeneratePythonComponent so tests can exercise the
        /// Edit First logic without invoking ShowDialog (which is modal/blocking
        /// and prone to auto-click issues in headless environments).
        ///
        /// Returns null when the descriptor is invalid or code generation fails.
        /// </summary>
        public static PythonEditorForm CreateEditorForComponent(ComponentDescriptor descriptor)
        {
            if (descriptor == null)
            {
                AppServices.MessageBox.ShowError(null,
                    I18n.T("Manager.MsgNoSelection"),
                    I18n.T("Common.MsgTitleError"));
                return null;
            }

            string existing = ResolveExistingPath(descriptor);

            string initialCode;
            try
            {
                initialCode = existing != null
                    ? File.ReadAllText(existing)
                    : new ComponentCodeGenerator().GeneratePythonCodeAsString(descriptor);
            }
            catch (System.Exception ex)
            {
                AppServices.MessageBox.ShowError(null, ex.Message, I18n.T("Common.MsgTitleError"));
                return null;
            }

            var editorForm = new PythonEditorForm(null, false, initialCode);
            editorForm.ComponentType = descriptor.Type ?? "";
            if (existing != null)
            {
                editorForm.SuggestedFileName = System.IO.Path.GetFileName(existing);
                editorForm.SuggestedInitialDirectory = System.IO.Path.GetDirectoryName(existing);
            }
            else
            {
                editorForm.SuggestedFileName = $"{descriptor.Type}.py";
                editorForm.SuggestedInitialDirectory = ResolveSaveDialogInitialDirectory();
            }
            editorForm.Executor = new PythonExecutor();
            return editorForm;
        }

        /// <summary>
        /// Resolve an existing Python file for this component type, in priority:
        ///   cwd/user_components, cwd/generated, sln/user_components, sln/generated
        /// Returns null when none exist.
        /// </summary>
        private static string ResolveExistingPath(ComponentDescriptor descriptor)
        {
            string typeName = descriptor.Type;
            if (string.IsNullOrEmpty(typeName)) return null;

            var candidates = new System.Collections.Generic.List<string>();

            var cwd = System.IO.Directory.GetCurrentDirectory();
            candidates.Add(System.IO.Path.Combine(cwd, "user_components", typeName + ".py"));
            candidates.Add(System.IO.Path.Combine(cwd, "generated", typeName + ".py"));

            var baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            var dir = baseDir;
            while (!string.IsNullOrEmpty(dir))
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(dir, "PowerSimBuilder.sln")))
                {
                    candidates.Add(System.IO.Path.Combine(dir, "user_components", typeName + ".py"));
                    candidates.Add(System.IO.Path.Combine(dir, "generated", typeName + ".py"));
                    break;
                }
                var parent = System.IO.Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }

            foreach (var c in candidates)
                if (System.IO.File.Exists(c)) return c;
            return null;
        }

        /// <summary>
        /// Directory proposed in SaveFileDialog on first save. Prefers
        /// <sln>/generated/ then cwd/generated/.
        /// </summary>
        private static string ResolveSaveDialogInitialDirectory()
        {
            var cwd = System.IO.Directory.GetCurrentDirectory();
            var cwdGen = System.IO.Path.Combine(cwd, "generated");
            if (System.IO.Directory.Exists(cwdGen)) return cwdGen;

            var baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            var dir = baseDir;
            while (!string.IsNullOrEmpty(dir))
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(dir, "PowerSimBuilder.sln")))
                {
                    var gen = System.IO.Path.Combine(dir, "generated");
                    if (System.IO.Directory.Exists(gen)) return gen;
                    break;
                }
                var parent = System.IO.Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            return null;
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