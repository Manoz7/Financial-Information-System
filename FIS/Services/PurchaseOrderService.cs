using FIS.Database;
using FIS.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace FIS.Services
{
    public class PurchaseOrderService
    {
        // ── Auto-reorder: generate POs for all materials below threshold ──────
        // Case §2.1.1: "raw material items are automatically ordered anytime the
        // stored inventory of raw materials falls below a certain threshold."
        //
        // This is the core FIS automation. It:
        //   1. Reads all materials below their reorder threshold (from RawMaterials)
        //   2. Creates one PurchaseOrder per vendor (groups materials by vendor)
        //   3. Creates PurchaseOrderItems for each material in that PO
        //   4. Returns the list of generated POs
        //
        // Called on a scheduled basis (e.g. nightly) or triggered manually.
        public List<PurchaseOrder> GenerateReorderPurchaseOrders()
        {
            var generatedOrders = new List<PurchaseOrder>();
            try
            {
                // Step 1 — Get all materials currently below threshold
                string materialSql = "SELECT RawMaterialId, MaterialName, UnitOfMeasure, " +
                                     "QuantityOnHand, ReorderThreshold, ReorderQuantity, " +
                                     "VendorId, UnitPrice, UpdatedAt " +
                                     "FROM RawMaterials " +
                                     "WHERE QuantityOnHand < ReorderThreshold;";

                var materialDt = DBHelper.ExecuteQuery(materialSql);
                if (materialDt.Rows.Count == 0) return generatedOrders;

                // Step 2 — Group materials by VendorId so one PO is raised per vendor
                var vendorGroups = new Dictionary<int, List<RawMaterial>>();
                foreach (System.Data.DataRow row in materialDt.Rows)
                {
                    var material = MapRowToRawMaterial(row);
                    if (!vendorGroups.ContainsKey(material.VendorId))
                        vendorGroups[material.VendorId] = new List<RawMaterial>();
                    vendorGroups[material.VendorId].Add(material);
                }

                // Step 3 — For each vendor group, create a PO and its line items
                foreach (var kvp in vendorGroups)
                {
                    int vendorId = kvp.Key;
                    List<RawMaterial> materials = kvp.Value;

                    // Calculate total amount for this PO
                    decimal totalAmount = 0;
                    foreach (var m in materials)
                        totalAmount += m.ReorderQuantity * m.UnitPrice;

                    // Insert the PurchaseOrder header
                    string poSql = "INSERT INTO PurchaseOrders " +
                                   "(VendorId, OrderDate, TotalAmount, Status) " +
                                   "VALUES (@VendorId, @OrderDate, @TotalAmount, @Status);";

                    DBHelper.ExecuteNonQuery(poSql,
                        new MySqlParameter("@VendorId", vendorId),
                        new MySqlParameter("@OrderDate", DateTime.Now),
                        new MySqlParameter("@TotalAmount", totalAmount),
                        new MySqlParameter("@Status", "Pending"));

                    int newPoId = Convert.ToInt32(DBHelper.GetLastInsertId());

                    // Insert one PurchaseOrderItem per raw material
                    foreach (var material in materials)
                    {
                        decimal lineTotal = material.ReorderQuantity * material.UnitPrice;

                        string itemSql = "INSERT INTO PurchaseOrderItems " +
                                         "(PurchaseOrderId, RawMaterialId, QuantityOrdered, UnitPrice, LineTotal) " +
                                         "VALUES (@PurchaseOrderId, @RawMaterialId, @QuantityOrdered, @UnitPrice, @LineTotal);";

                        DBHelper.ExecuteNonQuery(itemSql,
                            new MySqlParameter("@PurchaseOrderId", newPoId),
                            new MySqlParameter("@RawMaterialId", material.RawMaterialId),
                            new MySqlParameter("@QuantityOrdered", material.ReorderQuantity),
                            new MySqlParameter("@UnitPrice", material.UnitPrice),
                            new MySqlParameter("@LineTotal", lineTotal));
                    }

                    // Return the created PO for the caller to confirm/log
                    generatedOrders.Add(new PurchaseOrder
                    {
                        PurchaseOrderId = newPoId,
                        VendorId = vendorId,
                        OrderDate = DateTime.Now,
                        TotalAmount = totalAmount,
                        Status = "Pending"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GenerateReorderPurchaseOrders error: " + ex.Message);
            }
            return generatedOrders;
        }

        // ── Get all purchase orders ───────────────────────────────────────────
        // Case §2.1.4: "weekly report showing new orders that have been delivered"
        // Also used for the accounts payable workflow — invoices are matched to POs.
        public List<PurchaseOrder> GetAllPurchaseOrders()
        {
            var list = new List<PurchaseOrder>();
            try
            {
                string sql = "SELECT po.PurchaseOrderId, po.VendorId, po.OrderDate, " +
                             "po.ExpectedDeliveryDate, po.ActualDeliveryDate, " +
                             "po.TotalAmount, po.Status, " +
                             "v.VendorName " +
                             "FROM PurchaseOrders po " +
                             "INNER JOIN Vendors v ON v.VendorId = po.VendorId " +
                             "ORDER BY po.OrderDate DESC;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToPurchaseOrder(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllPurchaseOrders error: " + ex.Message);
            }
            return list;
        }

        // ── Get a single purchase order by ID ─────────────────────────────────
        public PurchaseOrder GetById(int purchaseOrderId)
        {
            try
            {
                string sql = "SELECT po.PurchaseOrderId, po.VendorId, po.OrderDate, " +
                             "po.ExpectedDeliveryDate, po.ActualDeliveryDate, " +
                             "po.TotalAmount, po.Status, " +
                             "v.VendorName " +
                             "FROM PurchaseOrders po " +
                             "INNER JOIN Vendors v ON v.VendorId = po.VendorId " +
                             "WHERE po.PurchaseOrderId = @PurchaseOrderId;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@PurchaseOrderId", purchaseOrderId));

                if (dt.Rows.Count == 0) return null;
                return MapRowToPurchaseOrder(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetById error: " + ex.Message);
                return null;
            }
        }

        // ── Get all line items for a purchase order ───────────────────────────
        // Used when displaying PO details or when an InvoiceAP is being matched
        // to verify the quantities and prices billed by the vendor are correct.
        public List<PurchaseOrderItem> GetItemsByPurchaseOrder(int purchaseOrderId)
        {
            var list = new List<PurchaseOrderItem>();
            try
            {
                string sql = "SELECT poi.PurchaseOrderItemId, poi.PurchaseOrderId, " +
                             "poi.RawMaterialId, poi.QuantityOrdered, " +
                             "poi.UnitPrice, poi.LineTotal, " +
                             "r.MaterialName " +
                             "FROM PurchaseOrderItems poi " +
                             "INNER JOIN RawMaterials r ON r.RawMaterialId = poi.RawMaterialId " +
                             "WHERE poi.PurchaseOrderId = @PurchaseOrderId " +
                             "ORDER BY r.MaterialName;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@PurchaseOrderId", purchaseOrderId));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToPurchaseOrderItem(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetItemsByPurchaseOrder error: " + ex.Message);
            }
            return list;
        }

        // ── Get pending purchase orders ───────────────────────────────────────
        // Used by accounts payable to know which POs are awaiting delivery
        // and a matching vendor invoice.
        public List<PurchaseOrder> GetPendingOrders()
        {
            var list = new List<PurchaseOrder>();
            try
            {
                string sql = "SELECT po.PurchaseOrderId, po.VendorId, po.OrderDate, " +
                             "po.ExpectedDeliveryDate, po.ActualDeliveryDate, " +
                             "po.TotalAmount, po.Status, " +
                             "v.VendorName " +
                             "FROM PurchaseOrders po " +
                             "INNER JOIN Vendors v ON v.VendorId = po.VendorId " +
                             "WHERE po.Status = 'Pending' " +
                             "ORDER BY po.OrderDate ASC;";

                var dt = DBHelper.ExecuteQuery(sql);
                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToPurchaseOrder(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetPendingOrders error: " + ex.Message);
            }
            return list;
        }

        // ── Mark a purchase order as delivered ────────────────────────────────
        // Case §2.1.1: "as new supplies are delivered from vendors, the POPS will
        // record the addition to the raw material file but bills from the vendors
        // will come to the FIS to be recorded and paid."
        //
        // When POPS confirms delivery, FIS marks the PO delivered and records
        // the actual delivery date. The vendor invoice (InvoiceAP) is then
        // matched to this PO in the accounts payable workflow.
        public bool MarkAsDelivered(int purchaseOrderId, DateTime actualDeliveryDate)
        {
            try
            {
                string sql = "UPDATE PurchaseOrders SET " +
                             "Status              = 'Delivered', " +
                             "ActualDeliveryDate  = @ActualDeliveryDate " +
                             "WHERE PurchaseOrderId = @PurchaseOrderId;";

                int rows = DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@ActualDeliveryDate", actualDeliveryDate),
                    new MySqlParameter("@PurchaseOrderId", purchaseOrderId));

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("MarkAsDelivered error: " + ex.Message);
                return false;
            }
        }

        // ── Cancel a purchase order ───────────────────────────────────────────
        // Only Pending orders can be cancelled. Once delivered, the vendor
        // invoice process has already begun and cancellation is not valid.
        public bool CancelPurchaseOrder(int purchaseOrderId)
        {
            try
            {
                string sql = "UPDATE PurchaseOrders SET Status = 'Cancelled' " +
                             "WHERE PurchaseOrderId = @PurchaseOrderId " +
                             "AND Status = 'Pending';";

                int rows = DBHelper.ExecuteNonQuery(sql,
                    new MySqlParameter("@PurchaseOrderId", purchaseOrderId));

                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("CancelPurchaseOrder error: " + ex.Message);
                return false;
            }
        }

        // ── Weekly report: POs delivered in the past 7 days ──────────────────
        // Case §2.1.4: "a weekly report showing new orders that have been delivered"
        public List<PurchaseOrder> GetDeliveredOrdersThisWeek()
        {
            var list = new List<PurchaseOrder>();
            try
            {
                string sql = "SELECT po.PurchaseOrderId, po.VendorId, po.OrderDate, " +
                             "po.ExpectedDeliveryDate, po.ActualDeliveryDate, " +
                             "po.TotalAmount, po.Status, " +
                             "v.VendorName " +
                             "FROM PurchaseOrders po " +
                             "INNER JOIN Vendors v ON v.VendorId = po.VendorId " +
                             "WHERE po.Status = 'Delivered' " +
                             "AND po.ActualDeliveryDate >= @Since " +
                             "ORDER BY po.ActualDeliveryDate DESC;";

                var dt = DBHelper.ExecuteQuery(sql,
                    new MySqlParameter("@Since", DateTime.Now.AddDays(-7)));

                foreach (System.Data.DataRow row in dt.Rows)
                    list.Add(MapRowToPurchaseOrder(row));
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetDeliveredOrdersThisWeek error: " + ex.Message);
            }
            return list;
        }

        // ── Private helper — maps a DataRow to a PurchaseOrder model ─────────
        // VendorName is a denormalised display field from the JOIN.
        private PurchaseOrder MapRowToPurchaseOrder(System.Data.DataRow row)
        {
            return new PurchaseOrder
            {
                PurchaseOrderId = Convert.ToInt32(row["PurchaseOrderId"]),
                VendorId = Convert.ToInt32(row["VendorId"]),
                OrderDate = Convert.ToDateTime(row["OrderDate"]),
                ExpectedDeliveryDate = row["ExpectedDeliveryDate"] == DBNull.Value
                                           ? (DateTime?)null
                                           : Convert.ToDateTime(row["ExpectedDeliveryDate"]),
                ActualDeliveryDate = row["ActualDeliveryDate"] == DBNull.Value
                                           ? (DateTime?)null
                                           : Convert.ToDateTime(row["ActualDeliveryDate"]),
                TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                Status = row["Status"] == DBNull.Value ? "Pending" : row["Status"].ToString()
            };
        }

        // ── Private helper — maps a DataRow to a PurchaseOrderItem model ──────
        // MaterialName is a denormalised display field from the JOIN.
        private PurchaseOrderItem MapRowToPurchaseOrderItem(System.Data.DataRow row)
        {
            return new PurchaseOrderItem
            {
                PurchaseOrderItemId = Convert.ToInt32(row["PurchaseOrderItemId"]),
                PurchaseOrderId = Convert.ToInt32(row["PurchaseOrderId"]),
                RawMaterialId = Convert.ToInt32(row["RawMaterialId"]),
                QuantityOrdered = Convert.ToDecimal(row["QuantityOrdered"]),
                UnitPrice = Convert.ToDecimal(row["UnitPrice"]),
                LineTotal = Convert.ToDecimal(row["LineTotal"])
            };
        }

        // ── Private helper — maps a DataRow to a RawMaterial model ───────────
        // Used internally by GenerateReorderPurchaseOrders only.
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