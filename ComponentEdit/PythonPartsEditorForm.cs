using System;
using System.IO;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class PythonPartsEditorForm : Form
    {
        private string _originalCode;
        private string _filePath;

        public bool IsModified { get; private set; }
        public string PythonCode => textEditor.Text;

        public PythonPartsEditorForm(string filePath)
        {
            InitializeComponent();
            _filePath = filePath;
            
            // 実行関連のボタンを非表示にする
            btnRun.Visible = false;
            btnReload.Visible = false;
            btnOpenFolder.Visible = false;
            lblStatus.Visible = false;
            
            // パネルの高さを調整
            panelTop.Height = 0;
            
            // エディターの位置を調整
            textEditor.Top = 0;
            textEditor.Height = this.ClientSize.Height - panel1.Height;
            
            LoadFile();
        }

        private void LoadFile()
        {
            try
            {
                _originalCode = File.ReadAllText(_filePath);
                textEditor.Text = _originalCode;
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
            IsModified = textEditor.Text != _originalCode;
            UpdateTitle();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText(_filePath, textEditor.Text);
                _originalCode = textEditor.Text;
                IsModified = false;
                UpdateTitle();
                
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイルの保存に失敗しました: {ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (IsModified)
            {
                var result = MessageBox.Show("変更内容が保存されていません。閉じますか？", 
                    "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.No)
                    return;
            }
            
            DialogResult = DialogResult.Cancel;
            Close();
        }
        
        // 実行機能が必要な場合にのみ使用するメソッド
        public void EnableRunFunctionality()
        {
            btnRun.Visible = true;
            btnReload.Visible = true;
            btnOpenFolder.Visible = true;
            lblStatus.Visible = true;
            
            // パネルの高さを元に戻す
            panelTop.Height = 40;
            
            // エディターの位置を調整
            textEditor.Top = panelTop.Height;
            textEditor.Height = this.ClientSize.Height - panelTop.Height - panel1.Height;
        }
        
        // 実行ボタンのイベントハンドラー（必要に応じて使用）
        private void btnRun_Click(object sender, EventArgs e)
        {
            // 実行機能は必要に応じて実装
            MessageBox.Show("実行機能はこのモードでは利用できません", "情報",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void btnReload_Click(object sender, EventArgs e)
        {
            // 再読み込み機能は必要に応じて実装
        }
        
        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            // フォルダを開く機能は必要に応じて実装
        }
        
        // GetPythonCodeメソッドを追加
        public string GetPythonCode()
        {
            return textEditor.Text;
        }
    }
}