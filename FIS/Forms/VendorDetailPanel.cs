using FIS.Models;
using FIS.Services;
using FIS.Utils;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FIS.Forms
{
    // Works for both ADD (vendorId = 0) and EDIT (vendorId > 0).
    // Edit mode keeps all fields enabled — vendors can be fully updated.
    public class VendorDetailPanel : UserControl
    {
        public event Action OnSaved;

        private readonly VendorService _vendorService = new VendorService();

        private readonly int _vendorId;
        private readonly bool _isNew;

        // Form controls
        private TextBox _txtName;
        private TextBox _txtContact;
        private TextBox _txtPhone;
        private TextBox _txtEmail;
        private TextBox _txtAddress;
        private ComboBox _cboStatus;
        private ErrorProvider _err;

        public VendorDetailPanel(int vendorId)
        {
            _vendorId = vendorId;
            _isNew = vendorId == 0;

            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(240, 243, 248);

            _err = new ErrorProvider(this);
            _err.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            BuildUI();
            if (!_isNew) LoadExisting();
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
                Text = _isNew ? "New Vendor" : "Edit Vendor",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = _isNew
                    ? "Add a new vendor to the system"
                    : $"Editing vendor #{_vendorId}",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(160, 200, 240),
                AutoSize = true,
                Location = new Point(22, 40)
            });

            // ── Card ──────────────────────────────────────────────────────────
            var pnlCard = new Panel
            {
                Location = new Point(40, 84),
                Size = new Size(620, 460),
                BackColor = Color.White
            };
            pnlCard.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(26, 55, 100), 4),
                    0, 0, pnlCard.Width, 0);

            int y = 24;
            int labelX = 30;
            int fieldX = 210;
            int fieldW = 360;

            // Vendor Name
            AddLabel(pnlCard, "Vendor Name *", labelX, y);
            _txtName = MakeTxt(pnlCard, fieldX, y, fieldW); y += 46;

            // Contact Name
            AddLabel(pnlCard, "Contact Name", labelX, y);
            _txtContact = MakeTxt(pnlCard, fieldX, y, fieldW); y += 46;

            // Phone
            AddLabel(pnlCard, "Phone", labelX, y);
            _txtPhone = MakeTxt(pnlCard, fieldX, y, 200); y += 46;

            // Email
            AddLabel(pnlCard, "Email", labelX, y);
            _txtEmail = MakeTxt(pnlCard, fieldX, y, fieldW); y += 46;

            // Address
            AddLabel(pnlCard, "Address", labelX, y);
            _txtAddress = MakeTxt(pnlCard, fieldX, y, fieldW); y += 46;

            // Relationship Status
            AddLabel(pnlCard, "Status *", labelX, y);
            _cboStatus = new ComboBox
            {
                Location = new Point(fieldX, y),
                Size = new Size(200, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            _cboStatus.Items.AddRange(new object[] {
                "Active", "Suspended", "Terminated" });
            _cboStatus.SelectedIndex = 0;
            pnlCard.Controls.Add(_cboStatus);
            y += 56;

            // ── Buttons ───────────────────────────────────────────────────────
            var btnSave = new Button
            {
                Text = _isNew ? "Save Vendor" : "Update Vendor",
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

            // Terminate / Suspend quick-action buttons (edit mode only)
            if (!_isNew)
            {
                var btnTerminate = new Button
                {
                    Text = "Terminate Vendor",
                    Location = new Point(fieldX + 160, y),
                    Size = new Size(150, 38),
                    BackColor = Color.FromArgb(160, 50, 50),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnTerminate.FlatAppearance.BorderSize = 0;
                btnTerminate.Click += BtnTerminate_Click;
                pnlCard.Controls.Add(btnTerminate);
            }

            var btnBack = new Button
            {
                Text = "← Back to List",
                Location = new Point(_isNew ? fieldX + 160 : fieldX + 320, y),
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

            this.Controls.Add(pnlCard);
            this.Controls.Add(pnlHeader);
        }

        // ── Load existing vendor into fields ──────────────────────────────────
        private void LoadExisting()
        {
            var v = _vendorService.GetVendorById(_vendorId);
            if (v == null) { MessageHelper.ShowError("Vendor not found."); return; }

            _txtName.Text = v.VendorName;
            _txtContact.Text = v.ContactName;
            _txtPhone.Text = v.Phone;
            _txtEmail.Text = v.Email;
            _txtAddress.Text = v.Address;

            // Select the correct status in the ComboBox
            for (int i = 0; i < _cboStatus.Items.Count; i++)
            {
                if (_cboStatus.Items[i].ToString() == v.RelationshipStatus)
                {
                    _cboStatus.SelectedIndex = i;
                    break;
                }
            }
        }

        // ── Save / Update ─────────────────────────────────────────────────────
        private void BtnSave_Click(object sender, EventArgs e)
        {
            FormValidator.ClearErrors(_err);
            bool valid = true;

            valid &= FormValidator.IsRequired(_txtName, "Vendor name", _err);
            valid &= FormValidator.IsValidEmail(_txtEmail, "Email", _err);
            if (!valid) return;

            var vendor = new Vendor
            {
                VendorId = _vendorId,
                VendorName = _txtName.Text.Trim(),
                ContactName = _txtContact.Text.Trim(),
                Phone = _txtPhone.Text.Trim(),
                Email = _txtEmail.Text.Trim(),
                Address = _txtAddress.Text.Trim(),
                RelationshipStatus = _cboStatus.SelectedItem.ToString()
            };

            bool ok = _isNew
                ? _vendorService.AddVendor(vendor)
                : _vendorService.UpdateVendor(vendor);

            if (ok)
            {
                MessageHelper.ShowSuccess(
                    _isNew ? "Vendor added successfully." : "Vendor updated successfully.");
                OnSaved?.Invoke();
                NavigateBack();
            }
            else
            {
                MessageHelper.ShowError("Could not save vendor. Please try again.");
            }
        }

        // ── Terminate vendor with confirmation ────────────────────────────────
        private void BtnTerminate_Click(object sender, EventArgs e)
        {
            if (!MessageHelper.Confirm(
                $"Terminate this vendor permanently?\n" +
                "They will no longer appear on new purchase orders.",
                "Terminate Vendor"))
                return;

            bool ok = _vendorService.UpdateVendorStatus(_vendorId, "Terminated");
            if (ok)
            {
                MessageHelper.ShowSuccess("Vendor terminated.");
                OnSaved?.Invoke();
                NavigateBack();
            }
            else
            {
                MessageHelper.ShowError("Could not update vendor status.");
            }
        }

        private void NavigateBack()
        {
            var dashboard = this.FindForm() as MainDashboard;
            if (dashboard == null) return;
            var list = new VendorListPanel();
            list.Dock = DockStyle.Fill;
            dashboard.pnlContent.Controls.Clear();
            dashboard.pnlContent.Controls.Add(list);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void AddLabel(Panel parent, string text, int x, int y)
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