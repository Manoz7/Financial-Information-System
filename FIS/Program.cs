using FIS.Database;
using FIS.Models;
using FIS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FIS
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // ── TEMPORARY TEST — remove once confirmed working ────────────────

            // Step 1: Test DB connection
            bool connected = DBHelper.TestConnection();
            Console.WriteLine("DB Connected: " + connected);

            if (connected)
            {
                var service = new VendorService();

                // Step 2: Insert a test vendor
                var testVendor = new Vendor
                {
                    VendorName = "Test Supplier Co.",
                    ContactName = "John Smith",
                    Phone = "555-1234",
                    Email = "john@testsupplier.com",
                    Address = "123 Main St",
                    RelationshipStatus = "Active"
                };

                bool added = service.AddVendor(testVendor);
                Console.WriteLine("Vendor added: " + added);

                // Step 3: Read back all vendors
                List<Vendor> vendors = service.GetAllVendors();
                Console.WriteLine("Total vendors in DB: " + vendors.Count);
                foreach (var v in vendors)
                    Console.WriteLine("  -> " + v.VendorName + " | " + v.RelationshipStatus);

                // Step 4: Test best price lookup (RawMaterialId = 1 as example)
                Vendor best = service.GetBestPriceVendor(1);
                Console.WriteLine("Best price vendor: " + (best != null ? best.VendorName : "None found"));
            }
            else
            {
                Console.WriteLine("Connection failed — check App.config and MySQL.");
            }

            Console.WriteLine("\nPress Enter to launch the app...");
            Console.ReadLine();

            // ── END TEST ─────────────────────────────────────────────────────

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}