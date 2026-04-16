using FIS.Database;
using FIS.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace FIS.Services
{
    public class PaymentARService
    {
        // ── Record a payment received from a customer ─────────────────────────
        // Case §2.1.3: "when payments are received from customers, the FIS should
        // check the payment against the A/R file and record the amount paid."
        // Case §2.1.3: "customers may submit a partial payment and this partial
        // payment should be recorded with the date it is received."
        //
        // This method:
        //   1. Validates the bill exists and is not already fully paid
        //   2. Inserts the PaymentAR record with date received
        //   3. Calls CustomerBillService.ApplyPayment to reduce BalanceRemaining
        //
        // Returns false if the bill does not exist, is already paid, or if
        // the amount exceeds the remaining balance.
        public bool RecordPayment(PaymentAR payment)
        {
            try
            {
                // Step 1 — Verify the bill exists and has an outstanding balance
                string billSql = "SELECT CustomerBillId, BalanceRemaining, Status " +
                                 "FROM CustomerBills " +
                                 "WHERE CustomerBillId = @CustomerBillId;";

                var billDt = DBHelper.ExecuteQuery(billSql,
                    new MySqlParameter("@CustomerBillId", payment.CustomerBillId));

                if (billDt.Rows.Count == 0) return false;

                string currentStatus = billDt.Rows[0]["Status"].ToString();
                if (currentStatus == "Paid") return false;

                decimal balanceRemaining = Convert.ToDecimal(billDt.Rows[0]["BalanceRemaining"]);
                if (payment.AmountReceived <= 0) return false;
                if (payment.AmountReceived > balanceRemaining)
                    payment.AmountReceived = balanceRemaining;

                // Step 2 — Insert the PaymentAR record
                // Case §2.1.3 explicit: partial payment must be recorded WITH
                // the date it is received.
                string insertSql = "INSERT INTO PaymentsAR " +
                                   "(CustomerBillId, AmountReceived, DateReceived, " +
                                   "PaymentMethod, ReferenceNumber) " +
                                   "VALUES (@CustomerBillId, @AmountReceived, @DateReceived, " +
                                   "@PaymentMethod, @ReferenceNumber);";

                int rows = DBHelper.ExecuteNonQuery(insertSql,
                    new MySqlParameter("@CustomerBillId", payment.CustomerBillId),
                    new MySqlParameter("@AmountReceived", payment.AmountReceived),
                    new MySqlParameter("@DateReceived", payment.DateReceived),
                    new MySqlParameter("@PaymentMethod", payment.PaymentMethod ?? (object)DBNull.Value),
                    new MySqlParameter("@ReferenceNumber", payment.ReferenceNumber ?? (object)DBNull.Value));

                if (rows == 0) return false;

                // Step 3 — Update the bill balance and status
                var billService = new CustomerBillService();
                return billService.ApplyPayment(payment.CustomerBillId, payment.AmountReceived);
            }
            catch (Exception ex)
            {
                Console.WriteLine("RecordPayment error: " + ex.Message);
                return false;
            }
        }

        // ── Get a single payment by ID ────────────────────────────────────────
        public PaymentAR GetById(int paymentARId)
        {
            try
            {
                string sql = "SELECT p.PaymentARId, p.CustomerBillId, p.AmountReceived, " +
                             "p.DateReceived, p.PaymentMethod, p.ReferenceNumber, " +
                             "cb.TotalAmountDue, cb.BalanceRemaining, cb.Status, " +
                             "c.CustomerName " +
                             "FROM PaymentsAR p " +
                             "INNER JOIN CustomerBills cb ON cb.CustomerBillId = p.CustomerBillId " +
                             "INNER JOIN Customers c     ON c.CustomerId       = cb.CustomerId " +
                             "WHERE p.PaymentARId = @PaymentARId;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@PaymentARId", paymentARId));

                if (dt.Rows.Count == 0) return null;
                return MapRowToPaymentAR(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetById error: " + ex.Message);
                return null;
            }
        }

        // ── Get all payments received against a specific bill ─────────────────
        // Case §2.1.3: a bill may have multiple partial payments — this returns
        // the full payment history for one bill so the running balance can be
        // verified and shown on the A/R report.
        public List<PaymentAR> GetPaymentsByBill(int customerBillId)
        {
            var list = new List<PaymentAR>();
            try
            {
                string sql = "SELECT p.PaymentARId, p.CustomerBillId, p.AmountReceived, " +
                             "p.DateReceived, p.PaymentMethod, p.ReferenceNumber, " +
                             "cb.TotalAmountDue, cb.BalanceRemaining, cb.Status, " +
                             "c.CustomerName " +
                             "FROM PaymentsAR p " +
                             "INNER JOIN CustomerBills cb ON cb.CustomerBillId = p.CustomerBillId " +
                             "INNER JOIN Customers c     ON c.CustomerId       = cb.CustomerId " +
                             "WHERE p.CustomerBillId = @CustomerBillId " +
                             "ORDER BY p.DateReceived ASC;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@CustomerBillId", customerBillId));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToPaymentAR(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetPaymentsByBill error: " + ex.Message);
            }
            return list;
        }

        // ── Get all payments received from a specific customer ────────────────
        // Case §2.1.3: A/R report needs full payment history per customer to
        // show total received vs total billed and outstanding balance.
        public List<PaymentAR> GetPaymentsByCustomer(int customerId)
        {
            var list = new List<PaymentAR>();
            try
            {
                string sql = "SELECT p.PaymentARId, p.CustomerBillId, p.AmountReceived, " +
                             "p.DateReceived, p.PaymentMethod, p.ReferenceNumber, " +
                             "cb.TotalAmountDue, cb.BalanceRemaining, cb.Status, " +
                             "c.CustomerName " +
                             "FROM PaymentsAR p " +
                             "INNER JOIN CustomerBills cb ON cb.CustomerBillId = p.CustomerBillId " +
                             "INNER JOIN Customers c     ON c.CustomerId       = cb.CustomerId " +
                             "WHERE cb.CustomerId = @CustomerId " +
                             "ORDER BY p.DateReceived DESC;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@CustomerId", customerId));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToPaymentAR(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetPaymentsByCustomer error: " + ex.Message);
            }
            return list;
        }

        // ── Get all payments received within a date range ─────────────────────
        // Case §2.1.4: A/R report scoped to a reporting period.
        // Also used to reconcile cash receipts for a given month.
        public List<PaymentAR> GetPaymentsByDateRange(DateTime from, DateTime to)
        {
            var list = new List<PaymentAR>();
            try
            {
                string sql = "SELECT p.PaymentARId, p.CustomerBillId, p.AmountReceived, " +
                             "p.DateReceived, p.PaymentMethod, p.ReferenceNumber, " +
                             "cb.TotalAmountDue, cb.BalanceRemaining, cb.Status, " +
                             "c.CustomerName " +
                             "FROM PaymentsAR p " +
                             "INNER JOIN CustomerBills cb ON cb.CustomerBillId = p.CustomerBillId " +
                             "INNER JOIN Customers c     ON c.CustomerId       = cb.CustomerId " +
                             "WHERE p.DateReceived >= @From " +
                             "AND   p.DateReceived <  @To " +
                             "ORDER BY p.DateReceived ASC;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@From", from),
                    new MySqlParameter("@To", to));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToPaymentAR(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetPaymentsByDateRange error: " + ex.Message);
            }
            return list;
        }

        // ── Get all payments ──────────────────────────────────────────────────
        // Case §2.1.4: full payment history for the A/R report.
        public List<PaymentAR> GetAllPayments()
        {
            var list = new List<PaymentAR>();
            try
            {
                string sql = "SELECT p.PaymentARId, p.CustomerBillId, p.AmountReceived, " +
                             "p.DateReceived, p.PaymentMethod, p.ReferenceNumber, " +
                             "cb.TotalAmountDue, cb.BalanceRemaining, cb.Status, " +
                             "c.CustomerName " +
                             "FROM PaymentsAR p " +
                             "INNER JOIN CustomerBills cb ON cb.CustomerBillId = p.CustomerBillId " +
                             "INNER JOIN Customers c     ON c.CustomerId       = cb.CustomerId " +
                             "ORDER BY p.DateReceived DESC;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToPaymentAR(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllPayments error: " + ex.Message);
            }
            return list;
        }

        // ── Private helper — maps a DataRow to a PaymentAR model ─────────────
        private PaymentAR MapRowToPaymentAR(System.Data.DataRow row)
        {
            return new PaymentAR
            {
                PaymentARId = Convert.ToInt32(row["PaymentARId"]),
                CustomerBillId = Convert.ToInt32(row["CustomerBillId"]),
                AmountReceived = Convert.ToDecimal(row["AmountReceived"]),
                DateReceived = Convert.ToDateTime(row["DateReceived"]),
                PaymentMethod = row["PaymentMethod"] == DBNull.Value ? string.Empty : row["PaymentMethod"].ToString(),
                ReferenceNumber = row["ReferenceNumber"] == DBNull.Value ? string.Empty : row["ReferenceNumber"].ToString()
            };
        }
    }
}