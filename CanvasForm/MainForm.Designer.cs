namespace PowerSimBuilder
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.canvasPanel = new System.Windows.Forms.Panel();
            this.palettePanel = new System.Windows.Forms.Panel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.saveButton = new System.Windows.Forms.ToolStripButton();
            this.saveAsButton = new System.Windows.Forms.ToolStripButton();
            this.loadButton = new System.Windows.Forms.ToolStripButton();
            this.undoButton = new System.Windows.Forms.ToolStripButton();
            this.redoButton = new System.Windows.Forms.ToolStripButton();
            this.createSimButton = new System.Windows.Forms.ToolStripButton();
            this.arrangeButton = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.addButton = new System.Windows.Forms.Button();
            this.editButton = new System.Windows.Forms.Button();
            this.removePalletButton = new System.Windows.Forms.Button();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // canvasPanel
            // 
            this.canvasPanel.BackColor = System.Drawing.SystemColors.Control;
            this.canvasPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.canvasPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvasPanel.Location = new System.Drawing.Point(0, 0);
            this.canvasPanel.Name = "canvasPanel";
            this.canvasPanel.Size = new System.Drawing.Size(900, 646);
            this.canvasPanel.TabIndex = 0;
            // 
            // palettePanel
            // 
            this.palettePanel.BackColor = System.Drawing.SystemColors.ControlLight;
            this.palettePanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.palettePanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.palettePanel.Location = new System.Drawing.Point(0, 31);
            this.palettePanel.Name = "palettePanel";
            this.palettePanel.Size = new System.Drawing.Size(300, 646);
            this.palettePanel.TabIndex = 2;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.staTableImportButton = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveButton,
            this.saveAsButton,
            this.loadButton,
            this.undoButton,
            this.redoButton,
            this.createSimButton,
            this.arrangeButton,
            this.staTableImportButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1500, 31);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // saveButton
            // 
            this.saveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.saveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(43, 28);
            this.saveButton.Text = I18n.T("Common.BtnSave");
            //
            // saveAsButton
            //
            this.saveAsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.saveAsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveAsButton.Name = "saveAsButton";
            this.saveAsButton.Size = new System.Drawing.Size(70, 28);
            this.saveAsButton.Text = I18n.T("MainForm.BtnSaveAs");
            // 
            // loadButton
            // 
            this.loadButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.loadButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.loadButton.Name = "loadButton";
            this.loadButton.Size = new System.Drawing.Size(69, 28);
            this.loadButton.Text = I18n.T("Common.BtnLoad");
            // 
            // undoButton
            // 
            this.undoButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.undoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.undoButton.Name = "undoButton";
            this.undoButton.Size = new System.Drawing.Size(67, 28);
            this.undoButton.Text = I18n.T("Common.BtnUndo");
            // 
            // redoButton
            // 
            this.redoButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.redoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.redoButton.Name = "redoButton";
            this.redoButton.Size = new System.Drawing.Size(61, 28);
            this.redoButton.Text = I18n.T("Common.BtnRedo");
            // 
            // runButton
            // 
            this.createSimButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.createSimButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.createSimButton.Name = "runButton";
            this.createSimButton.Size = new System.Drawing.Size(127, 28);
            this.createSimButton.Text = I18n.T("MainForm.BtnCreateSim");
            // 
            // arrangeButton
            // 
            this.arrangeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.arrangeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.arrangeButton.Name = "arrangeButton";
            this.arrangeButton.Size = new System.Drawing.Size(73, 28);
            this.arrangeButton.Text = I18n.T("MainForm.BtnArrange");
            //
            // staTableImportButton
            //
            // NOTE: instantiated earlier, before toolStrip1.Items.AddRange
            this.staTableImportButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.staTableImportButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.staTableImportButton.Name = "staTableImportButton";
            this.staTableImportButton.Size = new System.Drawing.Size(89, 28);
            this.staTableImportButton.Text = I18n.T("MainForm.BtnStaTableImport");
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(300, 31);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.canvasPanel);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.propertyGrid1);
            this.splitContainer1.Size = new System.Drawing.Size(1200, 646);
            this.splitContainer1.SplitterDistance = 900;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 1;
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
            this.propertyGrid1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(295, 646);
            this.propertyGrid1.TabIndex = 0;
            // 
            // addButton
            // 
            this.addButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.addButton.Location = new System.Drawing.Point(12, 637);
            this.addButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(80, 27);
            this.addButton.TabIndex = 3;
            this.addButton.Text = I18n.T("Common.BtnAdd");
            this.addButton.UseVisualStyleBackColor = true;
            // 
            // editButton
            // 
            this.editButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.editButton.Location = new System.Drawing.Point(100, 637);
            this.editButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.editButton.Name = "editButton";
            this.editButton.Size = new System.Drawing.Size(80, 27);
            this.editButton.TabIndex = 4;
            this.editButton.Text = I18n.T("Common.BtnEdit");
            this.editButton.UseVisualStyleBackColor = true;
            // 
            // removePalletButton
            // 
            this.removePalletButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.removePalletButton.Location = new System.Drawing.Point(188, 637);
            this.removePalletButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.removePalletButton.Name = "removePalletButton";
            this.removePalletButton.Size = new System.Drawing.Size(80, 27);
            this.removePalletButton.TabIndex = 5;
            this.removePalletButton.Text = I18n.T("Common.BtnDelete");
            this.removePalletButton.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1500, 677);
            this.Controls.Add(this.removePalletButton);
            this.Controls.Add(this.editButton);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.palettePanel);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "MainForm";
            this.Text = I18n.T("MainForm.Title");
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel canvasPanel;
        private System.Windows.Forms.Panel palettePanel;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton saveButton;
        private System.Windows.Forms.ToolStripButton saveAsButton;
        private System.Windows.Forms.ToolStripButton loadButton;
        private System.Windows.Forms.ToolStripButton undoButton;
        private System.Windows.Forms.ToolStripButton redoButton;
        private System.Windows.Forms.ToolStripButton createSimButton;
        private System.Windows.Forms.ToolStripButton arrangeButton;
        private System.Windows.Forms.ToolStripButton staTableImportButton;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.Button editButton;
        private System.Windows.Forms.Button removePalletButton;
    }
}