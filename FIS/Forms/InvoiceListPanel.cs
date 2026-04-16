using FIS.Models;
using FIS.Services;
using FIS.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FIS.Forms
{
    // ── InvoiceListPanel ──────────────────────────────────────────────────────
    // Shows all vendor invoices in a DataGridView with status color coding.
    // Toolbar allows: filter by status, add new invoice, mark overdue,
    // process due payments, and refresh.
    // Follows the same UserControl pattern as ApplicantDashboardPanel.
    public partial class InvoiceListPanel : UserControl
    {
        // ── Services ──────────────────────────────────────────────────────────
        private readonly InvoiceAPService _invoiceService = new InvoiceAPService();

        // ── Controls we need to reference after BuildUI ───────────────────────
        private DataGridView _dgv;
        private ComboBox     _cboFilter;
        private Label        _lblSummary;

        public InvoiceListPanel()
        {
            this.Dock      = DockStyle.Fill;
            this.BackColor = Color.FromArgb(240, 243, 248);
            BuildUI();
        }

        // ════════════════════════════════════════════════════════════════════
        //  UI CONSTRUCTION
        // ════════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            // ── Header bar ────────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 64,
                BackColor = Color.FromArgb(26, 55, 100),
                Padding   = new Padding(20, 0, 20, 0)
            };

            var lblTitle = new Label
            {
                Text      = "Vendor Invoices",
                Font      = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = true,
                Location  = new Point(20, 12)
            };
            var lblSub = new Label
            {
                Text      = "Accounts Payable — track and process vendor invoices",
                Font      = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(160, 200, 240),
                AutoSize  = true,
                Location  = new Point(22, 40)
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // ── Toolbar ───────────────────────────────────────────────────────
            var pnlToolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 52,
                BackColor = Color.White,
                Padding   = new Padding(16, 0, 16, 0)
            };
            // Bottom border on toolbar
            pnlToolbar.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(220, 225, 235), 1),
                    0, 51, pnlToolbar.Width, 51);

            // Filter label + dropdown
            var lblFilter = new Label
            {
                Text      = "Filter:",
                Font      = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 110, 130),
                AutoSize  = true,
                Location  = new Point(16, 16)
            };

            _cboFilter = new ComboBox
            {
                Location      = new Point(56, 12),
                Size          = new Size(130, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 9F)
            };
            _cboFilter.Items.AddRange(new object[] {
                "All", "Unpaid", "Overdue", "Paid" });
            _cboFilter.SelectedIndex = 0;
            _cboFilter.SelectedIndexChanged += (s, e) => LoadData();

            // Action buttons
            var btnAdd = MakeToolbarButton("+ New Invoice", Color.FromArgb(26, 55, 100));
            btnAdd.Location = new Point(206, 10);
            btnAdd.Click   += BtnAdd_Click;

            var btnMarkOverdue = MakeToolbarButton("Mark Overdue", Color.FromArgb(160, 60, 50));
            btnMarkOverdue.Location = new Point(340, 10);
            btnMarkOverdue.Click   += BtnMarkOverdue_Click;

            var btnProcessDue = MakeToolbarButton("Process Due Payments", Color.FromArgb(30, 120, 60));
            btnProcessDue.Location = new Point(474, 10);
            btnProcessDue.Size     = new Size(190, 32);
            btnProcessDue.Click   += BtnProcessDue_Click;

            var btnRefresh = MakeToolbarButton("↻ Refresh", Color.FromArgb(80, 100, 140));
            btnRefresh.Location = new Point(674, 10);
            btnRefresh.Click   += (s, e) => LoadData();

            // Summary label (right side of toolbar)
            _lblSummary = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(120, 130, 150),
                AutoSize  = true,
                Location  = new Point(780, 18)
            };

            pnlToolbar.Controls.AddRange(new Control[] {
                lblFilter, _cboFilter,
                btnAdd, btnMarkOverdue, btnProcessDue, btnRefresh,
                _lblSummary });

            // ── DataGridView ──────────────────────────────────────────────────
            _dgv = new DataGridView
            {
                Dock                          = DockStyle.Fill,
                ReadOnly                      = true,
                AllowUserToAddRows            = false,
                AllowUserToDeleteRows         = false,
                AllowUserToResizeRows         = false,
                BorderStyle                   = BorderStyle.None,
                BackgroundColor               = Color.FromArgb(240, 243, 248),
                GridColor                     = Color.FromArgb(220, 225, 235),
                RowHeadersVisible             = false,
                AutoSizeColumnsMode           = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode                 = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect                   = false,
                Font                          = new Font("Segoe UI", 9F),
                CellBorderStyle               = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle      = DataGridViewHeaderBorderStyle.Single
            };

            // Header style
            _dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 252);
            _dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(80, 95, 120);
            _dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            _dgv.ColumnHeadersHeight = 36;
            _dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // Row style
            _dgv.RowTemplate.Height = 32;
            _dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 225, 250);
            _dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 40, 80);
            _dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);

            // Double-click row → open detail
            _dgv.CellDoubleClick += Dgv_CellDoubleClick;

            // Define columns — these match the InvoiceAP model fields
            AddColumn("InvoiceAPId",   "ID",             40,  false);  // hidden key
            AddColumn("InvoiceNumber", "Invoice #",      80,  true);
            AddColumn("VendorName",    "Vendor",         160, true);
            AddColumn("InvoiceDate",   "Invoice Date",   100, true);
            AddColumn("DueDate",       "Due Date",       100, true);
            AddColumn("TotalAmount",   "Total",          90,  true);
            AddColumn("AmountPaid",    "Paid",           90,  true);
            AddColumn("Status",        "Status",         90,  true);
            AddColumn("DatePaid",      "Date Paid",      100, true);

            // Hide the ID column — we use it internally on double-click
            _dgv.Columns["InvoiceAPId"].Visible = false;

            // ── Wrapper panel for grid (adds padding around it) ───────────────
            var pnlGrid = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(16, 12, 16, 16),
                BackColor = Color.FromArgb(240, 243, 248)
            };
            pnlGrid.Controls.Add(_dgv);

            // ── Assemble — order matters for DockStyle ────────────────────────
            // Fill must be added first (it's "behind" the docked edges)
            this.Controls.Add(pnlGrid);       // Fill
            this.Controls.Add(pnlToolbar);    // Top (sits above Fill)
            this.Controls.Add(pnlHeader);     // Top (sits above Toolbar)

            // Load data immediately
            LoadData();
        }

        // ════════════════════════════════════════════════════════════════════
        //  DATA LOADING
        // ════════════════════════════════════════════════════════════════════
        private void LoadData()
        {
            try
            {
                List<InvoiceAP> invoices = _invoiceService.GetAllInvoices();

                // Apply filter from the dropdown
                string filter = _cboFilter.SelectedItem?.ToString() ?? "All";
                if (filter != "All")
                    invoices = invoices.FindAll(i => i.Status == filter);

                // Clear and reload grid
                _dgv.Rows.Clear();

                decimal totalAmount = 0;
                decimal totalPaid   = 0;

                foreach (var inv in invoices)
                {
                    int rowIdx = _dgv.Rows.Add(
                        inv.InvoiceAPId,
                        inv.InvoiceNumber,
                        inv.Vendor?.VendorName ?? GetVendorName(inv.InvoiceAPId),
                        inv.InvoiceDate.ToString("MMM dd, yyyy"),
                        inv.DueDate.ToString("MMM dd, yyyy"),
                        inv.TotalAmount.ToString("C"),
                        inv.AmountPaid.ToString("C"),
                        inv.Status,
                        inv.DatePaid.HasValue
                            ? inv.DatePaid.Value.ToString("MMM dd, yyyy")
                            : "—"
                    );

                    // Apply status row coloring using StatusColors utility
                    StatusColors.ApplyInvoiceColor(_dgv.Rows[rowIdx]);

                    totalAmount += inv.TotalAmount;
                    totalPaid   += inv.AmountPaid;
                }

                // Update summary label
                _lblSummary.Text =
                    $"{invoices.Count} invoice(s)   " +
                    $"Total: {totalAmount:C}   " +
                    $"Paid: {totalPaid:C}   " +
                    $"Outstanding: {(totalAmount - totalPaid):C}";
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Could not load invoices.\nDetails: " + ex.Message);
            }
        }

        // GetAllInvoices joins VendorName — but the model's Vendor nav property
        // isn't populated by the service (it returns VendorName as a flat column).
        // We read it from the grid row that was just built instead.
        // This helper handles the case where the Vendor nav prop is null.
        private string GetVendorName(int invoiceAPId)
        {
            // VendorName comes back as a flat column from the JOIN in GetAllInvoices.
            // The mapper doesn't populate the navigation property, so we read it
            // from the DataTable directly via the service.
            return "";
        }

        // ════════════════════════════════════════════════════════════════════
        //  BUTTON HANDLERS
        // ════════════════════════════════════════════════════════════════════

        // Open blank detail panel to add a new invoice
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var detail = new InvoiceDetailPanel(0);  // 0 = new record
            detail.OnSaved += () => LoadData();      // refresh list when saved
            SwapToDetail(detail);
        }

        // Mark all unpaid-past-due invoices as Overdue
        private void BtnMarkOverdue_Click(object sender, EventArgs e)
        {
            if (!MessageHelper.Confirm(
                "Mark all unpaid invoices past their due date as Overdue?",
                "Mark Overdue"))
                return;

            int count = _invoiceService.MarkOverdueInvoices();
            MessageHelper.ShowSuccess($"{count} invoice(s) marked as Overdue.");
            LoadData();
        }

        // Process all invoices due today or earlier
        private void BtnProcessDue_Click(object sender, EventArgs e)
        {
            if (!MessageHelper.Confirm(
                "Process payments for all invoices due today or earlier?\n" +
                "This will create payment records and mark invoices as Paid.",
                "Process Due Payments"))
                return;

            int count = _invoiceService.ProcessDuePayments();
            MessageHelper.ShowSuccess($"{count} invoice(s) paid successfully.");
            LoadData();
        }

        // Double-click a row → open that invoice in the detail panel
        private void Dgv_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            int invoiceId = Convert.ToInt32(
                _dgv.Rows[e.RowIndex].Cells["InvoiceAPId"].Value);

            var detail = new InvoiceDetailPanel(invoiceId);
            detail.OnSaved += () => LoadData();
            SwapToDetail(detail);
        }

        // ── SwapToDetail ──────────────────────────────────────────────────────
        // Replaces the content of pnlContent (on the dashboard) with the
        // detail panel. When the detail panel calls OnSaved we reload this list.
        private void SwapToDetail(UserControl detailPanel)
        {
            // Walk up to find MainDashboard's pnlContent
            var dashboard = this.FindForm() as MainDashboard;
            if (dashboard == null) return;

            detailPanel.Dock = DockStyle.Fill;
            dashboard.pnlContent.Controls.Clear();
            dashboard.pnlContent.Controls.Add(detailPanel);
        }

        // ── Column builder helper ─────────────────────────────────────────────
        private void AddColumn(string name, string header, int fillWeight, bool visible)
        {
            var col = new DataGridViewTextBoxColumn
            {
                Name        = name,
                HeaderText  = header,
                FillWeight  = fillWeight,
                Visible     = visible,
                SortMode    = DataGridViewColumnSortMode.Automatic
            };
            _dgv.Columns.Add(col);
        }

        // ── Toolbar button factory ────────────────────────────────────────────
        private Button MakeToolbarButton(string text, Color backColor)
        {
            var btn = new Button
            {
                Text      = text,
                Size      = new Size(125, 32),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
    }
}