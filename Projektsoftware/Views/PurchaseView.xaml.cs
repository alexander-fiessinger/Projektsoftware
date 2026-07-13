using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class PurchaseView : UserControl
    {
        private static readonly CultureInfo euroFormat = new CultureInfo("de-DE");
        private DatabaseService? _db;
        private EasybillService? _easybill;

        private List<Supplier> _suppliers = new();
        private List<PurchaseOrder> _orders = new();
        private List<PurchaseDocument> _documents = new();

        private string _searchText = "";
        private string _orderStatusFilter = "";
        private string _documentTypeFilter = "";

        public PurchaseView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _db = new DatabaseService();
                var ebConfig = EasybillConfig.Load();
                if (ebConfig.IsConfigured)
                    _easybill = new EasybillService();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            if (_db == null) return;
            _suppliers = await _db.GetAllSuppliersAsync();
            _orders = await _db.GetAllPurchaseOrdersAsync();
            _documents = await _db.GetAllPurchaseDocumentsAsync();
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_db == null) return;

            // KPIs
            TotalSuppliersText.Text = _suppliers.Count.ToString();
            OpenOrdersText.Text = _orders.Count(o => o.Status is "Offen" or "Bestellt").ToString();
            TotalDocumentsText.Text = _documents.Count.ToString();
            SyncedDocumentsText.Text = _documents.Count(d => d.EasybillAttachmentId.HasValue).ToString();

            // Lieferanten
            var filteredSuppliers = string.IsNullOrWhiteSpace(_searchText)
                ? _suppliers
                : _suppliers.Where(s => s.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                    || s.ContactPerson.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                    || s.Email.Contains(_searchText, StringComparison.OrdinalIgnoreCase)).ToList();
            SuppliersGrid.ItemsSource = filteredSuppliers;
            SupplierCountText.Text = $"{filteredSuppliers.Count} Lieferant(en)";

            // Bestellungen
            var filteredOrders = _orders.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(_orderStatusFilter))
                filteredOrders = filteredOrders.Where(o => o.Status == _orderStatusFilter);
            if (!string.IsNullOrWhiteSpace(_searchText))
                filteredOrders = filteredOrders.Where(o => o.OrderNumber.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                    || o.SupplierName.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
            var orderList = filteredOrders.ToList();
            OrdersGrid.ItemsSource = orderList;
            decimal orderTotal = orderList.Sum(o => o.TotalGross);
            OrderCountText.Text = $"{orderList.Count} Bestellung(en) | Gesamt Brutto: {orderTotal.ToString("C2", euroFormat)}";

            // Belegablage
            var filteredDocs = _documents.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(_documentTypeFilter))
                filteredDocs = filteredDocs.Where(d => d.DocumentType == _documentTypeFilter);
            if (!string.IsNullOrWhiteSpace(_searchText))
                filteredDocs = filteredDocs.Where(d =>
                    d.DocumentName.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                    || d.SupplierName.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                    || d.DocumentType.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
            var docList = filteredDocs.ToList();
            DocumentsGrid.ItemsSource = docList;
            DocumentCountText.Text = $"{docList.Count} Beleg(e) | {docList.Count(d => d.EasybillAttachmentId.HasValue)} in Easybill";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchBox.Text.Trim();
            if (_db != null) RefreshUI();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();

        #region Suppliers

        private async void NewSupplier_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SupplierDialog { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                int newId = await _db.AddSupplierAsync(dlg.Result);
                dlg.Result.Id = newId;
                await TrySyncSupplierToEasybillAsync(dlg.Result);
                await LoadDataAsync();
            }
        }

        private async void EditSupplier_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not Supplier s) return;
            var dlg = new SupplierDialog(s) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                await _db.UpdateSupplierAsync(dlg.Result);
                await TrySyncSupplierToEasybillAsync(dlg.Result);
                await LoadDataAsync();
            }
        }

        private async void DeleteSupplier_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not Supplier s) return;
            if (MessageBox.Show($"Lieferant '{s.Name}' löschen?", "Löschen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            if (_easybill != null && s.EasybillCustomerId.HasValue)
            {
                try { await _easybill.DeleteCustomerAsync(s.EasybillCustomerId.Value); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Easybill-Löschung fehlgeschlagen: {ex.Message}"); }
            }
            await _db.DeleteSupplierAsync(s.Id);
            await LoadDataAsync();
        }

        private void SupplierGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SuppliersGrid.SelectedItem is Supplier s)
            {
                var dlg = new SupplierDialog(s) { Owner = Window.GetWindow(this) };
                if (dlg.ShowDialog() == true)
                    _ = _db.UpdateSupplierAsync(dlg.Result)
                        .ContinueWith(_ => Dispatcher.Invoke(async () =>
                        {
                            await TrySyncSupplierToEasybillAsync(dlg.Result);
                            await LoadDataAsync();
                        }));
            }
        }

        private async Task TrySyncSupplierToEasybillAsync(Supplier supplier)
        {
            if (_easybill == null || _db == null) return;
            try
            {
                var result = await _easybill.SyncSupplierToEasybillAsync(supplier);
                if (result?.Id > 0)
                    await _db.UpdateSupplierEasybillIdAsync(supplier.Id, result.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Easybill Lieferant-Sync Fehler: {ex.Message}");
            }
        }

        #endregion

        #region Purchase Orders

        private async void NewOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_suppliers.Count == 0)
            {
                MessageBox.Show("Bitte legen Sie zuerst einen Lieferanten an.", "Kein Lieferant", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var dlg = new PurchaseOrderDialog(_suppliers) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                int newId = await _db.AddPurchaseOrderAsync(dlg.Result);
                dlg.Result.Id = newId;
                await TrySyncPurchaseOrderToEasybillAsync(dlg.Result, dlg.SaveAsDraft);
                await LoadDataAsync();
            }
        }

        private async void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not PurchaseOrder po) return;
            po.Items = await _db.GetPurchaseOrderItemsAsync(po.Id);
            var dlg = new PurchaseOrderDialog(_suppliers, po) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                await _db.UpdatePurchaseOrderAsync(dlg.Result);
                await TrySyncPurchaseOrderToEasybillAsync(dlg.Result, dlg.SaveAsDraft);
                await LoadDataAsync();
            }
        }

        private async void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not PurchaseOrder po) return;
            if (MessageBox.Show($"Bestellung '{po.OrderNumber}' löschen?", "Löschen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            if (_easybill != null && po.EasybillDocumentId.HasValue)
            {
                try { await _easybill.DeleteDocumentAsync(po.EasybillDocumentId.Value); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Easybill-Löschung fehlgeschlagen: {ex.Message}"); }
            }
            await _db.DeletePurchaseOrderAsync(po.Id);
            await LoadDataAsync();
        }

        private async void OrderGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (OrdersGrid.SelectedItem is PurchaseOrder po)
            {
                po.Items = await _db.GetPurchaseOrderItemsAsync(po.Id);
                var dlg = new PurchaseOrderDialog(_suppliers, po) { Owner = Window.GetWindow(this) };
                if (dlg.ShowDialog() == true)
                {
                    await _db.UpdatePurchaseOrderAsync(dlg.Result);
                    await TrySyncPurchaseOrderToEasybillAsync(dlg.Result, dlg.SaveAsDraft);
                }
                await LoadDataAsync();
            }
        }

        private void OrderFilter_Changed(object sender, RoutedEventArgs e)
        {
            _orderStatusFilter = (sender as RadioButton)?.Tag?.ToString() ?? "";
            RefreshUI();
        }

        private async Task TrySyncPurchaseOrderToEasybillAsync(PurchaseOrder order, bool isDraft = false)
        {
            if (_db == null) return;

            if (_easybill == null)
            {
                MessageBox.Show(
                    "Easybill ist nicht konfiguriert. Die Bestellung wurde lokal gespeichert.\n\n" +
                    "Bitte konfigurieren Sie Easybill unter Einstellungen, um die Synchronisation zu aktivieren.",
                    "Easybill nicht konfiguriert", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var supplier = _suppliers.FirstOrDefault(s => s.Id == order.SupplierId);
                var originalSupplierId = supplier?.EasybillCustomerId;

                var result = await _easybill.SyncPurchaseOrderToEasybillAsync(order, supplier, isDraft);

                if (result?.Id > 0)
                {
                    await _db.UpdatePurchaseOrderEasybillIdAsync(order.Id, result.Id.Value);
                    order.EasybillDocumentId = result.Id.Value;
                }

                if (!string.IsNullOrEmpty(result?.Number))
                {
                    await _db.UpdatePurchaseOrderNumberAsync(order.Id, result.Number);
                    order.OrderNumber = result.Number;
                }

                if (supplier != null && supplier.EasybillCustomerId.HasValue && !originalSupplierId.HasValue)
                    await _db.UpdateSupplierEasybillIdAsync(supplier.Id, supplier.EasybillCustomerId.Value);

                string nummer = string.IsNullOrEmpty(result?.Number) ? "(automatisch vergeben)" : result.Number;
                string modus = isDraft ? "als Entwurf" : "als finale Bestellung";
                MessageBox.Show(
                    $"✅ Bestellung erfolgreich {modus} in Easybill synchronisiert.\n\nEasybill-Nummer: {nummer}",
                    "Easybill Sync erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Easybill-Synchronisation fehlgeschlagen:\n\n{ex.Message}\n\n" +
                    "Die Bestellung wurde lokal gespeichert. Bitte prüfen Sie die Easybill-Konfiguration und versuchen Sie es erneut.",
                    "Easybill Sync Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Document Archive

        private async void NewDocument_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new PurchaseDocumentDialog(_suppliers) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                if (dlg.SelectedFileBytes != null)
                {
                    dlg.Result.FileData = dlg.SelectedFileBytes;
                    dlg.Result.LocalFilePath = SaveDocumentLocally(dlg.Result.OriginalFileName, dlg.SelectedFileBytes);
                }

                int newId = await _db.AddPurchaseDocumentAsync(dlg.Result);
                dlg.Result.Id = newId;

                if (_easybill != null && dlg.SelectedFileBytes != null)
                {
                    try
                    {
                        await TrySyncDocumentToEasybillAsync(dlg.Result, dlg.SelectedFileBytes);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Easybill-Synchronisation fehlgeschlagen:\n{ex.Message}",
                            "Easybill Sync Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                await LoadDataAsync();
            }
        }

        private async void EditDocument_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not PurchaseDocument doc) return;
            var dlg = new PurchaseDocumentDialog(_suppliers, doc) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                if (dlg.SelectedFileBytes != null)
                {
                    dlg.Result.FileData = dlg.SelectedFileBytes;
                    dlg.Result.LocalFilePath = SaveDocumentLocally(dlg.Result.OriginalFileName, dlg.SelectedFileBytes);
                }

                await _db.UpdatePurchaseDocumentAsync(dlg.Result);

                if (_easybill != null && dlg.SelectedFileBytes != null)
                {
                    try
                    {
                        await TrySyncDocumentToEasybillAsync(dlg.Result, dlg.SelectedFileBytes);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Easybill-Synchronisation fehlgeschlagen:\n{ex.Message}",
                            "Easybill Sync Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                await LoadDataAsync();
            }
        }

        private async void DeleteDocument_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not PurchaseDocument doc) return;
            if (MessageBox.Show($"Beleg '{doc.DocumentName}' löschen?", "Löschen",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            if (_easybill != null && doc.EasybillAttachmentId.HasValue)
            {
                try
                {
                    await _easybill.DeleteGlobalAttachmentAsync(doc.EasybillAttachmentId.Value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Beleg konnte in Easybill nicht gelöscht werden:\n{ex.Message}",
                        "Easybill Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            if (!string.IsNullOrEmpty(doc.LocalFilePath) && File.Exists(doc.LocalFilePath))
            {
                try { File.Delete(doc.LocalFilePath); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Datei-Löschung: {ex.Message}"); }
            }

            await _db.DeletePurchaseDocumentAsync(doc.Id);
            await LoadDataAsync();
        }

        private void OpenDocumentFile_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not PurchaseDocument doc) return;

            byte[]? bytes = null;
            string ext = Path.GetExtension(doc.OriginalFileName);
            if (string.IsNullOrEmpty(ext)) ext = ".pdf";

            // Primär: Bytes aus Datenbank
            if (doc.HasFileInDb)
            {
                bytes = doc.FileData;
            }
            // Fallback: lokale Datei (Altbestand)
            else if (!string.IsNullOrEmpty(doc.LocalFilePath) && File.Exists(doc.LocalFilePath))
            {
                try { bytes = File.ReadAllBytes(doc.LocalFilePath); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lokale Datei konnte nicht gelesen werden:\n{ex.Message}", "Fehler",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (bytes == null || bytes.Length == 0)
            {
                MessageBox.Show("Keine Datei vorhanden.\n\nBitte laden Sie die Datei erneut hoch.", "Datei nicht gefunden",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "ProjektsoftwareDocs");
                Directory.CreateDirectory(tempDir);
                var safeName = string.Concat((doc.OriginalFileName ?? $"Beleg{ext}").Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
                var tempPath = Path.Combine(tempDir, safeName);
                File.WriteAllBytes(tempPath, bytes);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tempPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Datei konnte nicht geöffnet werden:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SyncDocument_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not PurchaseDocument doc) return;
            if (_easybill == null)
            {
                MessageBox.Show("Easybill ist nicht konfiguriert.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            byte[]? fileBytes = null;
            if (doc.HasFileInDb)
            {
                fileBytes = doc.FileData;
            }
            else if (!string.IsNullOrEmpty(doc.LocalFilePath) && File.Exists(doc.LocalFilePath))
            {
                fileBytes = File.ReadAllBytes(doc.LocalFilePath);
            }

            if (fileBytes == null || fileBytes.Length == 0)
            {
                MessageBox.Show("Keine Datei vorhanden. Bitte laden Sie zuerst eine Datei hoch.", "Keine Datei",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                await TrySyncDocumentToEasybillAsync(doc, fileBytes);
                await LoadDataAsync();
                MessageBox.Show($"Beleg '{doc.DocumentName}' erfolgreich mit Easybill synchronisiert.", "Easybill Sync",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sync fehlgeschlagen:\n{ex.Message}", "Easybill Sync Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DocumentGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DocumentsGrid.SelectedItem is PurchaseDocument doc)
            {
                var dlg = new PurchaseDocumentDialog(_suppliers, doc) { Owner = Window.GetWindow(this) };
                if (dlg.ShowDialog() == true)
                {
                    if (dlg.SelectedFileBytes != null)
                    {
                        dlg.Result.FileData = dlg.SelectedFileBytes;
                        dlg.Result.LocalFilePath = SaveDocumentLocally(dlg.Result.OriginalFileName, dlg.SelectedFileBytes);
                    }

                    await _db.UpdatePurchaseDocumentAsync(dlg.Result);

                    if (_easybill != null && dlg.SelectedFileBytes != null)
                    {
                        try
                        {
                            await TrySyncDocumentToEasybillAsync(dlg.Result, dlg.SelectedFileBytes);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Easybill-Synchronisation fehlgeschlagen:\n{ex.Message}",
                                "Easybill Sync Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                await LoadDataAsync();
            }
        }

        private void DocumentFilter_Changed(object sender, RoutedEventArgs e)
        {
            _documentTypeFilter = (sender as RadioButton)?.Tag?.ToString() ?? "";
            RefreshUI();
        }

        private string SaveDocumentLocally(string originalFileName, byte[] fileData)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Projektsoftware", "PurchaseDocuments");
                Directory.CreateDirectory(dir);
                var safeName = string.Concat(originalFileName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
                var uniqueName = $"{DateTime.Now:yyyyMMddHHmmss}_{safeName}";
                var path = Path.Combine(dir, uniqueName);
                File.WriteAllBytes(path, fileData);
                return path;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lokale Speicherung fehlgeschlagen: {ex.Message}");
                return "";
            }
        }

        private async Task TrySyncDocumentToEasybillAsync(PurchaseDocument doc, byte[] fileBytes)
        {
            if (_easybill == null || _db == null) return;
            try
            {
                var supplier = _suppliers.FirstOrDefault(s => s.Id == doc.SupplierId);

                // Lieferant zuerst in Easybill synchronisieren
                if (supplier != null && !supplier.EasybillCustomerId.HasValue)
                {
                    var synced = await _easybill.SyncSupplierToEasybillAsync(supplier);
                    if (synced?.Id > 0)
                    {
                        supplier.EasybillCustomerId = synced.Id;
                        await _db.UpdateSupplierEasybillIdAsync(supplier.Id, synced.Id);
                    }
                }

                // Dateiname für Easybill zusammenstellen (ungültige Zeichen ersetzen)
                var ext = Path.GetExtension(doc.OriginalFileName);
                if (string.IsNullOrEmpty(ext)) ext = ".pdf";
                var rawName = $"{doc.DocumentType}_{doc.DocumentName}_{doc.DocumentDate:yyyyMMdd}";
                var safeName = string.Concat(rawName.Select(c => Path.GetInvalidFileNameChars().Contains(c) || c == ':' || c == '/' ? '_' : c));
                var ebFileName = safeName + ext;

                // Beleg zu Easybill hochladen – nur wenn noch nicht synchronisiert
                if (!doc.EasybillAttachmentId.HasValue)
                {
                    var attachment = await _easybill.UploadGlobalAttachmentAsync(ebFileName, fileBytes);
                    if (attachment?.Id > 0)
                    {
                        if (supplier?.EasybillCustomerId > 0)
                            await _easybill.UpdateAttachmentAsync(attachment.Id.Value, supplier.EasybillCustomerId);

                        await _db.UpdatePurchaseDocumentEasybillAttachmentIdAsync(doc.Id, attachment.Id.Value);
                        doc.EasybillAttachmentId = attachment.Id;
                        doc.EasybillSyncedAt = DateTime.Now;
                    }
                }

                // Beleg per E-Mail an Easybill-Belegerfassung senden (OCR) – immer bei neuem Upload
                await SendDocumentToEasybillBelegMailAsync(doc, fileBytes, ebFileName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dokument-Sync Fehler: {ex.Message}");
                throw;
            }
        }

        private async Task SendDocumentToEasybillBelegMailAsync(PurchaseDocument doc, byte[] fileBytes, string fileName)
        {
            var emailService = new ExchangeEmailService();
            if (!emailService.IsConfigured)
            {
                MessageBox.Show(
                    "E-Mail-Konto (SMTP) ist nicht konfiguriert.\nBitte unter Einstellungen → E-Mail einrichten.",
                    "Easybill Belegmail", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            const string easybillBelegEmail = "a0af9d17f3024149bfb14472dc4fb738@belege.easybill.de";

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var mimeType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".tiff" or ".tif" => "image/tiff",
                ".bmp" => "image/bmp",
                _ => "application/pdf"
            };

            var subject = $"Beleg: {doc.DocumentName} ({doc.DocumentDate:dd.MM.yyyy})";
            var body = $"<p>Eingangsbeleg vom {doc.DocumentDate:dd.MM.yyyy}</p>" +
                       $"<p>Bezeichnung: {doc.DocumentName}</p>" +
                       $"<p>Typ: {doc.DocumentType}</p>" +
                       (!string.IsNullOrWhiteSpace(doc.SupplierName) ? $"<p>Lieferant: {doc.SupplierName}</p>" : "");

            // Fehler werden nach oben geworfen und in den aufrufenden Methoden angezeigt
            await emailService.SendEmailAsync(
                to: easybillBelegEmail,
                subject: subject,
                body: body,
                attachmentFileName: fileName,
                attachmentBytes: fileBytes,
                attachmentMimeType: mimeType);
        }

        #endregion
    }
}
