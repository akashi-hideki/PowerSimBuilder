namespace PowerSimBuilder
{
    partial class MainForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.addComponentToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.saveButton = new System.Windows.Forms.ToolStripButton();
            this.loadButton = new System.Windows.Forms.ToolStripButton();
            this.undoButton = new System.Windows.Forms.ToolStripButton();
            this.redoButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.runButton = new System.Windows.Forms.ToolStripButton();
            this.arrangeButton = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.componentPalette1 = new PowerSimBuilder.ComponentPalette();
            this.designCanvas1 = new PowerSimBuilder.DesignCanvas();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.addButton = new System.Windows.Forms.Button();
            this.deleteButton = new System.Windows.Forms.Button();
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
            this.addComponentToolStripButton,
            this.saveButton,
            this.loadButton,
            this.undoButton,
            this.redoButton,
            this.toolStripSeparator1,
            this.runButton,
            this.arrangeButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1184, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // addComponentToolStripButton
            // 
            this.addComponentToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.addComponentToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("addComponentToolStripButton.Image")));
            this.addComponentToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.addComponentToolStripButton.Name = "addComponentToolStripButton";
            this.addComponentToolStripButton.Size = new System.Drawing.Size(63, 22);
            this.addComponentToolStripButton.Text = "部品追加";
            // 
            // saveButton
            // 
            this.saveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.saveButton.Enabled = false;
            this.saveButton.Image = ((System.Drawing.Image)(resources.GetObject("saveButton.Image")));
            this.saveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(35, 22);
            this.saveButton.Text = "保存";
            // 
            // loadButton
            // 
            this.loadButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.loadButton.Image = ((System.Drawing.Image)(resources.GetObject("loadButton.Image")));
            this.loadButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.loadButton.Name = "loadButton";
            this.loadButton.Size = new System.Drawing.Size(35, 22);
            this.loadButton.Text = "読込";
            // 
            // undoButton
            // 
            this.undoButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.undoButton.Enabled = false;
            this.undoButton.Image = ((System.Drawing.Image)(resources.GetObject("undoButton.Image")));
            this.undoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.undoButton.Name = "undoButton";
            this.undoButton.Size = new System.Drawing.Size(45, 22);
            this.undoButton.Text = "元に戻す";
            // 
            // redoButton
            // 
            this.redoButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.redoButton.Enabled = false;
            this.redoButton.Image = ((System.Drawing.Image)(resources.GetObject("redoButton.Image")));
            this.redoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.redoButton.Name = "redoButton";
            this.redoButton.Size = new System.Drawing.Size(45, 22);
            this.redoButton.Text = "やり直し";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // runButton
            // 
            this.runButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.runButton.Image = ((System.Drawing.Image)(resources.GetObject("runButton.Image")));
            this.runButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.runButton.Name = "runButton";
            this.runButton.Size = new System.Drawing.Size(35, 22);
            this.runButton.Text = "実行";
            // 
            // arrangeButton
            // 
            this.arrangeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.arrangeButton.Image = ((System.Drawing.Image)(resources.GetObject("arrangeButton.Image")));
            this.arrangeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.arrangeButton.Name = "arrangeButton";
            this.arrangeButton.Size = new System.Drawing.Size(47, 22);
            this.arrangeButton.Text = "配置整理";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(0, 28);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.componentPalette1);
            this.splitContainer1.Panel1MinSize = 200;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.designCanvas1);
            this.splitContainer1.Panel2MinSize = 600;
            this.splitContainer1.Size = new System.Drawing.Size(1000, 600);
            this.splitContainer1.SplitterDistance = 200;
            this.splitContainer1.TabIndex = 1;
            // 
            // componentPalette1
            // 
            this.componentPalette1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.componentPalette1.Location = new System.Drawing.Point(0, 0);
            this.componentPalette1.Name = "componentPalette1";
            this.componentPalette1.Size = new System.Drawing.Size(200, 600);
            this.componentPalette1.TabIndex = 0;
            // 
            // designCanvas1
            // 
            this.designCanvas1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.designCanvas1.Location = new System.Drawing.Point(0, 0);
            this.designCanvas1.Name = "designCanvas1";
            this.designCanvas1.Size = new System.Drawing.Size(796, 600);
            this.designCanvas1.TabIndex = 0;
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.propertyGrid1.Location = new System.Drawing.Point(1006, 28);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(250, 600);
            this.propertyGrid1.TabIndex = 2;
            // 
            // addButton
            // 
            this.addButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.addButton.Enabled = false;
            this.addButton.Location = new System.Drawing.Point(1006, 634);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(120, 30);
            this.addButton.TabIndex = 3;
            this.addButton.Text = "追加";
            this.addButton.UseVisualStyleBackColor = true;
            // 
            // deleteButton
            // 
            this.deleteButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.deleteButton.Enabled = false;
            this.deleteButton.Location = new System.Drawing.Point(1136, 634);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(120, 30);
            this.deleteButton.TabIndex = 4;
            this.deleteButton.Text = "削除";
            this.deleteButton.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1264, 681);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.propertyGrid1);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "MainForm";
            this.Text = "電力システムシミュレーションビルダー";
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

        private ToolStrip toolStrip1;
        private ToolStripButton addComponentToolStripButton;
        private ToolStripButton saveButton;
        private ToolStripButton loadButton;
        private ToolStripButton undoButton;
        private ToolStripButton redoButton;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton runButton;
        private ToolStripButton arrangeButton;
        private SplitContainer splitContainer1;
        private ComponentPalette componentPalette1;
        private DesignCanvas designCanvas1;
        private PropertyGrid propertyGrid1;
        private Button addButton;
        private Button deleteButton;
    }
}