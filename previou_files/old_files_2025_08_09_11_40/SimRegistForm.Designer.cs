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
		
			// Initialize TabControl
			this.mainTabControl = new System.Windows.Forms.TabControl();
			this.mainTabControl.Location = new System.Drawing.Point(10, 30);
			this.mainTabControl.Appearance = System.Windows.Forms.TabAppearance.Normal;
			this.mainTabControl.Name = "mainTabControl";
			this.mainTabControl.Size = new System.Drawing.Size(1935, 320);
			this.mainTabControl.TabIndex = 0;
			this.Controls.Add(this.mainTabControl);
			// UserGroup creation for userGroupMultiRegist
			this.userGroupMultiRegist = new System.Windows.Forms.GroupBox();
			this.userGroupMultiRegist.Location = new System.Drawing.Point(0,0);
			this.userGroupMultiRegist.Name = "userGroupMultiRegist";
			this.userGroupMultiRegist.Size = new System.Drawing.Size(1786,300);
			this.userGroupMultiRegist.TabIndex = 0;
			this.userGroupMultiRegist.TabStop = false;
			// TabPage creation for userGroupMultiRegist
			System.Windows.Forms.TabPage userGroupMultiRegist_TabPage = new System.Windows.Forms.TabPage();
			userGroupMultiRegist_TabPage.Name = "Registview";
			userGroupMultiRegist_TabPage.Text = "シュミレーション一覧";
			userGroupMultiRegist_TabPage.Controls.Add(this.userGroupMultiRegist);
			this.mainTabControl.TabPages.Add(userGroupMultiRegist_TabPage);
			// UserGroup creation for userGroupMultiRegParts
			this.userGroupMultiRegParts = new System.Windows.Forms.GroupBox();
			this.userGroupMultiRegParts.Location = new System.Drawing.Point(0,0);
			this.userGroupMultiRegParts.Name = "userGroupMultiRegParts";
			this.userGroupMultiRegParts.Size = new System.Drawing.Size(1915,300);
			this.userGroupMultiRegParts.TabIndex = 0;
			this.userGroupMultiRegParts.TabStop = false;
			// TabPage creation for userGroupMultiRegParts
			System.Windows.Forms.TabPage userGroupMultiRegParts_TabPage = new System.Windows.Forms.TabPage();
			userGroupMultiRegParts_TabPage.Name = "RegPartsview";
			userGroupMultiRegParts_TabPage.Text = "パーツ一覧";
			userGroupMultiRegParts_TabPage.Controls.Add(this.userGroupMultiRegParts);
			this.mainTabControl.TabPages.Add(userGroupMultiRegParts_TabPage);
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonOK.Location = new System.Drawing.Point(1735, 400);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(90, 25);
			this.buttonOK.TabIndex = 3;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonCancel.Location = new System.Drawing.Point(1845, 400);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(90, 25);
			this.buttonCancel.TabIndex = 4;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			this.toolStripSimRegistForm = new System.Windows.Forms.ToolStrip();
			this.toolButton1 = new System.Windows.Forms.ToolStripButton();
			this.toolButton2 = new System.Windows.Forms.ToolStripButton();
			this.toolButton3 = new System.Windows.Forms.ToolStripButton();
			this.toolButton4 = new System.Windows.Forms.ToolStripButton();
			this.toolButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolButton1.Name = "toolButton1";
			this.toolButton1.Size = new System.Drawing.Size(64, 28);
			this.toolButton1.Text = "Button1";
			this.toolButton1.Click += new System.EventHandler(this.toolButton1_Click);
			this.toolButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolButton2.Name = "toolButton2";
			this.toolButton2.Size = new System.Drawing.Size(64, 28);
			this.toolButton2.Text = "Button2";
			this.toolButton2.Click += new System.EventHandler(this.toolButton2_Click);
			this.toolButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolButton3.Name = "toolButton3";
			this.toolButton3.Size = new System.Drawing.Size(64, 28);
			this.toolButton3.Text = "Button3";
			this.toolButton3.Click += new System.EventHandler(this.toolButton3_Click);
			this.toolButton4.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolButton4.Name = "toolButton4";
			this.toolButton4.Size = new System.Drawing.Size(64, 28);
			this.toolButton4.Text = "Button4";
			this.toolButton4.Click += new System.EventHandler(this.toolButton4_Click);
			this.toolStripSimRegistForm.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.toolStripSimRegistForm.Location = new System.Drawing.Point(0, 0);
			this.toolStripSimRegistForm.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolButton1,
			this.toolButton2,
			this.toolButton3,
			this.toolButton4,
			});
			this.toolStripSimRegistForm.Name = "toolStripSimRegistForm";
			this.toolStripSimRegistForm.Size = new System.Drawing.Size(1955, 27);
			this.toolStripSimRegistForm.TabIndex = 5;
			this.toolStripSimRegistForm.Text = "toolStripSimRegistForm";
			this.Controls.Add(this.toolStripSimRegistForm);
			this.Name = "SimRegistForm";
			this.Text = "SimRegist";
			this.Load += new System.EventHandler(this.form_load);
			this.FormClosed += new  System.Windows.Forms.FormClosedEventHandler(this.form_close);
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1955, 450);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonCancel);
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
	}
}
