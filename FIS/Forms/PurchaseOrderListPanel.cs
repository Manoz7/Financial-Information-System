using FIS.Models;
using FIS.Services;
using FIS.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FIS.Forms
{
    public class PurchaseOrderListPanel : UserControl
    {
        private readonly PurchaseOrderService _poService = new PurchaseOrderService();

        private DataGridView _dgv;
        private ComboBox _cboFilter;
        private Label _lblSummary;

        public PurchaseOrderListPanel()
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
                Text = "Purchase Orders",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "Track vendor purchase orders — double-click to view line items and take action",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(160, 200, 240),
                AutoSize = true,
                Location = new Point(22, 40)
            });

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
                Size = new Size(150, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            _cboFilter.Items.AddRange(new object[] {
                "All", "Pending", "Delivered", "Cancelled" });
            _cboFilter.SelectedIndex = 0;
            _cboFilter.SelectedIndexChanged += (s, e) => LoadData();

            var btnRefresh = MakeToolbarButton("↻ Refresh", Color.FromArgb(80, 100, 140));
            btnRefresh.Location = new Point(226, 10);
            btnRefresh.Click += (s, e) => LoadData();

            _lblSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(120, 130, 150),
                AutoSize = true,
                Location = new Point(372, 18)
            };

            pnlToolbar.Controls.AddRange(new Control[] {
                lblFilter, _cboFilter, btnRefresh, _lblSummary });

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
            _dgv.CellDoubleClick += Dgv_CellDoubleClick;

            AddColumn("PurchaseOrderId", "ID", 40, false);
            AddColumn("VendorName", "Vendor", 180, true);
            AddColumn("OrderDate", "Order Date", 110, true);
            AddColumn("ExpectedDeliveryDate", "Expected", 110, true);
            AddColumn("ActualDeliveryDate", "Delivered", 110, true);
            AddColumn("TotalAmount", "Total", 100, true);
            AddColumn("Status", "Status", 90, true);
            _dgv.Columns["PurchaseOrderId"].Visible = false;

            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 16),
                BackColor = Color.FromArgb(240, 243, 248)
            };
            pnlGrid.Controls.Add(_dgv);

            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlHeader);

            LoadData();
        }

        // ── Load data ─────────────────────────────────────────────────────────
        private void LoadData()
        {
            try
            {
                List<PurchaseOrder> orders = _poService.GetAllPurchaseOrders();

                string filter = _cboFilter.SelectedItem?.ToString() ?? "All";
                if (filter != "All")
                    orders = orders.FindAll(o => o.Status == filter);

                _dgv.Rows.Clear();

                decimal totalValue = 0;
                int pendingCount = 0;

                foreach (var po in orders)
                {
                    int rowIdx = _dgv.Rows.Add(
                        po.PurchaseOrderId,
                        $"Vendor #{po.VendorId}",
                        po.OrderDate.ToString("MMM dd, yyyy"),
                        po.ExpectedDeliveryDate.HasValue
                            ? po.ExpectedDeliveryDate.Value.ToString("MMM dd, yyyy") : "—",
                        po.ActualDeliveryDate.HasValue
                            ? po.ActualDeliveryDate.Value.ToString("MMM dd, yyyy") : "—",
                        po.TotalAmount.ToString("C"),
                        po.Status);

                    ApplyPOColor(_dgv.Rows[rowIdx], po.Status);

                    totalValue += po.TotalAmount;
                    if (po.Status == "Pending") pendingCount++;
                }

                _lblSummary.Text =
                    $"{orders.Count} order(s)   " +
                    $"Pending: {pendingCount}   " +
                    $"Total value: {totalValue:C}";
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Could not load purchase orders.\nDetails: " + ex.Message);
            }
        }

        // ── Double-click → open detail panel ─────────────────────────────────
        private void Dgv_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int poId = Convert.ToInt32(
                _dgv.Rows[e.RowIndex].Cells["PurchaseOrderId"].Value);

            var detail = new PurchaseOrderDetailPanel(poId);
            detail.OnSaved += () => LoadData();
            SwapToDetail(detail);
        }

        private void SwapToDetail(UserControl panel)
        {
            var dashboard = this.FindForm() as MainDashboard;
            if (dashboard == null) return;
            panel.Dock = DockStyle.Fill;
            dashboard.pnlContent.Controls.Clear();
            dashboard.pnlContent.Controls.Add(panel);
        }

        // ── Row color by status ───────────────────────────────────────────────
        private void ApplyPOColor(DataGridViewRow row, string status)
        {
            if (status == "Delivered")
                row.DefaultCellStyle.BackColor = Color.FromArgb(204, 255, 204);
            else if (status == "Cancelled")
                row.DefaultCellStyle.BackColor = Color.FromArgb(220, 220, 220);
            else if (status == "Pending")
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 204);
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