using FIS.Models;
using FIS.Services;
using FIS.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FIS.Forms
{
    // Case §2.1.4: "a monthly report showing accounts paid" and
    // "reports on Accounts Payable will allow Veronica to track whether
    // invoices are being paid on time."
    //
    // Three tabs:
    //   1. Monthly Paid   — all invoices paid in a selected month
    //   2. Overdue        — all currently overdue invoices
    //   3. All Invoices   — full ledger with totals
    public class APReportPanel : UserControl
    {
        private readonly InvoiceAPService _invoiceService = new InvoiceAPService();

        // Tab buttons
        private Button _btnTabMonthly;
        private Button _btnTabOverdue;
        private Button _btnTabAll;

        // Content panels — one per tab
        private Panel _pnlMonthly;
        private Panel _pnlOverdue;
        private Panel _pnlAll;

        // Monthly tab controls
        private ComboBox _cboMonth;
        private ComboBox _cboYear;
        private DataGridView _dgvMonthly;
        private Label _lblMonthlySummary;

        // Overdue tab controls
        private DataGridView _dgvOverdue;
        private Label _lblOverdueSummary;

        // All invoices tab controls
        private DataGridView _dgvAll;
        private Label _lblAllSummary;

        public APReportPanel()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(240, 243, 248);
            BuildUI();
        }

        private void BuildUI()
        {
            // ── Header ────────────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(26, 55, 100)
            };
            pnlHeader.Controls.Add(new Label
            {
                Text = "Accounts Payable Report",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "Monthly paid report · Overdue invoices · Full AP ledger",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(160, 200, 240),
                AutoSize = true,
                Location = new Point(22, 40)
            });

            // ── Tab bar ───────────────────────────────────────────────────────
            var pnlTabs = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.White
            };
            pnlTabs.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(220, 225, 235), 1),
                    0, 41, pnlTabs.Width, 41);

            _btnTabMonthly = MakeTabButton("Monthly Paid", 0);
            _btnTabOverdue = MakeTabButton("Overdue", 200);
            _btnTabAll = MakeTabButton("All Invoices", 400);

            _btnTabMonthly.Click += (s, e) => ShowTab("Monthly");
            _btnTabOverdue.Click += (s, e) => ShowTab("Overdue");
            _btnTabAll.Click += (s, e) => ShowTab("All");

            pnlTabs.Controls.AddRange(new Control[] {
                _btnTabMonthly, _btnTabOverdue, _btnTabAll });

            // ── Tab content panels ────────────────────────────────────────────
            _pnlMonthly = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 243, 248),
                Visible = false
            };
            BuildMonthlyTab();

            _pnlOverdue = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 243, 248),
                Visible = false
            };
            BuildOverdueTab();

            _pnlAll = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 243, 248),
                Visible = false
            };
            BuildAllTab();

            // Assemble — Fill panels first, then Top panels
            this.Controls.Add(_pnlAll);
            this.Controls.Add(_pnlOverdue);
            this.Controls.Add(_pnlMonthly);
            this.Controls.Add(pnlTabs);
            this.Controls.Add(pnlHeader);

            // Show monthly tab by default
            ShowTab("Monthly");
        }

        // ════════════════════════════════════════════════════════════════════
        //  TAB 1 — MONTHLY PAID
        // ════════════════════════════════════════════════════════════════════
        private void BuildMonthlyTab()
        {
            // Controls row
            var pnlControls = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White
            };
            pnlControls.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(220, 225, 235), 1),
                    0, 51, pnlControls.Width, 51);

            pnlControls.Controls.Add(new Label
            {
                Text = "Month:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 110, 130),
                AutoSize = true,
                Location = new Point(16, 16)
            });

            _cboMonth = new ComboBox
            {
                Location = new Point(65, 12),
                Size = new Size(120, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            string[] months = { "January","February","March","April","May","June",
                                 "July","August","September","October","November","December" };
            _cboMonth.Items.AddRange(months);
            _cboMonth.SelectedIndex = DateTime.Now.Month - 1;
            pnlControls.Controls.Add(_cboMonth);

            pnlControls.Controls.Add(new Label
            {
                Text = "Year:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 110, 130),
                AutoSize = true,
                Location = new Point(200, 16)
            });

            _cboYear = new ComboBox
            {
                Location = new Point(240, 12),
                Size = new Size(90, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            for (int yr = DateTime.Now.Year; yr >= DateTime.Now.Year - 4; yr--)
                _cboYear.Items.Add(yr);
            _cboYear.SelectedIndex = 0;
            pnlControls.Controls.Add(_cboYear);

            var btnRun = MakeActionButton("Run Report", Color.FromArgb(26, 55, 100));
            btnRun.Location = new Point(345, 10);
            btnRun.Click += (s, e) => LoadMonthlyData();
            pnlControls.Controls.Add(btnRun);

            _lblMonthlySummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(120, 130, 150),
                AutoSize = true,
                Location = new Point(490, 18)
            };
            pnlControls.Controls.Add(_lblMonthlySummary);

            // Grid
            _dgvMonthly = BuildReportGrid();

            AddColumn(_dgvMonthly, "InvoiceNumber", "Invoice #", 80);
            AddColumn(_dgvMonthly, "VendorName", "Vendor", 160);
            AddColumn(_dgvMonthly, "InvoiceDate", "Invoice Date", 110);
            AddColumn(_dgvMonthly, "DueDate", "Due Date", 110);
            AddColumn(_dgvMonthly, "DatePaid", "Date Paid", 110);
            AddColumn(_dgvMonthly, "TotalAmount", "Total", 100);
            AddColumn(_dgvMonthly, "AmountPaid", "Paid", 100);
            AddColumn(_dgvMonthly, "OnTime", "Paid On Time?", 100);

            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 16) };
            pnlGrid.Controls.Add(_dgvMonthly);

            _pnlMonthly.Controls.Add(pnlGrid);
            _pnlMonthly.Controls.Add(pnlControls);
        }

        private void LoadMonthlyData()
        {
            try
            {
                int month = _cboMonth.SelectedIndex + 1;
                int year = (int)_cboYear.SelectedItem;

                List<InvoiceAP> invoices = _invoiceService.GetPaidInvoicesForMonth(year, month);
                _dgvMonthly.Rows.Clear();

                decimal totalPaid = 0;
                int onTime = 0;
                int late = 0;

                foreach (var inv in invoices)
                {
                    bool paidOnTime = inv.DatePaid.HasValue &&
                                      inv.DatePaid.Value.Date <= inv.DueDate.Date;

                    int rowIdx = _dgvMonthly.Rows.Add(
                        inv.InvoiceNumber,
                        $"Vendor #{inv.VendorId}",
                        inv.InvoiceDate.ToString("MMM dd, yyyy"),
                        inv.DueDate.ToString("MMM dd, yyyy"),
                        inv.DatePaid.HasValue
                            ? inv.DatePaid.Value.ToString("MMM dd, yyyy") : "—",
                        inv.TotalAmount.ToString("C"),
                        inv.AmountPaid.ToString("C"),
                        paidOnTime ? "✓ Yes" : "✕ Late");

                    // Color on-time vs late
                    _dgvMonthly.Rows[rowIdx].DefaultCellStyle.BackColor = paidOnTime
                        ? Color.FromArgb(230, 250, 235)
                        : Color.FromArgb(255, 230, 225);

                    totalPaid += inv.AmountPaid;
                    if (paidOnTime) onTime++; else late++;
                }

                _lblMonthlySummary.Text =
                    $"{invoices.Count} invoice(s) paid   " +
                    $"Total: {totalPaid:C}   " +
                    $"On time: {onTime}   Late: {late}";
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Could not load monthly report.\nDetails: " + ex.Message);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  TAB 2 — OVERDUE
        // ════════════════════════════════════════════════════════════════════
        private void BuildOverdueTab()
        {
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White
            };
            pnlTop.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(220, 225, 235), 1),
                    0, 51, pnlTop.Width, 51);

            var btnRefresh = MakeActionButton("↻ Refresh", Color.FromArgb(80, 100, 140));
            btnRefresh.Location = new Point(16, 10);
            btnRefresh.Click += (s, e) => LoadOverdueData();
            pnlTop.Controls.Add(btnRefresh);

            _lblOverdueSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 40, 40),
                AutoSize = true,
                Location = new Point(160, 18)
            };
            pnlTop.Controls.Add(_lblOverdueSummary);

            _dgvOverdue = BuildReportGrid();

            AddColumn(_dgvOverdue, "InvoiceNumber", "Invoice #", 80);
            AddColumn(_dgvOverdue, "VendorName", "Vendor", 160);
            AddColumn(_dgvOverdue, "InvoiceDate", "Invoice Date", 110);
            AddColumn(_dgvOverdue, "DueDate", "Due Date", 110);
            AddColumn(_dgvOverdue, "DaysOverdue", "Days Overdue", 100);
            AddColumn(_dgvOverdue, "TotalAmount", "Amount Due", 100);

            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 16) };
            pnlGrid.Controls.Add(_dgvOverdue);

            _pnlOverdue.Controls.Add(pnlGrid);
            _pnlOverdue.Controls.Add(pnlTop);

            LoadOverdueData();
        }

        private void LoadOverdueData()
        {
            try
            {
                // Get all invoices and filter to Overdue status
                List<InvoiceAP> invoices = _invoiceService.GetAllInvoices();
                invoices = invoices.FindAll(i => i.Status == "Overdue");

                _dgvOverdue.Rows.Clear();

                decimal totalOverdue = 0;

                foreach (var inv in invoices)
                {
                    int daysOverdue = (DateTime.Today - inv.DueDate.Date).Days;

                    int rowIdx = _dgvOverdue.Rows.Add(
                        inv.InvoiceNumber,
                        $"Vendor #{inv.VendorId}",
                        inv.InvoiceDate.ToString("MMM dd, yyyy"),
                        inv.DueDate.ToString("MMM dd, yyyy"),
                        daysOverdue,
                        inv.TotalAmount.ToString("C"));

                    // Shade darker red the older the overdue invoice
                    _dgvOverdue.Rows[rowIdx].DefaultCellStyle.BackColor =
                        daysOverdue > 30
                            ? Color.FromArgb(255, 180, 180)
                            : Color.FromArgb(255, 215, 215);

                    totalOverdue += inv.TotalAmount;
                }

                _lblOverdueSummary.Text = invoices.Count == 0
                    ? "✓ No overdue invoices"
                    : $"⚠ {invoices.Count} overdue invoice(s)   Total exposure: {totalOverdue:C}";

                _lblOverdueSummary.ForeColor = invoices.Count == 0
                    ? Color.FromArgb(30, 130, 60)
                    : Color.FromArgb(180, 40, 40);
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Could not load overdue report.\nDetails: " + ex.Message);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  TAB 3 — ALL INVOICES
        // ════════════════════════════════════════════════════════════════════
        private void BuildAllTab()
        {
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White
            };
            pnlTop.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(220, 225, 235), 1),
                    0, 51, pnlTop.Width, 51);

            var btnRefresh = MakeActionButton("↻ Refresh", Color.FromArgb(80, 100, 140));
            btnRefresh.Location = new Point(16, 10);
            btnRefresh.Click += (s, e) => LoadAllData();
            pnlTop.Controls.Add(btnRefresh);

            _lblAllSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(120, 130, 150),
                AutoSize = true,
                Location = new Point(160, 18)
            };
            pnlTop.Controls.Add(_lblAllSummary);

            _dgvAll = BuildReportGrid();

            AddColumn(_dgvAll, "InvoiceNumber", "Invoice #", 80);
            AddColumn(_dgvAll, "VendorName", "Vendor", 150);
            AddColumn(_dgvAll, "InvoiceDate", "Invoice Date", 105);
            AddColumn(_dgvAll, "DueDate", "Due Date", 105);
            AddColumn(_dgvAll, "TotalAmount", "Total", 90);
            AddColumn(_dgvAll, "AmountPaid", "Paid", 90);
            AddColumn(_dgvAll, "Balance", "Balance", 90);
            AddColumn(_dgvAll, "Status", "Status", 85);
            AddColumn(_dgvAll, "DatePaid", "Date Paid", 105);

            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 16) };
            pnlGrid.Controls.Add(_dgvAll);

            _pnlAll.Controls.Add(pnlGrid);
            _pnlAll.Controls.Add(pnlTop);

            LoadAllData();
        }

        private void LoadAllData()
        {
            try
            {
                List<InvoiceAP> invoices = _invoiceService.GetAllInvoices();
                _dgvAll.Rows.Clear();

                decimal totalBilled = 0;
                decimal totalPaid = 0;
                decimal totalUnpaid = 0;

                foreach (var inv in invoices)
                {
                    decimal balance = inv.TotalAmount - inv.AmountPaid;

                    int rowIdx = _dgvAll.Rows.Add(
                        inv.InvoiceNumber,
                        $"Vendor #{inv.VendorId}",
                        inv.InvoiceDate.ToString("MMM dd, yyyy"),
                        inv.DueDate.ToString("MMM dd, yyyy"),
                        inv.TotalAmount.ToString("C"),
                        inv.AmountPaid.ToString("C"),
                        balance.ToString("C"),
                        inv.Status,
                        inv.DatePaid.HasValue
                            ? inv.DatePaid.Value.ToString("MMM dd, yyyy") : "—");

                    StatusColors.ApplyInvoiceColor(_dgvAll.Rows[rowIdx]);

                    totalBilled += inv.TotalAmount;
                    totalPaid += inv.AmountPaid;
                    totalUnpaid += balance;
                }

                _lblAllSummary.Text =
                    $"{invoices.Count} invoice(s)   " +
                    $"Total billed: {totalBilled:C}   " +
                    $"Collected: {totalPaid:C}   " +
                    $"Outstanding: {totalUnpaid:C}";
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Could not load AP ledger.\nDetails: " + ex.Message);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  TAB SWITCHING
        // ════════════════════════════════════════════════════════════════════
        private void ShowTab(string tab)
        {
            _pnlMonthly.Visible = tab == "Monthly";
            _pnlOverdue.Visible = tab == "Overdue";
            _pnlAll.Visible = tab == "All";

            SetActiveTab(_btnTabMonthly, tab == "Monthly");
            SetActiveTab(_btnTabOverdue, tab == "Overdue");
            SetActiveTab(_btnTabAll, tab == "All");

            // Auto-load when switching to overdue or all
            if (tab == "Overdue") LoadOverdueData();
            if (tab == "All") LoadAllData();
        }

        private void SetActiveTab(Button btn, bool active)
        {
            btn.BackColor = active
                ? Color.FromArgb(235, 242, 255)
                : Color.White;
            btn.ForeColor = active
                ? Color.FromArgb(26, 55, 100)
                : Color.FromArgb(100, 110, 130);
            btn.Font = new Font("Segoe UI", 9F,
                active ? FontStyle.Bold : FontStyle.Regular);
        }

        // ════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════════
        private DataGridView BuildReportGrid()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.FromArgb(240, 243, 248),
                GridColor = Color.FromArgb(220, 225, 235),
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 9F),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 252);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(80, 95, 120);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 34;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.RowTemplate.Height = 30;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 225, 250);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 40, 80);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);
            return dgv;
        }

        private void AddColumn(DataGridView dgv, string name, string header, int fillWeight)
        {
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                FillWeight = fillWeight,
                SortMode = DataGridViewColumnSortMode.Automatic
            });
        }

        private Button MakeTabButton(string text, int x)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, 0),
                Size = new Size(190, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(100, 110, 130),
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 252);
            return btn;
        }

        private Button MakeActionButton(string text, Color backColor)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(125, 32),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
    }
}