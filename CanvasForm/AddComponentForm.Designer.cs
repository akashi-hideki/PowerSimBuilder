namespace PowerSimBuilder
{
    partial class AddComponentForm
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
            this.lblCategory = new System.Windows.Forms.Label();
            this.cmbCategory = new System.Windows.Forms.ComboBox();
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblType = new System.Windows.Forms.Label();
            this.txtType = new System.Windows.Forms.TextBox();
            this.lblColor = new System.Windows.Forms.Label();
            this.cmbColor = new System.Windows.Forms.ComboBox();
            this.lblParams = new System.Windows.Forms.Label();
            this.paramListBox = new System.Windows.Forms.ListBox();
            this.lblInputPorts = new System.Windows.Forms.Label();
            this.txtInputPorts = new System.Windows.Forms.TextBox();
            this.lblOutputPorts = new System.Windows.Forms.Label();
            this.txtOutputPorts = new System.Windows.Forms.TextBox();
            this.btnAddParam = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblCategory
            // 
            this.lblCategory.Location = new System.Drawing.Point(20, 20);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Size = new System.Drawing.Size(80, 20);
            this.lblCategory.TabIndex = 0;
            this.lblCategory.Text = "カテゴリ:";
            // 
            // cmbCategory
            // 
            this.cmbCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCategory.FormattingEnabled = true;
            this.cmbCategory.Location = new System.Drawing.Point(120, 20);
            this.cmbCategory.Name = "cmbCategory";
            this.cmbCategory.Size = new System.Drawing.Size(200, 21);
            this.cmbCategory.TabIndex = 1;
            // 
            // lblName
            // 
            this.lblName.Location = new System.Drawing.Point(20, 50);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(80, 20);
            this.lblName.TabIndex = 2;
            this.lblName.Text = "名前:";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(120, 50);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(200, 20);
            this.txtName.TabIndex = 3;
            // 
            // lblType
            // 
            this.lblType.Location = new System.Drawing.Point(20, 80);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(80, 20);
            this.lblType.TabIndex = 4;
            this.lblType.Text = "タイプ:";
            // 
            // txtType
            // 
            this.txtType.Location = new System.Drawing.Point(120, 80);
            this.txtType.Name = "txtType";
            this.txtType.Size = new System.Drawing.Size(200, 20);
            this.txtType.TabIndex = 5;
            // 
            // lblColor
            // 
            this.lblColor.Location = new System.Drawing.Point(20, 110);
            this.lblColor.Name = "lblColor";
            this.lblColor.Size = new System.Drawing.Size(80, 20);
            this.lblColor.TabIndex = 6;
            this.lblColor.Text = "アイコン色:";
            // 
            // cmbColor
            // 
            this.cmbColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbColor.FormattingEnabled = true;
            this.cmbColor.Location = new System.Drawing.Point(120, 110);
            this.cmbColor.Name = "cmbColor";
            this.cmbColor.Size = new System.Drawing.Size(200, 21);
            this.cmbColor.TabIndex = 7;
            // 
            // lblParams
            // 
            this.lblParams.Location = new System.Drawing.Point(20, 140);
            this.lblParams.Name = "lblParams";
            this.lblParams.Size = new System.Drawing.Size(80, 20);
            this.lblParams.TabIndex = 8;
            this.lblParams.Text = "パラメータ:";
            // 
            // paramListBox
            // 
            this.paramListBox.FormattingEnabled = true;
            this.paramListBox.Location = new System.Drawing.Point(120, 140);
            this.paramListBox.Name = "paramListBox";
            this.paramListBox.Size = new System.Drawing.Size(200, 95);
            this.paramListBox.TabIndex = 9;
            // 
            // lblInputPorts
            // 
            this.lblInputPorts.Location = new System.Drawing.Point(20, 250);
            this.lblInputPorts.Name = "lblInputPorts";
            this.lblInputPorts.Size = new System.Drawing.Size(80, 20);
            this.lblInputPorts.TabIndex = 10;
            this.lblInputPorts.Text = "入力ポート:";
            // 
            // txtInputPorts
            // 
            this.txtInputPorts.Location = new System.Drawing.Point(120, 250);
            this.txtInputPorts.Name = "txtInputPorts";
            this.txtInputPorts.Size = new System.Drawing.Size(200, 20);
            this.txtInputPorts.TabIndex = 11;
            // 
            // lblOutputPorts
            // 
            this.lblOutputPorts.Location = new System.Drawing.Point(20, 280);
            this.lblOutputPorts.Name = "lblOutputPorts";
            this.lblOutputPorts.Size = new System.Drawing.Size(80, 20);
            this.lblOutputPorts.TabIndex = 12;
            this.lblOutputPorts.Text = "出力ポート:";
            // 
            // txtOutputPorts
            // 
            this.txtOutputPorts.Location = new System.Drawing.Point(120, 280);
            this.txtOutputPorts.Name = "txtOutputPorts";
            this.txtOutputPorts.Size = new System.Drawing.Size(200, 20);
            this.txtOutputPorts.TabIndex = 13;
            // 
            // btnAddParam
            // 
            this.btnAddParam.Location = new System.Drawing.Point(330, 140);
            this.btnAddParam.Name = "btnAddParam";
            this.btnAddParam.Size = new System.Drawing.Size(120, 30);
            this.btnAddParam.TabIndex = 14;
            this.btnAddParam.Text = "パラメータ追加";
            this.btnAddParam.UseVisualStyleBackColor = true;
            this.btnAddParam.Click += new System.EventHandler(this.btnAddParam_Click);
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(120, 320);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(80, 30);
            this.btnOK.TabIndex = 15;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(220, 320);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 30);
            this.btnCancel.TabIndex = 16;
            this.btnCancel.Text = "キャンセル";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // AddComponentForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 400);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnAddParam);
            this.Controls.Add(this.txtOutputPorts);
            this.Controls.Add(this.lblOutputPorts);
            this.Controls.Add(this.txtInputPorts);
            this.Controls.Add(this.lblInputPorts);
            this.Controls.Add(this.paramListBox);
            this.Controls.Add(this.lblParams);
            this.Controls.Add(this.cmbColor);
            this.Controls.Add(this.lblColor);
            this.Controls.Add(this.txtType);
            this.Controls.Add(this.lblType);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.cmbCategory);
            this.Controls.Add(this.lblCategory);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddComponentForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "カスタムコンポーネント追加";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblCategory;
        private System.Windows.Forms.ComboBox cmbCategory;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.TextBox txtType;
        private System.Windows.Forms.Label lblColor;
        private System.Windows.Forms.ComboBox cmbColor;
        private System.Windows.Forms.Label lblParams;
        private System.Windows.Forms.ListBox paramListBox;
        private System.Windows.Forms.Label lblInputPorts;
        private System.Windows.Forms.TextBox txtInputPorts;
        private System.Windows.Forms.Label lblOutputPorts;
        private System.Windows.Forms.TextBox txtOutputPorts;
        private System.Windows.Forms.Button btnAddParam;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}