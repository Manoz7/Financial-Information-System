using FIS.Models;
using FIS.Services;
using FIS.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FIS.Forms
{
    // Case §2.1.3: FIS does NOT create customer orders — POPS does.
    // FIS reads this file to watch for Status = "Shipped" orders,
    // which trigger automatic bill generation.
    // This panel is intentionally read-only — no add, no edit buttons.
    public class CustomerOrderListPanel : UserControl
    {
        private readonly CustomerOrderService _orderService = new CustomerOrderService();
        private readonly CustomerBillService _billService = new CustomerBillService();

        private DataGridView _dgv;
        private ComboBox _cboFilter;
        private Label _lblSummary;
        private Label _lblUnbilledAlert;

        public CustomerOrderListPanel()
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
                Text = "Customer Orders",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "Read-only view from POPS — Shipped orders trigger automatic billing",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(160, 200, 240),
                AutoSize = true,
                Location = new Point(22, 40)
            });

            // ── Unbilled alert bar ────────────────────────────────────────────
            // Prominently shows how many shipped orders are waiting to be billed.
            // Disappears when all shipped orders have bills.
            var pnlAlert = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(255, 244, 220)
            };
            pnlAlert.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(230, 180, 80), 1),
                    0, 35, pnlAlert.Width, 35);

            _lblUnbilledAlert = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 90, 0),
                AutoSize = true,
                Location = new Point(16, 10)
            };
            pnlAlert.Controls.Add(_lblUnbilledAlert);

            // "Generate Bills" shortcut button inside the alert bar
            var btnGenerateFromAlert = new Button
            {
                Text = "Generate Bills Now →",
                Location = new Point(500, 5),
                Size = new Size(170, 26),
                BackColor = Color.FromArgb(200, 130, 20),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGenerateFromAlert.FlatAppearance.BorderSize = 0;
            btnGenerateFromAlert.Click += BtnGenerateBills_Click;
            pnlAlert.Controls.Add(btnGenerateFromAlert);

            // ── Toolbar ───────────────────────────────────────────────────────
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White
            };
            pnlToolbar.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(220, 225, 235), 1),
                    0, 51, pnlToolbar.Width, 51);

            var lblFilter = new Label
            {
                Text = "Filter:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 110, 130),
                AutoSize = true,
                Location = new Point(16, 16)
            };

            _cboFilter = new ComboBox
            {
                Location = new Point(56, 12),
                Size = new Size(160, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            _cboFilter.Items.AddRange(new object[] {
                "All", "Open", "Shipped", "Closed", "Cancelled" });
            _cboFilter.SelectedIndex = 0;
            _cboFilter.SelectedIndexChanged += (s, e) => LoadData();

            // Show only unbilled shipped orders
            var btnShowUnbilled = MakeToolbarButton("Shipped & Unbilled", Color.FromArgb(160, 80, 20));
            btnShowUnbilled.Location = new Point(236, 10);
            btnShowUnbilled.Size = new Size(165, 32);
            btnShowUnbilled.Click += (s, e) => LoadUnbilledOnly();

            var btnRefresh = MakeToolbarButton("↻ Refresh", Color.FromArgb(80, 100, 140));
            btnRefresh.Location = new Point(411, 10);
            btnRefresh.Click += (s, e) => LoadData();

            _lblSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(120, 130, 150),
                AutoSize = true,
                Location = new Point(557, 18)
            };

            pnlToolbar.Controls.AddRange(new Control[] {
                lblFilter, _cboFilter, btnShowUnbilled, btnRefresh, _lblSummary });

            // ── DataGridView ──────────────────────────────────────────────────
            _dgv = new DataGridView
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
            _dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 252);
            _dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(80, 95, 120);
            _dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            _dgv.ColumnHeadersHeight = 36;
            _dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _dgv.RowTemplate.Height = 32;
            _dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 225, 250);
            _dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 40, 80);
            _dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);

            AddColumn("CustomerOrderId", "ID", 30, false);
            AddColumn("CustomerName", "Customer", 160, true);
            AddColumn("ProductDescription", "Product", 180, true);
            AddColumn("Quantity", "Qty", 50, true);
            AddColumn("TotalAmount", "Total", 90, true);
            AddColumn("OrderDate", "Order Date", 110, true);
            AddColumn("ShipDate", "Ship Date", 110, true);
            AddColumn("Status", "Status", 90, true);
            AddColumn("BilledStatus", "Billed?", 90, true);
            _dgv.Columns["CustomerOrderId"].Visible = false;

            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 16),
                BackColor = Color.FromArgb(240, 243, 248)
            };
            pnlGrid.Controls.Add(_dgv);

            // Assemble
            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlAlert);
            this.Controls.Add(pnlHeader);

            LoadData();
        }

        // ── Load all orders (with filter) ─────────────────────────────────────
        private void LoadData()
        {
            try
            {
                List<CustomerOrder> orders = _orderService.GetAllOrders();

                string filter = _cboFilter.SelectedItem?.ToString() ?? "All";
                if (filter != "All")
                    orders = orders.FindAll(o => o.Status == filter);

                PopulateGrid(orders);
                UpdateAlertBar();
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Could not load orders.\nDetails: " + ex.Message);
            }
        }

        // ── Load only shipped-and-unbilled orders ─────────────────────────────
        private void LoadUnbilledOnly()
        {
            try
            {
                var orders = _orderService.GetShippedUnbilledOrders();
                _cboFilter.SelectedIndex = 0; // reset filter to "All" label
                PopulateGrid(orders);
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Could not load unbilled orders.\nDetails: " + ex.Message);
            }
        }

        // ── Fill the grid ─────────────────────────────────────────────────────
        private void PopulateGrid(List<CustomerOrder> orders)
        {
            _dgv.Rows.Clear();

            decimal totalValue = 0;

            foreach (var o in orders)
            {
                // Check if this shipped order has been billed
                bool billed = o.Status == "Shipped"
                    ? _orderService.OrderAlreadyBilled(o.CustomerOrderId)
                    : true; // non-shipped orders don't need a bill yet

                string billedDisplay;
                if (o.Status == "Shipped")
                    billedDisplay = billed ? "✓ Billed" : "⚠ Not Billed";
                else if (o.Status == "Open")
                    billedDisplay = "Pending ship";
                else
                    billedDisplay = "—";

                int rowIdx = _dgv.Rows.Add(
                    o.CustomerOrderId,
                    $"Customer #{o.CustomerId}",
                    o.ProductDescription,
                    o.Quantity,
                    o.TotalAmount.ToString("C"),
                    o.OrderDate.ToString("MMM dd, yyyy"),
                    o.ShipDate.HasValue ? o.ShipDate.Value.ToString("MMM dd, yyyy") : "—",
                    o.Status,
                    billedDisplay);

                // Color by order status
                ApplyOrderColor(_dgv.Rows[rowIdx], o.Status, billed);

                totalValue += o.TotalAmount;
            }

            _lblSummary.Text =
                $"{orders.Count} order(s)   " +
                $"Total value: {totalValue:C}";
        }

        // ── Alert bar: show count of unbilled shipped orders ──────────────────
        private void UpdateAlertBar()
        {
            var unbilled = _orderService.GetShippedUnbilledOrders();
            if (unbilled.Count > 0)
            {
                _lblUnbilledAlert.Text =
                    $"⚠  {unbilled.Count} shipped order(s) have not been billed yet. " +
                    "Generate bills to avoid delayed payments.";
            }
            else
            {
                _lblUnbilledAlert.Text = "✓  All shipped orders have been billed.";
                _lblUnbilledAlert.ForeColor = Color.FromArgb(30, 100, 50);
            }
        }

        // ── Generate bills shortcut ───────────────────────────────────────────
        private void BtnGenerateBills_Click(object sender, EventArgs e)
        {
            var unbilled = _orderService.GetShippedUnbilledOrders();
            if (unbilled.Count == 0)
            {
                MessageHelper.ShowWarning("All shipped orders are already billed.");
                return;
            }

            if (!MessageHelper.Confirm(
                $"Generate bills for {unbilled.Count} unbilled shipped order(s)?",
                "Generate Bills"))
                return;

            int count = _billService.GeneratePendingBills();
            MessageHelper.ShowSuccess(
                $"{count} bill(s) generated.\n\nGo to Customer Bills to view them.");
            LoadData();
        }

        // ── Row color ─────────────────────────────────────────────────────────
        private void ApplyOrderColor(DataGridViewRow row, string status, bool billed)
        {
            if (status == "Shipped" && !billed)
                // Shipped but not billed — needs attention
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 244, 200);
            else if (status == "Shipped")
                row.DefaultCellStyle.BackColor = Color.FromArgb(204, 229, 255);
            else if (status == "Closed")
                row.DefaultCellStyle.BackColor = Color.FromArgb(204, 255, 204);
            else if (status == "Cancelled")
                row.DefaultCellStyle.BackColor = Color.FromArgb(220, 220, 220);
            else
                row.DefaultCellStyle.BackColor = Color.White;
        }

        private void AddColumn(string name, string header, int fillWeight, bool visible)
        {
            _dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                FillWeight = fillWeight,
                Visible = visible,
                SortMode = DataGridViewColumnSortMode.Automatic
            });
        }

        private Button MakeToolbarButton(string text, Color backColor)
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