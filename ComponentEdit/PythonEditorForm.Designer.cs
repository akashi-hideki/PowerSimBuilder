namespace PowerSimBuilder
{
    partial class PythonEditorForm
    {
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated variables

        private System.Windows.Forms.SplitContainer splitMain;
        private PowerSimBuilder.ComponentEdit.EditPanel editPanel;
        private System.Windows.Forms.RichTextBox textEditor;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnOpenFolder;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblSimTime;
        private System.Windows.Forms.NumericUpDown numSimTime;

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
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.editPanel = new PowerSimBuilder.ComponentEdit.EditPanel();
            this.textEditor = new System.Windows.Forms.RichTextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.panelTop = new System.Windows.Forms.Panel();
            this.btnRun = new System.Windows.Forms.Button();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnOpenFolder = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblSimTime = new System.Windows.Forms.Label();
            this.numSimTime = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSimTime)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panelTop.SuspendLayout();
            this.SuspendLayout();
            //
            // splitMain
            //
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitMain.Location = new System.Drawing.Point(0, 40);
            this.splitMain.Name = "splitMain";
            this.splitMain.Size = new System.Drawing.Size(984, 476);
            this.splitMain.SplitterDistance = 280;
            this.splitMain.TabIndex = 0;
            //
            // splitMain.Panel1 (EditPanel)
            //
            this.splitMain.Panel1.Controls.Add(this.editPanel);
            this.splitMain.Panel1MinSize = 200;
            //
            // editPanel
            //
            this.editPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editPanel.Location = new System.Drawing.Point(0, 0);
            this.editPanel.Name = "editPanel";
            this.editPanel.Size = new System.Drawing.Size(280, 476);
            this.editPanel.TabIndex = 0;
            //
            // splitMain.Panel2 (textEditor)
            //
            this.splitMain.Panel2.Controls.Add(this.textEditor);
            //
            // textEditor
            //
            this.textEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textEditor.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textEditor.Location = new System.Drawing.Point(0, 0);
            this.textEditor.Name = "TextBoxCode";
            this.textEditor.Size = new System.Drawing.Size(700, 476);
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
            this.btnCancel.Text = I18n.T("Common.BtnCancel");
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
            this.btnSave.Text = I18n.T("Common.BtnSave");
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // panelTop
            //
            this.panelTop.Controls.Add(this.btnRun);
            this.panelTop.Controls.Add(this.btnReload);
            this.panelTop.Controls.Add(this.btnOpenFolder);
            this.panelTop.Controls.Add(this.lblSimTime);
            this.panelTop.Controls.Add(this.numSimTime);
            this.panelTop.Controls.Add(this.lblStatus);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(984, 40);
            //
            // lblSimTime
            //
            this.lblSimTime.AutoSize = false;
            this.lblSimTime.Location = new System.Drawing.Point(276, 13);
            this.lblSimTime.Name = "lblSimTime";
            this.lblSimTime.Size = new System.Drawing.Size(55, 15);
            this.lblSimTime.TabIndex = 4;
            this.lblSimTime.Text = I18n.T("PythonEditor.SimTime");
            //
            // numSimTime
            //
            this.numSimTime.DecimalPlaces = 0;
            this.numSimTime.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            this.numSimTime.Location = new System.Drawing.Point(335, 10);
            this.numSimTime.Maximum = 3600m;
            this.numSimTime.Minimum = 0.1m;
            this.numSimTime.Name = "numSimTime";
            this.numSimTime.ReadOnly = false;
            this.numSimTime.Enabled = true;
            this.numSimTime.Size = new System.Drawing.Size(70, 23);
            this.numSimTime.TabIndex = 5;
            this.numSimTime.Value = 300m;
            this.panelTop.TabIndex = 2;
            //
            // btnRun
            //
            this.btnRun.Location = new System.Drawing.Point(8, 8);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(75, 24);
            this.btnRun.TabIndex = 0;
            this.btnRun.Text = I18n.T("PythonEditor.BtnRun");
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.BtnRun_Click);
            //
            // btnReload
            //
            this.btnReload.Location = new System.Drawing.Point(89, 8);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(75, 24);
            this.btnReload.TabIndex = 1;
            this.btnReload.Text = I18n.T("Common.BtnReload");
            this.btnReload.UseVisualStyleBackColor = true;
            this.btnReload.Click += new System.EventHandler(this.BtnReload_Click);
            //
            // btnOpenFolder
            //
            this.btnOpenFolder.Location = new System.Drawing.Point(170, 8);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(100, 24);
            this.btnOpenFolder.TabIndex = 2;
            this.btnOpenFolder.Text = I18n.T("Common.BtnOpenFolder");
            this.btnOpenFolder.UseVisualStyleBackColor = true;
            this.btnOpenFolder.Click += new System.EventHandler(this.BtnOpenFolder_Click);
            //
            // lblStatus
            //
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.AutoEllipsis = true;
            this.lblStatus.Location = new System.Drawing.Point(415, 12);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(561, 20);
            this.lblStatus.TabIndex = 3;
            //
            // PythonEditorForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 566);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.panel1);
            this.Name = "PythonEditorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = I18n.T("PythonEditor.Title");
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSimTime)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panelTop.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}
