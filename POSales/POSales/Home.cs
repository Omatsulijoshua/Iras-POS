using POSales;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Quiz_App
{
    public partial class Home : Form
    {
        public Home()
        {
            InitializeComponent();
            ModernUI.Apply(this);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            panel1.Width += 3;
            if(panel1.Width >= 850)
            {
                timer1.Stop();
                string reason;
                if (POSales.LicenseManager.IsLicenseValid(out reason))
                {
                    Login f = new Login();
                    f.Show();
                }
                else
                {
                    ProductKeyForm f = new ProductKeyForm();
                    f.Show();
                }
                this.Hide();
            }
        }

        private void Home_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
