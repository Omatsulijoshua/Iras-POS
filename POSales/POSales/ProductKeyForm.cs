using System;
using System.Drawing;
using System.Windows.Forms;

namespace POSales
{
    public partial class ProductKeyForm : Form
    {
        public ProductKeyForm()
        {
            InitializeComponent();
            ModernUI.Apply(this);
            lblError.Visible = false;
        }

        private void btnActivate_Click(object sender, EventArgs e)
        {
            string errorMessage;
            if (LicenseManager.ActivateKey(txtProductKey.Text, out errorMessage))
            {
                string expDateStr = LicenseManager.ExpirationDate.HasValue 
                    ? LicenseManager.ExpirationDate.Value.ToString("yyyy-MM-dd") 
                    : "";
                
                MessageBox.Show("Product activated successfully!\r\nExpiration date: " + expDateStr, 
                                "Activation Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                Login login = new Login();
                login.Show();
                this.Hide();
            }
            else
            {
                lblError.Text = errorMessage;
                lblError.Visible = true;
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void picClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ProductKeyForm_Load(object sender, EventArgs e)
        {
            txtProductKey.Focus();
        }
    }
}
