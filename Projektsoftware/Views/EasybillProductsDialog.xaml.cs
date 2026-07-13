using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class EasybillProductsDialog : Window
    {
        private readonly EasybillService easybillService;
        private ObservableCollection<EasybillProduct> products;

        public EasybillProductsDialog()
        {
            InitializeComponent();
            easybillService = new EasybillService();
            products = new ObservableCollection<EasybillProduct>();
            ProductsDataGrid.ItemsSource = products;

            Loaded += async (s, e) => await LoadProductsAsync();

            App.ProductSyncCompleted += OnProductSyncCompleted;
            Closed += (s, e) => App.ProductSyncCompleted -= OnProductSyncCompleted;
        }

        private void OnProductSyncCompleted(Services.ProductSyncService.SyncResult result)
        {
            // Aus dem Hintergrund-Timer -> auf den UI-Thread marshallen
            Dispatcher.Invoke(() =>
            {
                if (result.Skipped)
                    return;
                SyncStatusTextBlock.Text = $"Portal-Katalog: {result.Message} ({DateTime.Now:HH:mm})";
            });
        }

        private async void SyncToLocal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsEnabled = false;
                StatusTextBlock.Text = "Synchronisiere mit Portal-Katalog...";

                var result = await new Services.ProductSyncService().SyncAsync();

                SyncStatusTextBlock.Text = $"Portal-Katalog: {result.Message} ({DateTime.Now:HH:mm})";

                MessageBox.Show(
                    result.Message,
                    result.Success ? "Synchronisation abgeschlossen" : "Synchronisation",
                    MessageBoxButton.OK,
                    result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);

                StatusTextBlock.Text = "Bereit";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler bei der Synchronisation:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                StatusTextBlock.Text = "Fehler bei der Synchronisation";
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task LoadProductsAsync()
        {
            try
            {
                StatusTextBlock.Text = "Lade Produkte...";
                products.Clear();

                if (!easybillService.IsConfigured)
                {
                    MessageBox.Show(
                        "Easybill ist nicht konfiguriert!\n\nBitte konfigurieren Sie zuerst die Easybill-API unter:\nEinstellungen → Easybill-Konfiguration",
                        "Nicht konfiguriert",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    StatusTextBlock.Text = "Nicht konfiguriert";
                    return;
                }

                var loadedProducts = await easybillService.GetAllProductsAsync();
                
                foreach (var product in loadedProducts.Where(p => !p.IsArchived).OrderBy(p => p.Number))
                {
                    products.Add(product);
                }

                ProductCountTextBlock.Text = $"📦 {products.Count} Produkt(e) geladen";
                StatusTextBlock.Text = "Bereit";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Laden der Produkte:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                StatusTextBlock.Text = "Fehler beim Laden";
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadProductsAsync();
        }

        private async void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProductDialog();
            if (dialog.ShowDialog() == true)
            {
                await LoadProductsAsync();
            }
        }

        private async void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (!easybillService.IsConfigured)
            {
                MessageBox.Show(
                    "Easybill ist nicht konfiguriert!\n\nBitte konfigurieren Sie zuerst die Easybill-API unter:\nEinstellungen → Easybill-Konfiguration",
                    "Nicht konfiguriert",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "CSV-Datei für Artikel-Import auswählen",
                Filter = "CSV-Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog(this) != true)
                return;

            var csvPath = openFileDialog.FileName;

            List<Services.CsvProductImportService.CsvProductRow> rows;
            try
            {
                rows = Services.CsvProductImportService.ParseCsv(csvPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Die CSV-Datei konnte nicht eingelesen werden:\n\n{ex.Message}\n\n" +
                    "Erwartete Spalten: Number;Description;SalePrice;PurchasePrice;Unit;VatPercent;Type;Note",
                    "Ungültige CSV-Datei",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (rows.Count == 0)
            {
                MessageBox.Show(
                    "Die CSV-Datei enthält keine Datenzeilen.",
                    "Keine Daten",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Es werden {rows.Count} Artikel aus der Datei\n{csvPath}\nin Easybill angelegt bzw. aktualisiert.\n\nFortfahren?",
                "CSV-Import",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                IsEnabled = false;
                StatusTextBlock.Text = "Starte CSV-Import...";

                var importer = new Services.CsvProductImportService(easybillService);
                var progress = new Progress<string>(msg => StatusTextBlock.Text = msg);
                var result = await importer.ImportAsync(csvPath, progress);

                var summary =
                    $"CSV-Import abgeschlossen:\n\n" +
                    $"• Neu erstellt: {result.Created}\n" +
                    $"• Aktualisiert: {result.Updated}\n" +
                    $"• Fehler: {result.Failed}\n" +
                    $"• Gesamt: {result.Total}";

                if (result.Errors.Count > 0)
                {
                    summary += "\n\nFehlerdetails (max. 10):\n" +
                               string.Join("\n", result.Errors.Take(10));
                }

                MessageBox.Show(
                    summary,
                    "Import abgeschlossen",
                    MessageBoxButton.OK,
                    result.Failed > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim CSV-Import:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                StatusTextBlock.Text = "Fehler beim Import";
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private async void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button?.DataContext as EasybillProduct;

            if (product?.Id == null)
                return;

            var dialog = new ProductDialog(product);
            if (dialog.ShowDialog() == true)
            {
                await LoadProductsAsync();
            }
        }

        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button?.DataContext as EasybillProduct;
            
            if (product?.Id == null)
                return;

            var result = MessageBox.Show(
                $"Möchten Sie das Produkt wirklich löschen?\n\n" +
                $"Nummer: {product.Number}\n" +
                $"Beschreibung: {product.Description}\n\n" +
                "Diese Aktion kann nicht rückgängig gemacht werden!",
                "Produkt löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    StatusTextBlock.Text = $"Lösche Produkt {product.Number}...";

                    await easybillService.DeleteProductAsync(product.Id.Value);

                    MessageBox.Show(
                        $"Produkt {product.Number} wurde erfolgreich gelöscht!",
                        "Erfolg",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    products.Remove(product);
                    ProductCountTextBlock.Text = $"📦 {products.Count} Produkt(e) geladen";
                    StatusTextBlock.Text = "Bereit";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Fehler beim Löschen:\n\n{ex.Message}",
                        "Fehler",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    StatusTextBlock.Text = "Fehler beim Löschen";
                }
            }
        }
    }
}
