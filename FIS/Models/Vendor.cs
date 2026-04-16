using System;
using System.Collections.Generic;

namespace FIS.Models
{
    // Suppliers of raw materials to WBS.
    // Vendor relationships are managed by Financial Management (Veronica Wright).
    // One vendor stopped working with WBS due to late payments — RelationshipStatus tracks this.
    public class Vendor
    {
        public int VendorId { get; set; }
        public string VendorName { get; set; }
        public string ContactName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }

        // "Active" | "Suspended" | "Terminated"
        public string RelationshipStatus { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public List<RawMaterial> RawMaterials { get; set; } = new List<RawMaterial>();
        public List<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
        public List<InvoiceAP> Invoices { get; set; } = new List<InvoiceAP>();
    }
}