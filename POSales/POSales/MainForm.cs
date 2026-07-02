using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
namespace POSales
{
    public partial class MainForm : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        SqlDataReader dr;
        Button btnStockRecords;
        Button btnSaveCloud;
        ProgressBar cloudProgress;
        Label lblCloudStatus;
        Timer cloudSyncTimer;
        bool cloudSyncRunning;
        public string _pass;
        public MainForm()
        {
            InitializeComponent();
            AddCloudMenu();
            ModernUI.Apply(this);
            AddStockRecordsMenu();
            customizeDesing();
            cn = new SqlConnection(dbcon.myConnection());            
            StartCloudSyncTimer();
        }


        #region panelSlide
        private void customizeDesing()
        {
            panelSubProduct.Visible = false;
            panelSubRecord.Visible = false;
            panelSubStock.Visible = false;
            panelSubSetting.Visible = false;
        }

        private void AddStockRecordsMenu()
        {
            if (btnStockRecords != null)
                return;

            btnStockRecords = new Button();
            btnStockRecords.Dock = DockStyle.Top;
            btnStockRecords.FlatAppearance.BorderSize = 0;
            btnStockRecords.FlatStyle = FlatStyle.Flat;
            btnStockRecords.ForeColor = Color.White;
            btnStockRecords.Location = new Point(0, 90);
            btnStockRecords.Name = "btnStockRecords";
            btnStockRecords.Padding = new Padding(35, 0, 0, 0);
            btnStockRecords.Size = new Size(panelSubRecord.Width, 45);
            btnStockRecords.TabIndex = 6;
            btnStockRecords.Text = "Stock Records";
            btnStockRecords.TextAlign = ContentAlignment.MiddleLeft;
            btnStockRecords.UseVisualStyleBackColor = true;
            btnStockRecords.Click += btnStockRecords_Click;

            panelSubRecord.Controls.Add(btnStockRecords);
            panelSubRecord.Controls.SetChildIndex(btnSaleHist, 0);
            panelSubRecord.Controls.SetChildIndex(btnPosRecord, 1);
            panelSubRecord.Controls.SetChildIndex(btnStockRecords, 2);
            panelSubRecord.Height = 135;
        }

        private void AddCloudMenu()
        {
            if (btnSaveCloud != null)
                return;

            btnSaveCloud = new Button();
            btnSaveCloud.Dock = DockStyle.Top;
            btnSaveCloud.FlatAppearance.BorderSize = 0;
            btnSaveCloud.FlatStyle = FlatStyle.Flat;
            btnSaveCloud.ForeColor = Color.White;
            btnSaveCloud.Name = "btnSaveCloud";
            btnSaveCloud.Padding = new Padding(5, 0, 0, 0);
            btnSaveCloud.Size = new Size(183, 45);
            btnSaveCloud.TabIndex = 10;
            btnSaveCloud.Text = "Save to Cloud";
            btnSaveCloud.TextAlign = ContentAlignment.MiddleLeft;
            btnSaveCloud.UseVisualStyleBackColor = true;
            btnSaveCloud.Click += btnSaveCloud_Click;

            cloudProgress = new ProgressBar();
            cloudProgress.Dock = DockStyle.Top;
            cloudProgress.Height = 14;
            cloudProgress.Visible = false;

            lblCloudStatus = new Label();
            lblCloudStatus.Dock = DockStyle.Top;
            lblCloudStatus.Height = 38;
            lblCloudStatus.ForeColor = Color.White;
            lblCloudStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblCloudStatus.Visible = false;

            panelSlide.Controls.Add(lblCloudStatus);
            panelSlide.Controls.Add(cloudProgress);
            panelSlide.Controls.Add(btnSaveCloud);
            panelSlide.Controls.SetChildIndex(btnSaveCloud, panelSlide.Controls.GetChildIndex(btnSetting));
            panelSlide.Controls.SetChildIndex(cloudProgress, panelSlide.Controls.GetChildIndex(btnSaveCloud) + 1);
            panelSlide.Controls.SetChildIndex(lblCloudStatus, panelSlide.Controls.GetChildIndex(cloudProgress) + 1);
        }

        private void StartCloudSyncTimer()
        {
            cloudSyncTimer = new Timer();
            cloudSyncTimer.Interval = 30 * 60 * 1000;
            cloudSyncTimer.Tick += async (s, e) => await SaveToCloudAsync(false);
            cloudSyncTimer.Start();
        }

        private void hideSubmenu()
        {
            if (panelSubProduct.Visible == true)
                panelSubProduct.Visible = false;
            if (panelSubRecord.Visible == true)
                panelSubRecord.Visible = false;
            if (panelSubSetting.Visible == true)
                panelSubSetting.Visible = false;
            if (panelSubStock.Visible == true)
                panelSubStock.Visible = false;
        }

        private void showSubmenu(Panel submenu)
        {
            if (submenu.Visible == false)
            {
                hideSubmenu();
                submenu.Visible = true;
            }
            else
                submenu.Visible = false;
        }
        #endregion panelSlide

        private Form activeForm = null;
        public void openChildForm(Form childForm)
        {
            if (activeForm != null)
                activeForm.Close();
            activeForm = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            lblTitle.Text = childForm.Text;
            panelMain.Controls.Add(childForm);
            panelMain.Tag = childForm;
            ModernUI.Apply(childForm);
            childForm.BringToFront();
            childForm.Show();
        }
        private void btnDashboard_Click(object sender, EventArgs e)
        {
            openChildForm(new Dashboard());
            hideSubmenu();
        }

        private void btnProduct_Click(object sender, EventArgs e)
        {
            showSubmenu(panelSubProduct);
        }

        private void btnProductList_Click(object sender, EventArgs e)
        {
            openChildForm(new Product());
            hideSubmenu();
        }

        private void btnCategory_Click(object sender, EventArgs e)
        {
            openChildForm(new Category());
            hideSubmenu();
        }

        private void btnBrand_Click(object sender, EventArgs e)
        {
            openChildForm(new Brand());
            hideSubmenu();
        }

        private void btnInStock_Click(object sender, EventArgs e)
        {
            showSubmenu(panelSubStock);
        }

        private void btnStockEntry_Click(object sender, EventArgs e)
        {
            openChildForm(new StockIn(this));
            hideSubmenu();
        }

        private void btnStockAdjustment_Click(object sender, EventArgs e)
        {
            openChildForm(new Adjustments(this));
            hideSubmenu();
        }

        private void btnSupplier_Click(object sender, EventArgs e)
        {
            openChildForm(new Supplier());
            hideSubmenu();
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            showSubmenu(panelSubRecord);
        }

        private void btnSaleHist_Click(object sender, EventArgs e)
        {           
            openChildForm(new DailySale(this));            
            hideSubmenu();
        }

        private void btnPosRecord_Click(object sender, EventArgs e)
        {
            openChildForm(new Record());
            hideSubmenu();
        }

        private void btnStockRecords_Click(object sender, EventArgs e)
        {
            openChildForm(new StockRecords());
            hideSubmenu();
        }

        private void btnSetting_Click(object sender, EventArgs e)
        {
            showSubmenu(panelSubSetting);
        }

        private async void btnSaveCloud_Click(object sender, EventArgs e)
        {
            hideSubmenu();
            await SaveToCloudAsync(true);
        }

        private async Task SaveToCloudAsync(bool showMessage)
        {
            if (cloudSyncRunning)
                return;

            cloudSyncRunning = true;
            btnSaveCloud.Enabled = false;
            cloudProgress.Value = 0;
            cloudProgress.Visible = true;
            lblCloudStatus.Text = "Saving reports...";
            lblCloudStatus.Visible = true;

            Progress<int> progress = new Progress<int>(value =>
            {
                cloudProgress.Value = Math.Max(0, Math.Min(100, value));
            });

            CloudSyncResult result = await new CloudSyncService().UploadAsync(lblUsername.Text, progress);
            lblCloudStatus.Text = result.Message;
            btnSaveCloud.Enabled = true;
            cloudSyncRunning = false;

            if (showMessage)
            {
                MessageBox.Show(result.Message, "Save to Cloud", MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
        }

        private void btnUser_Click(object sender, EventArgs e)
        {
            openChildForm(new UserAccount(this));
            hideSubmenu();
        }

        private void btnStore_Click(object sender, EventArgs e)
        {
            hideSubmenu();
            Store store = new Store();
            store.ShowDialog();
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            hideSubmenu();

            if (MessageBox.Show("Logout Application?", "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.Hide();
                Login login = new Login();
                login.ShowDialog();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            string storeName = dbcon.getStoreName();
            if (!string.IsNullOrEmpty(storeName))
            {
                this.Text = storeName;
            }
            btnDashboard.PerformClick();
            Noti();
        }

        // Noti Alart for critical items
        public void Noti()
        {
            int i = 0;
            cn.Open();
            cm = new SqlCommand("SELECT * FROM vwCriticalItems", cn);
            dr = cm.ExecuteReader();
            while (dr.Read())
            {
                i++;
                Alert alert = new Alert(this);
                alert.lblPcode.Text = dr["pcode"].ToString();
                alert.btnReorder.Enabled = true;
                alert.showAlert(i + ". " + dr["pdesc"].ToString() + " - " + dr["qty"].ToString());
            }
            dr.Close();
            cn.Close();
        }
    }
}
