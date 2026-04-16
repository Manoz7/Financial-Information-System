using FIS.Models;
using FIS.Services;
using FIS.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FIS.Forms
{
    public class VendorListPanel : UserControl
    {
        private readonly VendorService _vendorService = new VendorService();

        private DataGridView _dgv;
        private ComboBox _cboFilter;
        private Label _lblSummary;

        public VendorListPanel()
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
                Text = "Vendors",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "Manage vendor relationships and contact information",
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
                "All", "Active", "Suspended", "Terminated" });
            _cboFilter.SelectedIndex = 0;
            _cboFilter.SelectedIndexChanged += (s, e) => LoadData();

            var btnAdd = MakeToolbarButton("+ New Vendor", Color.FromArgb(26, 55, 100));
            btnAdd.Location = new Point(226, 10);
            btnAdd.Click += BtnAdd_Click;

            var btnRefresh = MakeToolbarButton("↻ Refresh", Color.FromArgb(80, 100, 140));
            btnRefresh.Location = new Point(360, 10);
            btnRefresh.Click += (s, e) => LoadData();

            _lblSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(120, 130, 150),
                AutoSize = true,
                Location = new Point(506, 18)
            };

            pnlToolbar.Controls.AddRange(new Control[] {
                lblFilter, _cboFilter, btnAdd, btnRefresh, _lblSummary });

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

            // Columns
            AddColumn("VendorId", "ID", 30, false);
            AddColumn("VendorName", "Vendor Name", 180, true);
            AddColumn("ContactName", "Contact", 130, true);
            AddColumn("Phone", "Phone", 110, true);
            AddColumn("Email", "Email", 160, true);
            AddColumn("Address", "Address", 180, true);
            AddColumn("RelationshipStatus", "Status", 80, true);
            _dgv.Columns["VendorId"].Visible = false;

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
            this.Controls.Add(pnlHeader);

            LoadData();
        }

        // ── Load data ─────────────────────────────────────────────────────────
        private void LoadData()
        {
            try
            {
                List<Vendor> vendors = _vendorService.GetAllVendors();

                string filter = _cboFilter.SelectedItem?.ToString() ?? "All";
                if (filter != "All")
                    vendors = vendors.FindAll(v => v.RelationshipStatus == filter);

                _dgv.Rows.Clear();

                foreach (var v in vendors)
                {
                    int rowIdx = _dgv.Rows.Add(
                        v.VendorId,
                        v.VendorName,
                        v.ContactName,
                        v.Phone,
                        v.Email,
                        v.Address,
                        v.RelationshipStatus);

                    string status = v.RelationshipStatus;
                    if (status == "Terminated")
                        _dgv.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(255, 204, 204);
                    else if (status == "Suspended")
                        _dgv.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 204);
                    else
                        _dgv.Rows[rowIdx].DefaultCellStyle.BackColor = Color.White;
                }

                int active = vendors.FindAll(v => v.RelationshipStatus == "Active").Count;
                int suspended = vendors.FindAll(v => v.RelationshipStatus == "Suspended").Count;
                int terminated = vendors.FindAll(v => v.RelationshipStatus == "Terminated").Count;

                _lblSummary.Text =
                    $"{vendors.Count} vendor(s)   " +
                    $"Active: {active}   " +
                    $"Suspended: {suspended}   " +
                    $"Terminated: {terminated}";
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Could not load vendors.\nDetails: " + ex.Message);
            }
        }

        // ── Add new vendor ────────────────────────────────────────────────────
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var detail = new VendorDetailPanel(0);
            detail.OnSaved += () => LoadData();
            SwapToDetail(detail);
        }

        // ── Double-click → edit ───────────────────────────────────────────────
        private void Dgv_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int vendorId = Convert.ToInt32(_dgv.Rows[e.RowIndex].Cells["VendorId"].Value);
            var detail = new VendorDetailPanel(vendorId);
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