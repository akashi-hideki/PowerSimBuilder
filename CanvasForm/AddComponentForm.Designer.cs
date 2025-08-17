namespace PowerSimBuilder
{
    partial class AddComponentForm
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
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblType = new System.Windows.Forms.Label();
            this.txtType = new System.Windows.Forms.TextBox();
            this.lblCategory = new System.Windows.Forms.Label();
            this.txtCategory = new System.Windows.Forms.TextBox();
            this.btnAddParam = new System.Windows.Forms.Button();
            this.paramListBox = new System.Windows.Forms.ListBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnEditParam = new System.Windows.Forms.Button();
            this.btnDeleteParam = new System.Windows.Forms.Button();
            this.lblInputPorts = new System.Windows.Forms.Label();
            this.inputPortsGrid = new System.Windows.Forms.DataGridView();
            this.lblOutputPorts = new System.Windows.Forms.Label();
            this.outputPortsGrid = new System.Windows.Forms.DataGridView();
            this.btnAddInputPort = new System.Windows.Forms.Button();
            this.btnRemoveInputPort = new System.Windows.Forms.Button();
            this.btnAddOutputPort = new System.Windows.Forms.Button();
            this.btnRemoveOutputPort = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.inputPortsGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.outputPortsGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.Location = new System.Drawing.Point(20, 20);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(80, 20);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "名前:";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(110, 20);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(250, 20);
            this.txtName.TabIndex = 1;
            // 
            // lblType
            // 
            this.lblType.Location = new System.Drawing.Point(20, 50);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(80, 20);
            this.lblType.TabIndex = 2;
            this.lblType.Text = "タイプ:";
            // 
            // txtType
            // 
            this.txtType.Location = new System.Drawing.Point(110, 50);
            this.txtType.Name = "txtType";
            this.txtType.Size = new System.Drawing.Size(250, 20);
            this.txtType.TabIndex = 3;
            // 
            // lblCategory
            // 
            this.lblCategory.Location = new System.Drawing.Point(20, 80);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Size = new System.Drawing.Size(80, 20);
            this.lblCategory.TabIndex = 4;
            this.lblCategory.Text = "カテゴリ:";
            // 
            // txtCategory
            // 
            this.txtCategory.Location = new System.Drawing.Point(110, 80);
            this.txtCategory.Name = "txtCategory";
            this.txtCategory.Size = new System.Drawing.Size(250, 20);
            this.txtCategory.TabIndex = 5;
            // 
            // btnAddParam
            // 
            this.btnAddParam.Location = new System.Drawing.Point(20, 320);
            this.btnAddParam.Name = "btnAddParam";
            this.btnAddParam.Size = new System.Drawing.Size(100, 30);
            this.btnAddParam.TabIndex = 6;
            this.btnAddParam.Text = "追加";
            this.btnAddParam.UseVisualStyleBackColor = true;
            this.btnAddParam.Click += new System.EventHandler(this.BtnAddParam_Click);
            // 
            // paramListBox
            // 
            this.paramListBox.FormattingEnabled = true;
            this.paramListBox.Location = new System.Drawing.Point(20, 360);
            this.paramListBox.Name = "paramListBox";
            this.paramListBox.Size = new System.Drawing.Size(340, 95);
            this.paramListBox.TabIndex = 7;
            this.paramListBox.DoubleClick += new System.EventHandler(this.paramListBox_DoubleClick);
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(150, 460);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(80, 30);
            this.btnOK.TabIndex = 8;
            this.btnOK.Text = "追加";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(250, 460);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 30);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "キャンセル";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnEditParam
            // 
            this.btnEditParam.Location = new System.Drawing.Point(130, 320);
            this.btnEditParam.Name = "btnEditParam";
            this.btnEditParam.Size = new System.Drawing.Size(100, 30);
            this.btnEditParam.TabIndex = 14;
            this.btnEditParam.Text = "編集";
            this.btnEditParam.UseVisualStyleBackColor = true;
            this.btnEditParam.Click += new System.EventHandler(this.BtnEditParam_Click);
            // 
            // btnDeleteParam
            // 
            this.btnDeleteParam.Location = new System.Drawing.Point(240, 320);
            this.btnDeleteParam.Name = "btnDeleteParam";
            this.btnDeleteParam.Size = new System.Drawing.Size(100, 30);
            this.btnDeleteParam.TabIndex = 15;
            this.btnDeleteParam.Text = "削除";
            this.btnDeleteParam.UseVisualStyleBackColor = true;
            this.btnDeleteParam.Click += new System.EventHandler(this.BtnDeleteParam_Click);
            // 
            // lblInputPorts
            // 
            this.lblInputPorts.Location = new System.Drawing.Point(20, 110);
            this.lblInputPorts.Name = "lblInputPorts";
            this.lblInputPorts.Size = new System.Drawing.Size(80, 20);
            this.lblInputPorts.TabIndex = 10;
            this.lblInputPorts.Text = "入力ポート:";
            // 
            // inputPortsGrid
            // 
            this.inputPortsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.inputPortsGrid.Location = new System.Drawing.Point(110, 110);
            this.inputPortsGrid.Name = "inputPortsGrid";
            this.inputPortsGrid.RowHeadersVisible = false;
            this.inputPortsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.inputPortsGrid.Size = new System.Drawing.Size(200, 80);
            this.inputPortsGrid.TabIndex = 16;
            // 
            // lblOutputPorts
            // 
            this.lblOutputPorts.Location = new System.Drawing.Point(20, 200);
            this.lblOutputPorts.Name = "lblOutputPorts";
            this.lblOutputPorts.Size = new System.Drawing.Size(80, 20);
            this.lblOutputPorts.TabIndex = 12;
            this.lblOutputPorts.Text = "出力ポート:";
            // 
            // outputPortsGrid
            // 
            this.outputPortsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.outputPortsGrid.Location = new System.Drawing.Point(110, 200);
            this.outputPortsGrid.Name = "outputPortsGrid";
            this.outputPortsGrid.RowHeadersVisible = false;
            this.outputPortsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.outputPortsGrid.Size = new System.Drawing.Size(200, 80);
            this.outputPortsGrid.TabIndex = 17;
            // 
            // btnAddInputPort
            // 
            this.btnAddInputPort.Location = new System.Drawing.Point(320, 110);
            this.btnAddInputPort.Name = "btnAddInputPort";
            this.btnAddInputPort.Size = new System.Drawing.Size(40, 30);
            this.btnAddInputPort.TabIndex = 18;
            this.btnAddInputPort.Text = "+";
            this.btnAddInputPort.UseVisualStyleBackColor = true;
            this.btnAddInputPort.Click += new System.EventHandler(this.btnAddInputPort_Click);
            // 
            // btnRemoveInputPort
            // 
            this.btnRemoveInputPort.Location = new System.Drawing.Point(320, 150);
            this.btnRemoveInputPort.Name = "btnRemoveInputPort";
            this.btnRemoveInputPort.Size = new System.Drawing.Size(40, 30);
            this.btnRemoveInputPort.TabIndex = 19;
            this.btnRemoveInputPort.Text = "-";
            this.btnRemoveInputPort.UseVisualStyleBackColor = true;
            this.btnRemoveInputPort.Click += new System.EventHandler(this.btnRemoveInputPort_Click);
            // 
            // btnAddOutputPort
            // 
            this.btnAddOutputPort.Location = new System.Drawing.Point(320, 200);
            this.btnAddOutputPort.Name = "btnAddOutputPort";
            this.btnAddOutputPort.Size = new System.Drawing.Size(40, 30);
            this.btnAddOutputPort.TabIndex = 20;
            this.btnAddOutputPort.Text = "+";
            this.btnAddOutputPort.UseVisualStyleBackColor = true;
            this.btnAddOutputPort.Click += new System.EventHandler(this.btnAddOutputPort_Click);
            // 
            // btnRemoveOutputPort
            // 
            this.btnRemoveOutputPort.Location = new System.Drawing.Point(320, 240);
            this.btnRemoveOutputPort.Name = "btnRemoveOutputPort";
            this.btnRemoveOutputPort.Size = new System.Drawing.Size(40, 30);
            this.btnRemoveOutputPort.TabIndex = 21;
            this.btnRemoveOutputPort.Text = "-";
            this.btnRemoveOutputPort.UseVisualStyleBackColor = true;
            this.btnRemoveOutputPort.Click += new System.EventHandler(this.btnRemoveOutputPort_Click);
            // 
            // AddComponentForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 500);
            this.Controls.Add(this.btnRemoveOutputPort);
            this.Controls.Add(this.btnAddOutputPort);
            this.Controls.Add(this.btnRemoveInputPort);
            this.Controls.Add(this.btnAddInputPort);
            this.Controls.Add(this.outputPortsGrid);
            this.Controls.Add(this.lblOutputPorts);
            this.Controls.Add(this.inputPortsGrid);
            this.Controls.Add(this.lblInputPorts);
            this.Controls.Add(this.btnDeleteParam);
            this.Controls.Add(this.btnEditParam);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.paramListBox);
            this.Controls.Add(this.btnAddParam);
            this.Controls.Add(this.txtCategory);
            this.Controls.Add(this.lblCategory);
            this.Controls.Add(this.txtType);
            this.Controls.Add(this.lblType);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lblName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddComponentForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "コンポーネントの追加";
            ((System.ComponentModel.ISupportInitialize)(this.inputPortsGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.outputPortsGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.TextBox txtType;
        private System.Windows.Forms.Label lblCategory;
        private System.Windows.Forms.TextBox txtCategory;
        private System.Windows.Forms.Button btnAddParam;
        private System.Windows.Forms.ListBox paramListBox;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnEditParam;
        private System.Windows.Forms.Button btnDeleteParam;
        private System.Windows.Forms.Label lblInputPorts;
        private System.Windows.Forms.DataGridView inputPortsGrid;
        private System.Windows.Forms.Label lblOutputPorts;
        private System.Windows.Forms.DataGridView outputPortsGrid;
        private System.Windows.Forms.Button btnAddInputPort;
        private System.Windows.Forms.Button btnRemoveInputPort;
        private System.Windows.Forms.Button btnAddOutputPort;
        private System.Windows.Forms.Button btnRemoveOutputPort;
    }
}