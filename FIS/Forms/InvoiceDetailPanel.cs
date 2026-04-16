using FIS.Models;
using FIS.Services;
using FIS.Utils;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FIS.Forms
{
    // ── InvoiceDetailPanel ────────────────────────────────────────────────────
    // Used for both ADD (invoiceId = 0) and VIEW (invoiceId > 0).
    // When invoiceId > 0 the fields are read-only (view mode).
    // OnSaved is fired after a successful save so the list can refresh.
    public class InvoiceDetailPanel : UserControl
    {
        // Callback — InvoiceListPanel subscribes to this
        public event Action OnSaved;

        private readonly InvoiceAPService _invoiceService = new InvoiceAPService();
        private readonly VendorService _vendorService = new VendorService();

        private readonly int _invoiceId;   // 0 = new, >0 = existing
        private readonly bool _isNew;

        // Form controls we need to read on Save
        private ComboBox _cboVendor;
        private TextBox _txtInvoiceNumber;
        private DateTimePicker _dtpInvoiceDate;
        private DateTimePicker _dtpDueDate;
        private TextBox _txtTotalAmount;
        private Label _lblStatus;
        private ErrorProvider _err;

        public InvoiceDetailPanel(int invoiceId)
        {
            _invoiceId = invoiceId;
            _isNew = invoiceId == 0;

            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(240, 243, 248);

            _err = new ErrorProvider(this);
            _err.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            BuildUI();

            if (!_isNew) LoadExisting();
        }

        // ════════════════════════════════════════════════════════════════════
        //  UI CONSTRUCTION
        // ════════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            // ── Header ────────────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(26, 55, 100)
            };
            var lblTitle = new Label
            {
                Text = _isNew ? "New Invoice" : "Invoice Details",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            };
            var lblSub = new Label
            {
                Text = _isNew
                    ? "Enter vendor invoice details below"
                    : $"Invoice #{_invoiceId} — view only",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(160, 200, 240),
                AutoSize = true,
                Location = new Point(22, 40)
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // ── Card container ────────────────────────────────────────────────
            var pnlCard = new Panel
            {
                Location = new Point(40, 84),
                Size = new Size(680, 480),
                BackColor = Color.White
            };
            // Top accent bar
            pnlCard.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(26, 55, 100), 4),
                    0, 0, pnlCard.Width, 0);

            // ── Form fields ───────────────────────────────────────────────────
            int y = 24;
            int labelX = 30, fieldX = 200, fieldW = 420;

            // Vendor
            AddFieldLabel(pnlCard, "Vendor *", labelX, y);
            _cboVendor = new ComboBox
            {
                Location = new Point(fieldX, y - 2),
                Size = new Size(fieldW, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F),
                Enabled = _isNew
            };
            LoadVendors();
            pnlCard.Controls.Add(_cboVendor);
            y += 46;

            // Invoice Number
            AddFieldLabel(pnlCard, "Invoice # *", labelX, y);
            _txtInvoiceNumber = MakeTextBox(pnlCard, fieldX, y, fieldW, _isNew);
            y += 46;

            // Invoice Date
            AddFieldLabel(pnlCard, "Invoice Date *", labelX, y);
            _dtpInvoiceDate = MakeDatePicker(pnlCard, fieldX, y, _isNew);
            y += 46;

            // Due Date
            AddFieldLabel(pnlCard, "Due Date *", labelX, y);
            _dtpDueDate = MakeDatePicker(pnlCard, fieldX, y, _isNew);
            _dtpDueDate.Value = DateTime.Today.AddDays(30);
            y += 46;

            // Total Amount
            AddFieldLabel(pnlCard, "Total Amount *", labelX, y);
            _txtTotalAmount = MakeTextBox(pnlCard, fieldX, y, 160, _isNew);
            y += 46;

            // Status (read-only display)
            AddFieldLabel(pnlCard, "Status", labelX, y);
            _lblStatus = new Label
            {
                Text = "Unpaid",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 80, 160),
                AutoSize = true,
                Location = new Point(fieldX, y)
            };
            pnlCard.Controls.Add(_lblStatus);
            y += 60;

            // ── Buttons ───────────────────────────────────────────────────────
            if (_isNew)
            {
                var btnSave = new Button
                {
                    Text = "Save Invoice",
                    Location = new Point(fieldX, y),
                    Size = new Size(150, 38),
                    BackColor = Color.FromArgb(26, 55, 100),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnSave.FlatAppearance.BorderSize = 0;
                btnSave.Click += BtnSave_Click;
                pnlCard.Controls.Add(btnSave);
            }

            var btnBack = new Button
            {
                Text = "← Back to List",
                Location = new Point(_isNew ? fieldX + 165 : fieldX, y),
                Size = new Size(150, 38),
                BackColor = Color.FromArgb(240, 242, 246),
                ForeColor = Color.FromArgb(60, 80, 120),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F),
                Cursor = Cursors.Hand
            };
            btnBack.FlatAppearance.BorderColor = Color.FromArgb(200, 210, 225);
            btnBack.Click += (s, e) => NavigateBack();
            pnlCard.Controls.Add(btnBack);

            // ── Assemble ──────────────────────────────────────────────────────
            this.Controls.Add(pnlCard);
            this.Controls.Add(pnlHeader);
        }

        // ════════════════════════════════════════════════════════════════════
        //  LOAD EXISTING INVOICE (view mode)
        // ════════════════════════════════════════════════════════════════════
        private void LoadExisting()
        {
            var inv = _invoiceService.GetById(_invoiceId);
            if (inv == null)
            {
                MessageHelper.ShowError("Invoice not found.");
                return;
            }

            // Populate vendor dropdown selection
            foreach (VendorDropdownItem item in _cboVendor.Items)
            {
                if (item.Id == inv.VendorId)
                {
                    _cboVendor.SelectedItem = item;
                    break;
                }
            }

            _txtInvoiceNumber.Text = inv.InvoiceNumber;
            _dtpInvoiceDate.Value = inv.InvoiceDate;
            _dtpDueDate.Value = inv.DueDate;
            _txtTotalAmount.Text = inv.TotalAmount.ToString("F2");
            _lblStatus.Text = inv.Status;

            // Color the status label
            if (inv.Status == "Overdue")
                _lblStatus.ForeColor = Color.FromArgb(180, 40, 40);
            else if (inv.Status == "Paid")
                _lblStatus.ForeColor = Color.FromArgb(30, 130, 60);
            else
                _lblStatus.ForeColor = Color.FromArgb(40, 80, 160);
        }

        // ════════════════════════════════════════════════════════════════════
        //  SAVE
        // ════════════════════════════════════════════════════════════════════
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Validate
            FormValidator.ClearErrors(_err);
            bool valid = true;

            valid &= FormValidator.IsRequired(_cboVendor, "Vendor", _err);
            valid &= FormValidator.IsRequired(_txtInvoiceNumber, "Invoice number", _err);
            valid &= FormValidator.IsPositiveDecimal(_txtTotalAmount, "Total amount", _err);
            valid &= FormValidator.IsDateAfter(_dtpDueDate, _dtpInvoiceDate,
                "Due date must be after invoice date", _err);

            if (!valid) return;

            var invoice = new InvoiceAP
            {
                VendorId = ((VendorDropdownItem)_cboVendor.SelectedItem).Id,
                InvoiceNumber = _txtInvoiceNumber.Text.Trim(),
                InvoiceDate = _dtpInvoiceDate.Value,
                DueDate = _dtpDueDate.Value,
                TotalAmount = decimal.Parse(_txtTotalAmount.Text.Trim()),
                Status = "Unpaid",
                ReceivedAt = DateTime.Now
            };

            bool ok = _invoiceService.AddInvoice(invoice);
            if (ok)
            {
                MessageHelper.ShowSuccess("Invoice saved successfully.");
                OnSaved?.Invoke();   // tell the list to refresh
                NavigateBack();
            }
            else
            {
                MessageHelper.ShowError("Could not save invoice. Please try again.");
            }
        }

        // ── Navigate back to InvoiceListPanel ────────────────────────────────
        private void NavigateBack()
        {
            var dashboard = this.FindForm() as MainDashboard;
            if (dashboard == null) return;

            var list = new InvoiceListPanel();
            list.Dock = DockStyle.Fill;
            dashboard.pnlContent.Controls.Clear();
            dashboard.pnlContent.Controls.Add(list);
        }

        // ── Load vendors into dropdown ────────────────────────────────────────
        private void LoadVendors()
        {
            var vendors = _vendorService.GetActiveVendors();
            foreach (var v in vendors)
                _cboVendor.Items.Add(new VendorDropdownItem(v.VendorId, v.VendorName));
            if (_cboVendor.Items.Count > 0)
                _cboVendor.SelectedIndex = 0;
        }

        // ── Field builder helpers ─────────────────────────────────────────────
        private void AddFieldLabel(Panel parent, string text, int x, int y)
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

        private TextBox MakeTextBox(Panel parent, int x, int y, int w, bool enabled)
        {
            var txt = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(w, 26),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = enabled,
                BackColor = enabled
                    ? Color.White
                    : Color.FromArgb(245, 246, 248)
            };
            parent.Controls.Add(txt);
            return txt;
        }

        private DateTimePicker MakeDatePicker(Panel parent, int x, int y, bool enabled)
        {
            var dtp = new DateTimePicker
            {
                Location = new Point(x, y),
                Size = new Size(200, 26),
                Font = new Font("Segoe UI", 9.5F),
                Format = DateTimePickerFormat.Short,
                Enabled = enabled,
                Value = DateTime.Today
            };
            parent.Controls.Add(dtp);
            return dtp;
        }

        // Dropdown item wraps VendorId + VendorName
        private class VendorDropdownItem
        {
            public int Id { get; }
            public string Name { get; }
            public VendorDropdownItem(int id, string name) { Id = id; Name = name; }
            public override string ToString() => Name;
        }
    }
}