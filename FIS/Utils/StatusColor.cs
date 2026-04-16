using System.Drawing;
using System.Windows.Forms;

namespace FIS.Utils
{
    // Centralises all DataGridView row coloring so every form shows the same
    // visual indicators for record status.
    //
    // Usage pattern in a form after loading data into a DataGridView:
    //   foreach (DataGridViewRow row in dgvInvoices.Rows)
    //       StatusColors.ApplyInvoiceColor(row);
    public static class StatusColors
    {
        // ── Shared color definitions ──────────────────────────────────────────
        // Keeping colors in one place means a single change updates every grid.

        private static readonly Color ColorOverdue = Color.FromArgb(255, 204, 204); // soft red
        private static readonly Color ColorPaid = Color.FromArgb(204, 255, 204); // soft green
        private static readonly Color ColorPartiallyPaid = Color.FromArgb(255, 255, 204); // soft yellow
        private static readonly Color ColorPending = Color.FromArgb(240, 240, 240); // light gray
        private static readonly Color ColorDelivered = Color.FromArgb(204, 255, 204); // soft green
        private static readonly Color ColorCancelled = Color.FromArgb(220, 220, 220); // mid gray
        private static readonly Color ColorProcessed = Color.FromArgb(204, 255, 204); // soft green
        private static readonly Color ColorFailed = Color.FromArgb(255, 204, 204); // soft red
        private static readonly Color ColorShipped = Color.FromArgb(204, 229, 255); // soft blue
        private static readonly Color ColorDefault = Color.White;

        // ── Vendor invoice / accounts payable rows ────────────────────────────
        // Status values: "Unpaid" | "Paid" | "Overdue"
        // Used in: InvoiceListForm, APReportForm
        public static void ApplyInvoiceColor(DataGridViewRow row)
        {
            string status = GetStatus(row);
            switch (status)
            {
                case "Overdue": SetRowColor(row, ColorOverdue); break;
                case "Paid": SetRowColor(row, ColorPaid); break;
                default: SetRowColor(row, ColorDefault); break;
            }
        }

        // ── Customer bill / accounts receivable rows ──────────────────────────
        // Status values: "Unpaid" | "PartiallyPaid" | "Paid" | "Overdue"
        // Used in: CustomerBillListForm, CustomerBillDetailForm, ARReportForm
        public static void ApplyBillColor(DataGridViewRow row)
        {
            string status = GetStatus(row);
            switch (status)
            {
                case "Overdue": SetRowColor(row, ColorOverdue); break;
                case "PartiallyPaid": SetRowColor(row, ColorPartiallyPaid); break;
                case "Paid": SetRowColor(row, ColorPaid); break;
                default: SetRowColor(row, ColorDefault); break;
            }
        }

        // ── Purchase order rows ───────────────────────────────────────────────
        // Status values: "Pending" | "Delivered" | "Cancelled"
        // Used in: PurchaseOrderListForm
        public static void ApplyPurchaseOrderColor(DataGridViewRow row)
        {
            string status = GetStatus(row);
            switch (status)
            {
                case "Delivered": SetRowColor(row, ColorDelivered); break;
                case "Cancelled": SetRowColor(row, ColorCancelled); break;
                case "Pending": SetRowColor(row, ColorPending); break;
                default: SetRowColor(row, ColorDefault); break;
            }
        }

        // ── Payroll record rows ───────────────────────────────────────────────
        // Status values: "Pending" | "Processed" | "Failed"
        // Used in: PayrollListForm
        public static void ApplyPayrollColor(DataGridViewRow row)
        {
            string status = GetStatus(row);
            switch (status)
            {
                case "Processed": SetRowColor(row, ColorProcessed); break;
                case "Failed": SetRowColor(row, ColorFailed); break;
                case "Pending": SetRowColor(row, ColorPending); break;
                default: SetRowColor(row, ColorDefault); break;
            }
        }

        // ── Customer order rows ───────────────────────────────────────────────
        // Status values: "Open" | "Shipped" | "Closed" | "Cancelled"
        // Used in: CustomerOrderListForm
        public static void ApplyCustomerOrderColor(DataGridViewRow row)
        {
            string status = GetStatus(row);
            switch (status)
            {
                case "Shipped": SetRowColor(row, ColorShipped); break;
                case "Closed": SetRowColor(row, ColorPaid); break;
                case "Cancelled": SetRowColor(row, ColorCancelled); break;
                default: SetRowColor(row, ColorDefault); break;
            }
        }

        // ── Raw material rows ─────────────────────────────────────────────────
        // No status column — color is based on whether the material is below
        // its reorder threshold. Pass the row and both quantity values.
        // Used in: RawMaterialListForm
        public static void ApplyRawMaterialColor(DataGridViewRow row,
                                                  decimal quantityOnHand,
                                                  decimal reorderThreshold)
        {
            if (quantityOnHand < reorderThreshold)
                SetRowColor(row, ColorOverdue);     // below threshold = needs reorder
            else if (quantityOnHand < reorderThreshold * 1.2m)
                SetRowColor(row, ColorPartiallyPaid); // within 20% of threshold = warning
            else
                SetRowColor(row, ColorDefault);
        }

        // ── Vendor rows ───────────────────────────────────────────────────────
        // RelationshipStatus values: "Active" | "Suspended" | "Terminated"
        // Used in: VendorListForm
        public static void ApplyVendorColor(DataGridViewRow row)
        {
            string status = GetStatus(row);
            switch (status)
            {
                case "Terminated": SetRowColor(row, ColorFailed); break;
                case "Suspended": SetRowColor(row, ColorPartiallyPaid); break;
                default: SetRowColor(row, ColorDefault); break;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        // Reads the "Status" cell safely — returns empty string if the column
        // doesn't exist or the cell value is null.
        private static string GetStatus(DataGridViewRow row)
        {
            if (row.Cells["Status"] == null) return string.Empty;
            return row.Cells["Status"].Value?.ToString() ?? string.Empty;
        }

        // Sets both the BackColor of every cell in the row and the row's own
        // DefaultCellStyle so the color persists after selection/focus changes.
        private static void SetRowColor(DataGridViewRow row, Color color)
        {
            row.DefaultCellStyle.BackColor = color;
        }
    }
}