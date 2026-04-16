using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FIS.Models
{
    // Employee record as needed by FIS — payment info only.
    // Full employee data (scheduling, training, hiring) is owned by HRS.
    // FIS holds ONLY what it needs to execute payroll disbursement.
    // Case: "employees who would like to change to automatic deposit should have
    // the means to do this through an online method."
    public class Employee
    {
        // Shared PK with HRS — same ID used across both systems
        public int EmployeeId { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }

        // "Check" | "DirectDeposit"
        // Employees can update this online (FIS requirement)
        public string PaymentMethod { get; set; } = "Check";

        // Only populated when PaymentMethod = "DirectDeposit"
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string RoutingNumber { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        public List<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();
    }
}
