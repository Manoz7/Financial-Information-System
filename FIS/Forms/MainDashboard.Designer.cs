namespace FIS.Forms
{
    partial class MainDashboard
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            // Declare controls
            this.pnlSidebar = new System.Windows.Forms.Panel();
            this.lblAppName = new System.Windows.Forms.Label();
            this.lblAppSub = new System.Windows.Forms.Label();
            this.lblSecAP = new System.Windows.Forms.Label();
            this.btnInvoices = new System.Windows.Forms.Button();
            this.btnAPReport = new System.Windows.Forms.Button();
            this.lblSecAR = new System.Windows.Forms.Label();
            this.btnCustomerOrders = new System.Windows.Forms.Button();
            this.btnCustomerBills = new System.Windows.Forms.Button();
            this.btnARReport = new System.Windows.Forms.Button();
            this.lblSecPurchasing = new System.Windows.Forms.Label();
            this.btnVendors = new System.Windows.Forms.Button();
            this.btnRawMaterials = new System.Windows.Forms.Button();
            this.btnPurchaseOrders = new System.Windows.Forms.Button();
            this.lblSecPayroll = new System.Windows.Forms.Label();
            this.btnPayroll = new System.Windows.Forms.Button();
            this.btnPaymentPreference = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();

            this.pnlTopBar = new System.Windows.Forms.Panel();
            this.lblKpiAPCaption = new System.Windows.Forms.Label();
            this.lblAPOverdue = new System.Windows.Forms.Label();
            this.lblKpiAPUnpaid = new System.Windows.Forms.Label();
            this.lblAPUnpaid = new System.Windows.Forms.Label();
            this.lblKpiARCaption = new System.Windows.Forms.Label();
            this.lblAROverdue = new System.Windows.Forms.Label();
            this.lblKpiAROut = new System.Windows.Forms.Label();
            this.lblAROutstanding = new System.Windows.Forms.Label();
            this.lblKpiPayCaption = new System.Windows.Forms.Label();
            this.lblPayrollPending = new System.Windows.Forms.Label();
            this.lblKpiInvCaption = new System.Windows.Forms.Label();
            this.lblLowStock = new System.Windows.Forms.Label();

            // THE KEY CONTROL: pnlContent is where UserControls get swapped in
            this.pnlContent = new System.Windows.Forms.Panel();

            this.SuspendLayout();
            this.pnlSidebar.SuspendLayout();
            this.pnlTopBar.SuspendLayout();

            // ════════════════════════════════════════════════════════════════
            //  FORM
            // ════════════════════════════════════════════════════════════════
            this.Text = "Financial Information System";
            this.ClientSize = new System.Drawing.Size(1280, 800);
            this.MinimumSize = new System.Drawing.Size(1100, 700);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.FromArgb(240, 243, 248);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // NO IsMdiContainer — we use panel swap instead
            this.Load += new System.EventHandler(this.MainDashboard_Load);

            // ════════════════════════════════════════════════════════════════
            //  SIDEBAR  (Dock = Left, 250px)
            // ════════════════════════════════════════════════════════════════
            this.pnlSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlSidebar.Width = 250;
            this.pnlSidebar.BackColor = System.Drawing.Color.FromArgb(18, 40, 76);

            // App branding
            this.lblAppName.Text = "WBS — FIS";
            this.lblAppName.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblAppName.ForeColor = System.Drawing.Color.White;
            this.lblAppName.Location = new System.Drawing.Point(16, 18);
            this.lblAppName.AutoSize = true;

            this.lblAppSub.Text = "Financial Information System";
            this.lblAppSub.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            this.lblAppSub.ForeColor = System.Drawing.Color.FromArgb(120, 155, 200);
            this.lblAppSub.Location = new System.Drawing.Point(16, 46);
            this.lblAppSub.AutoSize = true;

            // Divider
            var div = new System.Windows.Forms.Panel();
            div.Location = new System.Drawing.Point(16, 68);
            div.Size = new System.Drawing.Size(218, 1);
            div.BackColor = System.Drawing.Color.FromArgb(38, 68, 120);

            // Build nav items using y-tracker
            int y = 82;

            StyleSectionLabel(this.lblSecAP, "ACCOUNTS PAYABLE");
            this.lblSecAP.Location = new System.Drawing.Point(16, y); y += 22;

            StyleSidebarButton(this.btnInvoices, "  Invoices");
            this.btnInvoices.Location = new System.Drawing.Point(15, y);
            this.btnInvoices.Click += new System.EventHandler(this.btnInvoices_Click); y += 42;

            StyleSidebarButton(this.btnAPReport, "  AP Report");
            this.btnAPReport.Location = new System.Drawing.Point(15, y);
            this.btnAPReport.Click += new System.EventHandler(this.btnAPReport_Click); y += 50;

            StyleSectionLabel(this.lblSecAR, "ACCOUNTS RECEIVABLE");
            this.lblSecAR.Location = new System.Drawing.Point(16, y); y += 22;

            StyleSidebarButton(this.btnCustomerOrders, "  Customer Orders");
            this.btnCustomerOrders.Location = new System.Drawing.Point(15, y);
            this.btnCustomerOrders.Click += new System.EventHandler(this.btnCustomerOrders_Click); y += 42;

            StyleSidebarButton(this.btnCustomerBills, "  Customer Bills");
            this.btnCustomerBills.Location = new System.Drawing.Point(15, y);
            this.btnCustomerBills.Click += new System.EventHandler(this.btnCustomerBills_Click); y += 42;

            StyleSidebarButton(this.btnARReport, "  AR Report");
            this.btnARReport.Location = new System.Drawing.Point(15, y);
            this.btnARReport.Click += new System.EventHandler(this.btnARReport_Click); y += 50;

            StyleSectionLabel(this.lblSecPurchasing, "VENDORS & PURCHASING");
            this.lblSecPurchasing.Location = new System.Drawing.Point(16, y); y += 22;

            StyleSidebarButton(this.btnVendors, "  Vendors");
            this.btnVendors.Location = new System.Drawing.Point(15, y);
            this.btnVendors.Click += new System.EventHandler(this.btnVendors_Click); y += 42;

            StyleSidebarButton(this.btnRawMaterials, "  Raw Materials");
            this.btnRawMaterials.Location = new System.Drawing.Point(15, y);
            this.btnRawMaterials.Click += new System.EventHandler(this.btnRawMaterials_Click); y += 42;

            StyleSidebarButton(this.btnPurchaseOrders, "  Purchase Orders");
            this.btnPurchaseOrders.Location = new System.Drawing.Point(15, y);
            this.btnPurchaseOrders.Click += new System.EventHandler(this.btnPurchaseOrders_Click); y += 50;

            StyleSectionLabel(this.lblSecPayroll, "PAYROLL");
            this.lblSecPayroll.Location = new System.Drawing.Point(16, y); y += 22;

            StyleSidebarButton(this.btnPayroll, "  Payroll Records");
            this.btnPayroll.Location = new System.Drawing.Point(15, y);
            this.btnPayroll.Click += new System.EventHandler(this.btnPayroll_Click); y += 42;

            StyleSidebarButton(this.btnPaymentPreference, "  Payment Preference");
            this.btnPaymentPreference.Location = new System.Drawing.Point(15, y);
            this.btnPaymentPreference.Click += new System.EventHandler(this.btnPaymentPreference_Click);

            // Refresh at bottom of sidebar
            this.btnRefresh.Text = "↻  Refresh KPIs";
            this.btnRefresh.Location = new System.Drawing.Point(15, 740);
            this.btnRefresh.Size = new System.Drawing.Size(220, 32);
            this.btnRefresh.BackColor = System.Drawing.Color.FromArgb(28, 58, 108);
            this.btnRefresh.ForeColor = System.Drawing.Color.FromArgb(140, 175, 220);
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.FlatAppearance.BorderSize = 0;
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.btnRefresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            this.pnlSidebar.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.lblAppName, this.lblAppSub, div,
                this.lblSecAP, this.btnInvoices, this.btnAPReport,
                this.lblSecAR, this.btnCustomerOrders, this.btnCustomerBills, this.btnARReport,
                this.lblSecPurchasing, this.btnVendors, this.btnRawMaterials, this.btnPurchaseOrders,
                this.lblSecPayroll, this.btnPayroll, this.btnPaymentPreference,
                this.btnRefresh
            });

            // ════════════════════════════════════════════════════════════════
            //  KPI TOP BAR  (Dock = Top, sits above pnlContent)
            // ════════════════════════════════════════════════════════════════
            this.pnlTopBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTopBar.Height = 80;
            this.pnlTopBar.BackColor = System.Drawing.Color.White;

            // Bottom border on KPI bar
            this.pnlTopBar.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new System.Drawing.Pen(System.Drawing.Color.FromArgb(220, 225, 235), 1),
                    0, 79, 2000, 79);

            // Build KPI tiles
            AddKpiTile(this.pnlTopBar, this.lblKpiAPCaption, "AP Overdue", 20, this.lblAPOverdue, 100);
            AddKpiTile(this.pnlTopBar, this.lblKpiAPUnpaid, "AP Unpaid Total", 160, this.lblAPUnpaid, 200);
            this.lblAPUnpaid.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblAPUnpaid.Size = new System.Drawing.Size(200, 40);

            // Vertical divider
            var kpiDiv1 = new System.Windows.Forms.Panel();
            kpiDiv1.Location = new System.Drawing.Point(380, 15);
            kpiDiv1.Size = new System.Drawing.Size(1, 50);
            kpiDiv1.BackColor = System.Drawing.Color.FromArgb(220, 225, 235);
            this.pnlTopBar.Controls.Add(kpiDiv1);

            AddKpiTile(this.pnlTopBar, this.lblKpiARCaption, "AR Overdue", 400, this.lblAROverdue, 100);
            AddKpiTile(this.pnlTopBar, this.lblKpiAROut, "AR Outstanding", 540, this.lblAROutstanding, 200);
            this.lblAROutstanding.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblAROutstanding.Size = new System.Drawing.Size(200, 40);

            var kpiDiv2 = new System.Windows.Forms.Panel();
            kpiDiv2.Location = new System.Drawing.Point(760, 15);
            kpiDiv2.Size = new System.Drawing.Size(1, 50);
            kpiDiv2.BackColor = System.Drawing.Color.FromArgb(220, 225, 235);
            this.pnlTopBar.Controls.Add(kpiDiv2);

            AddKpiTile(this.pnlTopBar, this.lblKpiPayCaption, "Payroll Pending", 780, this.lblPayrollPending, 100);
            AddKpiTile(this.pnlTopBar, this.lblKpiInvCaption, "Low Stock Items", 940, this.lblLowStock, 100);

            // ════════════════════════════════════════════════════════════════
            //  CONTENT PANEL  (Dock = Fill — takes all remaining space)
            //  This is where UserControls are swapped in on nav click.
            // ════════════════════════════════════════════════════════════════
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.BackColor = System.Drawing.Color.FromArgb(240, 243, 248);

            // ── Add to form in correct dock order ────────────────────────────
            // IMPORTANT: Dock order determines layout.
            // Left must be added first, then Top, then Fill.
            this.Controls.Add(this.pnlContent);   // Fill — added first so it's "behind"
            this.Controls.Add(this.pnlTopBar);     // Top  — docks on top of Fill
            this.Controls.Add(this.pnlSidebar);    // Left — docks on left of remainder

            this.pnlSidebar.ResumeLayout(false);
            this.pnlTopBar.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        // Adds a caption + value label pair to the KPI bar
        private void AddKpiTile(System.Windows.Forms.Panel parent,
                                 System.Windows.Forms.Label caption, string captionText, int x,
                                 System.Windows.Forms.Label value, int valueWidth)
        {
            StyleKpiCaption(caption, captionText);
            caption.Location = new System.Drawing.Point(x, 14);
            parent.Controls.Add(caption);

            StyleKpiValue(value);
            value.Location = new System.Drawing.Point(x, 30);
            value.Size = new System.Drawing.Size(valueWidth, 42);
            parent.Controls.Add(value);
        }

        #endregion

        // ── Control field declarations ────────────────────────────────────────
        private System.Windows.Forms.Panel pnlSidebar;
        private System.Windows.Forms.Label lblAppName;
        private System.Windows.Forms.Label lblAppSub;
        private System.Windows.Forms.Label lblSecAP;
        private System.Windows.Forms.Button btnInvoices;
        private System.Windows.Forms.Button btnAPReport;
        private System.Windows.Forms.Label lblSecAR;
        private System.Windows.Forms.Button btnCustomerOrders;
        private System.Windows.Forms.Button btnCustomerBills;
        private System.Windows.Forms.Button btnARReport;
        private System.Windows.Forms.Label lblSecPurchasing;
        private System.Windows.Forms.Button btnVendors;
        private System.Windows.Forms.Button btnRawMaterials;
        private System.Windows.Forms.Button btnPurchaseOrders;
        private System.Windows.Forms.Label lblSecPayroll;
        private System.Windows.Forms.Button btnPayroll;
        private System.Windows.Forms.Button btnPaymentPreference;
        private System.Windows.Forms.Button btnRefresh;

        private System.Windows.Forms.Panel pnlTopBar;
        private System.Windows.Forms.Label lblKpiAPCaption;
        private System.Windows.Forms.Label lblAPOverdue;
        private System.Windows.Forms.Label lblKpiAPUnpaid;
        private System.Windows.Forms.Label lblAPUnpaid;
        private System.Windows.Forms.Label lblKpiARCaption;
        private System.Windows.Forms.Label lblAROverdue;
        private System.Windows.Forms.Label lblKpiAROut;
        private System.Windows.Forms.Label lblAROutstanding;
        private System.Windows.Forms.Label lblKpiPayCaption;
        private System.Windows.Forms.Label lblPayrollPending;
        private System.Windows.Forms.Label lblKpiInvCaption;
        private System.Windows.Forms.Label lblLowStock;

        // This is the swap target — all nav panels load into here
        internal System.Windows.Forms.Panel pnlContent;
    }
}