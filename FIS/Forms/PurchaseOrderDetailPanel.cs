using FIS.Models;
using FIS.Services;
using FIS.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FIS.Forms
{
    // Shows a purchase order's header details and its line items in a sub-grid.
    // Actions available: Mark as Delivered (Pending only), Cancel (Pending only).
    // This is the most complex detail panel — it has a master/detail layout
    // with a summary card on top and a line-items grid below.
    public class PurchaseOrderDetailPanel : UserControl
    {
        public event Action OnSaved;

        private readonly PurchaseOrderService _poService = new PurchaseOrderService();

        private readonly int _poId;
        private PurchaseOrder _po;

        public PurchaseOrderDetailPanel(int poId)
        {
            _poId = poId;
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(240, 243, 248);
            BuildUI();
        }

        private void BuildUI()
        {
            _po = _poService.GetById(_poId);

            // ── Header ────────────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(26, 55, 100)
            };
            pnlHeader.Controls.Add(new Label
            {
                Text = $"Purchase Order #{_poId}",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = _po != null
                    ? $"Vendor #{_po.VendorId}  —  Status: {_po.Status}"
                    : "Purchase order details",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(160, 200, 240),
                AutoSize = true,
                Location = new Point(22, 40)
            });

            if (_po == null)
            {
                this.Controls.Add(new Label
                {
                    Text = "Purchase order not found.",
                    Font = new Font("Segoe UI", 12F),
                    ForeColor = Color.FromArgb(180, 40, 40),
                    AutoSize = true,
                    Location = new Point(40, 100)
                });
                this.Controls.Add(pnlHeader);
                return;
            }

            // ── PO Summary card ───────────────────────────────────────────────
            var pnlSummary = new Panel
            {
                Location = new Point(20, 80),
                Size = new Size(500, 200),
                BackColor = Color.White
            };
            pnlSummary.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(26, 55, 100), 4),
                    0, 0, pnlSummary.Width, 0);

            int y = 16; int lx = 16; int vx = 200;

            AddRow(pnlSummary, "Vendor:", $"Vendor #{_po.VendorId}", lx, vx, ref y);
            AddRow(pnlSummary, "Order Date:", _po.OrderDate.ToString("MMM dd, yyyy"), lx, vx, ref y);
            AddRow(pnlSummary, "Expected Delivery:",
                _po.ExpectedDeliveryDate.HasValue
                    ? _po.ExpectedDeliveryDate.Value.ToString("MMM dd, yyyy") : "—", lx, vx, ref y);
            AddRow(pnlSummary, "Actual Delivery:",
                _po.ActualDeliveryDate.HasValue
                    ? _po.ActualDeliveryDate.Value.ToString("MMM dd, yyyy") : "Not yet delivered", lx, vx, ref y);
            AddRow(pnlSummary, "Total Amount:", _po.TotalAmount.ToString("C"), lx, vx, ref y);

            // Status with color
            AddLabel(pnlSummary, "Status:", lx, y);
            var lblStatus = new Label
            {
                Text = _po.Status,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = GetStatusColor(_po.Status),
                AutoSize = true,
                Location = new Point(vx, y)
            };
            pnlSummary.Controls.Add(lblStatus);

            // ── Action buttons card ───────────────────────────────────────────
            var pnlActions = new Panel
            {
                Location = new Point(540, 80),
                Size = new Size(300, 200),
                BackColor = Color.White
            };
            pnlActions.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(30, 120, 60), 4),
                    0, 0, pnlActions.Width, 0);

            pnlActions.Controls.Add(new Label
            {
                Text = "Actions",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 60, 80),
                AutoSize = true,
                Location = new Point(16, 14)
            });

            bool isPending = _po.Status == "Pending";

            // Mark as Delivered button
            var btnDeliver = new Button
            {
                Text = "✓ Mark as Delivered",
                Location = new Point(16, 46),
                Size = new Size(260, 38),
                BackColor = isPending
                    ? Color.FromArgb(30, 120, 60)
                    : Color.FromArgb(200, 210, 220),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = isPending ? Cursors.Hand : Cursors.Default,
                Enabled = isPending
            };
            btnDeliver.FlatAppearance.BorderSize = 0;
            btnDeliver.Click += BtnDeliver_Click;
            pnlActions.Controls.Add(btnDeliver);

            // Cancel PO button
            var btnCancel = new Button
            {
                Text = "✕ Cancel Order",
                Location = new Point(16, 96),
                Size = new Size(260, 38),
                BackColor = isPending
                    ? Color.FromArgb(160, 50, 50)
                    : Color.FromArgb(200, 210, 220),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = isPending ? Cursors.Hand : Cursors.Default,
                Enabled = isPending
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += BtnCancel_Click;
            pnlActions.Controls.Add(btnCancel);

            if (!isPending)
            {
                pnlActions.Controls.Add(new Label
                {
                    Text = $"No actions available — order is {_po.Status}.",
                    Font = new Font("Segoe UI", 8F),
                    ForeColor = Color.FromArgb(140, 150, 165),
                    AutoSize = true,
                    Location = new Point(16, 148)
                });
            }

            // ── Line items section title ───────────────────────────────────────
            var lblItemsTitle = new Label
            {
                Text = "Line Items",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 55, 100),
                AutoSize = true,
                Location = new Point(20, 296)
            };

            // ── Line items DataGridView ────────────────────────────────────────
            var dgvItems = new DataGridView
            {
                Location = new Point(20, 322),
                Size = new Size(820, 200),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.White,
                GridColor = Color.FromArgb(220, 225, 235),
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 9F),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single
            };
            dgvItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 252);
            dgvItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(80, 95, 120);
            dgvItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgvItems.ColumnHeadersHeight = 32;
            dgvItems.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvItems.RowTemplate.Height = 30;
            dgvItems.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 225, 250);
            dgvItems.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 40, 80);
            dgvItems.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);

            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Material", HeaderText = "Material", FillWeight = 200 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "Qty", FillWeight = 60 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "Unit Price", FillWeight = 80 });
            dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineTotal", HeaderText = "Line Total", FillWeight = 90 });

            // Load line items
            LoadLineItems(dgvItems);

            // ── Back button ───────────────────────────────────────────────────
            var btnBack = new Button
            {
                Text = "← Back to List",
                Location = new Point(20, 540),
                Size = new Size(150, 36),
                BackColor = Color.FromArgb(240, 242, 246),
                ForeColor = Color.FromArgb(60, 80, 120),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F),
                Cursor = Cursors.Hand
            };
            btnBack.FlatAppearance.BorderColor = Color.FromArgb(200, 210, 225);
            btnBack.Click += (s, e) => NavigateBack();

            // Assemble
            this.Controls.Add(btnBack);
            this.Controls.Add(dgvItems);
            this.Controls.Add(lblItemsTitle);
            this.Controls.Add(pnlActions);
            this.Controls.Add(pnlSummary);
            this.Controls.Add(pnlHeader);
        }

        // ── Load line items into sub-grid ─────────────────────────────────────
        private void LoadLineItems(DataGridView dgv)
        {
            try
            {
                List<PurchaseOrderItem> items = _poService.GetItemsByPurchaseOrder(_poId);
                dgv.Rows.Clear();

                foreach (var item in items)
                {
                    dgv.Rows.Add(
                        $"Material #{item.RawMaterialId}",
                        item.QuantityOrdered.ToString("N2"),
                        item.UnitPrice.ToString("C"),
                        item.LineTotal.ToString("C"));
                }

                if (items.Count == 0)
                    dgv.Rows.Add("No line items found.", "", "", "");
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Could not load line items.\nDetails: " + ex.Message);
            }
        }

        // ── Mark as Delivered ─────────────────────────────────────────────────
        private void BtnDeliver_Click(object sender, EventArgs e)
        {
            if (!MessageHelper.Confirm(
                $"Mark Purchase Order #{_poId} as Delivered?\n\n" +
                "This records today as the actual delivery date.",
                "Mark as Delivered"))
                return;

            bool ok = _poService.MarkAsDelivered(_poId, DateTime.Today);
            if (ok)
            {
                MessageHelper.ShowSuccess(
                    "Purchase order marked as Delivered.\n\n" +
                    "Remember to record the vendor's invoice in Invoices → New Invoice.");
                OnSaved?.Invoke();
                NavigateBack();
            }
            else
            {
                MessageHelper.ShowError("Could not update purchase order.");
            }
        }

        // ── Cancel PO ────────────────────────────────────────────────────────
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (!MessageHelper.Confirm(
                $"Cancel Purchase Order #{_poId}?\n\n" +
                "Only Pending orders can be cancelled. This cannot be undone.",
                "Cancel Order"))
                return;

            bool ok = _poService.CancelPurchaseOrder(_poId);
            if (ok)
            {
                MessageHelper.ShowSuccess("Purchase order cancelled.");
                OnSaved?.Invoke();
                NavigateBack();
            }
            else
            {
                MessageHelper.ShowError(
                    "Could not cancel order. It may no longer be in Pending status.");
            }
        }

        private void NavigateBack()
        {
            var dashboard = this.FindForm() as MainDashboard;
            if (dashboard == null) return;
            var list = new PurchaseOrderListPanel();
            list.Dock = DockStyle.Fill;
            dashboard.pnlContent.Controls.Clear();
            dashboard.pnlContent.Controls.Add(list);
        }

        // ── Layout helpers ────────────────────────────────────────────────────
        private void AddRow(Panel p, string label, string value, int lx, int vx, ref int y)
        {
            AddLabel(p, label, lx, y);
            p.Controls.Add(new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(40, 50, 70),
                AutoSize = true,
                Location = new Point(vx, y)
            });
            y += 30;
        }

        private void AddLabel(Panel p, string text, int x, int y)
        {
            p.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 110, 130),
                AutoSize = true,
                Location = new Point(x, y + 2)
            });
        }

        private Color GetStatusColor(string status)
        {
            if (status == "Delivered") return Color.FromArgb(30, 130, 60);
            if (status == "Cancelled") return Color.FromArgb(140, 140, 140);
            return Color.FromArgb(160, 110, 0);
        }
    }
}