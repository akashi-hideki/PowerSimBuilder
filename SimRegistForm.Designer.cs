namespace PowerSimBuilder
{
	partial class SimRegistForm
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
            this.mainTabControl = new System.Windows.Forms.TabControl();
            this.userGroupMultiRegist_TabPage = new System.Windows.Forms.TabPage();
            this.userGroupMultiRegist = new System.Windows.Forms.GroupBox();
            this.userGroupMultiRegParts_TabPage = new System.Windows.Forms.TabPage();
            this.userGroupMultiRegParts = new System.Windows.Forms.GroupBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.toolStripSimRegistForm = new System.Windows.Forms.ToolStrip();
            this.toolButton1 = new System.Windows.Forms.ToolStripButton();
            this.toolButton2 = new System.Windows.Forms.ToolStripButton();
            this.toolButton3 = new System.Windows.Forms.ToolStripButton();
            this.toolButton4 = new System.Windows.Forms.ToolStripButton();
            this.mainTabControl.SuspendLayout();
            this.userGroupMultiRegist_TabPage.SuspendLayout();
            this.userGroupMultiRegParts_TabPage.SuspendLayout();
            this.toolStripSimRegistForm.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTabControl
            // 
            this.mainTabControl.Controls.Add(this.userGroupMultiRegist_TabPage);
            this.mainTabControl.Controls.Add(this.userGroupMultiRegParts_TabPage);
            this.mainTabControl.Location = new System.Drawing.Point(13, 38);
            this.mainTabControl.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.mainTabControl.Name = "mainTabControl";
            this.mainTabControl.SelectedIndex = 0;
            this.mainTabControl.Size = new System.Drawing.Size(1513, 469);
            this.mainTabControl.TabIndex = 0;
            // 
            // userGroupMultiRegist_TabPage
            // 
            this.userGroupMultiRegist_TabPage.Controls.Add(this.userGroupMultiRegist);
            this.userGroupMultiRegist_TabPage.Location = new System.Drawing.Point(4, 25);
            this.userGroupMultiRegist_TabPage.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.userGroupMultiRegist_TabPage.Name = "userGroupMultiRegist_TabPage";
            this.userGroupMultiRegist_TabPage.Size = new System.Drawing.Size(1505, 440);
            this.userGroupMultiRegist_TabPage.TabIndex = 0;
            this.userGroupMultiRegist_TabPage.Text = "シュミレーション一覧";
            // 
            // userGroupMultiRegist
            // 
            this.userGroupMultiRegist.Location = new System.Drawing.Point(0, 0);
            this.userGroupMultiRegist.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.userGroupMultiRegist.Name = "userGroupMultiRegist";
            this.userGroupMultiRegist.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.userGroupMultiRegist.Size = new System.Drawing.Size(1315, 436);
            this.userGroupMultiRegist.TabIndex = 0;
            this.userGroupMultiRegist.TabStop = false;
            // 
            // userGroupMultiRegParts_TabPage
            // 
            this.userGroupMultiRegParts_TabPage.Controls.Add(this.userGroupMultiRegParts);
            this.userGroupMultiRegParts_TabPage.Location = new System.Drawing.Point(4, 25);
            this.userGroupMultiRegParts_TabPage.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.userGroupMultiRegParts_TabPage.Name = "userGroupMultiRegParts_TabPage";
            this.userGroupMultiRegParts_TabPage.Size = new System.Drawing.Size(1505, 440);
            this.userGroupMultiRegParts_TabPage.TabIndex = 1;
            this.userGroupMultiRegParts_TabPage.Text = "パーツ一覧";
            // 
            // userGroupMultiRegParts
            // 
            this.userGroupMultiRegParts.Location = new System.Drawing.Point(0, 0);
            this.userGroupMultiRegParts.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.userGroupMultiRegParts.Name = "userGroupMultiRegParts";
            this.userGroupMultiRegParts.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.userGroupMultiRegParts.Size = new System.Drawing.Size(1487, 436);
            this.userGroupMultiRegParts.TabIndex = 0;
            this.userGroupMultiRegParts.TabStop = false;
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(1247, 515);
            this.buttonOK.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(120, 31);
            this.buttonOK.TabIndex = 3;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(1393, 515);
            this.buttonCancel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(120, 31);
            this.buttonCancel.TabIndex = 4;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // toolStripSimRegistForm
            // 
            this.toolStripSimRegistForm.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStripSimRegistForm.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolButton1,
            this.toolButton2,
            this.toolButton3,
            this.toolButton4});
            this.toolStripSimRegistForm.Location = new System.Drawing.Point(0, 0);
            this.toolStripSimRegistForm.Name = "toolStripSimRegistForm";
            this.toolStripSimRegistForm.Size = new System.Drawing.Size(1540, 27);
            this.toolStripSimRegistForm.TabIndex = 5;
            this.toolStripSimRegistForm.Text = "toolStripSimRegistForm";
            // 
            // toolButton1
            // 
            this.toolButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolButton1.Name = "toolButton1";
            this.toolButton1.Size = new System.Drawing.Size(65, 24);
            this.toolButton1.Text = "Button1";
            this.toolButton1.Click += new System.EventHandler(this.toolButton1_Click);
            // 
            // toolButton2
            // 
            this.toolButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolButton2.Name = "toolButton2";
            this.toolButton2.Size = new System.Drawing.Size(65, 24);
            this.toolButton2.Text = "Button2";
            this.toolButton2.Click += new System.EventHandler(this.toolButton2_Click);
            // 
            // toolButton3
            // 
            this.toolButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolButton3.Name = "toolButton3";
            this.toolButton3.Size = new System.Drawing.Size(65, 24);
            this.toolButton3.Text = "Button3";
            this.toolButton3.Click += new System.EventHandler(this.toolButton3_Click);
            // 
            // toolButton4
            // 
            this.toolButton4.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolButton4.Name = "toolButton4";
            this.toolButton4.Size = new System.Drawing.Size(65, 24);
            this.toolButton4.Text = "Button4";
            this.toolButton4.Click += new System.EventHandler(this.toolButton4_Click);
            // 
            // SimRegistForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1540, 562);
            this.Controls.Add(this.mainTabControl);
            this.Controls.Add(this.toolStripSimRegistForm);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "SimRegistForm";
            this.Text = "SimRegist";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.form_close);
            this.Load += new System.EventHandler(this.form_load);
            this.mainTabControl.ResumeLayout(false);
            this.userGroupMultiRegist_TabPage.ResumeLayout(false);
            this.userGroupMultiRegParts_TabPage.ResumeLayout(false);
            this.toolStripSimRegistForm.ResumeLayout(false);
            this.toolStripSimRegistForm.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion
		private System.Windows.Forms.GroupBox userGroupMultiRegist;
		private System.Windows.Forms.GroupBox userGroupMultiRegParts;
		private System.Windows.Forms.TabControl mainTabControl;
		private System.Windows.Forms.ToolStrip	toolStripSimRegistForm;
		private System.Windows.Forms.ToolStripButton	toolButton1;
		private System.Windows.Forms.ToolStripButton	toolButton2;
		private System.Windows.Forms.ToolStripButton	toolButton3;
		private System.Windows.Forms.ToolStripButton	toolButton4;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.TabPage userGroupMultiRegist_TabPage;
        private System.Windows.Forms.TabPage userGroupMultiRegParts_TabPage;
    }
}
