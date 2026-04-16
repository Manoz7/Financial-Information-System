using FIS.Models;
using FIS.Services;
using FIS.Utils;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace FIS.Forms
{
    // Add (id=0) or Edit (id>0) a raw material record.
    // Allows updating: name, unit, reorder settings, preferred vendor, unit price.
    // QuantityOnHand is shown read-only — POPS owns that value.
    public class RawMaterialDetailPanel : UserControl
    {
        public event Action OnSaved;

        private readonly RawMaterialService _materialService = new RawMaterialService();
        private readonly VendorService _vendorService = new VendorService();

        private readonly int _materialId;
        private readonly bool _isNew;

        // Form controls
        private TextBox _txtName;
        private TextBox _txtUnit;
        private TextBox _txtQuantityOnHand;   // read-only
        private TextBox _txtThreshold;
        private TextBox _txtReorderQty;
        private ComboBox _cboVendor;
        private TextBox _txtUnitPrice;
        private ErrorProvider _err;

        public RawMaterialDetailPanel(int materialId)
        {
            _materialId = materialId;
            _isNew = materialId == 0;

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
                Text = _isNew ? "New Raw Material" : "Edit Raw Material",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 12)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = _isNew
                    ? "Register a new raw material and its reorder settings"
                    : $"Editing material #{_materialId}",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(160, 200, 240),
                AutoSize = true,
                Location = new Point(22, 40)
            });

            // ── Card ──────────────────────────────────────────────────────────
            var pnlCard = new Panel
            {
                Location = new Point(40, 84),
                Size = new Size(660, 500),
                BackColor = Color.White
            };
            pnlCard.Paint += (s, e) =>
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(26, 55, 100), 4),
                    0, 0, pnlCard.Width, 0);

            int y = 24; int lx = 30; int fx = 220; int fw = 380;

            // ── Section: Basic Info ───────────────────────────────────────────
            AddSectionTitle(pnlCard, "Material Info", lx, y); y += 28;

            AddLabel(pnlCard, "Material Name *", lx, y);
            _txtName = MakeTxt(pnlCard, fx, y, fw, true); y += 40;

            AddLabel(pnlCard, "Unit of Measure *", lx, y);
            _txtUnit = MakeTxt(pnlCard, fx, y, 120, true);
            AddHint(pnlCard, "e.g. each, kg, box", fx + 130, y);
            y += 40;

            // Quantity on hand — read-only, owned by POPS
            AddLabel(pnlCard, "Quantity On Hand", lx, y);
            _txtQuantityOnHand = MakeTxt(pnlCard, fx, y, 120, false);
            _txtQuantityOnHand.BackColor = Color.FromArgb(245, 246, 248);
            AddHint(pnlCard, "Managed by POPS — read only", fx + 130, y);
            y += 46;

            // ── Section: Reorder Settings ─────────────────────────────────────
            AddSectionTitle(pnlCard, "Reorder Settings", lx, y); y += 28;

            AddLabel(pnlCard, "Reorder Threshold *", lx, y);
            _txtThreshold = MakeTxt(pnlCard, fx, y, 120, true);
            AddHint(pnlCard, "Auto-reorder fires below this qty", fx + 130, y);
            y += 40;

            AddLabel(pnlCard, "Reorder Quantity *", lx, y);
            _txtReorderQty = MakeTxt(pnlCard, fx, y, 120, true);
            AddHint(pnlCard, "How many units to order each time", fx + 130, y);
            y += 46;

            // ── Section: Vendor & Pricing ─────────────────────────────────────
            AddSectionTitle(pnlCard, "Preferred Vendor & Price", lx, y); y += 28;

            AddLabel(pnlCard, "Preferred Vendor *", lx, y);
            _cboVendor = new ComboBox
            {
                Location = new Point(fx, y),
                Size = new Size(fw, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            LoadVendors();
            pnlCard.Controls.Add(_cboVendor);
            y += 40;

            AddLabel(pnlCard, "Unit Price *", lx, y);
            _txtUnitPrice = MakeTxt(pnlCard, fx, y, 120, true);
            AddHint(pnlCard, "Price per unit from this vendor", fx + 130, y);
            y += 50;

            // ── Buttons ───────────────────────────────────────────────────────
            var btnSave = new Button
            {
                Text = _isNew ? "Save Material" : "Update Material",
                Location = new Point(fx, y),
                Size = new Size(155, 38),
                BackColor = Color.FromArgb(26, 55, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            pnlCard.Controls.Add(btnSave);

            var btnBack = new Button
            {
                Text = "← Back to List",
                Location = new Point(fx + 165, y),
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

        // ── Load existing material ────────────────────────────────────────────
        private void LoadExisting()
        {
            var m = _materialService.GetById(_materialId);
            if (m == null) { MessageHelper.ShowError("Material not found."); return; }

            _txtName.Text = m.MaterialName;
            _txtUnit.Text = m.UnitOfMeasure;
            _txtQuantityOnHand.Text = m.QuantityOnHand.ToString("N2");
            _txtThreshold.Text = m.ReorderThreshold.ToString("N2");
            _txtReorderQty.Text = m.ReorderQuantity.ToString("N2");
            _txtUnitPrice.Text = m.UnitPrice.ToString("F2");

            // Select vendor in dropdown
            foreach (VendorItem item in _cboVendor.Items)
            {
                if (item.Id == m.VendorId)
                {
                    _cboVendor.SelectedItem = item;
                    break;
                }
            }
        }

        // ── Save ──────────────────────────────────────────────────────────────
        private void BtnSave_Click(object sender, EventArgs e)
        {
            FormValidator.ClearErrors(_err);
            bool valid = true;

            valid &= FormValidator.IsRequired(_txtName, "Material name", _err);
            valid &= FormValidator.IsRequired(_txtUnit, "Unit of measure", _err);
            valid &= FormValidator.IsRequired(_cboVendor, "Preferred vendor", _err);
            valid &= FormValidator.IsPositiveDecimal(_txtThreshold, "Reorder threshold", _err);
            valid &= FormValidator.IsPositiveDecimal(_txtReorderQty, "Reorder quantity", _err);
            valid &= FormValidator.IsPositiveDecimal(_txtUnitPrice, "Unit price", _err);
            if (!valid) return;

            var vendorItem = (VendorItem)_cboVendor.SelectedItem;

            bool ok;
            if (_isNew)
            {
                var material = new RawMaterial
                {
                    MaterialName = _txtName.Text.Trim(),
                    UnitOfMeasure = _txtUnit.Text.Trim(),
                    QuantityOnHand = 0,
                    ReorderThreshold = decimal.Parse(_txtThreshold.Text.Trim()),
                    ReorderQuantity = decimal.Parse(_txtReorderQty.Text.Trim()),
                    VendorId = vendorItem.Id,
                    UnitPrice = decimal.Parse(_txtUnitPrice.Text.Trim())
                };
                ok = _materialService.AddRawMaterial(material);
            }
            else
            {
                // Update reorder settings and vendor/price separately
                bool s1 = _materialService.UpdateReorderSettings(
                    _materialId,
                    decimal.Parse(_txtThreshold.Text.Trim()),
                    decimal.Parse(_txtReorderQty.Text.Trim()));

                bool s2 = _materialService.UpdateVendorAndPrice(
                    _materialId,
                    vendorItem.Id,
                    decimal.Parse(_txtUnitPrice.Text.Trim()));

                ok = s1 && s2;
            }

            if (ok)
            {
                MessageHelper.ShowSuccess(
                    _isNew ? "Material added successfully." : "Material updated successfully.");
                OnSaved?.Invoke();
                NavigateBack();
            }
            else
            {
                MessageHelper.ShowError("Could not save material. Please try again.");
            }
        }

        private void NavigateBack()
        {
            var dashboard = this.FindForm() as MainDashboard;
            if (dashboard == null) return;
            var list = new RawMaterialListPanel();
            list.Dock = DockStyle.Fill;
            dashboard.pnlContent.Controls.Clear();
            dashboard.pnlContent.Controls.Add(list);
        }

        private void LoadVendors()
        {
            var vendors = _vendorService.GetActiveVendors();
            foreach (var v in vendors)
                _cboVendor.Items.Add(new VendorItem(v.VendorId, v.VendorName));
            if (_cboVendor.Items.Count > 0)
                _cboVendor.SelectedIndex = 0;
        }

        // ── Layout helpers ────────────────────────────────────────────────────
        private void AddSectionTitle(Panel p, string text, int x, int y)
        {
            p.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 55, 100),
                AutoSize = true,
                Location = new Point(x, y)
            });
        }

        private void AddLabel(Panel p, string text, int x, int y)
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

        private void AddHint(Panel p, string text, int x, int y)
        {
            p.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = Color.FromArgb(160, 170, 185),
                AutoSize = true,
                Location = new Point(x, y + 6)
            });
        }

        private TextBox MakeTxt(Panel p, int x, int y, int w, bool enabled)
        {
            var txt = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(w, 26),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = enabled,
                BackColor = enabled ? Color.White : Color.FromArgb(245, 246, 248)
            };
            p.Controls.Add(txt);
            return txt;
        }

        private class VendorItem
        {
            public int Id { get; }
            public string Name { get; }
            public VendorItem(int id, string name) { Id = id; Name = name; }
            public override string ToString() => Name;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // RawMaterialDetailPanel
            // 
            this.Name = "RawMaterialDetailPanel";
            this.Size = new System.Drawing.Size(416, 343);
            this.ResumeLayout(false);

        }
    }
}