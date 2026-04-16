using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FIS.Models
{
    // Customer order tracked by POPS. FIS reads this file to watch for Status = "Shipped".
    // When an order is shipped, FIS auto-generates a CustomerBill.
    // FIS does NOT create orders — the sales team does that through POPS.
    // Billing is tied to order status: only "Shipped" orders get billed.
    public class CustomerOrder
    {
        public int CustomerOrderId { get; set; }

        // FK
        public int CustomerId { get; set; }

        public string ProductDescription { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }

        public DateTime OrderDate { get; set; }
        public DateTime? ShipDate { get; set; }     // Set by POPS warehouse; triggers FIS billing

        // "Open" | "Shipped" | "Closed" | "Cancelled"
        public string Status { get; set; } = "Open";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public Customer Customer { get; set; }
        public List<CustomerBill> Bills { get; set; } = new List<CustomerBill>();
    }
}
