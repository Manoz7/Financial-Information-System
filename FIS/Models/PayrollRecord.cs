using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FIS.Models
{
    // The payroll file prepared by HRS and consumed by FIS.
    // HRS calculates pay from emp_sched hours + benefits/insurance data and writes it here.
    // FIS reads this record and issues the actual payment (check or direct deposit).
    //
    // Case: "The HR department will prepare a payroll file. The FIS is responsible
    // for accessing the payroll file and sending salary amounts to employees."
    public class PayrollRecord
    {
        public int PayrollRecordId { get; set; }

        // FK
        public int EmployeeId { get; set; }

        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }

        // Computed by HRS; read by FIS
        public decimal HoursWorked { get; set; }
        public decimal GrossPay { get; set; }
        public decimal Deductions { get; set; }     // Benefits + insurance (from HRS)
        public decimal NetPay { get; set; }          // Amount FIS must disburse

        // "Pending" | "Processed" | "Failed"
        public string Status { get; set; } = "Pending";

        // Set by FIS when payment is sent
        public DateTime? ProcessedAt { get; set; }
        public string PaymentMethod { get; set; }    // "Check" or "DirectDeposit" at time of payment
        public string ConfirmationReference { get; set; }  // Check number or bank ref

        // Navigation
        public Employee Employee { get; set; }
    }
}
