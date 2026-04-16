using FIS.Database;
using FIS.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace FIS.Services
{
    public class CustomerBillService
    {
        // ── Generate a bill for a shipped order ───────────────────────────────
        // Case §2.1.3: "customers are billed when their order is shipped."
        // "The A/R file should be read by the FIS to generate bills to be sent
        // to the customer."
        //
        // Called by the auto-billing process after reading shipped unbilled orders
        // from CustomerOrderService.GetShippedUnbilledOrders().
        // DueDate defaults to 30 days from BillDate — standard payment term.
        public bool GenerateBill(CustomerOrder order)
        {
            try
            {
                string sql = "INSERT INTO CustomerBills " +
                             "(CustomerOrderId, CustomerId, BillDate, DueDate, " +
                             "TotalAmountDue, BalanceRemaining, Status) " +
                             "VALUES (@CustomerOrderId, @CustomerId, @BillDate, @DueDate, " +
                             "@TotalAmountDue, @BalanceRemaining, 'Unpaid');";

                DateTime billDate = DateTime.Today;

                int rows = DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@CustomerOrderId", order.CustomerOrderId),
                    new MySqlParameter("@CustomerId", order.CustomerId),
                    new MySqlParameter("@BillDate", billDate),
                    new MySqlParameter("@DueDate", billDate.AddDays(30)),
                    new MySqlParameter("@TotalAmountDue", order.TotalAmount),
                    new MySqlParameter("@BalanceRemaining", order.TotalAmount));

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GenerateBill error: " + ex.Message);
                return false;
            }
        }

        // ── Auto-bill: generate bills for all shipped unbilled orders ─────────
        // Case §2.1.3: "salespeople can focus on selling and let the computer
        // system do the work of sending out bills."
        // Reads all shipped orders not yet billed and generates a CustomerBill
        // for each one. Returns the count of bills successfully generated.
        public int GeneratePendingBills()
        {
            int generatedCount = 0;
            try
            {
                string sql = "SELECT co.CustomerOrderId, co.CustomerId, " +
                             "co.ProductDescription, co.Quantity, co.TotalAmount, " +
                             "co.OrderDate, co.ShipDate, co.Status, co.CreatedAt " +
                             "FROM CustomerOrders co " +
                             "WHERE co.Status = 'Shipped' " +
                             "AND co.CustomerOrderId NOT IN " +
                             "    (SELECT CustomerOrderId FROM CustomerBills);";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    var order = new CustomerOrder
                    {
                        CustomerOrderId = Convert.ToInt32(row["CustomerOrderId"]),
                        CustomerId = Convert.ToInt32(row["CustomerId"]),
                        ProductDescription = row["ProductDescription"] == DBNull.Value ? string.Empty : row["ProductDescription"].ToString(),
                        Quantity = Convert.ToInt32(row["Quantity"]),
                        TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                        OrderDate = Convert.ToDateTime(row["OrderDate"]),
                        ShipDate = row["ShipDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["ShipDate"]),
                        Status = row["Status"].ToString()
                    };

                    if (GenerateBill(order))
                        generatedCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GeneratePendingBills error: " + ex.Message);
            }
            return generatedCount;
        }

        // ── Get a single bill by ID ───────────────────────────────────────────
        public CustomerBill GetById(int customerBillId)
        {
            try
            {
                string sql = "SELECT cb.CustomerBillId, cb.CustomerOrderId, cb.CustomerId, " +
                             "cb.BillDate, cb.DueDate, cb.TotalAmountDue, " +
                             "cb.BalanceRemaining, cb.Status, " +
                             "c.CustomerName, c.BillingAddress, c.Email, c.Phone " +
                             "FROM CustomerBills cb " +
                             "INNER JOIN Customers c ON c.CustomerId = cb.CustomerId " +
                             "WHERE cb.CustomerBillId = @CustomerBillId;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@CustomerBillId", customerBillId));

                if (dt.Rows.Count == 0) return null;
                return MapRowToCustomerBill(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetById error: " + ex.Message);
                return null;
            }
        }

        // ── Get all bills ─────────────────────────────────────────────────────
        // Case §2.1.4: "a report on accounts receivable should also be created"
        // Full bill list is the basis for Veronica's A/R report.
        public List<CustomerBill> GetAllBills()
        {
            var list = new List<CustomerBill>();
            try
            {
                string sql = "SELECT cb.CustomerBillId, cb.CustomerOrderId, cb.CustomerId, " +
                             "cb.BillDate, cb.DueDate, cb.TotalAmountDue, " +
                             "cb.BalanceRemaining, cb.Status, " +
                             "c.CustomerName, c.BillingAddress, c.Email, c.Phone " +
                             "FROM CustomerBills cb " +
                             "INNER JOIN Customers c ON c.CustomerId = cb.CustomerId " +
                             "ORDER BY cb.DueDate ASC;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToCustomerBill(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllBills error: " + ex.Message);
            }
            return list;
        }

        // ── Get all unpaid and partially paid bills ───────────────────────────
        // Case §2.1.3: FIS must track outstanding balances.
        // Returns all bills that still have a BalanceRemaining > 0.
        public List<CustomerBill> GetOutstandingBills()
        {
            var list = new List<CustomerBill>();
            try
            {
                string sql = "SELECT cb.CustomerBillId, cb.CustomerOrderId, cb.CustomerId, " +
                             "cb.BillDate, cb.DueDate, cb.TotalAmountDue, " +
                             "cb.BalanceRemaining, cb.Status, " +
                             "c.CustomerName, c.BillingAddress, c.Email, c.Phone " +
                             "FROM CustomerBills cb " +
                             "INNER JOIN Customers c ON c.CustomerId = cb.CustomerId " +
                             "WHERE cb.Status IN ('Unpaid', 'PartiallyPaid', 'Overdue') " +
                             "ORDER BY cb.DueDate ASC;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToCustomerBill(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetOutstandingBills error: " + ex.Message);
            }
            return list;
        }

        // ── Get all bills for a specific customer ─────────────────────────────
        // Case §2.1.3: A/R report needs full billing and payment history
        // per customer to track outstanding balances.
        public List<CustomerBill> GetBillsByCustomer(int customerId)
        {
            var list = new List<CustomerBill>();
            try
            {
                string sql = "SELECT cb.CustomerBillId, cb.CustomerOrderId, cb.CustomerId, " +
                             "cb.BillDate, cb.DueDate, cb.TotalAmountDue, " +
                             "cb.BalanceRemaining, cb.Status, " +
                             "c.CustomerName, c.BillingAddress, c.Email, c.Phone " +
                             "FROM CustomerBills cb " +
                             "INNER JOIN Customers c ON c.CustomerId = cb.CustomerId " +
                             "WHERE cb.CustomerId = @CustomerId " +
                             "ORDER BY cb.BillDate DESC;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@CustomerId", customerId));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToCustomerBill(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetBillsByCustomer error: " + ex.Message);
            }
            return list;
        }

        // ── Apply a payment to a bill ─────────────────────────────────────────
        // Case §2.1.3: "when payments are received from customers, the FIS should
        // check the payment against the A/R file and record the amount paid."
        // Case §2.1.3: "customers may submit a partial payment and this partial
        // payment should be recorded with the date it is received."
        //
        // Called by PaymentARService after recording a PaymentAR.
        // Reduces BalanceRemaining by the amount received and updates Status:
        //   BalanceRemaining = 0          → "Paid"
        //   BalanceRemaining > 0          → "PartiallyPaid"
        public bool ApplyPayment(int customerBillId, decimal amountReceived)
        {
            try
            {
                // Step 1 — Get current balance
                CustomerBill bill = GetById(customerBillId);
                if (bill == null) return false;

                decimal newBalance = bill.BalanceRemaining - amountReceived;
                if (newBalance < 0) newBalance = 0;

                string newStatus = newBalance == 0 ? "Paid" : "PartiallyPaid";

                // Step 2 — Update bill with new balance and status
                string sql = "UPDATE CustomerBills SET " +
                             "BalanceRemaining = @BalanceRemaining, " +
                             "Status           = @Status " +
                             "WHERE CustomerBillId = @CustomerBillId;";

                int rows = DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@BalanceRemaining", newBalance),
                    new MySqlParameter("@Status", newStatus),
                    new MySqlParameter("@CustomerBillId", customerBillId));

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ApplyPayment error: " + ex.Message);
                return false;
            }
        }

        // ── Mark overdue bills ────────────────────────────────────────────────
        // Case §2.1.3: FIS must track unpaid bills that have passed their DueDate.
        // Run daily to flag bills where DueDate has passed and balance is still
        // outstanding — surfaces them clearly on the A/R report.
        public int MarkOverdueBills()
        {
            try
            {
                string sql = "UPDATE CustomerBills SET Status = 'Overdue' " +
                             "WHERE Status IN ('Unpaid', 'PartiallyPaid') " +
                             "AND DueDate < @Today;";

                return DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@Today", DateTime.Today));
            }
            catch (Exception ex)
            {
                Console.WriteLine("MarkOverdueBills error: " + ex.Message);
                return 0;
            }
        }

        // ── A/R report: all bills with outstanding balances ───────────────────
        // Case §2.1.4: "a report on accounts receivable should also be created"
        // Returns all bills grouped with their remaining balance and status —
        // gives Veronica a full picture of what customers owe.
        public List<CustomerBill> GetAccountsReceivableReport()
        {
            var list = new List<CustomerBill>();
            try
            {
                string sql = "SELECT cb.CustomerBillId, cb.CustomerOrderId, cb.CustomerId, " +
                             "cb.BillDate, cb.DueDate, cb.TotalAmountDue, " +
                             "cb.BalanceRemaining, cb.Status, " +
                             "c.CustomerName, c.BillingAddress, c.Email, c.Phone " +
                             "FROM CustomerBills cb " +
                             "INNER JOIN Customers c ON c.CustomerId = cb.CustomerId " +
                             "ORDER BY cb.Status ASC, cb.DueDate ASC;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToCustomerBill(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAccountsReceivableReport error: " + ex.Message);
            }
            return list;
        }

        /// <summary>
        /// Returns the number of customer bills with Status = 'Overdue'.
        /// Used by the dashboard KPI card.
        /// </summary>
        public int GetOverdueCount()
        {
            string sql = "SELECT COUNT(*) FROM customer_bill WHERE Status = 'Overdue';";
            object result = FIS.Database.DBHelper.ExecuteScalar(sql);
            return result == null || result == System.DBNull.Value
                ? 0
                : System.Convert.ToInt32(result);
        }

        /// <summary>
        /// Returns the sum of BalanceRemaining across all unpaid, partially-paid,
        /// and overdue bills. This is the total the company is still owed.
        /// Used by the dashboard KPI card.
        /// </summary>
        public decimal GetOutstandingTotal()
        {
            string sql = @"SELECT COALESCE(SUM(BalanceRemaining), 0)
                   FROM customer_bill
                   WHERE Status IN ('Unpaid', 'PartiallyPaid', 'Overdue');";
            object result = FIS.Database.DBHelper.ExecuteScalar(sql);
            return result == null || result == System.DBNull.Value
                ? 0m
                : System.Convert.ToDecimal(result);
        }

        // ── Private helper — maps a DataRow to a CustomerBill model ──────────
        private CustomerBill MapRowToCustomerBill(System.Data.DataRow row)
        {
            return new CustomerBill
            {
                CustomerBillId = Convert.ToInt32(row["CustomerBillId"]),
                CustomerOrderId = Convert.ToInt32(row["CustomerOrderId"]),
                CustomerId = Convert.ToInt32(row["CustomerId"]),
                BillDate = Convert.ToDateTime(row["BillDate"]),
                DueDate = Convert.ToDateTime(row["DueDate"]),
                TotalAmountDue = Convert.ToDecimal(row["TotalAmountDue"]),
                BalanceRemaining = Convert.ToDecimal(row["BalanceRemaining"]),
                Status = row["Status"] == DBNull.Value ? "Unpaid" : row["Status"].ToString()
            };
        }
    }
}