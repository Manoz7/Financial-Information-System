using FIS.Database;
using FIS.Services;
using FIS.Utils;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FIS.Forms
{
    public partial class MainDashboard : Form
    {
        private readonly InvoiceAPService _invoiceService = new InvoiceAPService();
        private readonly CustomerBillService _billService = new CustomerBillService();
        private readonly PayrollService _payrollService = new PayrollService();
        private readonly RawMaterialService _materialService = new RawMaterialService();

        // Tracks which sidebar button is currently active so we can highlight it
        private Button _activeNavBtn = null;

        public MainDashboard()
        {
            InitializeComponent();
        }

        private void MainDashboard_Load(object sender, EventArgs e)
        {
            if (!DBHelper.TestConnection())
                MessageHelper.ShowWarning(
                    "Cannot reach the database.\n" +
                    "Check that MySQL / XAMPP is running.");

            LoadKPIs();

            // Show a welcome screen in the content area on startup
            ShowWelcome();
        }

        // ── KPI Loader ───────────────────────────────────────────────────────
        private void LoadKPIs()
        {
            try
            {
                int overdueAP = _invoiceService.GetOverdueCount();
                decimal unpaidAP = _invoiceService.GetUnpaidTotal();
                lblAPOverdue.Text = overdueAP.ToString();
                lblAPUnpaid.Text = unpaidAP.ToString("C");
                lblAPOverdue.ForeColor = overdueAP > 0
                    ? Color.FromArgb(200, 40, 40) : Color.FromArgb(30, 140, 60);

                int overdueAR = _billService.GetOverdueCount();
                decimal outstandingAR = _billService.GetOutstandingTotal();
                lblAROverdue.Text = overdueAR.ToString();
                lblAROutstanding.Text = outstandingAR.ToString("C");
                lblAROverdue.ForeColor = overdueAR > 0
                    ? Color.FromArgb(200, 40, 40) : Color.FromArgb(30, 140, 60);

                int pending = _payrollService.GetPendingCount();
                lblPayrollPending.Text = pending.ToString();
                lblPayrollPending.ForeColor = pending > 0
                    ? Color.FromArgb(180, 100, 0) : Color.FromArgb(30, 140, 60);

                int lowStock = _materialService.GetLowStockCount();
                lblLowStock.Text = lowStock.ToString();
                lblLowStock.ForeColor = lowStock > 0
                    ? Color.FromArgb(200, 40, 40) : Color.FromArgb(30, 140, 60);
            }
            catch (Exception ex)
            {
                MessageHelper.ShowWarning(
                    "KPI data could not be loaded.\nDetails: " + ex.Message);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  PANEL SWAP — the core navigation engine
        //  Instead of opening child Forms, we drop a UserControl into
        //  pnlContent. It fills the space completely with no window chrome.
        // ════════════════════════════════════════════════════════════════════

        // Loads a UserControl into the content area.
        // T must be a UserControl with a no-arg constructor.
        private void LoadPanel<T>(Button senderBtn) where T : UserControl, new()
        {
            // Highlight the active sidebar button
            SetActiveNav(senderBtn);

            // Clear whatever is currently showing
            pnlContent.Controls.Clear();

            // Create the new panel and dock it to fill the content area
            var panel = new T();
            panel.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(panel);
        }

        // Shows a simple welcome label when no module is open
        private void ShowWelcome()
        {
            pnlContent.Controls.Clear();

            var lbl = new Label
            {
                Text = "Select a module from the sidebar to get started.",
                Font = new Font("Segoe UI", 13F),
                ForeColor = Color.FromArgb(160, 170, 190),
                AutoSize = false,
                Size = pnlContent.Size,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            pnlContent.Controls.Add(lbl);
        }

        // ── Sidebar button highlight ──────────────────────────────────────────
        private void SetActiveNav(Button btn)
        {
            // Reset previous active button
            if (_activeNavBtn != null)
            {
                _activeNavBtn.BackColor = Color.FromArgb(26, 55, 100);
                _activeNavBtn.ForeColor = Color.White;
            }

            // Highlight new active button
            _activeNavBtn = btn;
            if (_activeNavBtn != null)
            {
                _activeNavBtn.BackColor = Color.FromArgb(58, 110, 190);
                _activeNavBtn.ForeColor = Color.White;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  NAV BUTTON CLICK HANDLERS
        //  Each one calls LoadPanel<T> with the matching UserControl type.
        //  The stub UserControls (InvoiceListPanel etc.) just need to exist
        //  as empty classes for this to compile.
        // ════════════════════════════════════════════════════════════════════

        private void btnInvoices_Click(object sender, EventArgs e)
            => LoadPanel<InvoiceListPanel>(sender as Button);

        private void btnAPReport_Click(object sender, EventArgs e)
            => LoadPanel<APReportPanel>(sender as Button);

        private void btnCustomerOrders_Click(object sender, EventArgs e)
            => LoadPanel<CustomerOrderListPanel>(sender as Button);

        private void btnCustomerBills_Click(object sender, EventArgs e)
            => LoadPanel<CustomerBillListPanel>(sender as Button);

        private void btnARReport_Click(object sender, EventArgs e)
            => LoadPanel<ARReportPanel>(sender as Button);

        private void btnVendors_Click(object sender, EventArgs e)
            => LoadPanel<VendorListPanel>(sender as Button);

        private void btnRawMaterials_Click(object sender, EventArgs e)
            => LoadPanel<RawMaterialListPanel>(sender as Button);

        private void btnPurchaseOrders_Click(object sender, EventArgs e)
            => LoadPanel<PurchaseOrderListPanel>(sender as Button);

        private void btnPayroll_Click(object sender, EventArgs e)
            => LoadPanel<PayrollListPanel>(sender as Button);

        private void btnPaymentPreference_Click(object sender, EventArgs e)
            => LoadPanel<PaymentPreferencePanel>(sender as Button);

        private void btnRefresh_Click(object sender, EventArgs e)
            => LoadKPIs();

        // ── Style helpers ────────────────────────────────────────────────────
        internal void StyleSidebarButton(Button btn, string text)
        {
            btn.Text = text;
            btn.Size = new Size(220, 38);
            btn.BackColor = Color.FromArgb(26, 55, 100);
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 95, 160);
            btn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(10, 0, 0, 0);
        }

        internal void StyleSectionLabel(Label lbl, string text)
        {
            lbl.Text = text;
            lbl.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(120, 150, 190);
            lbl.AutoSize = false;
            lbl.Size = new Size(220, 20);
        }

        internal void StyleKpiValue(Label lbl)
        {
            lbl.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(26, 55, 100);
            lbl.AutoSize = false;
            lbl.Size = new Size(100, 42);
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Text = "—";
        }

        internal void StyleKpiCaption(Label lbl, string text)
        {
            lbl.Text = text;
            lbl.Font = new Font("Segoe UI", 7.5F);
            lbl.ForeColor = Color.FromArgb(140, 150, 170);
            lbl.AutoSize = true;
        }
    }
}