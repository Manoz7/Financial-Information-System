using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FIS.Models
{
    // A user of the FIS application (Veronica and her Financial Management team).
    // Supports login and access control for the FIS.
    // Not explicitly detailed in the case study but required for any operational system.
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }

        // "Admin" | "Manager" | "Clerk" | "Employee"
        // "Employee" role has restricted access — payment preference page only
        public string Role { get; set; } = "Clerk";

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLoginAt { get; set; }

        // FK — nullable because FIS staff users are not employees
        // Populated only when Role = "Employee"
        public int? EmployeeId { get; set; }

        // Navigation
        public Employee Employee { get; set; }
    }
}
