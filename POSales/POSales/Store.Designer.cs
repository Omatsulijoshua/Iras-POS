
namespace POSales
{
    partial class Store
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Store));
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtStName = new System.Windows.Forms.TextBox();
            this.txtAddress = new System.Windows.Forms.TextBox();
            this.lblVatType = new System.Windows.Forms.Label();
            this.cboVatType = new System.Windows.Forms.ComboBox();
            this.lblVatPercent = new System.Windows.Forms.Label();
            this.txtVatPercent = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.chkSpecialNote = new System.Windows.Forms.CheckBox();
            this.lblLogo = new System.Windows.Forms.Label();
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.btnUpload = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(70)))), ((int)(((byte)(160)))));
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 171);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(708, 50);
            this.panel1.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.Font = new System.Drawing.Font("Century Gothic", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Image = ((System.Drawing.Image)(resources.GetObject("label1.Image")));
            this.label1.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label1.Location = new System.Drawing.Point(7, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(89, 43);
            this.label1.TabIndex = 0;
            this.label1.Text = "Store Detail";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(50, 38);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(102, 20);
            this.label2.TabIndex = 6;
            this.label2.Text = "Store Name :";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(50, 77);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(75, 20);
            this.label3.TabIndex = 6;
            this.label3.Text = "Address :";
            // 
            // txtStName
            // 
            this.txtStName.Location = new System.Drawing.Point(169, 35);
            this.txtStName.Name = "txtStName";
            this.txtStName.Size = new System.Drawing.Size(473, 26);
            this.txtStName.TabIndex = 7;
            // 
            // txtAddress
            // 
            this.txtAddress.Location = new System.Drawing.Point(169, 74);
            this.txtAddress.Name = "txtAddress";
            this.txtAddress.Size = new System.Drawing.Size(473, 26);
            this.txtAddress.TabIndex = 7;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(70)))), ((int)(((byte)(160)))));
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(437, 255);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(94, 35);
            this.btnSave.TabIndex = 31;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.Gainsboro;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.ForeColor = System.Drawing.Color.Black;
            this.btnCancel.Location = new System.Drawing.Point(548, 255);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(94, 35);
            this.btnCancel.TabIndex = 32;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // chkSpecialNote
            // 
            this.chkSpecialNote.AutoSize = true;
            this.chkSpecialNote.Location = new System.Drawing.Point(169, 155);
            this.chkSpecialNote.Name = "chkSpecialNote";
            this.chkSpecialNote.Size = new System.Drawing.Size(262, 24);
            this.chkSpecialNote.TabIndex = 37;
            this.chkSpecialNote.Text = "Enable Special Notes on Receipt";
            this.chkSpecialNote.UseVisualStyleBackColor = true;
            // 
            // lblVatType
            // 
            this.lblVatType.AutoSize = true;
            this.lblVatType.Location = new System.Drawing.Point(50, 116);
            this.lblVatType.Name = "lblVatType";
            this.lblVatType.Size = new System.Drawing.Size(84, 20);
            this.lblVatType.TabIndex = 33;
            this.lblVatType.Text = "VAT Type :";
            // 
            // cboVatType
            // 
            this.cboVatType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboVatType.FormattingEnabled = true;
            this.cboVatType.Items.AddRange(new object[] {
            "Old",
            "New"});
            this.cboVatType.Location = new System.Drawing.Point(169, 113);
            this.cboVatType.Name = "cboVatType";
            this.cboVatType.Size = new System.Drawing.Size(180, 28);
            this.cboVatType.TabIndex = 34;
            // 
            // lblVatPercent
            // 
            this.lblVatPercent.AutoSize = true;
            this.lblVatPercent.Location = new System.Drawing.Point(390, 116);
            this.lblVatPercent.Name = "lblVatPercent";
            this.lblVatPercent.Size = new System.Drawing.Size(127, 20);
            this.lblVatPercent.TabIndex = 35;
            this.lblVatPercent.Text = "VAT Percent (%) :";
            // 
            // txtVatPercent
            // 
            this.txtVatPercent.Location = new System.Drawing.Point(520, 113);
            this.txtVatPercent.Name = "txtVatPercent";
            this.txtVatPercent.Size = new System.Drawing.Size(122, 26);
            this.txtVatPercent.TabIndex = 36;
            // 
            // lblLogo
            // 
            this.lblLogo.AutoSize = true;
            this.lblLogo.Location = new System.Drawing.Point(50, 195);
            this.lblLogo.Name = "lblLogo";
            this.lblLogo.Size = new System.Drawing.Size(97, 20);
            this.lblLogo.TabIndex = 38;
            this.lblLogo.Text = "Store Logo :";
            // 
            // picLogo
            // 
            this.picLogo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picLogo.Location = new System.Drawing.Point(169, 195);
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size(100, 80);
            this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picLogo.TabIndex = 39;
            this.picLogo.TabStop = false;
            // 
            // btnUpload
            // 
            this.btnUpload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(70)))), ((int)(((byte)(160)))));
            this.btnUpload.FlatAppearance.BorderSize = 0;
            this.btnUpload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUpload.ForeColor = System.Drawing.Color.White;
            this.btnUpload.Location = new System.Drawing.Point(285, 195);
            this.btnUpload.Name = "btnUpload";
            this.btnUpload.Size = new System.Drawing.Size(94, 35);
            this.btnUpload.TabIndex = 40;
            this.btnUpload.Text = "Upload";
            this.btnUpload.UseVisualStyleBackColor = false;
            this.btnUpload.Click += new System.EventHandler(this.btnUpload_Click);
            // 
            // btnClear
            // 
            this.btnClear.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(70)))), ((int)(((byte)(160)))));
            this.btnClear.FlatAppearance.BorderSize = 0;
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.ForeColor = System.Drawing.Color.White;
            this.btnClear.Location = new System.Drawing.Point(395, 195);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(94, 35);
            this.btnClear.TabIndex = 41;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = false;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // Store
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(708, 360);
            this.ControlBox = false;
            this.Controls.Add(this.lblLogo);
            this.Controls.Add(this.picLogo);
            this.Controls.Add(this.btnUpload);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.txtStName);
            this.Controls.Add(this.txtAddress);
            this.Controls.Add(this.lblVatType);
            this.Controls.Add(this.cboVatType);
            this.Controls.Add(this.lblVatPercent);
            this.Controls.Add(this.txtVatPercent);
            this.Controls.Add(this.chkSpecialNote);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Century Gothic", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "Store";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "STORE";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Store_KeyDown);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtStName;
        private System.Windows.Forms.TextBox txtAddress;
        private System.Windows.Forms.Label lblVatType;
        private System.Windows.Forms.Label lblVatPercent;
        private System.Windows.Forms.ComboBox cboVatType;
        private System.Windows.Forms.TextBox txtVatPercent;
        public System.Windows.Forms.Button btnSave;
        public System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox chkSpecialNote;
        private System.Windows.Forms.Label lblLogo;
        private System.Windows.Forms.PictureBox picLogo;
        public System.Windows.Forms.Button btnUpload;
        public System.Windows.Forms.Button btnClear;
    }
}