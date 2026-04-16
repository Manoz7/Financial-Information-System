using FIS.Database;
using FIS.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace FIS.Services
{
    public class PaymentAPService
    {
        // ── Get a single payment by ID ────────────────────────────────────────
        // Used to confirm a payment was recorded after ProcessDuePayments runs,
        // and to retrieve reference numbers for vendor confirmation.
        public PaymentAP GetById(int paymentAPId)
        {
            try
            {
                string sql = "SELECT p.PaymentAPId, p.InvoiceAPId, p.AmountPaid, " +
                             "p.DatePaid, p.PaymentMethod, p.ReferenceNumber, " +
                             "i.InvoiceNumber, i.DueDate, i.VendorId, " +
                             "v.VendorName " +
                             "FROM PaymentsAP p " +
                             "INNER JOIN InvoicesAP i ON i.InvoiceAPId = p.InvoiceAPId " +
                             "INNER JOIN Vendors v   ON v.VendorId     = i.VendorId " +
                             "WHERE p.PaymentAPId = @PaymentAPId;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@PaymentAPId", paymentAPId));

                if (dt.Rows.Count == 0) return null;
                return MapRowToPaymentAP(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetById error: " + ex.Message);
                return null;
            }
        }

        // ── Get the payment linked to a specific invoice ───────────────────────
        // Case §2.1.2: one InvoiceAP has at most one PaymentAP (full payment only).
        // Used to verify whether an invoice has already been paid before
        // attempting to process it again.
        public PaymentAP GetByInvoice(int invoiceAPId)
        {
            try
            {
                string sql = "SELECT p.PaymentAPId, p.InvoiceAPId, p.AmountPaid, " +
                             "p.DatePaid, p.PaymentMethod, p.ReferenceNumber, " +
                             "i.InvoiceNumber, i.DueDate, i.VendorId, " +
                             "v.VendorName " +
                             "FROM PaymentsAP p " +
                             "INNER JOIN InvoicesAP i ON i.InvoiceAPId = p.InvoiceAPId " +
                             "INNER JOIN Vendors v   ON v.VendorId     = i.VendorId " +
                             "WHERE p.InvoiceAPId = @InvoiceAPId;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@InvoiceAPId", invoiceAPId));

                if (dt.Rows.Count == 0) return null;
                return MapRowToPaymentAP(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetByInvoice error: " + ex.Message);
                return null;
            }
        }

        // ── Get all payments ──────────────────────────────────────────────────
        // Case §2.1.4: "monthly report showing accounts paid" — full payment
        // history is needed for Veronica's A/P reports.
        public List<PaymentAP> GetAllPayments()
        {
            var list = new List<PaymentAP>();
            try
            {
                string sql = "SELECT p.PaymentAPId, p.InvoiceAPId, p.AmountPaid, " +
                             "p.DatePaid, p.PaymentMethod, p.ReferenceNumber, " +
                             "i.InvoiceNumber, i.DueDate, i.VendorId, " +
                             "v.VendorName " +
                             "FROM PaymentsAP p " +
                             "INNER JOIN InvoicesAP i ON i.InvoiceAPId = p.InvoiceAPId " +
                             "INNER JOIN Vendors v   ON v.VendorId     = i.VendorId " +
                             "ORDER BY p.DatePaid DESC;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToPaymentAP(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllPayments error: " + ex.Message);
            }
            return list;
        }

        // ── Get all payments made to a specific vendor ────────────────────────
        // Case §2.1.2: Veronica needs to track payment history per vendor to
        // maintain vendor relationships and verify on-time payment record.
        public List<PaymentAP> GetPaymentsByVendor(int vendorId)
        {
            var list = new List<PaymentAP>();
            try
            {
                string sql = "SELECT p.PaymentAPId, p.InvoiceAPId, p.AmountPaid, " +
                             "p.DatePaid, p.PaymentMethod, p.ReferenceNumber, " +
                             "i.InvoiceNumber, i.DueDate, i.VendorId, " +
                             "v.VendorName " +
                             "FROM PaymentsAP p " +
                             "INNER JOIN InvoicesAP i ON i.InvoiceAPId = p.InvoiceAPId " +
                             "INNER JOIN Vendors v   ON v.VendorId     = i.VendorId " +
                             "WHERE i.VendorId = @VendorId " +
                             "ORDER BY p.DatePaid DESC;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@VendorId", vendorId));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToPaymentAP(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetPaymentsByVendor error: " + ex.Message);
            }
            return list;
        }

        // ── Get payments made within a date range ─────────────────────────────
        // Case §2.1.4: "monthly report showing accounts paid" — used to scope
        // the report to a specific month or custom date range.
        public List<PaymentAP> GetPaymentsByDateRange(DateTime from, DateTime to)
        {
            var list = new List<PaymentAP>();
            try
            {
                string sql = "SELECT p.PaymentAPId, p.InvoiceAPId, p.AmountPaid, " +
                             "p.DatePaid, p.PaymentMethod, p.ReferenceNumber, " +
                             "i.InvoiceNumber, i.DueDate, i.VendorId, " +
                             "v.VendorName " +
                             "FROM PaymentsAP p " +
                             "INNER JOIN InvoicesAP i ON i.InvoiceAPId = p.InvoiceAPId " +
                             "INNER JOIN Vendors v   ON v.VendorId     = i.VendorId " +
                             "WHERE p.DatePaid >= @From " +
                             "AND p.DatePaid   <  @To " +
                             "ORDER BY p.DatePaid ASC;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@From", from),
                    new MySqlParameter("@To", to));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToPaymentAP(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetPaymentsByDateRange error: " + ex.Message);
            }
            return list;
        }

        // ── Update the reference number on a payment ──────────────────────────
        // Case §2.1.2: reference number (check number or bank confirmation) is
        // stored per payment so WBS can reconcile payments with vendors.
        // Auto-payments created by ProcessDuePayments may not have a reference
        // number at the time of creation — this method updates it once available.
        public bool UpdateReferenceNumber(int paymentAPId, string referenceNumber)
        {
            try
            {
                string sql = "UPDATE PaymentsAP SET ReferenceNumber = @ReferenceNumber " +
                             "WHERE PaymentAPId = @PaymentAPId;";

                int rows = DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@ReferenceNumber", referenceNumber),
                    new MySqlParameter("@PaymentAPId", paymentAPId));

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("UpdateReferenceNumber error: " + ex.Message);
                return false;
            }
        }

        // ── Check if an invoice has already been paid ─────────────────────────
        // Guard used by ProcessDuePayments in InvoiceAPService to prevent
        // duplicate payments being issued for the same invoice.
        public bool InvoiceAlreadyPaid(int invoiceAPId)
        {
            try
            {
                string sql = "SELECT COUNT(*) FROM PaymentsAP " +
                             "WHERE InvoiceAPId = @InvoiceAPId;";

                int count = Convert.ToInt32(DBHelper.ExecuteScalar(sql,
                    new MySqlParameter("@InvoiceAPId", invoiceAPId)));

                return count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("InvoiceAlreadyPaid error: " + ex.Message);
                return false;
            }
        }

        // ── Private helper — maps a DataRow to a PaymentAP model ─────────────
        private PaymentAP MapRowToPaymentAP(System.Data.DataRow row)
        {
            return new PaymentAP
            {
                PaymentAPId = Convert.ToInt32(row["PaymentAPId"]),
                InvoiceAPId = Convert.ToInt32(row["InvoiceAPId"]),
                AmountPaid = Convert.ToDecimal(row["AmountPaid"]),
                DatePaid = Convert.ToDateTime(row["DatePaid"]),
                PaymentMethod = row["PaymentMethod"] == DBNull.Value ? string.Empty : row["PaymentMethod"].ToString(),
                ReferenceNumber = row["ReferenceNumber"] == DBNull.Value ? string.Empty : row["ReferenceNumber"].ToString()
            };
        }
    }
}