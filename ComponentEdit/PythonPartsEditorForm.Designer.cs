namespace PowerSimBuilder
{
    partial class PythonPartsEditorForm
    {
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated variables

        private System.Windows.Forms.RichTextBox textEditor;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnOpenFolder;
        private System.Windows.Forms.Label lblStatus;

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.textEditor = new System.Windows.Forms.RichTextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.panelTop = new System.Windows.Forms.Panel();
            this.btnRun = new System.Windows.Forms.Button();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnOpenFolder = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.panelTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // textEditor
            // 
            this.textEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textEditor.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textEditor.Location = new System.Drawing.Point(0, 40);
            this.textEditor.Name = "textEditor";
            this.textEditor.Size = new System.Drawing.Size(984, 476);
            this.textEditor.TabIndex = 0;
            this.textEditor.Text = "";
            this.textEditor.WordWrap = false;
            this.textEditor.TextChanged += new System.EventHandler(this.textEditor_TextChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnCancel);
            this.panel1.Controls.Add(this.btnSave);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 516);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(984, 50);
            this.panel1.TabIndex = 1;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(897, 15);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "キャンセル";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(816, 15);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "保存";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.btnRun);
            this.panelTop.Controls.Add(this.btnReload);
            this.panelTop.Controls.Add(this.btnOpenFolder);
            this.panelTop.Controls.Add(this.lblStatus);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(984, 40);
            this.panelTop.TabIndex = 2;
            // 
            // btnRun
            // 
            this.btnRun.Location = new System.Drawing.Point(225, 8);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(75, 23);
            this.btnRun.TabIndex = 0;
            this.btnRun.Text = "実行";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.BtnRun_Click);
            // 
            // btnReload
            // 
            this.btnReload.Location = new System.Drawing.Point(12, 8);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(101, 23);
            this.btnReload.TabIndex = 1;
            this.btnReload.Text = "再読み込み";
            this.btnReload.UseVisualStyleBackColor = true;
            this.btnReload.Click += new System.EventHandler(this.BtnReload_Click);
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.Location = new System.Drawing.Point(119, 8);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(100, 23);
            this.btnOpenFolder.TabIndex = 2;
            this.btnOpenFolder.Text = "フォルダを開く";
            this.btnOpenFolder.UseVisualStyleBackColor = true;
            this.btnOpenFolder.Click += new System.EventHandler(this.BtnOpenFolder_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(846, 16);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(45, 15);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "状態: ";
            // 
            // PythonPartsEditorForm
            // 
            this.ClientSize = new System.Drawing.Size(984, 566);
            this.Controls.Add(this.textEditor);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.panel1);
            this.Name = "PythonPartsEditorForm";
            this.Text = "Pythonコードエディタ";
            this.panel1.ResumeLayout(false);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
    }
}