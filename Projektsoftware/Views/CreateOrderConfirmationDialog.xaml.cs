using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Projektsoftware.Views
{
    public partial class CreateOrderConfirmationDialog : Window
    {
        private List<EasybillDocument> allOffers;
        private List<EasybillDocument> filteredOffers;
        private EasybillService easybillService;

        public EasybillDocument? CreatedDocument { get; private set; }
        public EasybillDocument? SelectedOffer { get; private set; }

        public CreateOrderConfirmationDialog()
        {
            InitializeComponent();
            easybillService = new EasybillService();
            allOffers = new List<EasybillDocument>();
            filteredOffers = new List<EasybillDocument>();
            LoadOffersAsync();
        }

        private async void LoadOffersAsync()
        {
            try
            {
                if (!easybillService.IsConfigured)
                {
                    MessageBox.Show(
                        "Easybill ist nicht konfiguriert!\n\nBitte konfigurieren Sie Easybill unter Einstellungen → Easybill → Konfiguration.",
                        "Nicht konfiguriert",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    DialogResult = false;
                    Close();
                    return;
                }

                // Lade alle Angebote
                var documents = await easybillService.GetAllDocumentsAsync("OFFER");

                if (documents == null)
                {
                    documents = new List<EasybillDocument>();
                }

                // Verarbeite Dokumente einzeln mit null-Checks
                var processedOffers = new List<EasybillDocument>();

                foreach (var doc in documents)
                {
                    // Überspringe null-Dokumente
                    if (doc == null)
                        continue;

                    // Überspringe stornierte und archivierte
                    if (doc.Status == "CANCELLED" || doc.IsArchive)
                        continue;

                    // Setze CustomerDisplay
                    if (doc.CustomerSnapshot != null)
                    {
                        if (!string.IsNullOrWhiteSpace(doc.CustomerSnapshot.CompanyName))
                        {
                            doc.CustomerDisplay = doc.CustomerSnapshot.CompanyName;
                        }
                        else
                        {
                            var firstName = doc.CustomerSnapshot.FirstName ?? "";
                            var lastName = doc.CustomerSnapshot.LastName ?? "";
                            var fullName = $"{firstName} {lastName}".Trim();
                            doc.CustomerDisplay = string.IsNullOrWhiteSpace(fullName) 
                                ? "Unbekannter Kunde" 
                                : fullName;
                        }
                    }
                    else
                    {
                        doc.CustomerDisplay = "Unbekannter Kunde";
                    }

                    processedOffers.Add(doc);
                }

                // Sortiere nach Datum (neueste zuerst) - nur wenn Liste nicht leer
                if (processedOffers != null && processedOffers.Any())
                {
                    try
                    {
                        allOffers = processedOffers
                            .Where(d => d != null)
                            .OrderByDescending(d => 
                            {
                                // Sichere Datums-Sortierung
                                if (d == null || string.IsNullOrWhiteSpace(d.DocumentDate))
                                    return DateTime.MinValue;

                                if (DateTime.TryParse(d.DocumentDate, out DateTime date))
                                    return date;

                                return DateTime.MinValue;
                            })
                            .ToList();
                    }
                    catch (Exception sortEx)
                    {
                        // Fallback: keine Sortierung, einfach die Liste verwenden
                        MessageBox.Show($"Warnung: Sortierung fehlgeschlagen: {sortEx.Message}\nAngebote werden unsortiert angezeigt.", 
                            "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning);
                        allOffers = processedOffers;
                    }
                }
                else
                {
                    allOffers = new List<EasybillDocument>();
                }

                filteredOffers = allOffers.ToList();

                if (!allOffers.Any())
                {
                    MessageBox.Show(
                        "Es wurden keine aktiven Angebote gefunden!\n\n" +
                        "Erstellen Sie zuerst ein Angebot, aus dem eine Auftragsbestätigung generiert werden kann.",
                        "Keine Angebote",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    DialogResult = false;
                    Close();
                    return;
                }

                OffersDataGrid.ItemsSource = filteredOffers;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Laden der Angebote:\n\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                DialogResult = false;
                Close();
            }
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Sicherheitscheck: falls allOffers noch nicht initialisiert
                if (allOffers == null)
                {
                    return;
                }

                var searchText = SearchTextBox?.Text ?? "";

                if (string.IsNullOrWhiteSpace(searchText) || searchText == "Suchen...")
                {
                    filteredOffers = allOffers.ToList();
                }
                else
                {
                    filteredOffers = allOffers.Where(o =>
                        o != null && (
                            (o.Number?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (o.CustomerDisplay?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (o.Id?.ToString()?.Contains(searchText) ?? false)
                        )
                    ).ToList();
                }

                if (OffersDataGrid != null)
                {
                    OffersDataGrid.ItemsSource = filteredOffers;
                }
            }
            catch (Exception ex)
            {
                // Verhindere, dass Suchfehler die Anwendung zum Absturz bringen
                System.Diagnostics.Debug.WriteLine($"Fehler in Search_TextChanged: {ex.Message}");
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Suchen...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Suchen...";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void OffersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedOffer = OffersDataGrid.SelectedItem as EasybillDocument;
            CreateButton.IsEnabled = SelectedOffer != null;
        }

        private void OffersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (OffersDataGrid.SelectedItem != null)
            {
                Create_Click(sender, e);
            }
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedOffer == null)
            {
                MessageBox.Show(
                    "Bitte wählen Sie ein Angebot aus der Liste aus.",
                    "Kein Angebot ausgewählt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                CreateButton.IsEnabled = false;
                CreateButton.Content = "Wird erstellt...";

                var confirmationText = string.IsNullOrWhiteSpace(ConfirmationTextBox.Text) 
                    ? null 
                    : ConfirmationTextBox.Text;

                var isDraft = IsDraftCheckBox.IsChecked == true;

                CreatedDocument = await easybillService.CreateOrderConfirmationFromOfferAsync(
                    SelectedOffer.Id ?? 0,
                    confirmationText,
                    isDraft);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Erstellen der Auftragsbestätigung:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                CreateButton.IsEnabled = true;
                CreateButton.Content = "Auftragsbestätigung erstellen";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
