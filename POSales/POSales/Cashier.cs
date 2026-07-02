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
    public partial class Cashier : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        SqlDataReader dr;
        Button btnPrintInvoice;
        Button btnUnsettledPayment;
        Button btnSaveCloud;
        ProgressBar cloudProgress;
        Label lblCloudStatus;
        Timer cloudSyncTimer;
        bool cloudSyncRunning;

        int qty;
        string id;
        string price;
        Form activeCenterForm;
            
        string stitle = "Point Of Sales";
        public Cashier()
        {
            InitializeComponent();
            AddInvoiceAndCloudControls();
            ModernUI.Apply(this);
            cn = new SqlConnection(dbcon.myConnection());
            GetTranNo();
            lblDate.Text = DateTime.Now.ToShortDateString();
            StartCloudSyncTimer();
        }

        private void AddInvoiceAndCloudControls()
        {
            btnPrintInvoice = CreateMenuButton("btnPrintInvoice", " Print Invoice");
            btnPrintInvoice.Enabled = false;
            btnPrintInvoice.Click += btnPrintInvoice_Click;
            panel1.Controls.Add(btnPrintInvoice);
            panel1.Controls.SetChildIndex(btnPrintInvoice, panel1.Controls.GetChildIndex(btnSettle));

            btnUnsettledPayment = CreateMenuButton("btnUnsettledPayment", " Unsettled Payment");
            btnUnsettledPayment.Enabled = false;
            btnUnsettledPayment.Click += btnUnsettledPayment_Click;
            panel1.Controls.Add(btnUnsettledPayment);
            panel1.Controls.SetChildIndex(btnUnsettledPayment, panel1.Controls.GetChildIndex(btnSettle));

            btnSaveCloud = CreateMenuButton("btnSaveCloud", " Save to Cloud");
            btnSaveCloud.Click += btnSaveCloud_Click;
            panel1.Controls.Add(btnSaveCloud);
            panel1.Controls.SetChildIndex(btnSaveCloud, panel1.Controls.GetChildIndex(btnDSales));

            cloudProgress = new ProgressBar();
            cloudProgress.Dock = DockStyle.Top;
            cloudProgress.Height = 14;
            cloudProgress.Visible = false;
            panel1.Controls.Add(cloudProgress);
            panel1.Controls.SetChildIndex(cloudProgress, panel1.Controls.GetChildIndex(btnSaveCloud) + 1);

            lblCloudStatus = new Label();
            lblCloudStatus.Dock = DockStyle.Top;
            lblCloudStatus.Height = 38;
            lblCloudStatus.ForeColor = Color.White;
            lblCloudStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblCloudStatus.Visible = false;
            panel1.Controls.Add(lblCloudStatus);
            panel1.Controls.SetChildIndex(lblCloudStatus, panel1.Controls.GetChildIndex(cloudProgress) + 1);
        }

        private Button CreateMenuButton(string name, string text)
        {
            Button button = new Button();
            button.Dock = DockStyle.Top;
            button.FlatAppearance.BorderSize = 0;
            button.FlatStyle = FlatStyle.Flat;
            button.ForeColor = Color.White;
            button.ImageAlign = ContentAlignment.MiddleLeft;
            button.Name = name;
            button.Size = new Size(200, 50);
            button.Text = text;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.UseVisualStyleBackColor = true;
            return button;
        }

        private void StartCloudSyncTimer()
        {
            cloudSyncTimer = new Timer();
            cloudSyncTimer.Interval = 30 * 60 * 1000;
            cloudSyncTimer.Tick += async (s, e) => await SaveToCloudAsync(false);
            cloudSyncTimer.Start();
        }

        private void picClose_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Exit Application?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        public void slide(Button button)
        {
            panelSlide.BackColor = Color.White;
            panelSlide.Height = button.Height;
            panelSlide.Top = button.Top;
        }
        #region button
        private void btnNTran_Click(object sender, EventArgs e)
        {
            slide(btnNTran);
            GetTranNo();

        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            slide(btnSearch);
            LookUpProduct lookUp = new LookUpProduct(this);
            lookUp.LoadProduct();
            lookUp.ShowDialog(this);
        }

        private void btnDiscount_Click(object sender, EventArgs e)
        {
            slide(btnDiscount);
            Discount discount = new Discount(this);
            discount.lbId.Text = id;
            discount.txtTotalPrice.Text = price;
            discount.ShowDialog(this);
        }

        private void btnSettle_Click(object sender, EventArgs e)
        {
            slide(btnSettle);
            Settle settle = new Settle(this);
            settle.txtSale.Text = lblDisplayTotal.Text;
            settle.ShowDialog(this);
        }

        private void btnPrintInvoice_Click(object sender, EventArgs e)
        {
            slide(btnPrintInvoice);
            if (!dbcon.GetPrintInvoiceEnabled())
            {
                MessageBox.Show("Print Invoice is disabled in Store Settings.", "Invoice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (dgvCash.Rows.Count == 0)
            {
                MessageBox.Show("Add items to the cart before printing an invoice.", "Invoice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Recept invoice = new Recept(this);
            invoice.LoadInvoice();
            invoice.ShowDialog(this);
        }

        private void btnUnsettledPayment_Click(object sender, EventArgs e)
        {
            slide(btnUnsettledPayment);
            if (!dbcon.GetUnsettledPaymentEnabled())
            {
                MessageBox.Show("Unsettled Payment is disabled in Store Settings.", "Unsettled Payment", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (dgvCash.Rows.Count == 0)
            {
                MessageBox.Show("Add items to the cart before printing unsettled payment.", "Unsettled Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Recept receipt = new Recept(this);
            receipt.LoadUnsettledPayment();
            receipt.ShowDialog(this);
        }

        private async void btnSaveCloud_Click(object sender, EventArgs e)
        {
            slide(btnSaveCloud);
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
            lblCloudStatus.Text = "Saving reports to cloud...";
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

        private void btnClear_Click(object sender, EventArgs e)
        {
            slide(btnClear);
            if (MessageBox.Show("Remove all items from cart?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                cn.Open();
                cm = new SqlCommand("Delete from tbCart where transno like'" + lblTranNo.Text + "'", cn);
                cm.ExecuteNonQuery();
                cn.Close();
                MessageBox.Show("All items has been successfully remove", "Remove item", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadCart();
            }
        }

        private void btnDSales_Click(object sender, EventArgs e)
        {
            slide(btnDSales);
            DailySale dailySale = new DailySale(new MainForm());
            dailySale.solduser = lblUsername.Text;
            dailySale.dtFrom.Enabled = true;
            dailySale.dtTo.Enabled = true;
            dailySale.cboCashier.Enabled = true;
            dailySale.cboCashier.Text = lblUsername.Text;
            dailySale.picClose.Visible = false;
            dailySale.lblTitle.Visible = true;
            dailySale.LoadSold();
            dailySale.ShowDialog(this);
        }

        private void btnPass_Click(object sender, EventArgs e)
        {
            slide(btnPass);
            ChangePassword change = new ChangePassword(this);
            change.ShowDialog(this);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            slide(btnLogout);
            if(dgvCash.Rows.Count > 0)
            {
                MessageBox.Show("Unable to logout. Please cancel the transaction.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Logout Application?", "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.Hide();
                Login login = new Login();
                login.ShowDialog();
            }
        }
        #endregion button

        private void ShowCenterForm(Form form)
        {
            CloseCenterForm();

            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.StartPosition = FormStartPosition.Manual;
            form.Bounds = GetCenterFormBounds();
            form.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            form.Disposed += CenterForm_Disposed;

            dgvCash.Visible = false;
            activeCenterForm = form;
            Controls.Add(form);
            form.BringToFront();
            panel5.BringToFront();
            panel1.BringToFront();
            panel3.BringToFront();
            panel4.BringToFront();
            panelSlide.BringToFront();
            form.Show();
        }

        private void CloseCenterForm()
        {
            if (activeCenterForm != null && !activeCenterForm.IsDisposed)
                activeCenterForm.Dispose();

            activeCenterForm = null;
            RestoreTransactionView(false);
        }

        private Rectangle GetCenterFormBounds()
        {
            int left = panel3.Right + 8;
            int top = panel4.Bottom + 10;
            int right = panel5.Left - 12;
            int bottom = ClientSize.Height - 10;

            return new Rectangle(left, top, Math.Max(320, right - left), Math.Max(260, bottom - top));
        }

        private void CenterForm_Disposed(object sender, EventArgs e)
        {
            if (ReferenceEquals(activeCenterForm, sender))
            {
                activeCenterForm = null;
                RestoreTransactionView(true);
            }
        }

        private void RestoreTransactionView(bool reloadCart)
        {
            dgvCash.Visible = true;
            dgvCash.Dock = DockStyle.None;
            dgvCash.SetBounds(panel3.Right, panel4.Bottom, Math.Max(320, panel5.Left - panel3.Right), Math.Max(260, ClientSize.Height - panel4.Bottom));
            dgvCash.SendToBack();
            panelSlide.BringToFront();

            if (reloadCart)
                LoadCart();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (activeCenterForm != null && !activeCenterForm.IsDisposed)
                activeCenterForm.Bounds = GetCenterFormBounds();
        }

        public void LoadCart()
        {
            try
            {
                Boolean hascart = false;
                int i = 0;
                double total = 0;
                double discount = 0;
                dgvCash.Rows.Clear();
                cn.Open();
                cm = new SqlCommand("SELECT c.id, c.pcode, p.pdesc, c.price, c.qty, c.disc, c.total FROM tbCart AS c INNER JOIN tbProduct AS p ON c.pcode=p.pcode WHERE c.transno LIKE @transno and c.status LIKE 'Pending'", cn);
                cm.Parameters.AddWithValue("@transno", lblTranNo.Text);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {

                    i++;
                    total += Convert.ToDouble(dr["total"].ToString());
                    discount += Convert.ToDouble(dr["disc"].ToString());
                    dgvCash.Rows.Add(i, dr["id"].ToString(), dr["pcode"].ToString(), dr["pdesc"].ToString(), dr["price"].ToString(), dr["qty"].ToString(), dr["disc"].ToString(), double.Parse(dr["total"].ToString()).ToString("#,##0.00"));//
                    hascart = true;
                }
                dr.Close();
                cn.Close();
                lblSaleTotal.Text = total.ToString("#,##0.00");
                lblDiscount.Text = discount.ToString("#,##0.00");
                GetCartTotal();
                if (hascart) { btnClear.Enabled = true; btnSettle.Enabled = true; btnDiscount.Enabled = true; btnPrintInvoice.Enabled = dbcon.GetPrintInvoiceEnabled(); btnUnsettledPayment.Enabled = dbcon.GetUnsettledPaymentEnabled(); }
                else { btnClear.Enabled = false; btnSettle.Enabled = false; btnDiscount.Enabled = false; btnPrintInvoice.Enabled = false; btnUnsettledPayment.Enabled = false; }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle);
            }
        
        }

        public void GetCartTotal()
        {
            double discount = double.Parse(lblDiscount.Text);
            double baseAmount = double.Parse(lblSaleTotal.Text) - discount;

            string vatType = dbcon.GetVatType();
            double vatPercent = dbcon.GetVatPercent();

            double vat = 0;
            double vatable = 0;
            double finalTotal = 0;

            if (vatType == "New")
            {
                vatable = baseAmount;
                vat = vatable * (vatPercent / 100.0);
                finalTotal = vatable + vat;
            }
            else
            {
                // Old VAT
                finalTotal = baseAmount;
                vat = finalTotal * (vatPercent / 100.0);
                vatable = finalTotal - vat;
            }

            lblVat.Text = vat.ToString("#,##0.00");
            lblVatable.Text = vatable.ToString("#,##0.00");
            lblDisplayTotal.Text = finalTotal.ToString("#,##0.00");
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            lblTimer.Text = DateTime.Now.ToString("hh:mm:ss tt");
        }

        public void GetTranNo()
        {
            try
            {
                string sdate = DateTime.Now.ToString("yyyyMMdd");
                int count;
                string transno;
                cn.Open();
                cm = new SqlCommand("SELECT TOP 1 transno FROM tbCart WHERE transno LIKE '" + sdate + "%' ORDER BY id desc", cn);
                dr = cm.ExecuteReader();
                dr.Read();
                if (dr.HasRows)
                {
                    transno = dr[0].ToString();
                    count = int.Parse(transno.Substring(8, 4));
                    lblTranNo.Text = sdate + (count + 1);
                }
                else
                {
                    transno = sdate + "1001";
                    lblTranNo.Text = transno;
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {

                cn.Close();
                MessageBox.Show(ex.Message, stitle);

            }
            
        }

        private void txtBarcode_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtBarcode.Text == string.Empty) return;
                else
                {
                    string _pcode;
                    double _price;
                    int _qty;
                    cn.Open();
                    cm = new SqlCommand("SELECT * FROM tbProduct WHERE barcode LIKE '" + txtBarcode.Text + "'", cn);
                    dr = cm.ExecuteReader();
                    dr.Read();
                    if(dr.HasRows)
                    {
                        qty = int.Parse(dr["qty"].ToString());
                        _pcode = dr["pcode"].ToString();
                        _price = double.Parse(dr["price"].ToString());
                        _qty = int.Parse(txtQty.Text);
                       
                        dr.Close();
                        cn.Close();
                        //insert to tbCart
                        AddToCart(_pcode, _price, _qty);
                    }
                    dr.Close();
                    cn.Close();
                }
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void AddToCart(string _pcode, double _price,int _qty)
        {
            try
            {
                string id = "";
                int cart_qty = 0;
                bool found = false;
                cn.Open();
                cm = new SqlCommand("Select * from tbCart Where transno = @transno and pcode = @pcode", cn);
                cm.Parameters.AddWithValue("@transno", lblTranNo.Text);
                cm.Parameters.AddWithValue("@pcode", _pcode);
                dr = cm.ExecuteReader();
                dr.Read();
                if (dr.HasRows)
                {
                    id = dr["id"].ToString();
                    cart_qty = int.Parse(dr["qty"].ToString());
                    found = true;
                }
                else found = false;
                dr.Close();
                cn.Close();

                if (found)
                {
                    if (qty < (int.Parse(txtQty.Text) + cart_qty))
                    {
                        MessageBox.Show("Unable to procced. Remaining quantity on hand is " + qty, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    cn.Open();
                    cm = new SqlCommand("Update tbCart set qty = (qty + " + _qty + ")Where id= '" + id + "'", cn);
                    cm.ExecuteReader();
                    cn.Close();
                    txtBarcode.SelectionStart = 0;
                    txtBarcode.SelectionLength = txtBarcode.Text.Length;
                    LoadCart();                    
                }
                else
                {
                    if (qty < (int.Parse(txtQty.Text) + cart_qty))
                    {
                        MessageBox.Show("Unable to procced. Remaining qty on hand is" + qty, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    cn.Open();
                    cm = new SqlCommand("INSERT INTO tbCart(transno, pcode, price, qty, sdate, cashier)VALUES(@transno, @pcode, @price, @qty, @sdate, @cashier)", cn);
                    cm.Parameters.AddWithValue("@transno", lblTranNo.Text);
                    cm.Parameters.AddWithValue("@pcode", _pcode);
                    cm.Parameters.AddWithValue("@price", _price);
                    cm.Parameters.AddWithValue("@qty", _qty);
                    cm.Parameters.AddWithValue("@sdate", DateTime.Now);
                    cm.Parameters.AddWithValue("@cashier", lblUsername.Text);
                    cm.ExecuteNonQuery();
                    cn.Close();
                    LoadCart();
                }
            }
            catch (Exception ex)
            {
               MessageBox.Show(ex.Message, stitle);
            }
        }

        private void dgvCash_SelectionChanged(object sender, EventArgs e)
        {
            int i = dgvCash.CurrentRow.Index;
            id = dgvCash[1, i].Value.ToString();
            price = dgvCash[7, i].Value.ToString();
        }

        private void dgvCash_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            string colName = dgvCash.Columns[e.ColumnIndex].Name;
           

            if (colName == "Delete")
            {
                if (MessageBox.Show("Remove this item", "Remove item", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    dbcon.ExecuteQuery("Delete from tbCart where id like'" + dgvCash.Rows[e.RowIndex].Cells[1].Value.ToString() + "'");
                    MessageBox.Show("Items has been successfully remove", "Remove item", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadCart();
                }
            }
            else if (colName == "colAdd")
            {
                int i = 0;
                cn.Open();
                cm = new SqlCommand("SELECT SUM(qty) as qty FROM tbProduct WHERE pcode LIKE'" + dgvCash.Rows[e.RowIndex].Cells[2].Value.ToString() + "' GROUP BY pcode", cn);
                i = int.Parse(cm.ExecuteScalar().ToString());
                cn.Close();
                if (int.Parse(dgvCash.Rows[e.RowIndex].Cells[5].Value.ToString()) < i)
                {
                    dbcon.ExecuteQuery("UPDATE tbCart SET qty = qty + " + int.Parse(txtQty.Text) + " WHERE transno LIKE '" + lblTranNo.Text + "'  AND pcode LIKE '" + dgvCash.Rows[e.RowIndex].Cells[2].Value.ToString() + "'");
                    LoadCart();
                }
                else
                {
                    MessageBox.Show("Remaining qty on hand is " + i + "!", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (colName == "colReduce")
            {
                int i = 0;
                cn.Open();
                cm = new SqlCommand("SELECT SUM(qty) as qty FROM tbCart WHERE pcode LIKE'" + dgvCash.Rows[e.RowIndex].Cells[2].Value.ToString() + "' GROUP BY pcode", cn);
                i = int.Parse(cm.ExecuteScalar().ToString());
                cn.Close();
                if (i > 1)
                {
                    dbcon.ExecuteQuery("UPDATE tbCart SET qty = qty - " + int.Parse(txtQty.Text) + " WHERE transno LIKE '" + lblTranNo.Text + "'  AND pcode LIKE '" + dgvCash.Rows[e.RowIndex].Cells[2].Value.ToString() + "'");
                    LoadCart();
                }
                else
                {
                    MessageBox.Show("Remaining qty on cart is " + i + "!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
        }

        public void Noti()
        {
            int i = 0;
            cn.Open();
            cm = new SqlCommand("SELECT * FROM vwCriticalItems", cn);
            dr = cm.ExecuteReader();
            while (dr.Read())
            {
                i++;
                Alert alert = new Alert(new MainForm());
                alert.lblPcode.Text = dr["pcode"].ToString();              
                alert.showAlert(i + ". " + dr["pdesc"].ToString() + " - " + dr["qty"].ToString());
            }
            dr.Close();
            cn.Close();
        }

        private void Cashier_Load(object sender, EventArgs e)
        {
            string storeName = dbcon.getStoreName();
            if (!string.IsNullOrEmpty(storeName))
            {
                this.Text = storeName;
            }
            Noti();
        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
