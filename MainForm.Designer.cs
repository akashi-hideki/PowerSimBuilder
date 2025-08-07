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
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.runButton = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.designCanvas1 = new PowerSimBuilder.DesignCanvas();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.componentPalette1 = new PowerSimBuilder.ComponentPalette();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(884, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // runButton
            // 
            this.runButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.runButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.runButton.Name = "runButton";
            this.runButton.Size = new System.Drawing.Size(33, 22);
            this.runButton.Text = "実行";
            this.runButton.Click += new System.EventHandler(this.runButton_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(200, 25);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.designCanvas1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.propertyGrid1);
            this.splitContainer1.Size = new System.Drawing.Size(684, 425);
            this.splitContainer1.SplitterDistance = 456;
            this.splitContainer1.TabIndex = 1;
            // 
            // designCanvas1
            // 
            this.designCanvas1.BackColor = System.Drawing.Color.White;
            this.designCanvas1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.designCanvas1.Location = new System.Drawing.Point(0, 0);
            this.designCanvas1.Name = "designCanvas1";
            this.designCanvas1.Size = new System.Drawing.Size(456, 425);
            this.designCanvas1.TabIndex = 0;
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(224, 425);
            this.propertyGrid1.TabIndex = 0;
            // 
            // componentPalette1
            // 
            this.componentPalette1.Dock = System.Windows.Forms.DockStyle.Left;
            this.componentPalette1.Location = new System.Drawing.Point(0, 25);
            this.componentPalette1.Name = "componentPalette1";
            this.componentPalette1.Size = new System.Drawing.Size(200, 425);
            this.componentPalette1.TabIndex = 2;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 450);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.componentPalette1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "MainForm";
            this.Text = "電力系統シミュレーションビルダー";
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

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton runButton;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private DesignCanvas designCanvas1;
        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private ComponentPalette componentPalette1;
    }
}