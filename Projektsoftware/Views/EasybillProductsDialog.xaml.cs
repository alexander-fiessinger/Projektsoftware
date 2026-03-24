using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
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
