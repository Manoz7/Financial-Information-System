using System.Windows.Forms;

namespace FIS.Utils
{
    // Centralises all dialog boxes so every form shows the same style of
    // message. Avoids scattered MessageBox.Show calls with inconsistent
    // captions and icons throughout the codebase.
    public static class MessageHelper
    {
        // ── Application name shown in every dialog title bar ─────────────────
        private const string AppTitle = "Financial Information System";

        // ── Success ───────────────────────────────────────────────────────────
        // Use after a record is saved, payment processed, PO generated, etc.
        public static void ShowSuccess(string message)
        {
            MessageBox.Show(
                message,
                AppTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ── Error ─────────────────────────────────────────────────────────────
        // Use when a service method returns false or throws — DB errors,
        // failed inserts, payment processing failures, etc.
        public static void ShowError(string message)
        {
            MessageBox.Show(
                message,
                AppTitle + " — Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        // ── Warning ───────────────────────────────────────────────────────────
        // Use for non-fatal issues: no records found, invoice already paid,
        // order already billed, vendor already terminated, etc.
        public static void ShowWarning(string message)
        {
            MessageBox.Show(
                message,
                AppTitle + " — Warning",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        // ── Confirm ───────────────────────────────────────────────────────────
        // Use before any irreversible action:
        //   - Cancelling a purchase order
        //   - Terminating a vendor relationship
        //   - Running payroll disbursement
        //   - Processing all due invoice payments
        // Returns true if the user clicked Yes, false if No.
        public static bool Confirm(string message)
        {
            return MessageBox.Show(
                message,
                AppTitle + " — Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2)   // Default to No for safety
                == DialogResult.Yes;
        }

        // ── Confirm with custom title ─────────────────────────────────────────
        // Use when the action being confirmed needs a more specific title,
        // e.g. "Process Payroll" or "Generate Purchase Orders".
        public static bool Confirm(string message, string actionTitle)
        {
            return MessageBox.Show(
                message,
                AppTitle + " — " + actionTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2)
                == DialogResult.Yes;
        }
    }
}