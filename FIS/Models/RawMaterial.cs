using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FIS.Models
{
    // Raw materials used in production (nails, screws, brackets, wood, etc.).
    // FIS monitors QuantityOnHand (updated by POPS) against ReorderThreshold.
    // When quantity falls below threshold, FIS auto-generates a PurchaseOrder.
    public class RawMaterial
    {
        public int RawMaterialId { get; set; }
        public string MaterialName { get; set; }
        public string UnitOfMeasure { get; set; }   // e.g. "each", "box", "kg"

        // Written by POPS; read by FIS to check reorder need
        public decimal QuantityOnHand { get; set; }

        // Auto-reorder fires when QuantityOnHand < ReorderThreshold
        public decimal ReorderThreshold { get; set; }
        public decimal ReorderQuantity { get; set; }

        // FK — preferred vendor for this material
        public int VendorId { get; set; }
        public decimal UnitPrice { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        public Vendor Vendor { get; set; }
        public List<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    }
}
