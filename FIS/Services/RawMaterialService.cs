using FIS.Database;
using FIS.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace FIS.Services
{
    public class RawMaterialService
    {
        // ── Get all raw materials ─────────────────────────────────────────────
        // Used for inventory overview and reorder monitoring dashboard.
        // Case §2.1.1: FIS must know what materials exist and their current levels.
        public List<RawMaterial> GetAllRawMaterials()
        {
            var list = new List<RawMaterial>();
            try
            {
                string sql = "SELECT r.RawMaterialId, r.MaterialName, r.UnitOfMeasure, " +
                             "r.QuantityOnHand, r.ReorderThreshold, r.ReorderQuantity, " +
                             "r.VendorId, r.UnitPrice, r.UpdatedAt, " +
                             "v.VendorName " +
                             "FROM RawMaterials r " +
                             "INNER JOIN Vendors v ON v.VendorId = r.VendorId " +
                             "ORDER BY r.MaterialName;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToRawMaterial(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllRawMaterials error: " + ex.Message);
            }
            return list;
        }

        // ── Get a single raw material by ID ───────────────────────────────────
        public RawMaterial GetById(int rawMaterialId)
        {
            try
            {
                string sql = "SELECT r.RawMaterialId, r.MaterialName, r.UnitOfMeasure, " +
                             "r.QuantityOnHand, r.ReorderThreshold, r.ReorderQuantity, " +
                             "r.VendorId, r.UnitPrice, r.UpdatedAt, " +
                             "v.VendorName " +
                             "FROM RawMaterials r " +
                             "INNER JOIN Vendors v ON v.VendorId = r.VendorId " +
                             "WHERE r.RawMaterialId = @RawMaterialId;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@RawMaterialId", rawMaterialId));

                if (dt.Rows.Count == 0) return null;
                return MapRowToRawMaterial(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetById error: " + ex.Message);
                return null;
            }
        }

        // ── Get all materials that are below reorder threshold ────────────────
        // Case §2.1.1: "raw material items are automatically ordered anytime the
        // stored inventory falls below a certain threshold."
        // This is the core check — called by PurchaseOrderService to know
        // which materials need a PO generated.
        public List<RawMaterial> GetMaterialsBelowThreshold()
        {
            var list = new List<RawMaterial>();
            try
            {
                string sql = "SELECT r.RawMaterialId, r.MaterialName, r.UnitOfMeasure, " +
                             "r.QuantityOnHand, r.ReorderThreshold, r.ReorderQuantity, " +
                             "r.VendorId, r.UnitPrice, r.UpdatedAt, " +
                             "v.VendorName " +
                             "FROM RawMaterials r " +
                             "INNER JOIN Vendors v ON v.VendorId = r.VendorId " +
                             "WHERE r.QuantityOnHand < r.ReorderThreshold " +
                             "ORDER BY r.MaterialName;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToRawMaterial(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetMaterialsBelowThreshold error: " + ex.Message);
            }
            return list;
        }

        // ── Add a new raw material record ─────────────────────────────────────
        // Case §2.1.1: system must track all raw materials (nails, screws,
        // brackets, wood, etc.) used to produce finished products.
        public bool AddRawMaterial(RawMaterial material)
        {
            try
            {
                string sql = "INSERT INTO RawMaterials " +
                             "(MaterialName, UnitOfMeasure, QuantityOnHand, " +
                             "ReorderThreshold, ReorderQuantity, VendorId, UnitPrice) " +
                             "VALUES (@MaterialName, @UnitOfMeasure, @QuantityOnHand, " +
                             "@ReorderThreshold, @ReorderQuantity, @VendorId, @UnitPrice);";

                int rows = DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@MaterialName", material.MaterialName),
                    new MySqlParameter("@UnitOfMeasure", material.UnitOfMeasure),
                    new MySqlParameter("@QuantityOnHand", material.QuantityOnHand),
                    new MySqlParameter("@ReorderThreshold", material.ReorderThreshold),
                    new MySqlParameter("@ReorderQuantity", material.ReorderQuantity),
                    new MySqlParameter("@VendorId", material.VendorId),
                    new MySqlParameter("@UnitPrice", material.UnitPrice));

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("AddRawMaterial error: " + ex.Message);
                return false;
            }
        }

        // ── Update reorder settings for a material ────────────────────────────
        // Case §2.1.1: threshold and reorder quantity must be configurable
        // so WBS can adjust levels as order volumes grow.
        public bool UpdateReorderSettings(int rawMaterialId, decimal threshold, decimal reorderQuantity)
        {
            try
            {
                string sql = "UPDATE RawMaterials SET " +
                             "ReorderThreshold = @Threshold, " +
                             "ReorderQuantity  = @ReorderQuantity, " +
                             "UpdatedAt        = @UpdatedAt " +
                             "WHERE RawMaterialId = @RawMaterialId;";

                int rows = DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@Threshold", threshold),
                    new MySqlParameter("@ReorderQuantity", reorderQuantity),
                    new MySqlParameter("@UpdatedAt", DateTime.Now),
                    new MySqlParameter("@RawMaterialId", rawMaterialId));

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("UpdateReorderSettings error: " + ex.Message);
                return false;
            }
        }

        // ── Update the preferred vendor and unit price for a material ─────────
        // Case §2.1.1: "prices for raw materials bought from vendors" must be
        // stored and kept current so FIS can determine the best price on reorder.
        public bool UpdateVendorAndPrice(int rawMaterialId, int vendorId, decimal unitPrice)
        {
            try
            {
                string sql = "UPDATE RawMaterials SET " +
                             "VendorId  = @VendorId, " +
                             "UnitPrice = @UnitPrice, " +
                             "UpdatedAt = @UpdatedAt " +
                             "WHERE RawMaterialId = @RawMaterialId;";

                int rows = DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@VendorId", vendorId),
                    new MySqlParameter("@UnitPrice", unitPrice),
                    new MySqlParameter("@UpdatedAt", DateTime.Now),
                    new MySqlParameter("@RawMaterialId", rawMaterialId));

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("UpdateVendorAndPrice error: " + ex.Message);
                return false;
            }
        }

        // ── Update quantity on hand ───────────────────────────────────────────
        // Case §2.1.1: POPS writes to the raw material file when materials are
        // used in production or when a delivery arrives. FIS reads this to know
        // when to reorder. This method keeps the FIS-side record in sync.
        // NOTE: POPS owns the source of truth for quantity. FIS reads it.
        public bool UpdateQuantityOnHand(int rawMaterialId, decimal newQuantity)
        {
            try
            {
                string sql = "UPDATE RawMaterials SET " +
                             "QuantityOnHand = @Quantity, " +
                             "UpdatedAt      = @UpdatedAt " +
                             "WHERE RawMaterialId = @RawMaterialId;";

                int rows = DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@Quantity", newQuantity),
                    new MySqlParameter("@UpdatedAt", DateTime.Now),
                    new MySqlParameter("@RawMaterialId", rawMaterialId));

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("UpdateQuantityOnHand error: " + ex.Message);
                return false;
            }
        }

        // ── Check if a specific material is below its reorder threshold ───────
        // Called by PurchaseOrderService before generating a PO for one material.
        // Returns true = reorder needed, false = stock is sufficient.
        public bool IsBelowThreshold(int rawMaterialId)
        {
            try
            {
                string sql = "SELECT COUNT(*) FROM RawMaterials " +
                             "WHERE RawMaterialId = @RawMaterialId " +
                             "AND QuantityOnHand < ReorderThreshold;";

                int count = Convert.ToInt32(DBHelper.ExecuteScalar(sql,
                    new MySqlParameter("@RawMaterialId", rawMaterialId)));

                return count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("IsBelowThreshold error: " + ex.Message);
                return false;
            }
        }

        // ── Weekly report: raw materials delivered ────────────────────────────
        // Case §2.1.4: "a weekly report showing new orders that have been delivered"
        // Returns materials whose quantity was updated in the past 7 days,
        // indicating a delivery was recorded by POPS.
        public List<RawMaterial> GetRecentlyDeliveredMaterials()
        {
            var list = new List<RawMaterial>();
            try
            {
                string sql = "SELECT r.RawMaterialId, r.MaterialName, r.UnitOfMeasure, " +
                             "r.QuantityOnHand, r.ReorderThreshold, r.ReorderQuantity, " +
                             "r.VendorId, r.UnitPrice, r.UpdatedAt, " +
                             "v.VendorName " +
                             "FROM RawMaterials r " +
                             "INNER JOIN Vendors v ON v.VendorId = r.VendorId " +
                             "WHERE r.UpdatedAt >= @Since " +
                             "ORDER BY r.UpdatedAt DESC;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@Since", DateTime.Now.AddDays(-7)));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToRawMaterial(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetRecentlyDeliveredMaterials error: " + ex.Message);
            }
            return list;
        }

        // ── Private helper — maps a DataRow to a RawMaterial model ───────────
        // VendorName is a denormalised display field from the JOIN — not stored
        // on the RawMaterial table itself but useful for UI display.
        private RawMaterial MapRowToRawMaterial(System.Data.DataRow row)
        {
            return new RawMaterial
            {
                RawMaterialId = Convert.ToInt32(row["RawMaterialId"]),
                MaterialName = row["MaterialName"] == DBNull.Value ? string.Empty : row["MaterialName"].ToString(),
                UnitOfMeasure = row["UnitOfMeasure"] == DBNull.Value ? string.Empty : row["UnitOfMeasure"].ToString(),
                QuantityOnHand = Convert.ToDecimal(row["QuantityOnHand"]),
                ReorderThreshold = Convert.ToDecimal(row["ReorderThreshold"]),
                ReorderQuantity = Convert.ToDecimal(row["ReorderQuantity"]),
                VendorId = Convert.ToInt32(row["VendorId"]),
                UnitPrice = Convert.ToDecimal(row["UnitPrice"]),
                UpdatedAt = row["UpdatedAt"] == DBNull.Value
                                       ? DateTime.MinValue
                                       : Convert.ToDateTime(row["UpdatedAt"])
            };
        }
    }
}