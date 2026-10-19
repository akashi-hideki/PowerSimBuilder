namespace PowerSimBuilder.ComponentEdit
{
    partial class EditPanel
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel tableLayout;
        private System.Windows.Forms.GroupBox grpComponents;
        private System.Windows.Forms.GroupBox grpRoleFunctions;
        private System.Windows.Forms.TreeView treeComponents;
        private System.Windows.Forms.TreeView treeRoleFunctions;
        private System.Windows.Forms.Label lblRoleCount;
        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Button btnOverride;
        private System.Windows.Forms.Button btnAdapter;
        private System.Windows.Forms.Button btnImportC;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpComponents = new System.Windows.Forms.GroupBox();
            this.treeComponents = new System.Windows.Forms.TreeView();
            this.grpRoleFunctions = new System.Windows.Forms.GroupBox();
            this.treeRoleFunctions = new System.Windows.Forms.TreeView();
            this.lblRoleCount = new System.Windows.Forms.Label();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.btnOverride = new System.Windows.Forms.Button();
            this.btnAdapter = new System.Windows.Forms.Button();
            this.btnImportC = new System.Windows.Forms.Button();
            this.tableLayout.SuspendLayout();
            this.grpComponents.SuspendLayout();
            this.grpRoleFunctions.SuspendLayout();
            this.panelButtons.SuspendLayout();
            this.SuspendLayout();
            //
            // tableLayout
            //
            this.tableLayout.ColumnCount = 1;
            this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayout.Controls.Add(this.grpComponents, 0, 0);
            this.tableLayout.Controls.Add(this.grpRoleFunctions, 0, 1);
            this.tableLayout.Controls.Add(this.panelButtons, 0, 2);
            this.tableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayout.Name = "tableLayout";
            this.tableLayout.RowCount = 3;
            this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayout.TabIndex = 0;
            //
            // grpComponents
            //
            this.grpComponents.Controls.Add(this.treeComponents);
            this.grpComponents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpComponents.Name = "grpComponents";
            this.grpComponents.TabIndex = 0;
            this.grpComponents.TabStop = false;
            //
            // treeComponents
            //
            this.treeComponents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeComponents.HideSelection = false;
            this.treeComponents.Name = "treeComponents";
            this.treeComponents.TabIndex = 0;
            //
            // grpRoleFunctions
            //
            this.grpRoleFunctions.Controls.Add(this.treeRoleFunctions);
            this.grpRoleFunctions.Controls.Add(this.lblRoleCount);
            this.grpRoleFunctions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpRoleFunctions.Name = "grpRoleFunctions";
            this.grpRoleFunctions.TabIndex = 1;
            this.grpRoleFunctions.TabStop = false;
            //
            // treeRoleFunctions
            //
            this.treeRoleFunctions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeRoleFunctions.HideSelection = false;
            this.treeRoleFunctions.Name = "treeRoleFunctions";
            this.treeRoleFunctions.TabIndex = 0;
            //
            // lblRoleCount
            //
            this.lblRoleCount.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblRoleCount.Height = 20;
            this.lblRoleCount.Name = "lblRoleCount";
            this.lblRoleCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblRoleCount.TabIndex = 1;
            //
            // panelButtons
            //
            this.panelButtons.Controls.Add(this.btnOverride);
            this.panelButtons.Controls.Add(this.btnAdapter);
            this.panelButtons.Controls.Add(this.btnImportC);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.TabIndex = 2;
            //
            // btnOverride
            //
            this.btnOverride.Location = new System.Drawing.Point(4, 6);
            this.btnOverride.Name = "btnOverride";
            this.btnOverride.Size = new System.Drawing.Size(85, 26);
            this.btnOverride.TabIndex = 0;
            this.btnOverride.UseVisualStyleBackColor = true;
            //
            // btnAdapter
            //
            this.btnAdapter.Location = new System.Drawing.Point(95, 6);
            this.btnAdapter.Name = "btnAdapter";
            this.btnAdapter.Size = new System.Drawing.Size(85, 26);
            this.btnAdapter.TabIndex = 1;
            this.btnAdapter.UseVisualStyleBackColor = true;
            //
            // btnImportC
            //
            this.btnImportC.Location = new System.Drawing.Point(186, 6);
            this.btnImportC.Name = "btnImportC";
            this.btnImportC.Size = new System.Drawing.Size(85, 26);
            this.btnImportC.TabIndex = 2;
            this.btnImportC.UseVisualStyleBackColor = true;
            //
            // EditPanel
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayout);
            this.Name = "EditPanel";
            this.Size = new System.Drawing.Size(280, 500);
            this.tableLayout.ResumeLayout(false);
            this.grpComponents.ResumeLayout(false);
            this.grpRoleFunctions.ResumeLayout(false);
            this.panelButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}