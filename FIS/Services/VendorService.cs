using FIS.Database;
using FIS.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace FIS.Services
{
    public class VendorService
    {
        // ── Get all vendors ───────────────────────────────────────────────────
        // Case §2.1.4: "a report that lists all vendors used by the company"
        public List<Vendor> GetAllVendors()
        {
            var vendors = new List<Vendor>();
            try
            {
                string sql = "SELECT VendorId, VendorName, ContactName, Phone, " +
                             "Email, Address, RelationshipStatus, CreatedAt " +
                             "FROM Vendors ORDER BY VendorName;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    vendors.Add(MapRowToVendor(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllVendors error: " + ex.Message);
            }
            return vendors;
        }

        // ── Get single vendor by ID ───────────────────────────────────────────
        public Vendor GetVendorById(int vendorId)
        {
            try
            {
                string sql = "SELECT VendorId, VendorName, ContactName, Phone, " +
                             "Email, Address, RelationshipStatus, CreatedAt " +
                             "FROM Vendors WHERE VendorId = @VendorId;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@VendorId", vendorId));

                if (dt.Rows.Count == 0) return null;
                return MapRowToVendor(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetVendorById error: " + ex.Message);
                return null;
            }
        }

        // ── Add a new vendor ──────────────────────────────────────────────────
        // Case §2.1.1: "system must contain information on all vendors currently
        // being used and the prices for the raw materials bought from vendors"
        public bool AddVendor(Vendor vendor)
        {
            try
            {
                string sql = "INSERT INTO Vendors " +
                             "(VendorName, ContactName, Phone, Email, Address, RelationshipStatus) " +
                             "VALUES (@VendorName, @ContactName, @Phone, @Email, @Address, @Status);";

                int rows = DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@VendorName", vendor.VendorName),
                    new MySqlParameter("@ContactName", vendor.ContactName),
                    new MySqlParameter("@Phone", vendor.Phone),
                    new MySqlParameter("@Email", vendor.Email),
                    new MySqlParameter("@Address", vendor.Address),
                    new MySqlParameter("@Status", vendor.RelationshipStatus));

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("AddVendor error: " + ex.Message);
                return false;
            }
        }

        // ── Update vendor details ─────────────────────────────────────────────
        public bool UpdateVendor(Vendor vendor)
        {
            try
            {
                string sql = "UPDATE Vendors SET " +
                             "VendorName         = @VendorName, " +
                             "ContactName        = @ContactName, " +
                             "Phone              = @Phone, " +
                             "Email              = @Email, " +
                             "Address            = @Address, " +
                             "RelationshipStatus = @Status " +
                             "WHERE VendorId = @VendorId;";

                int rows = DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@VendorName", vendor.VendorName),
                    new MySqlParameter("@ContactName", vendor.ContactName),
                    new MySqlParameter("@Phone", vendor.Phone),
                    new MySqlParameter("@Email", vendor.Email),
                    new MySqlParameter("@Address", vendor.Address),
                    new MySqlParameter("@Status", vendor.RelationshipStatus),
                    new MySqlParameter("@VendorId", vendor.VendorId));

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("UpdateVendor error: " + ex.Message);
                return false;
            }
        }

        // ── Get best-price vendor for a raw material ──────────────────────────
        // Case §2.1.1: "WBS is not certain if they are purchasing raw materials
        // at the best price" — auto-reorder selects the active vendor offering
        // the lowest unit price for the given material.
        //
        // RawMaterials holds VendorId and UnitPrice directly (one preferred vendor
        // per material). The join goes through RawMaterials — there is no bridge
        // table in this schema.
        public Vendor GetBestPriceVendor(int rawMaterialId)
        {
            try
            {
                string sql = "SELECT v.VendorId, v.VendorName, v.ContactName, v.Phone, " +
                             "v.Email, v.Address, v.RelationshipStatus, v.CreatedAt " +
                             "FROM Vendors v " +
                             "INNER JOIN RawMaterials r ON r.VendorId = v.VendorId " +
                             "WHERE r.RawMaterialId = @RawMaterialId " +
                             "AND v.RelationshipStatus = 'Active' " +
                             "ORDER BY r.UnitPrice ASC " +
                             "LIMIT 1;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@RawMaterialId", rawMaterialId));

                if (dt.Rows.Count == 0) return null;
                return MapRowToVendor(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetBestPriceVendor error: " + ex.Message);
                return null;
            }
        }

        // ── Get only active vendors ───────────────────────────────────────────
        // Case §2.1.1: "at least one vendor will no longer work with WBS" —
        // terminated vendors must never appear on new purchase orders.
        public List<Vendor> GetActiveVendors()
        {
            var vendors = new List<Vendor>();
            try
            {
                string sql = "SELECT VendorId, VendorName, ContactName, Phone, " +
                             "Email, Address, RelationshipStatus, CreatedAt " +
                             "FROM Vendors WHERE RelationshipStatus = 'Active' " +
                             "ORDER BY VendorName;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    vendors.Add(MapRowToVendor(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetActiveVendors error: " + ex.Message);
            }
            return vendors;
        }

        // ── Update vendor relationship status ─────────────────────────────────
        // Case §2.1.1: vendor relations must be tracked and maintained.
        // Status values: 'Active' | 'Suspended' | 'Terminated'
        // Vendors are never hard-deleted — a status change is the only way to
        // remove a vendor from active use, preserving historical invoice records.
        public bool UpdateVendorStatus(int vendorId, string status)
        {
            try
            {
                string sql = "UPDATE Vendors SET RelationshipStatus = @Status " +
                             "WHERE VendorId = @VendorId;";

                int rows = DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@Status", status),
                    new MySqlParameter("@VendorId", vendorId));

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("UpdateVendorStatus error: " + ex.Message);
                return false;
            }
        }

        // ── Private helper — maps a DataRow to a Vendor model ─────────────────
        private Vendor MapRowToVendor(System.Data.DataRow row)
        {
            return new Vendor
            {
                VendorId = Convert.ToInt32(row["VendorId"]),
                VendorName = row["VendorName"] == DBNull.Value ? string.Empty : row["VendorName"].ToString(),
                ContactName = row["ContactName"] == DBNull.Value ? string.Empty : row["ContactName"].ToString(),
                Phone = row["Phone"] == DBNull.Value ? string.Empty : row["Phone"].ToString(),
                Email = row["Email"] == DBNull.Value ? string.Empty : row["Email"].ToString(),
                Address = row["Address"] == DBNull.Value ? string.Empty : row["Address"].ToString(),
                RelationshipStatus = row["RelationshipStatus"] == DBNull.Value ? "Active" : row["RelationshipStatus"].ToString(),
                CreatedAt = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"])
            };
        }
    }
}
