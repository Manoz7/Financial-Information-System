using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FIS.Utils
{
    // Centralises all form input validation so every form validates the same
    // way. Each method sets an ErrorProvider message on the control if
    // validation fails, and clears it if validation passes.
    //
    // Usage pattern in a form:
    //   bool valid = true;
    //   valid &= FormValidator.IsRequired(txtVendorName, "Vendor name", errorProvider1);
    //   valid &= FormValidator.IsDecimal(txtUnitPrice, "Unit price", errorProvider1);
    //   if (!valid) return;
    public static class FormValidator
    {
        // ── Required text field ───────────────────────────────────────────────
        // Fails if the TextBox is empty or whitespace only.
        // Used on: vendor name, invoice number, employee name, material name, etc.
        public static bool IsRequired(TextBox control, string fieldName,
                                      ErrorProvider errorProvider)
        {
            if (string.IsNullOrWhiteSpace(control.Text))
            {
                errorProvider.SetError(control, fieldName + " is required.");
                return false;
            }
            errorProvider.SetError(control, string.Empty);
            return true;
        }

        // ── Required ComboBox selection ───────────────────────────────────────
        // Fails if nothing is selected (SelectedIndex = -1).
        // Used on: vendor dropdown, payment method, status selectors.
        public static bool IsRequired(ComboBox control, string fieldName,
                                      ErrorProvider errorProvider)
        {
            if (control.SelectedIndex == -1)
            {
                errorProvider.SetError(control, fieldName + " must be selected.");
                return false;
            }
            errorProvider.SetError(control, string.Empty);
            return true;
        }

        // ── Valid decimal ─────────────────────────────────────────────────────
        // Fails if the text cannot be parsed as a decimal.
        // Used on: unit price, total amount, quantity fields.
        public static bool IsDecimal(TextBox control, string fieldName,
                                     ErrorProvider errorProvider)
        {
            if (!decimal.TryParse(control.Text.Trim(), out _))
            {
                errorProvider.SetError(control, fieldName + " must be a valid number.");
                return false;
            }
            errorProvider.SetError(control, string.Empty);
            return true;
        }

        // ── Positive decimal (> 0) ────────────────────────────────────────────
        // Fails if the value is zero or negative.
        // Used on: payment amounts, invoice totals, reorder quantities,
        //          unit prices — none of these can be zero or negative.
        public static bool IsPositiveDecimal(TextBox control, string fieldName,
                                             ErrorProvider errorProvider)
        {
            if (!decimal.TryParse(control.Text.Trim(), out decimal value) || value <= 0)
            {
                errorProvider.SetError(control, fieldName + " must be greater than zero.");
                return false;
            }
            errorProvider.SetError(control, string.Empty);
            return true;
        }

        // ── Non-negative decimal (>= 0) ───────────────────────────────────────
        // Fails if the value is negative.
        // Used on: quantity on hand — can legitimately be zero.
        public static bool IsNonNegativeDecimal(TextBox control, string fieldName,
                                                ErrorProvider errorProvider)
        {
            if (!decimal.TryParse(control.Text.Trim(), out decimal value) || value < 0)
            {
                errorProvider.SetError(control, fieldName + " cannot be negative.");
                return false;
            }
            errorProvider.SetError(control, string.Empty);
            return true;
        }

        // ── Valid integer ─────────────────────────────────────────────────────
        // Fails if the text cannot be parsed as an integer.
        // Used on: order quantity.
        public static bool IsInteger(TextBox control, string fieldName,
                                     ErrorProvider errorProvider)
        {
            if (!int.TryParse(control.Text.Trim(), out _))
            {
                errorProvider.SetError(control, fieldName + " must be a whole number.");
                return false;
            }
            errorProvider.SetError(control, string.Empty);
            return true;
        }

        // ── Positive integer (> 0) ────────────────────────────────────────────
        // Fails if the value is zero or negative.
        // Used on: order quantity — must order at least 1.
        public static bool IsPositiveInteger(TextBox control, string fieldName,
                                             ErrorProvider errorProvider)
        {
            if (!int.TryParse(control.Text.Trim(), out int value) || value <= 0)
            {
                errorProvider.SetError(control, fieldName + " must be a whole number greater than zero.");
                return false;
            }
            errorProvider.SetError(control, string.Empty);
            return true;
        }

        // ── Date is not in the past ───────────────────────────────────────────
        // Fails if the selected date is earlier than today.
        // Used on: invoice due date, expected delivery date.
        public static bool IsNotPastDate(DateTimePicker control, string fieldName,
                                         ErrorProvider errorProvider)
        {
            if (control.Value.Date < DateTime.Today)
            {
                errorProvider.SetError(control, fieldName + " cannot be in the past.");
                return false;
            }
            errorProvider.SetError(control, string.Empty);
            return true;
        }

        // ── Second date is after first date ──────────────────────────────────
        // Fails if laterControl.Value <= earlierControl.Value.
        // Used on:
        //   - InvoiceDetailForm: DueDate must be after InvoiceDate
        //   - PayrollListForm:   PayPeriodEnd must be after PayPeriodStart
        public static bool IsDateAfter(DateTimePicker laterControl,
                                       DateTimePicker earlierControl,
                                       string message,
                                       ErrorProvider errorProvider)
        {
            if (laterControl.Value.Date <= earlierControl.Value.Date)
            {
                errorProvider.SetError(laterControl, message);
                return false;
            }
            errorProvider.SetError(laterControl, string.Empty);
            return true;
        }

        // ── Valid email address ───────────────────────────────────────────────
        // Fails if the text is not empty AND does not match a basic email pattern.
        // Email is optional on vendors and customers — empty is allowed.
        // Used on: vendor email, customer email, employee email.
        public static bool IsValidEmail(TextBox control, string fieldName,
                                        ErrorProvider errorProvider)
        {
            string value = control.Text.Trim();
            if (string.IsNullOrEmpty(value))
            {
                errorProvider.SetError(control, string.Empty);
                return true;    // Email is optional — blank is valid
            }

            bool valid = Regex.IsMatch(value,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase);

            if (!valid)
            {
                errorProvider.SetError(control, fieldName + " is not a valid email address.");
                return false;
            }
            errorProvider.SetError(control, string.Empty);
            return true;
        }

        // ── Payment does not exceed balance ───────────────────────────────────
        // Fails if the entered payment amount exceeds the bill's remaining balance.
        // Used on: RecordPaymentForm — prevents overpayment entry.
        public static bool IsWithinBalance(TextBox control, decimal balanceRemaining,
                                           ErrorProvider errorProvider)
        {
            if (!decimal.TryParse(control.Text.Trim(), out decimal value) || value <= 0)
            {
                errorProvider.SetError(control, "Payment amount must be greater than zero.");
                return false;
            }
            if (value > balanceRemaining)
            {
                errorProvider.SetError(control,
                    string.Format("Payment cannot exceed the remaining balance of {0:C}.",
                                  balanceRemaining));
                return false;
            }
            errorProvider.SetError(control, string.Empty);
            return true;
        }

        // ── Clear all errors on an ErrorProvider ──────────────────────────────
        // Call at the start of every Save/Submit button click handler to reset
        // all field highlights before re-running validation.
        public static void ClearErrors(ErrorProvider errorProvider)
        {
            errorProvider.Clear();
        }
    }
}