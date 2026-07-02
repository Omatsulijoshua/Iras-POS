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
using Microsoft.Reporting.WinForms;

namespace POSales
{
    public partial class Recept : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        SqlDataReader dr;
        string store;
        string address;
        Cashier cashier;
        public Recept(Cashier cash)
        {
            InitializeComponent();
            ModernUI.Apply(this);
            cn = new SqlConnection(dbcon.myConnection());
            cashier = cash;
            LoadStore();
        }

        public void LoadStore()
        {
            cn.Open();
            cm = new SqlCommand("SELECT * FROM tbStore", cn);
            dr = cm.ExecuteReader();
            dr.Read();
            if(dr.HasRows)
            {
                store = dr["store"].ToString();
                address = dr["address"].ToString();
            }
            dr.Close();
            cn.Close();
        }

        private void Recept_Load(object sender, EventArgs e)
        {

            this.reportViewer1.RefreshReport();
        }

        public void LoadRecept(string pcash, string pchange)
        {
            LoadRecept(pcash, pchange, "Cash");
        }

        public void LoadRecept(string pcash, string pchange, string paymentType)
        {
            LoadReceptCore(pcash, pchange, paymentType, false);
        }

        public void LoadInvoice()
        {
            LoadReceptCore("", "", "Invoice", true);
        }

        public void LoadUnsettledPayment()
        {
            LoadReceptCore("0.00", "0.00", "Unsettled", true);
        }

        private void LoadReceptCore(string pcash, string pchange, string paymentType, bool invoiceMode)
        {
            ReportDataSource rptDataSourece;
            try
            {
                ModernUI.LoadReportWithCustomLogo(this.reportViewer1.LocalReport, Application.StartupPath + @"\Reports\rptRecept.rdlc");
                this.reportViewer1.LocalReport.DataSources.Clear();

                DataSet1 ds = new DataSet1();
                SqlDataAdapter da = new SqlDataAdapter();

                cn.Open();
                da.SelectCommand = new SqlCommand("SELECT c.id, c.transno, c.pcode, c.price, c.qty, c.disc, c.total, c.sdate, c.status, p.pdesc FROM tbCart AS c INNER JOIN tbProduct AS p ON p.pcode=c.pcode WHERE c.transno LIKE '"+cashier.lblTranNo.Text+"'",cn);
                da.Fill(ds.Tables["dtRecept"]);
                cn.Close();

                bool hideAmounts = paymentType == "Invoice";
                ReportParameter pVatable = new ReportParameter("pVatable", hideAmounts ? " " : cashier.lblVatable.Text);
                ReportParameter pVat = new ReportParameter("pVat", hideAmounts ? " " : cashier.lblVat.Text);
                ReportParameter pDiscount = new ReportParameter("pDiscount", hideAmounts ? " " : cashier.lblDiscount.Text);
                ReportParameter pTotal = new ReportParameter("pTotal", hideAmounts ? " " : cashier.lblDisplayTotal.Text);
                ReportParameter pCash = new ReportParameter("pCash", string.IsNullOrWhiteSpace(pcash) ? " " : pcash);
                ReportParameter pChange = new ReportParameter("pChange", string.IsNullOrWhiteSpace(pchange) ? " " : pchange);
                ReportParameter pStore = new ReportParameter("pStore", string.IsNullOrWhiteSpace(store) ? " " : store);
                ReportParameter pAddress = new ReportParameter("pAddress", string.IsNullOrWhiteSpace(address) ? " " : address);
                ReportParameter pTransaction = new ReportParameter("pTransaction", (paymentType == "Invoice" ? "INVOICE NO: " : invoiceMode ? "UNSETTLED NO: " : "Invoice #: ") + cashier.lblTranNo.Text);
                ReportParameter pCashier = new ReportParameter("pCashier", cashier.lblUsername.Text);
                ReportParameter pPaymentType = new ReportParameter("pPaymentType", string.IsNullOrWhiteSpace(paymentType) ? "Cash" : paymentType);
                ReportParameter pVatPercent = new ReportParameter("pVatPercent", dbcon.GetVatPercent().ToString("0.00"));
                ReportParameter pVatType = new ReportParameter("pVatType", dbcon.GetVatType());

                string specialNote = "";
                if (dbcon.GetSpecialNoteEnabled())
                {
                    specialNote = dbcon.GetTransactionNote(cashier.lblTranNo.Text);
                }
                ReportParameter pSpecialNote = new ReportParameter("pSpecialNote", string.IsNullOrWhiteSpace(specialNote) ? " " : specialNote);

                reportViewer1.LocalReport.SetParameters(pVatable);
                reportViewer1.LocalReport.SetParameters(pVat);
                reportViewer1.LocalReport.SetParameters(pDiscount);
                reportViewer1.LocalReport.SetParameters(pTotal);
                reportViewer1.LocalReport.SetParameters(pCash);
                reportViewer1.LocalReport.SetParameters(pChange);
                reportViewer1.LocalReport.SetParameters(pStore);
                reportViewer1.LocalReport.SetParameters(pAddress);
                reportViewer1.LocalReport.SetParameters(pTransaction);
                reportViewer1.LocalReport.SetParameters(pCashier);
                reportViewer1.LocalReport.SetParameters(pPaymentType);
                reportViewer1.LocalReport.SetParameters(pVatPercent);
                reportViewer1.LocalReport.SetParameters(pVatType);
                reportViewer1.LocalReport.SetParameters(pSpecialNote);

                rptDataSourece = new ReportDataSource("DataSet1", ds.Tables["dtRecept"]);
                reportViewer1.LocalReport.DataSources.Add(rptDataSourece);
                reportViewer1.SetDisplayMode(Microsoft.Reporting.WinForms.DisplayMode.PrintLayout);
                reportViewer1.ZoomMode = ZoomMode.Percent;
                reportViewer1.ZoomPercent = 30;
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message);
            }
        }

        private void Recept_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Escape)
            {
                this.Dispose();
            }
        }
    }
}
