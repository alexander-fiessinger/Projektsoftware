using MySql.Data.MySqlClient;
using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    public partial class DatabaseService
    {
        #region Suppliers

        public async Task<List<Supplier>> GetAllSuppliersAsync()
        {
            var list = new List<Supplier>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = "SELECT * FROM suppliers ORDER BY name";
            using var cmd = new MySqlCommand(query, connection);
            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(MapSupplier(reader));
            return list;
        }

        public async Task<int> AddSupplierAsync(Supplier s)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = @"INSERT INTO suppliers (name, contact_person, email, phone, address, zip_code, city, country, tax_number, bank_iban, bank_bic, notes, created_at)
                             VALUES (@name,@cp,@email,@phone,@addr,@zip,@city,@country,@tax,@iban,@bic,@notes,@now);
                             SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(query, connection);
            AddSupplierParams(cmd, s);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task UpdateSupplierAsync(Supplier s)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = @"UPDATE suppliers SET name=@name, contact_person=@cp, email=@email, phone=@phone,
                             address=@addr, zip_code=@zip, city=@city, country=@country, tax_number=@tax,
                             bank_iban=@iban, bank_bic=@bic, notes=@notes, updated_at=@now WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            AddSupplierParams(cmd, s);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            cmd.Parameters.AddWithValue("@id", s.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteSupplierAsync(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var cmd = new MySqlCommand("DELETE FROM suppliers WHERE id=@id", connection);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        private static void AddSupplierParams(MySqlCommand cmd, Supplier s)
        {
            cmd.Parameters.AddWithValue("@name", s.Name);
            cmd.Parameters.AddWithValue("@cp", s.ContactPerson ?? "");
            cmd.Parameters.AddWithValue("@email", s.Email ?? "");
            cmd.Parameters.AddWithValue("@phone", s.Phone ?? "");
            cmd.Parameters.AddWithValue("@addr", s.Address ?? "");
            cmd.Parameters.AddWithValue("@zip", s.ZipCode ?? "");
            cmd.Parameters.AddWithValue("@city", s.City ?? "");
            cmd.Parameters.AddWithValue("@country", s.Country ?? "Deutschland");
            cmd.Parameters.AddWithValue("@tax", s.TaxNumber ?? "");
            cmd.Parameters.AddWithValue("@iban", s.BankIban ?? "");
            cmd.Parameters.AddWithValue("@bic", s.BankBic ?? "");
            cmd.Parameters.AddWithValue("@notes", s.Notes ?? "");
        }

        private static Supplier MapSupplier(MySqlDataReader r)
        {
            int ebIdOrd = -1;
            int ebSyncOrd = -1;
            try { ebIdOrd = r.GetOrdinal("easybill_customer_id"); } catch { }
            try { ebSyncOrd = r.GetOrdinal("easybill_synced_at"); } catch { }
            return new Supplier
            {
                Id = r.GetInt32("id"),
                Name = r.IsDBNull(r.GetOrdinal("name")) ? "" : r.GetString("name"),
                ContactPerson = r.IsDBNull(r.GetOrdinal("contact_person")) ? "" : r.GetString("contact_person"),
                Email = r.IsDBNull(r.GetOrdinal("email")) ? "" : r.GetString("email"),
                Phone = r.IsDBNull(r.GetOrdinal("phone")) ? "" : r.GetString("phone"),
                Address = r.IsDBNull(r.GetOrdinal("address")) ? "" : r.GetString("address"),
                ZipCode = r.IsDBNull(r.GetOrdinal("zip_code")) ? "" : r.GetString("zip_code"),
                City = r.IsDBNull(r.GetOrdinal("city")) ? "" : r.GetString("city"),
                Country = r.IsDBNull(r.GetOrdinal("country")) ? "Deutschland" : r.GetString("country"),
                TaxNumber = r.IsDBNull(r.GetOrdinal("tax_number")) ? "" : r.GetString("tax_number"),
                BankIban = r.IsDBNull(r.GetOrdinal("bank_iban")) ? "" : r.GetString("bank_iban"),
                BankBic = r.IsDBNull(r.GetOrdinal("bank_bic")) ? "" : r.GetString("bank_bic"),
                Notes = r.IsDBNull(r.GetOrdinal("notes")) ? "" : r.GetString("notes"),
                CreatedAt = r.IsDBNull(r.GetOrdinal("created_at")) ? DateTime.Now : r.GetDateTime("created_at"),
                UpdatedAt = r.IsDBNull(r.GetOrdinal("updated_at")) ? null : r.GetDateTime("updated_at"),
                EasybillCustomerId = ebIdOrd >= 0 && !r.IsDBNull(ebIdOrd) ? r.GetInt64(ebIdOrd) : null,
                EasybillSyncedAt = ebSyncOrd >= 0 && !r.IsDBNull(ebSyncOrd) ? r.GetDateTime(ebSyncOrd) : null
            };
        }

        public async Task UpdateSupplierEasybillIdAsync(int supplierId, long easybillCustomerId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = "UPDATE suppliers SET easybill_customer_id=@ebid, easybill_synced_at=@now WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ebid", easybillCustomerId);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            cmd.Parameters.AddWithValue("@id", supplierId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdatePurchaseInvoiceEasybillIdAsync(int invoiceId, long easybillDocumentId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = "UPDATE purchase_invoices SET easybill_document_id=@ebid, easybill_synced_at=@now WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ebid", easybillDocumentId);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            cmd.Parameters.AddWithValue("@id", invoiceId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdatePurchaseInvoiceNumberAsync(int invoiceId, string invoiceNumber)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var cmd = new MySqlCommand("UPDATE purchase_invoices SET invoice_number=@num WHERE id=@id", connection);
            cmd.Parameters.AddWithValue("@num", invoiceNumber);
            cmd.Parameters.AddWithValue("@id", invoiceId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdatePurchaseInvoiceAttachmentIdAsync(int invoiceId, long attachmentId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = "UPDATE purchase_invoices SET easybill_attachment_id=@aid, easybill_synced_at=@now WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@aid", attachmentId);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            cmd.Parameters.AddWithValue("@id", invoiceId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdatePurchaseOrderEasybillIdAsync(int orderId, long easybillDocumentId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = "UPDATE purchase_orders SET easybill_document_id=@ebid, easybill_synced_at=@now WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ebid", easybillDocumentId);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            cmd.Parameters.AddWithValue("@id", orderId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdatePurchaseOrderNumberAsync(int orderId, string orderNumber)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var cmd = new MySqlCommand("UPDATE purchase_orders SET order_number=@num WHERE id=@id", connection);
            cmd.Parameters.AddWithValue("@num", orderNumber);
            cmd.Parameters.AddWithValue("@id", orderId);
            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region Purchase Orders

        public async Task<List<PurchaseOrder>> GetAllPurchaseOrdersAsync()
        {
            var list = new List<PurchaseOrder>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = @"SELECT po.*, COALESCE(s.name,'') as supplier_name
                             FROM purchase_orders po
                             LEFT JOIN suppliers s ON s.id = po.supplier_id
                             ORDER BY po.created_at DESC";
            using var cmd = new MySqlCommand(query, connection);
            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(MapPurchaseOrder(reader));
            return list;
        }

        public async Task<List<PurchaseOrderItem>> GetPurchaseOrderItemsAsync(int orderId)
        {
            var list = new List<PurchaseOrderItem>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = "SELECT * FROM purchase_order_items WHERE purchase_order_id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", orderId);
            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(MapPurchaseOrderItem(reader));
            return list;
        }

        public async Task<int> AddPurchaseOrderAsync(PurchaseOrder po)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = @"INSERT INTO purchase_orders (supplier_id, order_number, order_date, delivery_date_expected, status, total_net, total_gross, notes, created_at)
                             VALUES (@sid,@num,@odate,@ddate,@status,@tnet,@tgross,@notes,@now);
                             SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(query, connection);
            AddPurchaseOrderParams(cmd, po);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            int newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            foreach (var item in po.Items)
            {
                item.PurchaseOrderId = newId;
                await AddPurchaseOrderItemAsync(item, connection);
            }
            return newId;
        }

        public async Task UpdatePurchaseOrderAsync(PurchaseOrder po)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = @"UPDATE purchase_orders SET supplier_id=@sid, order_number=@num, order_date=@odate,
                             delivery_date_expected=@ddate, status=@status, total_net=@tnet, total_gross=@tgross,
                             notes=@notes WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            AddPurchaseOrderParams(cmd, po);
            cmd.Parameters.AddWithValue("@id", po.Id);
            await cmd.ExecuteNonQueryAsync();

            using var delCmd = new MySqlCommand("DELETE FROM purchase_order_items WHERE purchase_order_id=@id", connection);
            delCmd.Parameters.AddWithValue("@id", po.Id);
            await delCmd.ExecuteNonQueryAsync();

            foreach (var item in po.Items)
            {
                item.PurchaseOrderId = po.Id;
                await AddPurchaseOrderItemAsync(item, connection);
            }
        }

        public async Task DeletePurchaseOrderAsync(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var cmd = new MySqlCommand("DELETE FROM purchase_orders WHERE id=@id", connection);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task AddPurchaseOrderItemAsync(PurchaseOrderItem item, MySqlConnection connection)
        {
            string q = @"INSERT INTO purchase_order_items (purchase_order_id, description, quantity, unit, unit_price_net, total_net, vat_percent)
                         VALUES (@oid,@desc,@qty,@unit,@uprice,@tnet,@vat)";
            using var cmd = new MySqlCommand(q, connection);
            cmd.Parameters.AddWithValue("@oid", item.PurchaseOrderId);
            cmd.Parameters.AddWithValue("@desc", item.Description);
            cmd.Parameters.AddWithValue("@qty", item.Quantity);
            cmd.Parameters.AddWithValue("@unit", item.Unit);
            cmd.Parameters.AddWithValue("@uprice", item.UnitPriceNet);
            cmd.Parameters.AddWithValue("@tnet", item.TotalNet);
            cmd.Parameters.AddWithValue("@vat", item.VatPercent);
            await cmd.ExecuteNonQueryAsync();
        }

        private static void AddPurchaseOrderParams(MySqlCommand cmd, PurchaseOrder po)
        {
            cmd.Parameters.AddWithValue("@sid", (object?)po.SupplierId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@num", po.OrderNumber ?? "");
            cmd.Parameters.AddWithValue("@odate", po.OrderDate.Date);
            cmd.Parameters.AddWithValue("@ddate", (object?)po.DeliveryDateExpected?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", po.Status ?? "Offen");
            cmd.Parameters.AddWithValue("@tnet", po.TotalNet);
            cmd.Parameters.AddWithValue("@tgross", po.TotalGross);
            cmd.Parameters.AddWithValue("@notes", po.Notes ?? "");
        }

        private static PurchaseOrder MapPurchaseOrder(MySqlDataReader r)
        {
            int ddaOrdinal = r.GetOrdinal("delivery_date_actual");
            int ddeOrdinal = r.GetOrdinal("delivery_date_expected");
            int ebIdOrd = -1; int ebSyncOrd = -1;
            try { ebIdOrd = r.GetOrdinal("easybill_document_id"); } catch { }
            try { ebSyncOrd = r.GetOrdinal("easybill_synced_at"); } catch { }
            return new PurchaseOrder
            {
                Id = r.GetInt32("id"),
                SupplierId = r.IsDBNull(r.GetOrdinal("supplier_id")) ? null : r.GetInt32("supplier_id"),
                SupplierName = r.IsDBNull(r.GetOrdinal("supplier_name")) ? "" : r.GetString("supplier_name"),
                OrderNumber = r.IsDBNull(r.GetOrdinal("order_number")) ? "" : r.GetString("order_number"),
                OrderDate = r.GetDateTime("order_date"),
                DeliveryDateExpected = r.IsDBNull(ddeOrdinal) ? null : r.GetDateTime(ddeOrdinal),
                DeliveryDateActual = r.IsDBNull(ddaOrdinal) ? null : r.GetDateTime(ddaOrdinal),
                Status = r.IsDBNull(r.GetOrdinal("status")) ? "Offen" : r.GetString("status"),
                TotalNet = r.IsDBNull(r.GetOrdinal("total_net")) ? 0 : r.GetDecimal("total_net"),
                TotalGross = r.IsDBNull(r.GetOrdinal("total_gross")) ? 0 : r.GetDecimal("total_gross"),
                Notes = r.IsDBNull(r.GetOrdinal("notes")) ? "" : r.GetString("notes"),
                CreatedAt = r.IsDBNull(r.GetOrdinal("created_at")) ? DateTime.Now : r.GetDateTime("created_at"),
                EasybillDocumentId = ebIdOrd >= 0 && !r.IsDBNull(ebIdOrd) ? r.GetInt64(ebIdOrd) : null,
                EasybillDocSyncedAt = ebSyncOrd >= 0 && !r.IsDBNull(ebSyncOrd) ? r.GetDateTime(ebSyncOrd) : null
            };
        }

        private static PurchaseOrderItem MapPurchaseOrderItem(MySqlDataReader r) => new PurchaseOrderItem
        {
            Id = r.GetInt32("id"),
            PurchaseOrderId = r.GetInt32("purchase_order_id"),
            Description = r.IsDBNull(r.GetOrdinal("description")) ? "" : r.GetString("description"),
            Quantity = r.IsDBNull(r.GetOrdinal("quantity")) ? 1 : r.GetDecimal("quantity"),
            Unit = r.IsDBNull(r.GetOrdinal("unit")) ? "Stk." : r.GetString("unit"),
            UnitPriceNet = r.IsDBNull(r.GetOrdinal("unit_price_net")) ? 0 : r.GetDecimal("unit_price_net"),
            TotalNet = r.IsDBNull(r.GetOrdinal("total_net")) ? 0 : r.GetDecimal("total_net"),
            VatPercent = r.IsDBNull(r.GetOrdinal("vat_percent")) ? 19 : r.GetDecimal("vat_percent")
        };

        #endregion

        #region Purchase Invoices

        public async Task<List<PurchaseInvoice>> GetAllPurchaseInvoicesAsync()
        {
            var list = new List<PurchaseInvoice>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = @"SELECT pi.*, COALESCE(s.name,'') as supplier_name
                             FROM purchase_invoices pi
                             LEFT JOIN suppliers s ON s.id = pi.supplier_id
                             ORDER BY pi.invoice_date DESC";
            using var cmd = new MySqlCommand(query, connection);
            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(MapPurchaseInvoice(reader));
            return list;
        }

        public async Task<int> AddPurchaseInvoiceAsync(PurchaseInvoice inv)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = @"INSERT INTO purchase_invoices (supplier_id, invoice_number, invoice_date, due_date, total_net, total_gross, status, payment_date, purchase_order_id, notes, created_at)
                             VALUES (@sid,@num,@idate,@ddate,@tnet,@tgross,@status,@pdate,@poid,@notes,@now);
                             SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(query, connection);
            AddPurchaseInvoiceParams(cmd, inv);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task UpdatePurchaseInvoiceAsync(PurchaseInvoice inv)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = @"UPDATE purchase_invoices SET supplier_id=@sid, invoice_number=@num, invoice_date=@idate,
                             due_date=@ddate, total_net=@tnet, total_gross=@tgross, status=@status,
                             payment_date=@pdate, purchase_order_id=@poid, notes=@notes WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            AddPurchaseInvoiceParams(cmd, inv);
            cmd.Parameters.AddWithValue("@id", inv.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeletePurchaseInvoiceAsync(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var cmd = new MySqlCommand("DELETE FROM purchase_invoices WHERE id=@id", connection);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task MarkPurchaseInvoicePaidAsync(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = "UPDATE purchase_invoices SET status='Bezahlt', payment_date=@today WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@today", DateTime.Today);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<(int OpenPoCount, int TotalDocsCount, int SyncedDocsCount)> GetPurchaseDashboardStatsAsync()
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string poQuery = "SELECT COUNT(*) FROM purchase_orders WHERE status IN ('Offen','Bestellt')";
            using var cmd1 = new MySqlCommand(poQuery, connection);
            int poCount = Convert.ToInt32(await cmd1.ExecuteScalarAsync());

            string totalDocsQuery = "SELECT COUNT(*) FROM purchase_documents";
            using var cmd2 = new MySqlCommand(totalDocsQuery, connection);
            int totalDocs = Convert.ToInt32(await cmd2.ExecuteScalarAsync());

            string syncedDocsQuery = "SELECT COUNT(*) FROM purchase_documents WHERE easybill_attachment_id IS NOT NULL";
            using var cmd3 = new MySqlCommand(syncedDocsQuery, connection);
            int syncedDocs = Convert.ToInt32(await cmd3.ExecuteScalarAsync());

            return (poCount, totalDocs, syncedDocs);
        }

        private static void AddPurchaseInvoiceParams(MySqlCommand cmd, PurchaseInvoice inv)
        {
            cmd.Parameters.AddWithValue("@sid", (object?)inv.SupplierId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@num", inv.InvoiceNumber);
            cmd.Parameters.AddWithValue("@idate", inv.InvoiceDate.Date);
            cmd.Parameters.AddWithValue("@ddate", (object?)inv.DueDate?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tnet", inv.TotalNet);
            cmd.Parameters.AddWithValue("@tgross", inv.TotalGross);
            cmd.Parameters.AddWithValue("@status", inv.Status ?? "Offen");
            cmd.Parameters.AddWithValue("@pdate", (object?)inv.PaymentDate?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@poid", (object?)inv.PurchaseOrderId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", inv.Notes ?? "");
        }

        private static PurchaseInvoice MapPurchaseInvoice(MySqlDataReader r)
        {
            int ddOrdinal = r.GetOrdinal("due_date");
            int pdOrdinal = r.GetOrdinal("payment_date");
            int poOrdinal = r.GetOrdinal("purchase_order_id");
            var inv = new PurchaseInvoice
            {
                Id = r.GetInt32("id"),
                SupplierId = r.IsDBNull(r.GetOrdinal("supplier_id")) ? null : r.GetInt32("supplier_id"),
                SupplierName = r.IsDBNull(r.GetOrdinal("supplier_name")) ? "" : r.GetString("supplier_name"),
                InvoiceNumber = r.IsDBNull(r.GetOrdinal("invoice_number")) ? "" : r.GetString("invoice_number"),
                InvoiceDate = r.GetDateTime("invoice_date"),
                DueDate = r.IsDBNull(ddOrdinal) ? null : r.GetDateTime(ddOrdinal),
                TotalNet = r.IsDBNull(r.GetOrdinal("total_net")) ? 0 : r.GetDecimal("total_net"),
                TotalGross = r.IsDBNull(r.GetOrdinal("total_gross")) ? 0 : r.GetDecimal("total_gross"),
                Status = r.IsDBNull(r.GetOrdinal("status")) ? "Offen" : r.GetString("status"),
                PaymentDate = r.IsDBNull(pdOrdinal) ? null : r.GetDateTime(pdOrdinal),
                PurchaseOrderId = r.IsDBNull(poOrdinal) ? null : r.GetInt32(poOrdinal),
                Notes = r.IsDBNull(r.GetOrdinal("notes")) ? "" : r.GetString("notes"),
                CreatedAt = r.IsDBNull(r.GetOrdinal("created_at")) ? DateTime.Now : r.GetDateTime("created_at")
            };
            try
            {
                int ebDocOrdinal = r.GetOrdinal("easybill_document_id");
                int ebSyncOrdinal = r.GetOrdinal("easybill_synced_at");
                int ebAttOrdinal = -1;
                try { ebAttOrdinal = r.GetOrdinal("easybill_attachment_id"); } catch { }
                inv.EasybillDocumentId = r.IsDBNull(ebDocOrdinal) ? null : r.GetInt64(ebDocOrdinal);
                inv.EasybillDocSyncedAt = r.IsDBNull(ebSyncOrdinal) ? null : r.GetDateTime(ebSyncOrdinal);
                inv.EasybillAttachmentId = ebAttOrdinal >= 0 && !r.IsDBNull(ebAttOrdinal) ? r.GetInt64(ebAttOrdinal) : null;
            }
            catch { /* Spalten noch nicht migriert */ }
            return inv;
        }

        #endregion

        #region Purchase Documents

        public async Task<List<PurchaseDocument>> GetAllPurchaseDocumentsAsync()
        {
            var list = new List<PurchaseDocument>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = @"SELECT pd.*, COALESCE(s.name,'') as supplier_name
                             FROM purchase_documents pd
                             LEFT JOIN suppliers s ON s.id = pd.supplier_id
                             ORDER BY pd.document_date DESC";
            using var cmd = new MySqlCommand(query, connection);
            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(MapPurchaseDocument(reader));
            return list;
        }

        public async Task<int> AddPurchaseDocumentAsync(PurchaseDocument doc)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = @"INSERT INTO purchase_documents
                             (supplier_id, document_name, document_type, document_date,
                              original_file_name, local_file_path, notes, created_at)
                             VALUES (@sid,@name,@type,@date,@fname,@fpath,@notes,@now);
                             SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(query, connection);
            AddPurchaseDocumentParams(cmd, doc);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task UpdatePurchaseDocumentAsync(PurchaseDocument doc)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = @"UPDATE purchase_documents SET
                             supplier_id=@sid, document_name=@name, document_type=@type,
                             document_date=@date, original_file_name=@fname,
                             local_file_path=@fpath, notes=@notes WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            AddPurchaseDocumentParams(cmd, doc);
            cmd.Parameters.AddWithValue("@id", doc.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeletePurchaseDocumentAsync(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var cmd = new MySqlCommand("DELETE FROM purchase_documents WHERE id=@id", connection);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdatePurchaseDocumentEasybillAttachmentIdAsync(int docId, long attachmentId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            string query = @"UPDATE purchase_documents
                             SET easybill_attachment_id=@aid, easybill_synced_at=@now WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@aid", attachmentId);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            cmd.Parameters.AddWithValue("@id", docId);
            await cmd.ExecuteNonQueryAsync();
        }

        private static void AddPurchaseDocumentParams(MySqlCommand cmd, PurchaseDocument doc)
        {
            cmd.Parameters.AddWithValue("@sid", (object?)doc.SupplierId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@name", doc.DocumentName ?? "");
            cmd.Parameters.AddWithValue("@type", doc.DocumentType ?? "Rechnung");
            cmd.Parameters.AddWithValue("@date", doc.DocumentDate.Date);
            cmd.Parameters.AddWithValue("@fname", (object?)doc.OriginalFileName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fpath", (object?)doc.LocalFilePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", doc.Notes ?? "");
        }

        private static PurchaseDocument MapPurchaseDocument(MySqlDataReader r)
        {
            var doc = new PurchaseDocument
            {
                Id = r.GetInt32("id"),
                SupplierId = r.IsDBNull(r.GetOrdinal("supplier_id")) ? null : r.GetInt32("supplier_id"),
                SupplierName = r.IsDBNull(r.GetOrdinal("supplier_name")) ? "" : r.GetString("supplier_name"),
                DocumentName = r.IsDBNull(r.GetOrdinal("document_name")) ? "" : r.GetString("document_name"),
                DocumentType = r.IsDBNull(r.GetOrdinal("document_type")) ? "Rechnung" : r.GetString("document_type"),
                DocumentDate = r.GetDateTime("document_date"),
                OriginalFileName = r.IsDBNull(r.GetOrdinal("original_file_name")) ? "" : r.GetString("original_file_name"),
                LocalFilePath = r.IsDBNull(r.GetOrdinal("local_file_path")) ? "" : r.GetString("local_file_path"),
                Notes = r.IsDBNull(r.GetOrdinal("notes")) ? "" : r.GetString("notes"),
                CreatedAt = r.IsDBNull(r.GetOrdinal("created_at")) ? DateTime.Now : r.GetDateTime("created_at")
            };
            try
            {
                int ebAttOrd = r.GetOrdinal("easybill_attachment_id");
                int ebSyncOrd = r.GetOrdinal("easybill_synced_at");
                doc.EasybillAttachmentId = r.IsDBNull(ebAttOrd) ? null : r.GetInt64(ebAttOrd);
                doc.EasybillSyncedAt = r.IsDBNull(ebSyncOrd) ? null : r.GetDateTime(ebSyncOrd);
            }
            catch { }
            return doc;
        }

        #endregion
    }
}
