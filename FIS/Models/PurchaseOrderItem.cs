using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FIS.Models
{
    // Line-item detail of a PurchaseOrder — which material, how many, at what price.
    // Supports ordering multiple raw materials in one PO.
    public class PurchaseOrderItem
    {
        public int PurchaseOrderItemId { get; set; }

        // FK
        public int PurchaseOrderId { get; set; }
        public int RawMaterialId { get; set; }

        public decimal QuantityOrdered { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }   // QuantityOrdered * UnitPrice

        // Navigation
        public PurchaseOrder PurchaseOrder { get; set; }
        public RawMaterial RawMaterial { get; set; }
    }
}

