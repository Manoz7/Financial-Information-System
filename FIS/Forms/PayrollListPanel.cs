using FIS.Models;
using FIS.Services;
using FIS.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FIS.Forms
{
    public class PayrollListPanel : UserControl
    {
        private readonly PayrollService _payrollService = new PayrollService();

        private DataGridView _dgv;
        private ComboBox _cboFilter;
        private Label _lblSummary;

        public PayrollListPanel()
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
                Text = "Payroll Records",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "Review and process employee payroll disbursements",
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
            _cboFilter.Items.AddRange(new object[] { "Pending", "Processed", "Failed", "All" });
            _cboFilter.SelectedIndex = 0;   // default: show Pending only
            _cboFilter.SelectedIndexChanged += (s, e) => LoadData();

            // Process all pending — the main bulk action
            var btnProcess = MakeToolbarButton("▶ Process All Pending", Color.FromArgb(30, 120, 60));
            btnProcess.Location = new Point(226, 10);
            btnProcess.Size = new Size(190, 32);
            btnProcess.Click += BtnProcess_Click;

            var btnRefresh = MakeToolbarButton("↻ Refresh", Color.FromArgb(80, 100, 140));
            btnRefresh.Location = new Point(426, 10);
            btnRefresh.Click += (s, e) => LoadData();

            _lblSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(120, 130, 150),
                AutoSize = true,
                Location = new Point(572, 18)
            };

            pnlToolbar.Controls.AddRange(new Control[] {
                lblFilter, _cboFilter, btnProcess, btnRefresh, _lblSummary });

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

            AddColumn("PayrollRecordId", "ID", 30, false);
            AddColumn("EmployeeName", "Employee", 160, true);
            AddColumn("PayPeriodStart", "Period Start", 110, true);
            AddColumn("PayPeriodEnd", "Period End", 110, true);
            AddColumn("HoursWorked", "Hours", 70, true);
            AddColumn("GrossPay", "Gross Pay", 100, true);
            AddColumn("Deductions", "Deductions", 100, true);
            AddColumn("NetPay", "Net Pay", 100, true);
            AddColumn("PaymentMethod", "Method", 110, true);
            AddColumn("Status", "Status", 90, true);
            AddColumn("ConfirmationReference", "Reference", 130, true);
            _dgv.Columns["PayrollRecordId"].Visible = false;

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
                string filter = _cboFilter.SelectedItem?.ToString() ?? "Pending";

                // Use the dedicated pending method when filter is Pending,
                // otherwise fetch all and filter client-side
                List<PayrollRecord> records;
                if (filter == "Pending")
                    records = _payrollService.GetPendingPayrollRecords();
                else
                {
                    // GetByPayPeriod with wide range gives us everything
                    records = _payrollService.GetByPayPeriod(
                        new DateTime(2000, 1, 1), DateTime.Today.AddYears(1));
                    if (filter != "All")
                        records = records.FindAll(r => r.Status == filter);
                }

                _dgv.Rows.Clear();

                decimal totalGross = 0;
                decimal totalNet = 0;

                foreach (var r in records)
                {
                    int rowIdx = _dgv.Rows.Add(
                        r.PayrollRecordId,
                        r.Employee?.FullName ?? $"Employee #{r.EmployeeId}",
                        r.PayPeriodStart.ToString("MMM dd, yyyy"),
                        r.PayPeriodEnd.ToString("MMM dd, yyyy"),
                        r.HoursWorked.ToString("N1"),
                        r.GrossPay.ToString("C"),
                        r.Deductions.ToString("C"),
                        r.NetPay.ToString("C"),
                        r.PaymentMethod ?? "—",
                        r.Status,
                        string.IsNullOrEmpty(r.ConfirmationReference) ? "—" : r.ConfirmationReference);

                    ApplyPayrollColor(_dgv.Rows[rowIdx], r.Status);

                    totalGross += r.GrossPay;
                    totalNet += r.NetPay;
                }

                _lblSummary.Text =
                    $"{records.Count} record(s)   " +
                    $"Gross: {totalGross:C}   " +
                    $"Net: {totalNet:C}";
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Could not load payroll records.\nDetails: " + ex.Message);
            }
        }

        // ── Process all pending ───────────────────────────────────────────────
        private void BtnProcess_Click(object sender, EventArgs e)
        {
            // Count pending first so the confirmation message is meaningful
            var pending = _payrollService.GetPendingPayrollRecords();
            if (pending.Count == 0)
            {
                MessageHelper.ShowWarning("There are no pending payroll records to process.");
                return;
            }

            if (!MessageHelper.Confirm(
                $"Process payroll for {pending.Count} employee(s)?\n\n" +
                "Each employee will be paid via their registered payment method\n" +
                "(Check or Direct Deposit). This cannot be undone.",
                "Process Payroll"))
                return;

            int processed = _payrollService.ProcessPendingPayroll();
            MessageHelper.ShowSuccess(
                $"{processed} payroll record(s) processed successfully.");

            // Switch filter to Processed so user can see what just ran
            _cboFilter.SelectedItem = "Processed";
            LoadData();
        }

        // ── Row color by status ───────────────────────────────────────────────
        private void ApplyPayrollColor(DataGridViewRow row, string status)
        {
            if (status == "Processed")
                row.DefaultCellStyle.BackColor = Color.FromArgb(204, 255, 204);
            else if (status == "Failed")
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 204, 204);
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