using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FIS.Models
{
    // Customer of WBS. Created and maintained by POPS (sales team).
    // FIS reads customer data to send bills and record incoming payments.
    // FIS does NOT create customer records — it consumes them from POPS.
    public class Customer
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string BillingAddress { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public List<CustomerOrder> Orders { get; set; } = new List<CustomerOrder>();
        public List<CustomerBill> Bills { get; set; } = new List<CustomerBill>();
    }
}
