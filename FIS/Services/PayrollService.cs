using FIS.Database;
using FIS.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace FIS.Services
{
    public class PayrollService
    {
        // ── Get all pending payroll records ───────────────────────────────────
        // Case §2.1.2: "the FIS is responsible for accessing the payroll file
        // and sending salary amounts to employees."
        // HRS writes records to the payroll file with Status = "Pending".
        // FIS reads only Pending records — already processed ones are skipped.
        public List<PayrollRecord> GetPendingPayrollRecords()
        {
            var list = new List<PayrollRecord>();
            try
            {
                string sql = "SELECT pr.PayrollRecordId, pr.EmployeeId, " +
                             "pr.PayPeriodStart, pr.PayPeriodEnd, pr.HoursWorked, " +
                             "pr.GrossPay, pr.Deductions, pr.NetPay, pr.Status, " +
                             "pr.ProcessedAt, pr.PaymentMethod, pr.ConfirmationReference, " +
                             "e.FullName, e.PaymentMethod AS EmployeePaymentMethod " +
                             "FROM PayrollRecords pr " +
                             "INNER JOIN Employees e ON e.EmployeeId = pr.EmployeeId " +
                             "WHERE pr.Status = 'Pending' " +
                             "ORDER BY pr.PayPeriodEnd ASC;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToPayrollRecord(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetPendingPayrollRecords error: " + ex.Message);
            }
            return list;
        }

        // ── Get a single payroll record by ID ─────────────────────────────────
        public PayrollRecord GetById(int payrollRecordId)
        {
            try
            {
                string sql = "SELECT pr.PayrollRecordId, pr.EmployeeId, " +
                             "pr.PayPeriodStart, pr.PayPeriodEnd, pr.HoursWorked, " +
                             "pr.GrossPay, pr.Deductions, pr.NetPay, pr.Status, " +
                             "pr.ProcessedAt, pr.PaymentMethod, pr.ConfirmationReference, " +
                             "e.FullName, e.PaymentMethod AS EmployeePaymentMethod " +
                             "FROM PayrollRecords pr " +
                             "INNER JOIN Employees e ON e.EmployeeId = pr.EmployeeId " +
                             "WHERE pr.PayrollRecordId = @PayrollRecordId;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@PayrollRecordId", payrollRecordId));

                if (dt.Rows.Count == 0) return null;
                return MapRowToPayrollRecord(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetById error: " + ex.Message);
                return null;
            }
        }

        // ── Get all payroll records for a specific employee ───────────────────
        // Used to show an employee's full pay history and verify disbursements.
        public List<PayrollRecord> GetByEmployee(int employeeId)
        {
            var list = new List<PayrollRecord>();
            try
            {
                string sql = "SELECT pr.PayrollRecordId, pr.EmployeeId, " +
                             "pr.PayPeriodStart, pr.PayPeriodEnd, pr.HoursWorked, " +
                             "pr.GrossPay, pr.Deductions, pr.NetPay, pr.Status, " +
                             "pr.ProcessedAt, pr.PaymentMethod, pr.ConfirmationReference, " +
                             "e.FullName, e.PaymentMethod AS EmployeePaymentMethod " +
                             "FROM PayrollRecords pr " +
                             "INNER JOIN Employees e ON e.EmployeeId = pr.EmployeeId " +
                             "WHERE pr.EmployeeId = @EmployeeId " +
                             "ORDER BY pr.PayPeriodEnd DESC;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@EmployeeId", employeeId));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToPayrollRecord(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetByEmployee error: " + ex.Message);
            }
            return list;
        }

        // ── Get all payroll records for a pay period ──────────────────────────
        // Used to produce the full payroll run report for a given period.
        public List<PayrollRecord> GetByPayPeriod(DateTime periodStart, DateTime periodEnd)
        {
            var list = new List<PayrollRecord>();
            try
            {
                string sql = "SELECT pr.PayrollRecordId, pr.EmployeeId, " +
                             "pr.PayPeriodStart, pr.PayPeriodEnd, pr.HoursWorked, " +
                             "pr.GrossPay, pr.Deductions, pr.NetPay, pr.Status, " +
                             "pr.ProcessedAt, pr.PaymentMethod, pr.ConfirmationReference, " +
                             "e.FullName, e.PaymentMethod AS EmployeePaymentMethod " +
                             "FROM PayrollRecords pr " +
                             "INNER JOIN Employees e ON e.EmployeeId = pr.EmployeeId " +
                             "WHERE pr.PayPeriodStart >= @PeriodStart " +
                             "AND   pr.PayPeriodEnd   <= @PeriodEnd " +
                             "ORDER BY e.FullName ASC;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@PeriodStart", periodStart),
                    new MySqlParameter("@PeriodEnd", periodEnd));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToPayrollRecord(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetByPayPeriod error: " + ex.Message);
            }
            return list;
        }

        // ── Process all pending payroll records ───────────────────────────────
        // Case §2.1.2: "the FIS is responsible for accessing the payroll file
        // and sending salary amounts to employees either directly deposited to
        // their bank or in the form of a check to the employee."
        //
        // For each Pending record this method:
        //   1. Reads the employee's current PaymentMethod (Check or DirectDeposit)
        //   2. Dispatches pay via the appropriate method
        //   3. Updates the PayrollRecord: Status = "Processed", ProcessedAt,
        //      PaymentMethod used, and ConfirmationReference
        //
        // Returns the count of records successfully processed.
        // NOTE: FIS does NOT calculate pay — HRS prepares GrossPay, Deductions,
        // and NetPay. FIS reads and disburses NetPay only.
        public int ProcessPendingPayroll()
        {
            int processedCount = 0;
            try
            {
                List<PayrollRecord> pending = GetPendingPayrollRecords();

                foreach (var record in pending)
                {
                    // Step 1 — Read the employee's current payment preference
                    string empSql = "SELECT PaymentMethod, BankName, AccountNumber, RoutingNumber " +
                                    "FROM Employees WHERE EmployeeId = @EmployeeId;";

                    var empDt = DBHelper.ExecuteQuery(empSql,
                        new MySqlParameter("@EmployeeId", record.EmployeeId));

                    if (empDt.Rows.Count == 0) continue;

                    string paymentMethod = empDt.Rows[0]["PaymentMethod"].ToString();

                    // Step 2 — Dispatch pay based on payment method
                    // "Check" — a physical check is issued to the employee
                    // "DirectDeposit" — amount is sent to employee's bank account
                    // In a real integration this would call a payment gateway or
                    // generate a check file. Here we record the confirmation reference.
                    string confirmationRef = paymentMethod == "DirectDeposit"
                        ? "DD-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper()
                        : "CHK-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

                    // Step 3 — Update the PayrollRecord to Processed
                    string updateSql = "UPDATE PayrollRecords SET " +
                                       "Status                = 'Processed', " +
                                       "ProcessedAt           = @ProcessedAt, " +
                                       "PaymentMethod         = @PaymentMethod, " +
                                       "ConfirmationReference = @ConfirmationRef " +
                                       "WHERE PayrollRecordId = @PayrollRecordId;";

                    int rows = DBHelper.ExecuteNonQuery(updateSql,
                        new MySqlParameter("@ProcessedAt", DateTime.Now),
                        new MySqlParameter("@PaymentMethod", paymentMethod),
                        new MySqlParameter("@ConfirmationRef", confirmationRef),
                        new MySqlParameter("@PayrollRecordId", record.PayrollRecordId));

                    if (rows > 0) processedCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ProcessPendingPayroll error: " + ex.Message);
            }
            return processedCount;
        }

        // ── Mark a payroll record as failed ──────────────────────────────────
        // Used when a direct deposit is rejected (e.g. invalid bank details)
        // or a check cannot be issued. Keeps the record visible for manual
        // follow-up rather than silently leaving it as Pending.
        public bool MarkAsFailed(int payrollRecordId)
        {
            try
            {
                string sql = "UPDATE PayrollRecords SET Status = 'Failed' " +
                             "WHERE PayrollRecordId = @PayrollRecordId " +
                             "AND Status = 'Pending';";

                int rows = DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@PayrollRecordId", payrollRecordId));

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("MarkAsFailed error: " + ex.Message);
                return false;
            }
        }

        // ── Update employee payment method preference ─────────────────────────
        // Case §2.1.2: "employees who currently receive their pay by check and
        // would like to change to automatic deposit should have the means to do
        // this through an online method."
        //
        // When switching to DirectDeposit, bank details are required.
        // When switching back to Check, bank details are cleared.
        public bool UpdatePaymentPreference(int employeeId, string paymentMethod,
                                            string bankName, string accountNumber,
                                            string routingNumber)
        {
            try
            {
                string sql = "UPDATE Employees SET " +
                             "PaymentMethod = @PaymentMethod, " +
                             "BankName      = @BankName, " +
                             "AccountNumber = @AccountNumber, " +
                             "RoutingNumber = @RoutingNumber, " +
                             "UpdatedAt     = @UpdatedAt " +
                             "WHERE EmployeeId = @EmployeeId;";

                int rows = DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@PaymentMethod", paymentMethod),
                    new MySqlParameter("@BankName", string.IsNullOrEmpty(bankName) ? (object)DBNull.Value : bankName),
                    new MySqlParameter("@AccountNumber", string.IsNullOrEmpty(accountNumber) ? (object)DBNull.Value : accountNumber),
                    new MySqlParameter("@RoutingNumber", string.IsNullOrEmpty(routingNumber) ? (object)DBNull.Value : routingNumber),
                    new MySqlParameter("@UpdatedAt", DateTime.Now),
                    new MySqlParameter("@EmployeeId", employeeId));

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("UpdatePaymentPreference error: " + ex.Message);
                return false;
            }
        }

        // ── Get employee payment preference ───────────────────────────────────
        // Used by the online payment preference form to show the employee
        // their current setting before they make a change.
        public Employee GetEmployeePaymentInfo(int employeeId)
        {
            try
            {
                string sql = "SELECT EmployeeId, FullName, Email, PaymentMethod, " +
                             "BankName, AccountNumber, RoutingNumber, UpdatedAt " +
                             "FROM Employees WHERE EmployeeId = @EmployeeId;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@EmployeeId", employeeId));

                if (dt.Rows.Count == 0) return null;
                return MapRowToEmployee(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetEmployeePaymentInfo error: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Returns the number of payroll records with Status = 'Pending'.
        /// This tells Veronica how many employees are waiting to be paid.
        /// Used by the dashboard KPI card.
        /// </summary>
        public int GetPendingCount()
        {
            string sql = "SELECT COUNT(*) FROM payroll_record WHERE Status = 'Pending';";
            object result = FIS.Database.DBHelper.ExecuteScalar(sql);
            return result == null || result == System.DBNull.Value
                ? 0
                : System.Convert.ToInt32(result);
        }

        // ── Private helper — maps a DataRow to a PayrollRecord model ─────────
        private PayrollRecord MapRowToPayrollRecord(System.Data.DataRow row)
        {
            return new PayrollRecord
            {
                PayrollRecordId = Convert.ToInt32(row["PayrollRecordId"]),
                EmployeeId = Convert.ToInt32(row["EmployeeId"]),
                PayPeriodStart = Convert.ToDateTime(row["PayPeriodStart"]),
                PayPeriodEnd = Convert.ToDateTime(row["PayPeriodEnd"]),
                HoursWorked = Convert.ToDecimal(row["HoursWorked"]),
                GrossPay = Convert.ToDecimal(row["GrossPay"]),
                Deductions = Convert.ToDecimal(row["Deductions"]),
                NetPay = Convert.ToDecimal(row["NetPay"]),
                Status = row["Status"] == DBNull.Value ? "Pending" : row["Status"].ToString(),
                ProcessedAt = row["ProcessedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["ProcessedAt"]),
                PaymentMethod = row["PaymentMethod"] == DBNull.Value ? string.Empty : row["PaymentMethod"].ToString(),
                ConfirmationReference = row["ConfirmationReference"] == DBNull.Value ? string.Empty : row["ConfirmationReference"].ToString()
            };
        }

        // ── Private helper — maps a DataRow to an Employee model ─────────────
        // FIS-scoped: only maps fields FIS needs (payment info only).
        // Full employee data is owned by HRS.
        private Employee MapRowToEmployee(System.Data.DataRow row)
        {
            return new Employee
            {
                EmployeeId = Convert.ToInt32(row["EmployeeId"]),
                FullName = row["FullName"] == DBNull.Value ? string.Empty : row["FullName"].ToString(),
                Email = row["Email"] == DBNull.Value ? string.Empty : row["Email"].ToString(),
                PaymentMethod = row["PaymentMethod"] == DBNull.Value ? "Check" : row["PaymentMethod"].ToString(),
                BankName = row["BankName"] == DBNull.Value ? string.Empty : row["BankName"].ToString(),
                AccountNumber = row["AccountNumber"] == DBNull.Value ? string.Empty : row["AccountNumber"].ToString(),
                RoutingNumber = row["RoutingNumber"] == DBNull.Value ? string.Empty : row["RoutingNumber"].ToString(),
                UpdatedAt = row["UpdatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["UpdatedAt"])
            };
        }
    }
}