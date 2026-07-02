using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace POSales
{
    public partial class Store : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        SqlDataReader dr;
        bool havestoreinfo = false;
        CheckBox chkPrintInvoice;
        CheckBox chkUnsettledPayment;
        CheckBox chkCloudSync;
        CheckBox chkCloudUnsettled;
        Label lblCloudUrl;
        TextBox txtCloudUrl;
        public Store()
        {
            InitializeComponent();
            AddCloudAndInvoiceSettings();
            ModernUI.Apply(this);
            cn = new SqlConnection(dbcon.myConnection());
            LoadStore();
        }

        private void AddCloudAndInvoiceSettings()
        {
            chkPrintInvoice = new CheckBox();
            chkPrintInvoice.AutoSize = true;
            chkPrintInvoice.Location = new Point(169, 285);
            chkPrintInvoice.Name = "chkPrintInvoice";
            chkPrintInvoice.Size = new Size(260, 24);
            chkPrintInvoice.Text = "Enable Print Invoice Before Payment";
            chkPrintInvoice.UseVisualStyleBackColor = true;
            Controls.Add(chkPrintInvoice);

            chkUnsettledPayment = new CheckBox();
            chkUnsettledPayment.AutoSize = true;
            chkUnsettledPayment.Location = new Point(450, 285);
            chkUnsettledPayment.Name = "chkUnsettledPayment";
            chkUnsettledPayment.Size = new Size(205, 24);
            chkUnsettledPayment.Text = "Enable Unsettled Payment";
            chkUnsettledPayment.UseVisualStyleBackColor = true;
            Controls.Add(chkUnsettledPayment);

            chkCloudSync = new CheckBox();
            chkCloudSync.AutoSize = true;
            chkCloudSync.Location = new Point(169, 315);
            chkCloudSync.Name = "chkCloudSync";
            chkCloudSync.Size = new Size(190, 24);
            chkCloudSync.Text = "Enable Save to Cloud";
            chkCloudSync.UseVisualStyleBackColor = true;
            Controls.Add(chkCloudSync);

            chkCloudUnsettled = new CheckBox();
            chkCloudUnsettled.AutoSize = true;
            chkCloudUnsettled.Location = new Point(385, 315);
            chkCloudUnsettled.Name = "chkCloudUnsettled";
            chkCloudUnsettled.Size = new Size(215, 24);
            chkCloudUnsettled.Text = "Include Unsettled Payments";
            chkCloudUnsettled.UseVisualStyleBackColor = true;
            Controls.Add(chkCloudUnsettled);

            lblCloudUrl = new Label();
            lblCloudUrl.AutoSize = true;
            lblCloudUrl.Location = new Point(50, 352);
            lblCloudUrl.Name = "lblCloudUrl";
            lblCloudUrl.Size = new Size(88, 20);
            lblCloudUrl.Text = "Cloud URL :";
            Controls.Add(lblCloudUrl);

            txtCloudUrl = new TextBox();
            txtCloudUrl.Location = new Point(169, 349);
            txtCloudUrl.Name = "txtCloudUrl";
            txtCloudUrl.Size = new Size(473, 26);
            txtCloudUrl.Text = "http://localhost:3000/api/upload";
            Controls.Add(txtCloudUrl);

            ClientSize = new Size(ClientSize.Width, 465);
            panel1.Location = new Point(0, 416);
            btnSave.Location = new Point(437, 385);
            btnCancel.Location = new Point(548, 385);
        }

        public void LoadStore()
        {
            try
            {
                cn.Open();
                cm = new SqlCommand("SELECT * FROM tbStore", cn);
                dr = cm.ExecuteReader();
                if (dr.Read())
                {
                    havestoreinfo = true;
                    txtStName.Text = dr["store"].ToString();
                    txtAddress.Text = dr["address"].ToString();
                    cboVatType.Text = dr["vat_type"] != DBNull.Value && !string.IsNullOrEmpty(dr["vat_type"].ToString()) ? dr["vat_type"].ToString() : "Old";
                    txtVatPercent.Text = dr["vat_percent"] != DBNull.Value ? Convert.ToDouble(dr["vat_percent"]).ToString("0.00") : "12.00";
                    chkSpecialNote.Checked = dr["special_note_enabled"] != DBNull.Value ? Convert.ToBoolean(dr["special_note_enabled"]) : false;
                    chkPrintInvoice.Checked = dr["print_invoice_enabled"] != DBNull.Value ? Convert.ToBoolean(dr["print_invoice_enabled"]) : true;
                    chkUnsettledPayment.Checked = dr["unsettled_payment_enabled"] != DBNull.Value ? Convert.ToBoolean(dr["unsettled_payment_enabled"]) : true;
                    chkCloudSync.Checked = dr["cloud_sync_enabled"] != DBNull.Value ? Convert.ToBoolean(dr["cloud_sync_enabled"]) : false;
                    chkCloudUnsettled.Checked = dr["cloud_include_unsettled"] != DBNull.Value ? Convert.ToBoolean(dr["cloud_include_unsettled"]) : true;
                    txtCloudUrl.Text = dr["cloud_url"] != DBNull.Value && !string.IsNullOrWhiteSpace(dr["cloud_url"].ToString()) ? dr["cloud_url"].ToString() : "http://localhost:3000/api/upload";
                    
                    if (dr["logo"] != DBNull.Value)
                    {
                        byte[] logoBytes = (byte[])dr["logo"];
                        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(logoBytes))
                        {
                            picLogo.Image = Image.FromStream(ms);
                        }
                    }
                    else
                    {
                        picLogo.Image = null;
                    }
                }
                else
                {
                    txtStName.Clear();
                    txtAddress.Clear();
                    cboVatType.SelectedIndex = 0;
                    txtVatPercent.Text = "12.00";
                    chkSpecialNote.Checked = false;
                    chkPrintInvoice.Checked = true;
                    chkUnsettledPayment.Checked = true;
                    chkCloudSync.Checked = false;
                    chkCloudUnsettled.Checked = true;
                    txtCloudUrl.Text = "http://localhost:3000/api/upload";
                    picLogo.Image = null;
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error");
            }
            
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Save store details?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    double vatPercent = 12.00;
                    double.TryParse(txtVatPercent.Text, out vatPercent);

                    byte[] logoBytes = null;
                    if (picLogo.Image != null)
                    {
                        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                        {
                            using (Bitmap bmp = new Bitmap(picLogo.Image))
                            {
                                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            }
                            logoBytes = ms.ToArray();
                        }
                    }

                    cn.Open();
                    if(havestoreinfo)
                    {
                        cm = new SqlCommand("UPDATE tbStore SET store = @store, address = @address, vat_type = @vat_type, vat_percent = @vat_percent, special_note_enabled = @special_note_enabled, logo = @logo, print_invoice_enabled = @print_invoice_enabled, unsettled_payment_enabled = @unsettled_payment_enabled, cloud_sync_enabled = @cloud_sync_enabled, cloud_include_unsettled = @cloud_include_unsettled, cloud_url = @cloud_url", cn);
                    }
                    else
                    {
                        cm = new SqlCommand("INSERT INTO tbStore (store, address, vat_type, vat_percent, special_note_enabled, logo, print_invoice_enabled, unsettled_payment_enabled, cloud_sync_enabled, cloud_include_unsettled, cloud_url) VALUES (@store, @address, @vat_type, @vat_percent, @special_note_enabled, @logo, @print_invoice_enabled, @unsettled_payment_enabled, @cloud_sync_enabled, @cloud_include_unsettled, @cloud_url)", cn);
                    }
                    cm.Parameters.AddWithValue("@store", txtStName.Text);
                    cm.Parameters.AddWithValue("@address", txtAddress.Text);
                    cm.Parameters.AddWithValue("@vat_type", cboVatType.Text);
                    cm.Parameters.AddWithValue("@vat_percent", vatPercent);
                    cm.Parameters.AddWithValue("@special_note_enabled", chkSpecialNote.Checked);
                    cm.Parameters.AddWithValue("@print_invoice_enabled", chkPrintInvoice.Checked);
                    cm.Parameters.AddWithValue("@unsettled_payment_enabled", chkUnsettledPayment.Checked);
                    cm.Parameters.AddWithValue("@cloud_sync_enabled", chkCloudSync.Checked);
                    cm.Parameters.AddWithValue("@cloud_include_unsettled", chkCloudUnsettled.Checked);
                    cm.Parameters.AddWithValue("@cloud_url", txtCloudUrl.Text.Trim());
                    if (logoBytes != null)
                    {
                        cm.Parameters.AddWithValue("@logo", logoBytes);
                    }
                    else
                    {
                        cm.Parameters.Add("@logo", SqlDbType.VarBinary).Value = DBNull.Value;
                    }
                    cm.ExecuteNonQuery();
                    cn.Close();

                    // Refresh branding cache and open forms branding
                    ModernUI.RefreshBranding();

                    MessageBox.Show("Store detail has been successfully saved!", "Save Record", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Dispose();
                }
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        picLogo.Image = Image.FromFile(ofd.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            picLogo.Image = null;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void Store_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode==Keys.Escape)
            { this.Dispose(); }
        }
    }
}
