using FIS.Database;
using FIS.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace FIS.Services
{
    public class CustomerOrderService
    {
        // ── Get a single order by ID ──────────────────────────────────────────
        public CustomerOrder GetById(int customerOrderId)
        {
            try
            {
                string sql = "SELECT co.CustomerOrderId, co.CustomerId, " +
                             "co.ProductDescription, co.Quantity, co.TotalAmount, " +
                             "co.OrderDate, co.ShipDate, co.Status, co.CreatedAt, " +
                             "c.CustomerName, c.BillingAddress, c.Email, c.Phone " +
                             "FROM CustomerOrders co " +
                             "INNER JOIN Customers c ON c.CustomerId = co.CustomerId " +
                             "WHERE co.CustomerOrderId = @CustomerOrderId;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@CustomerOrderId", customerOrderId));

                if (dt.Rows.Count == 0) return null;
                return MapRowToCustomerOrder(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetById error: " + ex.Message);
                return null;
            }
        }

        // ── Get all orders ────────────────────────────────────────────────────
        // Case §2.1.3: FIS reads the A/R file (order file) to monitor order
        // status. Full order list used for the A/R report.
        public List<CustomerOrder> GetAllOrders()
        {
            var list = new List<CustomerOrder>();
            try
            {
                string sql = "SELECT co.CustomerOrderId, co.CustomerId, " +
                             "co.ProductDescription, co.Quantity, co.TotalAmount, " +
                             "co.OrderDate, co.ShipDate, co.Status, co.CreatedAt, " +
                             "c.CustomerName, c.BillingAddress, c.Email, c.Phone " +
                             "FROM CustomerOrders co " +
                             "INNER JOIN Customers c ON c.CustomerId = co.CustomerId " +
                             "ORDER BY co.OrderDate DESC;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToCustomerOrder(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllOrders error: " + ex.Message);
            }
            return list;
        }

        // ── Get all shipped orders not yet billed ─────────────────────────────
        // Case §2.1.3: "the A/R file should be read by the FIS to generate bills
        // to be sent to the customer." Customers are billed only when their order
        // has been shipped. FIS watches for Status = "Shipped" orders that do not
        // yet have a CustomerBill — these are the ones needing a bill generated.
        public List<CustomerOrder> GetShippedUnbilledOrders()
        {
            var list = new List<CustomerOrder>();
            try
            {
                string sql = "SELECT co.CustomerOrderId, co.CustomerId, " +
                             "co.ProductDescription, co.Quantity, co.TotalAmount, " +
                             "co.OrderDate, co.ShipDate, co.Status, co.CreatedAt, " +
                             "c.CustomerName, c.BillingAddress, c.Email, c.Phone " +
                             "FROM CustomerOrders co " +
                             "INNER JOIN Customers c ON c.CustomerId = co.CustomerId " +
                             "WHERE co.Status = 'Shipped' " +
                             "AND co.CustomerOrderId NOT IN " +
                             "    (SELECT CustomerOrderId FROM CustomerBills) " +
                             "ORDER BY co.ShipDate ASC;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToCustomerOrder(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetShippedUnbilledOrders error: " + ex.Message);
            }
            return list;
        }

        // ── Get all open orders ───────────────────────────────────────────────
        // Case §2.1.3: "billing is tied to whether an order is Open or Shipped"
        // Open orders are in production or awaiting shipment — not yet billable.
        public List<CustomerOrder> GetOpenOrders()
        {
            var list = new List<CustomerOrder>();
            try
            {
                string sql = "SELECT co.CustomerOrderId, co.CustomerId, " +
                             "co.ProductDescription, co.Quantity, co.TotalAmount, " +
                             "co.OrderDate, co.ShipDate, co.Status, co.CreatedAt, " +
                             "c.CustomerName, c.BillingAddress, c.Email, c.Phone " +
                             "FROM CustomerOrders co " +
                             "INNER JOIN Customers c ON c.CustomerId = co.CustomerId " +
                             "WHERE co.Status = 'Open' " +
                             "ORDER BY co.OrderDate ASC;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToCustomerOrder(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetOpenOrders error: " + ex.Message);
            }
            return list;
        }

        // ── Get all orders for a specific customer ────────────────────────────
        // Case §2.1.3: A/R report needs full order and billing history per
        // customer to track outstanding balances.
        public List<CustomerOrder> GetOrdersByCustomer(int customerId)
        {
            var list = new List<CustomerOrder>();
            try
            {
                string sql = "SELECT co.CustomerOrderId, co.CustomerId, " +
                             "co.ProductDescription, co.Quantity, co.TotalAmount, " +
                             "co.OrderDate, co.ShipDate, co.Status, co.CreatedAt, " +
                             "c.CustomerName, c.BillingAddress, c.Email, c.Phone " +
                             "FROM CustomerOrders co " +
                             "INNER JOIN Customers c ON c.CustomerId = co.CustomerId " +
                             "WHERE co.CustomerId = @CustomerId " +
                             "ORDER BY co.OrderDate DESC;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@CustomerId", customerId));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToCustomerOrder(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetOrdersByCustomer error: " + ex.Message);
            }
            return list;
        }

        // ── Get orders by status ──────────────────────────────────────────────
        // General-purpose status filter used by reports and the A/R dashboard.
        // Valid status values: "Open" | "Shipped" | "Closed" | "Cancelled"
        public List<CustomerOrder> GetOrdersByStatus(string status)
        {
            var list = new List<CustomerOrder>();
            try
            {
                string sql = "SELECT co.CustomerOrderId, co.CustomerId, " +
                             "co.ProductDescription, co.Quantity, co.TotalAmount, " +
                             "co.OrderDate, co.ShipDate, co.Status, co.CreatedAt, " +
                             "c.CustomerName, c.BillingAddress, c.Email, c.Phone " +
                             "FROM CustomerOrders co " +
                             "INNER JOIN Customers c ON c.CustomerId = co.CustomerId " +
                             "WHERE co.Status = @Status " +
                             "ORDER BY co.OrderDate DESC;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@Status", status));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToCustomerOrder(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetOrdersByStatus error: " + ex.Message);
            }
            return list;
        }

        // ── Check whether an order has already been billed ────────────────────
        // Guard used by CustomerBillService before generating a new bill.
        // Prevents duplicate bills being issued for the same shipped order.
        public bool OrderAlreadyBilled(int customerOrderId)
        {
            try
            {
                string sql = "SELECT COUNT(*) FROM CustomerBills " +
                             "WHERE CustomerOrderId = @CustomerOrderId;";

                int count = Convert.ToInt32(DBHelper.ExecuteScalar(sql,
                    new MySqlParameter("@CustomerOrderId", customerOrderId)));

                return count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("OrderAlreadyBilled error: " + ex.Message);
                return false;
            }
        }

        // ── Private helper — maps a DataRow to a CustomerOrder model ─────────
        // CustomerName, BillingAddress, Email, Phone are denormalised display
        // fields from the Customers JOIN — not stored on the order table.
        private CustomerOrder MapRowToCustomerOrder(System.Data.DataRow row)
        {
            return new CustomerOrder
            {
                CustomerOrderId = Convert.ToInt32(row["CustomerOrderId"]),
                CustomerId = Convert.ToInt32(row["CustomerId"]),
                ProductDescription = row["ProductDescription"] == DBNull.Value ? string.Empty : row["ProductDescription"].ToString(),
                Quantity = Convert.ToInt32(row["Quantity"]),
                TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                OrderDate = Convert.ToDateTime(row["OrderDate"]),
                ShipDate = row["ShipDate"] == DBNull.Value
                                         ? (DateTime?)null
                                         : Convert.ToDateTime(row["ShipDate"]),
                Status = row["Status"] == DBNull.Value ? "Open" : row["Status"].ToString(),
                CreatedAt = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"])
            };
        }
    }
}