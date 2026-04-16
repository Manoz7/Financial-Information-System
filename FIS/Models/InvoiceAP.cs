using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FIS.Models
{
    // Invoice received from a vendor for raw materials delivered.
    // Case requirement: invoices must be recorded when received and auto-paid at DueDate.
    // Must store: DueDate, DatePaid, TotalAmount.
    // Replaces the broken manual process where invoices sat unpaid, causing late fees
    // and damaged vendor relationships.
    public class InvoiceAP
    {
        public int InvoiceAPId { get; set; }

        // FK
        public int VendorId { get; set; }
        public int? PurchaseOrderId { get; set; }   // Nullable: invoice may arrive before PO match

        public string InvoiceNumber { get; set; }   // Vendor's own reference number
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }

        // Explicitly required by case: store date it was due, date paid, total paid
        public decimal AmountPaid { get; set; } = 0;
        public DateTime? DatePaid { get; set; }

        // "Unpaid" | "Paid" | "Overdue"
        public string Status { get; set; } = "Unpaid";

        public DateTime ReceivedAt { get; set; } = DateTime.Now;

        // Navigation
        public Vendor Vendor { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; }
        public PaymentAP Payment { get; set; }
    }
}

