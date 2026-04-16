using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FIS.Models
{
    // Payment made by FIS to a vendor against an InvoiceAP.
    // Auto-generated at invoice DueDate to ensure on-time payment.
    // Veronica's monthly "accounts paid" report is driven by this entity.
    public class PaymentAP
    {
        public int PaymentAPId { get; set; }

        // FK
        public int InvoiceAPId { get; set; }

        public decimal AmountPaid { get; set; }
        public DateTime DatePaid { get; set; } = DateTime.Now;

        // "Check" | "ElectronicTransfer" | "ACH"
        public string PaymentMethod { get; set; }
        public string ReferenceNumber { get; set; }   // Check number or bank confirmation

        // Navigation
        public InvoiceAP Invoice { get; set; }
    }
}
