using FIS.Models;
using FIS.Services;
using FIS.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FIS.Forms
{
    public class RawMaterialListPanel : UserControl
    {
        private readonly RawMaterialService _materialService = new RawMaterialService();
        private readonly PurchaseOrderService _poService = new PurchaseOrderService();

        private DataGridView _dgv;
        private ComboBox _cboFilter;
        private Label _lblSummary;

        public RawMaterialListPanel()
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
                Text = "Raw Materials",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "Monitor inventory levels and trigger auto-reorder",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(160, 200, 240),
                AutoSize = true,
                Location = new Point(22, 40)
            });

            // ── Legend strip ──────────────────────────────────────────────────
            // Raw materials have no Status column — color is based on quantity
            // vs threshold. A legend tells the user what each color means.
            var pnlLegend = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(250, 251, 253)
            };
            pnlLegend.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(220, 225, 235), 1),
                    0, 29, pnlLegend.Width, 29);

            AddLegendItem(pnlLegend, "Below threshold (reorder needed)", Color.FromArgb(255, 204, 204), 16);
            AddLegendItem(pnlLegend, "Within 20% of threshold (warning)", Color.FromArgb(255, 255, 204), 270);
            AddLegendItem(pnlLegend, "Stock OK", Color.White, 524);

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
                Text = "Show:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 110, 130),
                AutoSize = true,
                Location = new Point(16, 16)
            };

            _cboFilter = new ComboBox
            {
                Location = new Point(60, 12),
                Size = new Size(160, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            _cboFilter.Items.AddRange(new object[] {
                "All Materials", "Below Threshold Only" });
            _cboFilter.SelectedIndex = 0;
            _cboFilter.SelectedIndexChanged += (s, e) => LoadData();

            var btnAdd = MakeToolbarButton("+ New Material", Color.FromArgb(26, 55, 100));
            btnAdd.Location = new Point(240, 10);
            btnAdd.Click += (s, e) => SwapToDetail(new RawMaterialDetailPanel(0));

            var btnAutoReorder = MakeToolbarButton("Auto-Reorder Now", Color.FromArgb(160, 80, 20));
            btnAutoReorder.Location = new Point(374, 10);
            btnAutoReorder.Size = new Size(155, 32);
            btnAutoReorder.Click += BtnAutoReorder_Click;

            var btnRefresh = MakeToolbarButton("↻ Refresh", Color.FromArgb(80, 100, 140));
            btnRefresh.Location = new Point(539, 10);
            btnRefresh.Click += (s, e) => LoadData();

            _lblSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(120, 130, 150),
                AutoSize = true,
                Location = new Point(690, 18)
            };

            pnlToolbar.Controls.AddRange(new Control[] {
                lblFilter, _cboFilter, btnAdd, btnAutoReorder, btnRefresh, _lblSummary });

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

            AddColumn("RawMaterialId", "ID", 30, false);
            AddColumn("MaterialName", "Material", 160, true);
            AddColumn("UnitOfMeasure", "Unit", 70, true);
            AddColumn("QuantityOnHand", "On Hand", 90, true);
            AddColumn("ReorderThreshold", "Threshold", 90, true);
            AddColumn("ReorderQuantity", "Reorder Qty", 100, true);
            AddColumn("VendorName", "Vendor", 160, true);
            AddColumn("UnitPrice", "Unit Price", 90, true);
            AddColumn("StockStatus", "Stock Status", 110, true);
            _dgv.Columns["RawMaterialId"].Visible = false;

            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 16),
                BackColor = Color.FromArgb(240, 243, 248)
            };
            pnlGrid.Controls.Add(_dgv);

            // Assemble — Fill first, then Top panels in reverse display order
            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlLegend);
            this.Controls.Add(pnlHeader);

            LoadData();
        }

        // ── Load data ─────────────────────────────────────────────────────────
        private void LoadData()
        {
            try
            {
                List<RawMaterial> materials;
                string filter = _cboFilter.SelectedItem?.ToString() ?? "All Materials";

                materials = filter == "Below Threshold Only"
                    ? _materialService.GetMaterialsBelowThreshold()
                    : _materialService.GetAllRawMaterials();

                _dgv.Rows.Clear();

                int belowCount = 0;
                int warningCount = 0;

                foreach (var m in materials)
                {
                    // Determine stock status text
                    string stockStatus;
                    if (m.QuantityOnHand < m.ReorderThreshold)
                    {
                        stockStatus = "⚠ Reorder Now";
                        belowCount++;
                    }
                    else if (m.QuantityOnHand < m.ReorderThreshold * 1.2m)
                    {
                        stockStatus = "△ Low Stock";
                        warningCount++;
                    }
                    else
                    {
                        stockStatus = "✓ OK";
                    }

                    // VendorName comes from the JOIN in GetAllRawMaterials
                    // The mapper doesn't store it on the model so we pass it
                    // directly from the service call result
                    string vendorName = GetVendorName(m);

                    int rowIdx = _dgv.Rows.Add(
                        m.RawMaterialId,
                        m.MaterialName,
                        m.UnitOfMeasure,
                        m.QuantityOnHand.ToString("N2"),
                        m.ReorderThreshold.ToString("N2"),
                        m.ReorderQuantity.ToString("N2"),
                        vendorName,
                        m.UnitPrice.ToString("C"),
                        stockStatus);

                    // Color row by stock level — no Status column exists
                    // so we use StatusColors.ApplyRawMaterialColor with qty values
                    StatusColors.ApplyRawMaterialColor(
                        _dgv.Rows[rowIdx],
                        m.QuantityOnHand,
                        m.ReorderThreshold);
                }

                _lblSummary.Text =
                    $"{materials.Count} material(s)   " +
                    $"Need reorder: {belowCount}   " +
                    $"Low stock: {warningCount}";
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Could not load materials.\nDetails: " + ex.Message);
            }
        }

        // ── Auto-reorder ──────────────────────────────────────────────────────
        // Generates POs for all materials below threshold in one click.
        // Case §2.1.1: "raw material items should be automatically ordered anytime
        // the stored inventory falls below a certain threshold."
        private void BtnAutoReorder_Click(object sender, EventArgs e)
        {
            var belowThreshold = _materialService.GetMaterialsBelowThreshold();
            if (belowThreshold.Count == 0)
            {
                MessageHelper.ShowWarning("All materials are above their reorder threshold. No orders needed.");
                return;
            }

            if (!MessageHelper.Confirm(
                $"{belowThreshold.Count} material(s) are below threshold.\n\n" +
                "Generate purchase orders for all of them now?",
                "Auto-Reorder"))
                return;

            var generated = _poService.GenerateReorderPurchaseOrders();
            MessageHelper.ShowSuccess(
                $"{generated.Count} purchase order(s) generated.\n\n" +
                "Go to Purchase Orders to review them.");
            LoadData();
        }

        // ── Double-click → edit material ──────────────────────────────────────
        private void Dgv_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = Convert.ToInt32(
                _dgv.Rows[e.RowIndex].Cells["RawMaterialId"].Value);
            SwapToDetail(new RawMaterialDetailPanel(id));
        }

        private void SwapToDetail(UserControl panel)
        {
            var dashboard = this.FindForm() as MainDashboard;
            if (dashboard == null) return;
            panel.Dock = DockStyle.Fill;
            dashboard.pnlContent.Controls.Clear();
            dashboard.pnlContent.Controls.Add(panel);
        }

        // ── VendorName from model ─────────────────────────────────────────────
        // GetAllRawMaterials JOINs Vendors and the mapper stores VendorId,
        // but VendorName isn't on the RawMaterial model. We display VendorId
        // as fallback; the detail panel shows the full vendor name via ComboBox.
        private string GetVendorName(RawMaterial m)
        {
            return $"Vendor #{m.VendorId}";
        }

        // ── Legend item builder ───────────────────────────────────────────────
        private void AddLegendItem(Panel parent, string text, Color swatch, int x)
        {
            var box = new Panel
            {
                Location = new Point(x, 8),
                Size = new Size(14, 14),
                BackColor = swatch,
                BorderStyle = BorderStyle.FixedSingle
            };
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = Color.FromArgb(100, 110, 130),
                AutoSize = true,
                Location = new Point(x + 18, 9)
            };
            parent.Controls.Add(box);
            parent.Controls.Add(lbl);
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