using FIS.Models;
using FIS.Services;
using FIS.Utils;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FIS.Forms
{
    // Case §2.1.2: "employees who currently receive their pay by check and
    // would like to change to automatic deposit should have the means to do
    // this through an online method."
    //
    // This panel lets a clerk look up any employee by ID and update their
    // payment preference. In a full system this would be employee self-service
    // behind a login; for FIS it's clerk-operated from the dashboard.
    public class PaymentPreferencePanel : UserControl
    {
        private readonly PayrollService _payrollService = new PayrollService();

        // Employee lookup controls
        private TextBox _txtEmployeeId;
        private Button _btnLookup;
        private Label _lblEmployeeName;

        // Preference controls
        private Panel _pnlForm;
        private ComboBox _cboMethod;
        private Panel _pnlBankDetails;
        private TextBox _txtBankName;
        private TextBox _txtAccountNumber;
        private TextBox _txtRoutingNumber;

        private int _currentEmployeeId = -1;
        private ErrorProvider _err;

        public PaymentPreferencePanel()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(240, 243, 248);
            _err = new ErrorProvider(this);
            _err.BlinkStyle = ErrorBlinkStyle.NeverBlink;
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
                Text = "Payment Preference",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "Update employee payment method — Check or Direct Deposit",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(160, 200, 240),
                AutoSize = true,
                Location = new Point(22, 40)
            });

            // ── Employee lookup card ──────────────────────────────────────────
            var pnlLookup = new Panel
            {
                Location = new Point(40, 84),
                Size = new Size(580, 90),
                BackColor = Color.White
            };
            pnlLookup.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(26, 55, 100), 4),
                    0, 0, pnlLookup.Width, 0);

            pnlLookup.Controls.Add(new Label
            {
                Text = "Employee ID",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 110, 130),
                AutoSize = true,
                Location = new Point(20, 22)
            });

            _txtEmployeeId = new TextBox
            {
                Location = new Point(130, 18),
                Size = new Size(100, 26),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlLookup.Controls.Add(_txtEmployeeId);

            _btnLookup = new Button
            {
                Text = "Look Up",
                Location = new Point(242, 17),
                Size = new Size(90, 28),
                BackColor = Color.FromArgb(26, 55, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnLookup.FlatAppearance.BorderSize = 0;
            _btnLookup.Click += BtnLookup_Click;
            pnlLookup.Controls.Add(_btnLookup);

            _lblEmployeeName = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 55, 100),
                AutoSize = true,
                Location = new Point(350, 22)
            };
            pnlLookup.Controls.Add(_lblEmployeeName);

            // ── Preference form card (hidden until employee is looked up) ──────
            _pnlForm = new Panel
            {
                Location = new Point(40, 190),
                Size = new Size(580, 340),
                BackColor = Color.White,
                Visible = false
            };
            _pnlForm.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(30, 120, 60), 4),
                    0, 0, _pnlForm.Width, 0);

            _pnlForm.Controls.Add(new Label
            {
                Text = "Payment Method",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 60, 80),
                AutoSize = true,
                Location = new Point(20, 16)
            });

            // Method selector
            AddFormLabel(_pnlForm, "Pay via", 20, 56);
            _cboMethod = new ComboBox
            {
                Location = new Point(160, 52),
                Size = new Size(200, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            _cboMethod.Items.AddRange(new object[] { "Check", "DirectDeposit" });
            _cboMethod.SelectedIndex = 0;
            _cboMethod.SelectedIndexChanged += CboMethod_Changed;
            _pnlForm.Controls.Add(_cboMethod);

            // Bank details sub-panel (shown only when DirectDeposit selected)
            _pnlBankDetails = new Panel
            {
                Location = new Point(0, 96),
                Size = new Size(580, 170),
                BackColor = Color.FromArgb(248, 250, 253),
                Visible = false
            };

            _pnlBankDetails.Controls.Add(new Label
            {
                Text = "Direct Deposit Details",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 100, 140),
                AutoSize = true,
                Location = new Point(20, 10)
            });

            int by = 36;
            AddFormLabel(_pnlBankDetails, "Bank Name *", 20, by);
            _txtBankName = MakeTxt(_pnlBankDetails, 160, by, 360); by += 40;

            AddFormLabel(_pnlBankDetails, "Account # *", 20, by);
            _txtAccountNumber = MakeTxt(_pnlBankDetails, 160, by, 200); by += 40;

            AddFormLabel(_pnlBankDetails, "Routing # *", 20, by);
            _txtRoutingNumber = MakeTxt(_pnlBankDetails, 160, by, 160);

            _pnlForm.Controls.Add(_pnlBankDetails);

            // Save button
            var btnSave = new Button
            {
                Text = "Save Preference",
                Location = new Point(20, 280),
                Size = new Size(160, 38),
                BackColor = Color.FromArgb(26, 55, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            _pnlForm.Controls.Add(btnSave);

            // Assemble
            this.Controls.Add(_pnlForm);
            this.Controls.Add(pnlLookup);
            this.Controls.Add(pnlHeader);
        }

        // ── Look up employee ──────────────────────────────────────────────────
        private void BtnLookup_Click(object sender, EventArgs e)
        {
            _err.Clear();

            if (!int.TryParse(_txtEmployeeId.Text.Trim(), out int empId) || empId <= 0)
            {
                _err.SetError(_txtEmployeeId, "Enter a valid Employee ID.");
                return;
            }

            Employee emp = _payrollService.GetEmployeePaymentInfo(empId);
            if (emp == null)
            {
                MessageHelper.ShowWarning($"No employee found with ID {empId}.");
                _pnlForm.Visible = false;
                return;
            }

            // Employee found — populate the form
            _currentEmployeeId = emp.EmployeeId;
            _lblEmployeeName.Text = emp.FullName;

            // Set current method
            _cboMethod.SelectedItem = emp.PaymentMethod == "DirectDeposit"
                ? "DirectDeposit" : "Check";

            // Populate bank details if they exist
            _txtBankName.Text = emp.BankName ?? "";
            _txtAccountNumber.Text = emp.AccountNumber ?? "";
            _txtRoutingNumber.Text = emp.RoutingNumber ?? "";

            // Show the form
            _pnlForm.Visible = true;
            UpdateBankDetailsVisibility();
        }

        // ── Show/hide bank details based on selected method ───────────────────
        private void CboMethod_Changed(object sender, EventArgs e)
        {
            UpdateBankDetailsVisibility();
        }

        private void UpdateBankDetailsVisibility()
        {
            bool isDirect = _cboMethod.SelectedItem?.ToString() == "DirectDeposit";
            _pnlBankDetails.Visible = isDirect;

            // Clear bank fields when switching back to Check
            if (!isDirect)
            {
                _txtBankName.Text = "";
                _txtAccountNumber.Text = "";
                _txtRoutingNumber.Text = "";
            }
        }

        // ── Save ──────────────────────────────────────────────────────────────
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_currentEmployeeId == -1)
            {
                MessageHelper.ShowWarning("Please look up an employee first.");
                return;
            }

            _err.Clear();
            string method = _cboMethod.SelectedItem.ToString();

            // Validate bank details when DirectDeposit is selected
            if (method == "DirectDeposit")
            {
                bool valid = true;
                valid &= FormValidator.IsRequired(_txtBankName, "Bank name", _err);
                valid &= FormValidator.IsRequired(_txtAccountNumber, "Account number", _err);
                valid &= FormValidator.IsRequired(_txtRoutingNumber, "Routing number", _err);
                if (!valid) return;
            }

            if (!MessageHelper.Confirm(
                $"Update payment preference for employee #{_currentEmployeeId}?\n" +
                $"New method: {method}",
                "Update Payment Preference"))
                return;

            bool ok = _payrollService.UpdatePaymentPreference(
                _currentEmployeeId,
                method,
                method == "DirectDeposit" ? _txtBankName.Text.Trim() : "",
                method == "DirectDeposit" ? _txtAccountNumber.Text.Trim() : "",
                method == "DirectDeposit" ? _txtRoutingNumber.Text.Trim() : "");

            if (ok)
            {
                MessageHelper.ShowSuccess("Payment preference updated successfully.");
                // Reset form for next lookup
                _pnlForm.Visible = false;
                _lblEmployeeName.Text = "";
                _txtEmployeeId.Clear();
                _currentEmployeeId = -1;
            }
            else
            {
                MessageHelper.ShowError("Could not update payment preference.");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void AddFormLabel(Panel parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 110, 130),
                AutoSize = true,
                Location = new Point(x, y + 4)
            });
        }

        private TextBox MakeTxt(Panel parent, int x, int y, int w)
        {
            var txt = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(w, 26),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            parent.Controls.Add(txt);
            return txt;
        }
    }
}