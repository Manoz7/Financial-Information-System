using FIS.Database;
using FIS.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace FIS.Services
{
    public class InvoiceAPService
    {
        // ── Record a new vendor invoice ───────────────────────────────────────
        // Case §2.1.2: "invoice information should be input when received by
        // vendors" — replaces the old system where invoices sat in a central
        // location with no one responsible for recording or paying them.
        public bool AddInvoice(InvoiceAP invoice)
        {
            try
            {
                string sql = "INSERT INTO InvoicesAP " +
                             "(VendorId, PurchaseOrderId, InvoiceNumber, InvoiceDate, " +
                             "DueDate, TotalAmount, AmountPaid, Status, ReceivedAt) " +
                             "VALUES (@VendorId, @PurchaseOrderId, @InvoiceNumber, @InvoiceDate, " +
                             "@DueDate, @TotalAmount, 0, 'Unpaid', @ReceivedAt);";

                int rows = DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@VendorId", invoice.VendorId),
                    new MySqlParameter("@PurchaseOrderId", invoice.PurchaseOrderId.HasValue
                                                               ? (object)invoice.PurchaseOrderId.Value
                                                               : DBNull.Value),
                    new MySqlParameter("@InvoiceNumber", invoice.InvoiceNumber),
                    new MySqlParameter("@InvoiceDate", invoice.InvoiceDate),
                    new MySqlParameter("@DueDate", invoice.DueDate),
                    new MySqlParameter("@TotalAmount", invoice.TotalAmount),
                    new MySqlParameter("@ReceivedAt", DateTime.Now));

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("AddInvoice error: " + ex.Message);
                return false;
            }
        }

        // ── Get all invoices ──────────────────────────────────────────────────
        // Case §2.1.4: "monthly report showing accounts paid" — full invoice
        // list is the basis for Veronica's A/P reports.
        public List<InvoiceAP> GetAllInvoices()
        {
            var list = new List<InvoiceAP>();
            try
            {
                string sql = "SELECT i.InvoiceAPId, i.VendorId, i.PurchaseOrderId, " +
                             "i.InvoiceNumber, i.InvoiceDate, i.DueDate, i.TotalAmount, " +
                             "i.AmountPaid, i.DatePaid, i.Status, i.ReceivedAt, " +
                             "v.VendorName " +
                             "FROM InvoicesAP i " +
                             "INNER JOIN Vendors v ON v.VendorId = i.VendorId " +
                             "ORDER BY i.DueDate ASC;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToInvoice(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllInvoices error: " + ex.Message);
            }
            return list;
        }

        // ── Get a single invoice by ID ────────────────────────────────────────
        public InvoiceAP GetById(int invoiceAPId)
        {
            try
            {
                string sql = "SELECT i.InvoiceAPId, i.VendorId, i.PurchaseOrderId, " +
                             "i.InvoiceNumber, i.InvoiceDate, i.DueDate, i.TotalAmount, " +
                             "i.AmountPaid, i.DatePaid, i.Status, i.ReceivedAt, " +
                             "v.VendorName " +
                             "FROM InvoicesAP i " +
                             "INNER JOIN Vendors v ON v.VendorId = i.VendorId " +
                             "WHERE i.InvoiceAPId = @InvoiceAPId;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@InvoiceAPId", invoiceAPId));

                if (dt.Rows.Count == 0) return null;
                return MapRowToInvoice(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetById error: " + ex.Message);
                return null;
            }
        }

        // ── Get all unpaid invoices ───────────────────────────────────────────
        // Case §2.1.2: "the system should automatically generate payments at the
        // appropriate time so invoices are paid on time."
        // This method feeds the auto-payment process — only Unpaid invoices
        // are candidates for payment generation.
        public List<InvoiceAP> GetUnpaidInvoices()
        {
            var list = new List<InvoiceAP>();
            try
            {
                string sql = "SELECT i.InvoiceAPId, i.VendorId, i.PurchaseOrderId, " +
                             "i.InvoiceNumber, i.InvoiceDate, i.DueDate, i.TotalAmount, " +
                             "i.AmountPaid, i.DatePaid, i.Status, i.ReceivedAt, " +
                             "v.VendorName " +
                             "FROM InvoicesAP i " +
                             "INNER JOIN Vendors v ON v.VendorId = i.VendorId " +
                             "WHERE i.Status = 'Unpaid' " +
                             "ORDER BY i.DueDate ASC;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToInvoice(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetUnpaidInvoices error: " + ex.Message);
            }
            return list;
        }

        // ── Get invoices due on or before a given date ────────────────────────
        // Case §2.1.2: auto-payment is triggered at the appropriate time —
        // this returns all unpaid invoices whose DueDate has been reached.
        // Called by the auto-payment scheduler (e.g. run daily).
        public List<InvoiceAP> GetInvoicesDueBy(DateTime dueBy)
        {
            var list = new List<InvoiceAP>();
            try
            {
                string sql = "SELECT i.InvoiceAPId, i.VendorId, i.PurchaseOrderId, " +
                             "i.InvoiceNumber, i.InvoiceDate, i.DueDate, i.TotalAmount, " +
                             "i.AmountPaid, i.DatePaid, i.Status, i.ReceivedAt, " +
                             "v.VendorName " +
                             "FROM InvoicesAP i " +
                             "INNER JOIN Vendors v ON v.VendorId = i.VendorId " +
                             "WHERE i.Status = 'Unpaid' " +
                             "AND i.DueDate <= @DueBy " +
                             "ORDER BY i.DueDate ASC;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@DueBy", dueBy));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToInvoice(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetInvoicesDueBy error: " + ex.Message);
            }
            return list;
        }

        // ── Auto-pay: process all invoices due today or earlier ───────────────
        // Case §2.1.2: "the system should automatically generate payments at the
        // appropriate time so invoices are paid on time."
        // Case §2.1.2: "the system should record when each invoice was paid.
        // Data on the date the invoice was due, the date it was paid, as well as
        // the total amount paid should be stored in the system."
        //
        // For each due invoice this method:
        //   1. Creates a PaymentAP record
        //   2. Updates InvoiceAP: AmountPaid, DatePaid, Status = "Paid"
        //
        // Returns the count of invoices successfully processed.
        public int ProcessDuePayments()
        {
            int processedCount = 0;
            try
            {
                List<InvoiceAP> dueInvoices = GetInvoicesDueBy(DateTime.Today);

                foreach (var invoice in dueInvoices)
                {
                    // Step 1 — Create the PaymentAP record
                    string paymentSql = "INSERT INTO PaymentsAP " +
                                        "(InvoiceAPId, AmountPaid, DatePaid, PaymentMethod) " +
                                        "VALUES (@InvoiceAPId, @AmountPaid, @DatePaid, @PaymentMethod);";

                    int paymentRows = DBHelper.ExecuteNonQuery(paymentSql,
                        new MySqlParameter("@InvoiceAPId", invoice.InvoiceAPId),
                        new MySqlParameter("@AmountPaid", invoice.TotalAmount),
                        new MySqlParameter("@DatePaid", DateTime.Now),
                        new MySqlParameter("@PaymentMethod", "ElectronicTransfer"));

                    if (paymentRows == 0) continue;

                    // Step 2 — Update the invoice: record DatePaid, AmountPaid, Status
                    // Case §2.1.2 explicit: store DueDate (already stored), DatePaid,
                    // and total amount paid.
                    string updateSql = "UPDATE InvoicesAP SET " +
                                       "AmountPaid = @AmountPaid, " +
                                       "DatePaid   = @DatePaid, " +
                                       "Status     = 'Paid' " +
                                       "WHERE InvoiceAPId = @InvoiceAPId;";

                    DBHelper.ExecuteNonQuery(updateSql,
                        new MySqlParameter("@AmountPaid", invoice.TotalAmount),
                        new MySqlParameter("@DatePaid", DateTime.Now),
                        new MySqlParameter("@InvoiceAPId", invoice.InvoiceAPId));

                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ProcessDuePayments error: " + ex.Message);
            }
            return processedCount;
        }

        // ── Mark overdue invoices ─────────────────────────────────────────────
        // Case §2.1.2: "reports on Accounts Payable will allow Veronica to track
        // whether invoices are being paid on time."
        // Any unpaid invoice past its DueDate is flagged Overdue.
        // Run daily alongside ProcessDuePayments.
        public int MarkOverdueInvoices()
        {
            try
            {
                string sql = "UPDATE InvoicesAP SET Status = 'Overdue' " +
                             "WHERE Status = 'Unpaid' " +
                             "AND DueDate < @Today;";

                return DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@Today", DateTime.Today));
            }
            catch (Exception ex)
            {
                Console.WriteLine("MarkOverdueInvoices error: " + ex.Message);
                return 0;
            }
        }

        // ── Get invoices by vendor ────────────────────────────────────────────
        // Case §2.1.2: Veronica needs to track payment timeliness per vendor
        // to maintain and improve vendor relationships.
        public List<InvoiceAP> GetInvoicesByVendor(int vendorId)
        {
            var list = new List<InvoiceAP>();
            try
            {
                string sql = "SELECT i.InvoiceAPId, i.VendorId, i.PurchaseOrderId, " +
                             "i.InvoiceNumber, i.InvoiceDate, i.DueDate, i.TotalAmount, " +
                             "i.AmountPaid, i.DatePaid, i.Status, i.ReceivedAt, " +
                             "v.VendorName " +
                             "FROM InvoicesAP i " +
                             "INNER JOIN Vendors v ON v.VendorId = i.VendorId " +
                             "WHERE i.VendorId = @VendorId " +
                             "ORDER BY i.DueDate DESC;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@VendorId", vendorId));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToInvoice(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetInvoicesByVendor error: " + ex.Message);
            }
            return list;
        }

        // ── Monthly report: paid invoices ─────────────────────────────────────
        // Case §2.1.4: "a monthly report showing accounts paid"
        // Returns all invoices paid within the given month.
        public List<InvoiceAP> GetPaidInvoicesForMonth(int year, int month)
        {
            var list = new List<InvoiceAP>();
            try
            {
                DateTime start = new DateTime(year, month, 1);
                DateTime end = start.AddMonths(1);

                string sql = "SELECT i.InvoiceAPId, i.VendorId, i.PurchaseOrderId, " +
                             "i.InvoiceNumber, i.InvoiceDate, i.DueDate, i.TotalAmount, " +
                             "i.AmountPaid, i.DatePaid, i.Status, i.ReceivedAt, " +
                             "v.VendorName " +
                             "FROM InvoicesAP i " +
                             "INNER JOIN Vendors v ON v.VendorId = i.VendorId " +
                             "WHERE i.Status = 'Paid' " +
                             "AND i.DatePaid >= @Start " +
                             "AND i.DatePaid < @End " +
                             "ORDER BY i.DatePaid ASC;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@Start", start),
                    new MySqlParameter("@End", end));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToInvoice(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetPaidInvoicesForMonth error: " + ex.Message);
            }
            return list;
        }

        // ── Private helper — maps a DataRow to an InvoiceAP model ────────────
        private InvoiceAP MapRowToInvoice(System.Data.DataRow row)
        {
            return new InvoiceAP
            {
                InvoiceAPId = Convert.ToInt32(row["InvoiceAPId"]),
                VendorId = Convert.ToInt32(row["VendorId"]),
                PurchaseOrderId = row["PurchaseOrderId"] == DBNull.Value
                                      ? (int?)null
                                      : Convert.ToInt32(row["PurchaseOrderId"]),
                InvoiceNumber = row["InvoiceNumber"] == DBNull.Value ? string.Empty : row["InvoiceNumber"].ToString(),
                InvoiceDate = Convert.ToDateTime(row["InvoiceDate"]),
                DueDate = Convert.ToDateTime(row["DueDate"]),
                TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                AmountPaid = Convert.ToDecimal(row["AmountPaid"]),
                DatePaid = row["DatePaid"] == DBNull.Value
                                      ? (DateTime?)null
                                      : Convert.ToDateTime(row["DatePaid"]),
                Status = row["Status"] == DBNull.Value ? "Unpaid" : row["Status"].ToString(),
                ReceivedAt = row["ReceivedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["ReceivedAt"])
            };
        }
    }
}