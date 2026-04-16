using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FIS.Models
{
    // A payment received from a customer against a CustomerBill.
    // Multiple PaymentAR records can exist per bill (partial payment support).
    // Each payment reduces CustomerBill.BalanceRemaining.
    // Case: "check the payment against the A/R file and record the amount paid."
    public class PaymentAR
    {
        public int PaymentARId { get; set; }

        // FK
        public int CustomerBillId { get; set; }

        public decimal AmountReceived { get; set; }

        // Case explicit: partial payment must be recorded WITH the date received
        public DateTime DateReceived { get; set; } = DateTime.Now;

        // "Check" | "ElectronicTransfer" | "CreditCard"
        public string PaymentMethod { get; set; }
        public string ReferenceNumber { get; set; }

        // Navigation
        public CustomerBill CustomerBill { get; set; }
    }
}
