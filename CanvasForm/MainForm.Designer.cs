namespace PowerSimBuilder
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

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
            this.components = new System.ComponentModel.Container();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.saveButton = new System.Windows.Forms.ToolStripButton();
            this.loadButton = new System.Windows.Forms.ToolStripButton();
            this.undoButton = new System.Windows.Forms.ToolStripButton();
            this.redoButton = new System.Windows.Forms.ToolStripButton();
            this.deleteButton = new System.Windows.Forms.ToolStripButton();
            this.runButton = new System.Windows.Forms.ToolStripButton();
            this.addComponentToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.arrangeButton = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.designCanvas1 = new PowerSimBuilder.DesignCanvas();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.componentPalette1 = new PowerSimBuilder.ComponentPalette();
            this.addButton = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.loadingProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.componentContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.propertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.connectionContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.editConnectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteConnectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.componentContextMenu.SuspendLayout();
            this.connectionContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveButton,
            this.loadButton,
            this.undoButton,
            this.redoButton,
            this.deleteButton,
            this.runButton,
            this.addComponentToolStripButton,
            this.arrangeButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(884, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // saveButton
            // 
            this.saveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.saveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(35, 22);
            this.saveButton.Text = "保存";
            // 
            // loadButton
            // 
            this.loadButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.loadButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.loadButton.Name = "loadButton";
            this.loadButton.Size = new System.Drawing.Size(47, 22);
            this.loadButton.Text = "読み込み";
            // 
            // undoButton
            // 
            this.undoButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.undoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.undoButton.Name = "undoButton";
            this.undoButton.Size = new System.Drawing.Size(51, 22);
            this.undoButton.Text = "元に戻す";
            // 
            // "toolStripStatusLabel" と "loadingProgressBar" の宣言が2重になっているため、Designerファイル内の重複宣言を削除します。
            // 既に MainForm クラスのフィールドとして宣言されているため、下記2行を削除してください。

            // private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
            // private System.Windows.Forms.ToolStripProgressBar loadingProgressBar;
            // redoButton
            // 
            this.redoButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.redoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.redoButton.Name = "redoButton";
            this.redoButton.Size = new System.Drawing.Size(47, 22);
            this.redoButton.Text = "やり直し";
            // 
            // deleteButton
            // 
            this.deleteButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.deleteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(35, 22);
            this.deleteButton.Text = "削除";
            // 
            // runButton
            // 
            this.runButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.runButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.runButton.Name = "runButton";
            this.runButton.Size = new System.Drawing.Size(33, 22);
            this.runButton.Text = "実行";
            // 
            // addComponentToolStripButton
            // 
            this.addComponentToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.addComponentToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.addComponentToolStripButton.Name = "addComponentToolStripButton";
            this.addComponentToolStripButton.Size = new System.Drawing.Size(87, 22);
            this.addComponentToolStripButton.Text = "コンポーネント追加";
            // 
            // arrangeButton
            // 
            this.arrangeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.arrangeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.arrangeButton.Name = "arrangeButton";
            this.arrangeButton.Size = new System.Drawing.Size(51, 22);
            this.arrangeButton.Text = "配置整理";
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
            this.splitContainer1.Size = new System.Drawing.Size(684, 400);
            this.splitContainer1.SplitterDistance = 456;
            this.splitContainer1.TabIndex = 1;
            // 
            // designCanvas1
            // 
            this.designCanvas1.BackColor = System.Drawing.Color.White;
            this.designCanvas1.ContextMenuStrip = this.componentContextMenu;
            this.designCanvas1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.designCanvas1.Location = new System.Drawing.Point(0, 0);
            this.designCanvas1.Name = "designCanvas1";
            this.designCanvas1.Size = new System.Drawing.Size(456, 400);
            this.designCanvas1.TabIndex = 0;
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(224, 400);
            this.propertyGrid1.TabIndex = 0;
            // 
            // componentPalette1
            // 
            this.componentPalette1.Dock = System.Windows.Forms.DockStyle.Left;
            this.componentPalette1.Location = new System.Drawing.Point(0, 25);
            this.componentPalette1.Name = "componentPalette1";
            this.componentPalette1.Size = new System.Drawing.Size(200, 400);
            this.componentPalette1.TabIndex = 2;
            // 
            // addButton
            // 
            this.addButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.addButton.Enabled = false;
            this.addButton.Location = new System.Drawing.Point(12, 392);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(75, 23);
            this.addButton.TabIndex = 3;
            this.addButton.Text = "追加";
            this.addButton.UseVisualStyleBackColor = true;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel,
            this.loadingProgressBar});
            this.statusStrip1.Location = new System.Drawing.Point(0, 428);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(884, 22);
            this.statusStrip1.TabIndex = 4;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(118, 17);
            this.toolStripStatusLabel.Text = "システムの準備ができました";
            // 
            // loadingProgressBar
            // 
            this.loadingProgressBar.Name = "loadingProgressBar";
            this.loadingProgressBar.Size = new System.Drawing.Size(100, 16);
            this.loadingProgressBar.Visible = false;
            // 
            // componentContextMenu
            // 
            this.componentContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.propertiesToolStripMenuItem,
            this.deleteToolStripMenuItem});
            this.componentContextMenu.Name = "componentContextMenu";
            this.componentContextMenu.Size = new System.Drawing.Size(181, 70);
            // 
            // propertiesToolStripMenuItem
            // 
            this.propertiesToolStripMenuItem.Name = "propertiesToolStripMenuItem";
            this.propertiesToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.propertiesToolStripMenuItem.Text = "プロパティ(&P)";
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.deleteToolStripMenuItem.Text = "削除(&D)";
            // 
            // connectionContextMenu
            // 
            this.connectionContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.editConnectionToolStripMenuItem,
            this.deleteConnectionToolStripMenuItem});
            this.connectionContextMenu.Name = "connectionContextMenu";
            this.connectionContextMenu.Size = new System.Drawing.Size(181, 70);
            // 
            // editConnectionToolStripMenuItem
            // 
            this.editConnectionToolStripMenuItem.Name = "editConnectionToolStripMenuItem";
            this.editConnectionToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.editConnectionToolStripMenuItem.Text = "接続を編集(&E)";
            // 
            // deleteConnectionToolStripMenuItem
            // 
            this.deleteConnectionToolStripMenuItem.Name = "deleteConnectionToolStripMenuItem";
            this.deleteConnectionToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.deleteConnectionToolStripMenuItem.Text = "削除(&D)";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 450);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.componentPalette1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "MainForm";
            this.Text = "電力系統シミュレーションビルダー";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.componentContextMenu.ResumeLayout(false);
            this.connectionContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton saveButton;
        private System.Windows.Forms.ToolStripButton loadButton;
        private System.Windows.Forms.ToolStripButton undoButton;
        private System.Windows.Forms.ToolStripButton redoButton;
        private System.Windows.Forms.ToolStripButton deleteButton;
        private System.Windows.Forms.ToolStripButton runButton;
        private System.Windows.Forms.ToolStripButton addComponentToolStripButton;
        private System.Windows.Forms.ToolStripButton arrangeButton;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private DesignCanvas designCanvas1;
        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private ComponentPalette componentPalette1;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.ToolStripProgressBar loadingProgressBar;
        private System.Windows.Forms.ContextMenuStrip componentContextMenu;
        private System.Windows.Forms.ToolStripMenuItem propertiesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip connectionContextMenu;
        private System.Windows.Forms.ToolStripMenuItem editConnectionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteConnectionToolStripMenuItem;
    }
}