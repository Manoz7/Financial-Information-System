using FIS.Models;
using FIS.Services;
using FIS.Utils;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FIS.Forms
{
    // Shows full bill details and lets the user record a full or partial payment.
    // This is view + action combined — no edit of the bill itself (bills are
    // auto-generated from shipped orders and should not be manually changed).
    public class CustomerBillDetailPanel : UserControl
    {
        public event Action OnSaved;

        private readonly CustomerBillService _billService = new CustomerBillService();
        private readonly PaymentARService _paymentService = new PaymentARService();

        private readonly int _billId;
        private CustomerBill _bill;

        // Payment entry controls
        private TextBox _txtAmount;
        private ComboBox _cboMethod;
        private TextBox _txtReference;
        private Label _lblBalance;
        private Label _lblStatus;
        private ErrorProvider _err;

        public CustomerBillDetailPanel(int billId)
        {
            _billId = billId;
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(240, 243, 248);
            _err = new ErrorProvider(this);
            _err.BlinkStyle = ErrorBlinkStyle.NeverBlink;
            BuildUI();
        }

        private void BuildUI()
        {
            // Load the bill first so we can display its values
            _bill = _billService.GetById(_billId);

            // ── Header ────────────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(26, 55, 100)
            };
            pnlHeader.Controls.Add(new Label
            {
                Text = $"Bill #{_billId}",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "Customer Bill Details — record incoming payment below",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(160, 200, 240),
                AutoSize = true,
                Location = new Point(22, 40)
            });

            if (_bill == null)
            {
                this.Controls.Add(new Label
                {
                    Text = "Bill not found.",
                    Font = new Font("Segoe UI", 12F),
                    ForeColor = Color.FromArgb(180, 40, 40),
                    AutoSize = true,
                    Location = new Point(40, 100)
                });
                this.Controls.Add(pnlHeader);
                return;
            }

            // ── Bill Summary Card ─────────────────────────────────────────────
            var pnlSummary = new Panel
            {
                Location = new Point(40, 84),
                Size = new Size(580, 200),
                BackColor = Color.White
            };
            pnlSummary.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(26, 55, 100), 4),
                    0, 0, pnlSummary.Width, 0);

            int y = 20; int lx = 20; int vx = 220;

            AddSummaryRow(pnlSummary, "Customer:", $"Customer #{_bill.CustomerId}", lx, vx, y); y += 30;
            AddSummaryRow(pnlSummary, "Bill Date:", _bill.BillDate.ToString("MMM dd, yyyy"), lx, vx, y); y += 30;
            AddSummaryRow(pnlSummary, "Due Date:", _bill.DueDate.ToString("MMM dd, yyyy"), lx, vx, y); y += 30;
            AddSummaryRow(pnlSummary, "Total Billed:", _bill.TotalAmountDue.ToString("C"), lx, vx, y); y += 30;

            // Balance remaining — shown large and colored
            AddSummaryRow(pnlSummary, "Balance Remaining:", "", lx, vx, y);
            _lblBalance = new Label
            {
                Text = _bill.BalanceRemaining.ToString("C"),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = _bill.BalanceRemaining > 0
                    ? Color.FromArgb(180, 80, 30)
                    : Color.FromArgb(30, 130, 60),
                AutoSize = true,
                Location = new Point(vx, y - 2)
            };
            pnlSummary.Controls.Add(_lblBalance);
            y += 34;

            // Status badge
            AddSummaryRow(pnlSummary, "Status:", "", lx, vx, y);
            _lblStatus = new Label
            {
                Text = _bill.Status,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = GetStatusColor(_bill.Status),
                AutoSize = true,
                Location = new Point(vx, y)
            };
            pnlSummary.Controls.Add(_lblStatus);

            // ── Payment Entry Card ────────────────────────────────────────────
            var pnlPayment = new Panel
            {
                Location = new Point(40, 300),
                Size = new Size(580, 240),
                BackColor = Color.White
            };
            pnlPayment.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(30, 130, 60), 4),
                    0, 0, pnlPayment.Width, 0);
                e.Graphics.DrawRectangle(
                    new Pen(Color.FromArgb(220, 225, 235), 1),
                    0, 0, pnlPayment.Width - 1, pnlPayment.Height - 1);
            };

            // Card title
            pnlPayment.Controls.Add(new Label
            {
                Text = "Record Payment",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 80, 40),
                AutoSize = true,
                Location = new Point(20, 16)
            });

            // Disable payment entry if already paid
            bool canPay = _bill.Status != "Paid";

            int py = 50; int plx = 20; int pvx = 200; int pw = 330;

            // Amount
            AddPayLabel(pnlPayment, "Amount *", plx, py);
            _txtAmount = new TextBox
            {
                Location = new Point(pvx, py),
                Size = new Size(160, 26),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = canPay,
                BackColor = canPay ? Color.White : Color.FromArgb(245, 246, 248)
            };
            pnlPayment.Controls.Add(_txtAmount);
            py += 40;

            // Payment Method
            AddPayLabel(pnlPayment, "Method *", plx, py);
            _cboMethod = new ComboBox
            {
                Location = new Point(pvx, py),
                Size = new Size(200, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F),
                Enabled = canPay
            };
            _cboMethod.Items.AddRange(new object[] {
                "Check", "ElectronicTransfer", "CreditCard" });
            _cboMethod.SelectedIndex = 0;
            pnlPayment.Controls.Add(_cboMethod);
            py += 40;

            // Reference Number
            AddPayLabel(pnlPayment, "Reference #", plx, py);
            _txtReference = new TextBox
            {
                Location = new Point(pvx, py),
                Size = new Size(200, 26),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = canPay,
                BackColor = canPay ? Color.White : Color.FromArgb(245, 246, 248)
            };
            pnlPayment.Controls.Add(_txtReference);
            py += 46;

            // Submit / Already Paid message
            if (canPay)
            {
                var btnRecord = new Button
                {
                    Text = "Record Payment",
                    Location = new Point(pvx, py),
                    Size = new Size(160, 38),
                    BackColor = Color.FromArgb(30, 120, 60),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnRecord.FlatAppearance.BorderSize = 0;
                btnRecord.Click += BtnRecord_Click;
                pnlPayment.Controls.Add(btnRecord);
            }
            else
            {
                pnlPayment.Controls.Add(new Label
                {
                    Text = "✓ This bill has been fully paid.",
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 130, 60),
                    AutoSize = true,
                    Location = new Point(pvx, py)
                });
            }

            // ── Back button ───────────────────────────────────────────────────
            var btnBack = new Button
            {
                Text = "← Back to Bills",
                Location = new Point(40, 558),
                Size = new Size(150, 36),
                BackColor = Color.FromArgb(240, 242, 246),
                ForeColor = Color.FromArgb(60, 80, 120),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F),
                Cursor = Cursors.Hand
            };
            btnBack.FlatAppearance.BorderColor = Color.FromArgb(200, 210, 225);
            btnBack.Click += (s, e) => NavigateBack();

            this.Controls.Add(btnBack);
            this.Controls.Add(pnlPayment);
            this.Controls.Add(pnlSummary);
            this.Controls.Add(pnlHeader);
        }

        // ── Record Payment ────────────────────────────────────────────────────
        private void BtnRecord_Click(object sender, EventArgs e)
        {
            FormValidator.ClearErrors(_err);

            if (!FormValidator.IsWithinBalance(_txtAmount, _bill.BalanceRemaining, _err))
                return;

            if (!FormValidator.IsRequired(_cboMethod, "Payment method", _err))
                return;

            var payment = new PaymentAR
            {
                CustomerBillId = _billId,
                AmountReceived = decimal.Parse(_txtAmount.Text.Trim()),
                DateReceived = DateTime.Now,
                PaymentMethod = _cboMethod.SelectedItem.ToString(),
                ReferenceNumber = _txtReference.Text.Trim()
            };

            bool ok = _paymentService.RecordPayment(payment);
            if (ok)
            {
                MessageHelper.ShowSuccess(
                    $"Payment of {payment.AmountReceived:C} recorded successfully.");
                OnSaved?.Invoke();
                NavigateBack();
            }
            else
            {
                MessageHelper.ShowError("Could not record payment. Please try again.");
            }
        }

        private void NavigateBack()
        {
            var dashboard = this.FindForm() as MainDashboard;
            if (dashboard == null) return;
            var list = new CustomerBillListPanel();
            list.Dock = DockStyle.Fill;
            dashboard.pnlContent.Controls.Clear();
            dashboard.pnlContent.Controls.Add(list);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void AddSummaryRow(Panel p, string label, string value, int lx, int vx, int y)
        {
            p.Controls.Add(new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 110, 130),
                AutoSize = true,
                Location = new Point(lx, y + 2)
            });
            if (!string.IsNullOrEmpty(value))
                p.Controls.Add(new Label
                {
                    Text = value,
                    Font = new Font("Segoe UI", 9.5F),
                    ForeColor = Color.FromArgb(40, 50, 70),
                    AutoSize = true,
                    Location = new Point(vx, y)
                });
        }

        private void AddPayLabel(Panel p, string text, int x, int y)
        {
            p.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 110, 130),
                AutoSize = true,
                Location = new Point(x, y + 4)
            });
        }

        private Color GetStatusColor(string status)
        {
            if (status == "Overdue") return Color.FromArgb(180, 40, 40);
            if (status == "Paid") return Color.FromArgb(30, 130, 60);
            if (status == "PartiallyPaid") return Color.FromArgb(160, 110, 0);
            return Color.FromArgb(40, 80, 160);
        }
    }
}