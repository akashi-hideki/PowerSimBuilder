using System;
using System.IO;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class PythonCodeEditorForm : Form
    {
        private string _originalCode;
        private string _currentCode;
        private string _filePath;

        public bool IsModified { get; private set; }

        public PythonCodeEditorForm(string filePath)
        {
            InitializeComponent();
            _filePath = filePath;
            LoadFile();
        }

        private void LoadFile()
        {
            try
            {
                _originalCode = File.ReadAllText(_filePath);
                _currentCode = _originalCode;
                textEditor.Text = _currentCode;
                IsModified = false;
                UpdateTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイルの読み込みに失敗しました: {ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void UpdateTitle()
        {
            this.Text = $"Pythonコードエディタ - {Path.GetFileName(_filePath)}" +
                       (IsModified ? " *" : "");
        }

        private void textEditor_TextChanged(object sender, EventArgs e)
        {
            _currentCode = textEditor.Text;
            IsModified = _currentCode != _originalCode;
            UpdateTitle();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        public string GetPythonCode()
        {
            return _currentCode;
        }
    }
}