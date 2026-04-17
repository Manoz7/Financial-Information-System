using FIS.Models;
using FIS.Services;
using FIS.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FIS.Forms
{
    // Case §2.1.4: "a report on accounts receivable should also be created"
    // Case §2.1.4: "a weekly report showing new orders that have been delivered"
    // Case §2.1.3: FIS must track outstanding balances and incoming payments.
    //
    // Three tabs:
    //   1. Outstanding  — all unpaid/partial/overdue bills with aging
    //   2. Weekly       — shipped orders and payments received this week
    //   3. Full Ledger  — every bill with full payment history
    public class ARReportPanel : UserControl
    {
        private readonly CustomerBillService _billService = new CustomerBillService();
        private readonly CustomerOrderService _orderService = new CustomerOrderService();
        private readonly PaymentARService _paymentService = new PaymentARService();

        // Tab buttons
        private Button _btnTabOutstanding;
        private Button _btnTabWeekly;
        private Button _btnTabLedger;

        // Content panels
        private Panel _pnlOutstanding;
        private Panel _pnlWeekly;
        private Panel _pnlLedger;

        // Outstanding tab
        private DataGridView _dgvOutstanding;
        private Label _lblOutstandingSummary;

        // Weekly tab
        private DataGridView _dgvShipped;
        private DataGridView _dgvPayments;
        private Label _lblWeeklySummary;

        // Ledger tab
        private DataGridView _dgvLedger;
        private Label _lblLedgerSummary;

        public ARReportPanel()
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
                Text = "Accounts Receivable Report",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "Outstanding balances · Weekly shipped & collected · Full AR ledger",
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

            _btnTabOutstanding = MakeTabButton("Outstanding", 0);
            _btnTabWeekly = MakeTabButton("This Week", 200);
            _btnTabLedger = MakeTabButton("Full Ledger", 400);

            _btnTabOutstanding.Click += (s, e) => ShowTab("Outstanding");
            _btnTabWeekly.Click += (s, e) => ShowTab("Weekly");
            _btnTabLedger.Click += (s, e) => ShowTab("Ledger");

            pnlTabs.Controls.AddRange(new Control[] {
                _btnTabOutstanding, _btnTabWeekly, _btnTabLedger });

            // ── Tab content panels ────────────────────────────────────────────
            _pnlOutstanding = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 243, 248),
                Visible = false
            };
            BuildOutstandingTab();

            _pnlWeekly = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 243, 248),
                Visible = false
            };
            BuildWeeklyTab();

            _pnlLedger = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 243, 248),
                Visible = false
            };
            BuildLedgerTab();

            // Assemble
            this.Controls.Add(_pnlLedger);
            this.Controls.Add(_pnlWeekly);
            this.Controls.Add(_pnlOutstanding);
            this.Controls.Add(pnlTabs);
            this.Controls.Add(pnlHeader);

            ShowTab("Outstanding");
        }

        // ════════════════════════════════════════════════════════════════════
        //  TAB 1 — OUTSTANDING BALANCES (AR Aging)
        // ════════════════════════════════════════════════════════════════════
        private void BuildOutstandingTab()
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
            btnRefresh.Click += (s, e) => LoadOutstandingData();
            pnlTop.Controls.Add(btnRefresh);

            var btnMarkOverdue = MakeActionButton("Mark Overdue", Color.FromArgb(160, 60, 50));
            btnMarkOverdue.Location = new Point(150, 10);
            btnMarkOverdue.Click += BtnMarkOverdue_Click;
            pnlTop.Controls.Add(btnMarkOverdue);

            _lblOutstandingSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(120, 130, 150),
                AutoSize = true,
                Location = new Point(295, 18)
            };
            pnlTop.Controls.Add(_lblOutstandingSummary);

            _dgvOutstanding = BuildReportGrid();

            AddColumn(_dgvOutstanding, "BillId", "Bill #", 50);
            AddColumn(_dgvOutstanding, "Customer", "Customer", 160);
            AddColumn(_dgvOutstanding, "BillDate", "Bill Date", 105);
            AddColumn(_dgvOutstanding, "DueDate", "Due Date", 105);
            AddColumn(_dgvOutstanding, "DaysAging", "Days Aging", 90);
            AddColumn(_dgvOutstanding, "Total", "Total Billed", 100);
            AddColumn(_dgvOutstanding, "Balance", "Balance Due", 100);
            AddColumn(_dgvOutstanding, "Status", "Status", 90);

            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 16) };
            pnlGrid.Controls.Add(_dgvOutstanding);

            _pnlOutstanding.Controls.Add(pnlGrid);
            _pnlOutstanding.Controls.Add(pnlTop);

            LoadOutstandingData();
        }

        private void LoadOutstandingData()
        {
            try
            {
                List<CustomerBill> bills = _billService.GetOutstandingBills();
                _dgvOutstanding.Rows.Clear();

                decimal totalOutstanding = 0;
                int overdueCount = 0;

                foreach (var b in bills)
                {
                    int daysAging = (DateTime.Today - b.BillDate.Date).Days;

                    int rowIdx = _dgvOutstanding.Rows.Add(
                        b.CustomerBillId,
                        $"Customer #{b.CustomerId}",
                        b.BillDate.ToString("MMM dd, yyyy"),
                        b.DueDate.ToString("MMM dd, yyyy"),
                        daysAging,
                        b.TotalAmountDue.ToString("C"),
                        b.BalanceRemaining.ToString("C"),
                        b.Status);

                    // Color by status
                    if (b.Status == "Overdue")
                    {
                        _dgvOutstanding.Rows[rowIdx].DefaultCellStyle.BackColor =
                            daysAging > 60
                                ? Color.FromArgb(255, 160, 160)
                                : Color.FromArgb(255, 204, 204);
                        overdueCount++;
                    }
                    else if (b.Status == "PartiallyPaid")
                        _dgvOutstanding.Rows[rowIdx].DefaultCellStyle.BackColor =
                            Color.FromArgb(255, 255, 204);
                    else
                        _dgvOutstanding.Rows[rowIdx].DefaultCellStyle.BackColor =
                            Color.White;

                    totalOutstanding += b.BalanceRemaining;
                }

                _lblOutstandingSummary.Text =
                    $"{bills.Count} outstanding bill(s)   " +
                    $"Overdue: {overdueCount}   " +
                    $"Total owed: {totalOutstanding:C}";

                _lblOutstandingSummary.ForeColor = overdueCount > 0
                    ? Color.FromArgb(180, 40, 40)
                    : Color.FromArgb(80, 100, 130);
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Could not load outstanding bills.\nDetails: " + ex.Message);
            }
        }

        private void BtnMarkOverdue_Click(object sender, EventArgs e)
        {
            if (!MessageHelper.Confirm(
                "Mark all unpaid bills past their due date as Overdue?",
                "Mark Overdue"))
                return;

            int count = _billService.MarkOverdueBills();
            MessageHelper.ShowSuccess($"{count} bill(s) marked as Overdue.");
            LoadOutstandingData();
        }

        // ════════════════════════════════════════════════════════════════════
        //  TAB 2 — THIS WEEK (shipped orders + payments received)
        // ════════════════════════════════════════════════════════════════════
        private void BuildWeeklyTab()
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
            btnRefresh.Click += (s, e) => LoadWeeklyData();
            pnlTop.Controls.Add(btnRefresh);

            _lblWeeklySummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(120, 130, 150),
                AutoSize = true,
                Location = new Point(160, 18)
            };
            pnlTop.Controls.Add(_lblWeeklySummary);

            // Shipped orders this week section
            var lblShippedTitle = new Label
            {
                Text = "Orders Shipped This Week",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 55, 100),
                AutoSize = true,
                Location = new Point(16, 8)
            };

            _dgvShipped = BuildReportGrid();
            _dgvShipped.Size = new Size(860, 180);

            AddColumn(_dgvShipped, "OrderId", "Order #", 60);
            AddColumn(_dgvShipped, "Customer", "Customer", 160);
            AddColumn(_dgvShipped, "Product", "Product", 180);
            AddColumn(_dgvShipped, "ShipDate", "Ship Date", 110);
            AddColumn(_dgvShipped, "TotalAmount", "Amount", 100);
            AddColumn(_dgvShipped, "Billed", "Billed?", 90);

            // Payments received this week section
            var lblPayTitle = new Label
            {
                Text = "Payments Received This Week",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 55, 100),
                AutoSize = true,
                Location = new Point(16, 8)
            };

            _dgvPayments = BuildReportGrid();
            _dgvPayments.Size = new Size(860, 160);

            AddColumn(_dgvPayments, "PaymentId", "Payment #", 70);
            AddColumn(_dgvPayments, "BillId", "Bill #", 60);
            AddColumn(_dgvPayments, "Customer", "Customer", 160);
            AddColumn(_dgvPayments, "DateReceived", "Date", 110);
            AddColumn(_dgvPayments, "Amount", "Amount", 100);
            AddColumn(_dgvPayments, "Method", "Method", 110);
            AddColumn(_dgvPayments, "Reference", "Reference", 130);

            // Two-section layout inside the tab panel
            var pnlShippedSection = new Panel
            {
                Dock = DockStyle.Top,
                Height = 230,
                Padding = new Padding(16, 8, 16, 8),
                BackColor = Color.FromArgb(240, 243, 248)
            };
            pnlShippedSection.Controls.Add(_dgvShipped);
            pnlShippedSection.Controls.Add(lblShippedTitle);

            var pnlPaySection = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 8, 16, 16),
                BackColor = Color.FromArgb(240, 243, 248)
            };
            pnlPaySection.Controls.Add(_dgvPayments);
            pnlPaySection.Controls.Add(lblPayTitle);

            _pnlWeekly.Controls.Add(pnlPaySection);
            _pnlWeekly.Controls.Add(pnlShippedSection);
            _pnlWeekly.Controls.Add(pnlTop);

            LoadWeeklyData();
        }

        private void LoadWeeklyData()
        {
            try
            {
                // Shipped orders this week
                var shippedOrders = _orderService.GetOrdersByStatus("Shipped");
                DateTime weekStart = DateTime.Today.AddDays(-7);
                shippedOrders = shippedOrders.FindAll(o =>
                    o.ShipDate.HasValue && o.ShipDate.Value.Date >= weekStart);

                _dgvShipped.Rows.Clear();
                decimal shippedValue = 0;

                foreach (var o in shippedOrders)
                {
                    bool billed = _orderService.OrderAlreadyBilled(o.CustomerOrderId);
                    int rowIdx = _dgvShipped.Rows.Add(
                        o.CustomerOrderId,
                        $"Customer #{o.CustomerId}",
                        o.ProductDescription,
                        o.ShipDate.Value.ToString("MMM dd, yyyy"),
                        o.TotalAmount.ToString("C"),
                        billed ? "✓ Yes" : "⚠ Not yet");

                    _dgvShipped.Rows[rowIdx].DefaultCellStyle.BackColor = billed
                        ? Color.FromArgb(230, 250, 235)
                        : Color.FromArgb(255, 244, 200);

                    shippedValue += o.TotalAmount;
                }

                // Payments received this week
                List<PaymentAR> payments = _paymentService.GetPaymentsByDateRange(
                    weekStart, DateTime.Today.AddDays(1));

                _dgvPayments.Rows.Clear();
                decimal collectedValue = 0;

                foreach (var p in payments)
                {
                    _dgvPayments.Rows.Add(
                        p.PaymentARId,
                        p.CustomerBillId,
                        $"Bill #{p.CustomerBillId}",
                        p.DateReceived.ToString("MMM dd, yyyy"),
                        p.AmountReceived.ToString("C"),
                        p.PaymentMethod ?? "—",
                        string.IsNullOrEmpty(p.ReferenceNumber) ? "—" : p.ReferenceNumber);

                    _dgvPayments.Rows[_dgvPayments.Rows.Count - 1]
                        .DefaultCellStyle.BackColor = Color.FromArgb(230, 250, 235);

                    collectedValue += p.AmountReceived;
                }

                _lblWeeklySummary.Text =
                    $"Shipped this week: {shippedOrders.Count} order(s) worth {shippedValue:C}   " +
                    $"Collected: {collectedValue:C} across {payments.Count} payment(s)";
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Could not load weekly report.\nDetails: " + ex.Message);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  TAB 3 — FULL AR LEDGER
        // ════════════════════════════════════════════════════════════════════
        private void BuildLedgerTab()
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
            btnRefresh.Click += (s, e) => LoadLedgerData();
            pnlTop.Controls.Add(btnRefresh);

            _lblLedgerSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(120, 130, 150),
                AutoSize = true,
                Location = new Point(160, 18)
            };
            pnlTop.Controls.Add(_lblLedgerSummary);

            _dgvLedger = BuildReportGrid();

            AddColumn(_dgvLedger, "BillId", "Bill #", 50);
            AddColumn(_dgvLedger, "Customer", "Customer", 150);
            AddColumn(_dgvLedger, "BillDate", "Bill Date", 105);
            AddColumn(_dgvLedger, "DueDate", "Due Date", 105);
            AddColumn(_dgvLedger, "Total", "Total", 90);
            AddColumn(_dgvLedger, "Paid", "Paid", 90);
            AddColumn(_dgvLedger, "Balance", "Balance", 90);
            AddColumn(_dgvLedger, "Status", "Status", 95);

            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 16) };
            pnlGrid.Controls.Add(_dgvLedger);

            _pnlLedger.Controls.Add(pnlGrid);
            _pnlLedger.Controls.Add(pnlTop);

            LoadLedgerData();
        }

        private void LoadLedgerData()
        {
            try
            {
                List<CustomerBill> bills = _billService.GetAccountsReceivableReport();
                _dgvLedger.Rows.Clear();

                decimal totalBilled = 0;
                decimal totalCollected = 0;
                decimal totalOutstanding = 0;

                foreach (var b in bills)
                {
                    decimal paid = b.TotalAmountDue - b.BalanceRemaining;

                    int rowIdx = _dgvLedger.Rows.Add(
                        b.CustomerBillId,
                        $"Customer #{b.CustomerId}",
                        b.BillDate.ToString("MMM dd, yyyy"),
                        b.DueDate.ToString("MMM dd, yyyy"),
                        b.TotalAmountDue.ToString("C"),
                        paid.ToString("C"),
                        b.BalanceRemaining.ToString("C"),
                        b.Status);

                    // Status coloring
                    if (b.Status == "Overdue")
                        _dgvLedger.Rows[rowIdx].DefaultCellStyle.BackColor =
                            Color.FromArgb(255, 204, 204);
                    else if (b.Status == "PartiallyPaid")
                        _dgvLedger.Rows[rowIdx].DefaultCellStyle.BackColor =
                            Color.FromArgb(255, 255, 204);
                    else if (b.Status == "Paid")
                        _dgvLedger.Rows[rowIdx].DefaultCellStyle.BackColor =
                            Color.FromArgb(204, 255, 204);
                    else
                        _dgvLedger.Rows[rowIdx].DefaultCellStyle.BackColor =
                            Color.White;

                    totalBilled += b.TotalAmountDue;
                    totalCollected += paid;
                    totalOutstanding += b.BalanceRemaining;
                }

                _lblLedgerSummary.Text =
                    $"{bills.Count} bill(s)   " +
                    $"Total billed: {totalBilled:C}   " +
                    $"Collected: {totalCollected:C}   " +
                    $"Outstanding: {totalOutstanding:C}";
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Could not load AR ledger.\nDetails: " + ex.Message);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  TAB SWITCHING
        // ════════════════════════════════════════════════════════════════════
        private void ShowTab(string tab)
        {
            _pnlOutstanding.Visible = tab == "Outstanding";
            _pnlWeekly.Visible = tab == "Weekly";
            _pnlLedger.Visible = tab == "Ledger";

            SetActiveTab(_btnTabOutstanding, tab == "Outstanding");
            SetActiveTab(_btnTabWeekly, tab == "Weekly");
            SetActiveTab(_btnTabLedger, tab == "Ledger");

            if (tab == "Weekly") LoadWeeklyData();
            if (tab == "Ledger") LoadLedgerData();
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