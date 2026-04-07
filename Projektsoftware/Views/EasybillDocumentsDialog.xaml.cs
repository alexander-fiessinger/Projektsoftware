using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Projektsoftware.Views
{
    public partial class EasybillDocumentsDialog : Window
    {
        private readonly EasybillService easybillService;
        private ObservableCollection<EasybillDocument> documents;

        public EasybillDocumentsDialog()
        {
            InitializeComponent();
            easybillService = new EasybillService();
            documents = new ObservableCollection<EasybillDocument>();
            DocumentsDataGrid.ItemsSource = documents;

            Loaded += async (s, e) => await LoadDocumentsAsync();
        }

        private async System.Threading.Tasks.Task LoadDocumentsAsync(string? type = null)
        {
            try
            {
                StatusTextBlock.Text = "Lade Dokumente...";
                documents.Clear();

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

                var loadedDocuments = await easybillService.GetAllDocumentsAsync(type);
                
                foreach (var doc in loadedDocuments.OrderByDescending(d => d.DocumentDate))
                {
                    documents.Add(doc);
                }

                DocumentCountTextBlock.Text = $"📄 {documents.Count} Dokument(e) geladen";
                StatusTextBlock.Text = "Bereit";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Laden der Dokumente:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                StatusTextBlock.Text = "Fehler beim Laden";
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = TypeFilterComboBox.SelectedItem as ComboBoxItem;
            var type = selectedItem?.Tag?.ToString();
            await LoadDocumentsAsync(string.IsNullOrEmpty(type) ? null : type);
        }

        private async void TypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                var selectedItem = TypeFilterComboBox.SelectedItem as ComboBoxItem;
                var type = selectedItem?.Tag?.ToString();
                await LoadDocumentsAsync(string.IsNullOrEmpty(type) ? null : type);
            }
        }

        private void CreateDocument_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private async void CreateDocumentType_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var documentType = menuItem?.Tag?.ToString();

            if (string.IsNullOrEmpty(documentType))
                return;

            try
            {
                var dialog = new CreateEasybillDocumentDialog(documentType);
                if (dialog.ShowDialog() == true)
                {
                    // Dokument wurde erfolgreich erstellt, Liste aktualisieren
                    var selectedItem = TypeFilterComboBox.SelectedItem as ComboBoxItem;
                    var type = selectedItem?.Tag?.ToString();
                    await LoadDocumentsAsync(string.IsNullOrEmpty(type) ? null : type);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void DownloadPdf_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var document = button?.DataContext as EasybillDocument;
            
            if (document?.Id == null)
                return;

            try
            {
                StatusTextBlock.Text = $"Lade PDF für Dokument {document.Number}...";

                var pdfData = await easybillService.DownloadDocumentPdfAsync(document.Id.Value);

                // Dateidialog zum Speichern
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PDF Dateien (*.pdf)|*.pdf",
                    FileName = $"{document.Type}_{document.Number}.pdf",
                    DefaultExt = "pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(saveDialog.FileName, pdfData);
                    
                    MessageBox.Show(
                        $"PDF erfolgreich gespeichert:\n\n{saveDialog.FileName}",
                        "Erfolg",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    StatusTextBlock.Text = "PDF erfolgreich heruntergeladen";
                }
                else
                {
                    StatusTextBlock.Text = "Bereit";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Download des PDFs:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                StatusTextBlock.Text = "Fehler beim PDF-Download";
            }
        }

        private async void SendDocument_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var document = button?.DataContext as EasybillDocument;
            
            if (document?.Id == null)
                return;

            var exchangeConfig = ExchangeConfig.Load();
            if (!exchangeConfig.IsConfigured)
            {
                MessageBox.Show(
                    "SMTP ist nicht konfiguriert.\nBitte zuerst unter Einstellungen → SMTP E-Mail → Konfiguration einrichten.",
                    "SMTP nicht konfiguriert",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var dialog = new EasybillSendEmailDialog(document);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    StatusTextBlock.Text = $"Sende Dokument {document.Number}...";

                    byte[] pdfBytes = null;
                    try { pdfBytes = await easybillService.DownloadDocumentPdfAsync(document.Id.Value); } catch { }
                    var pdfFileName = $"{document.DisplayType}_{document.Number?.Replace("/", "-") ?? "Dokument"}.pdf";

                    await new ExchangeEmailService(exchangeConfig).SendEmailAsync(
                        dialog.To, dialog.EmailSubject, dialog.Message,
                        dialog.Cc, dialog.Bcc, pdfFileName, pdfBytes);

                    MessageBox.Show(
                        $"Dokument {document.Number} wurde erfolgreich per E-Mail versendet!",
                        "Erfolg",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    StatusTextBlock.Text = "Dokument erfolgreich versendet";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Fehler beim Versenden:\n\n{ex.Message}",
                        "Fehler",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    StatusTextBlock.Text = "Fehler beim Versenden";
                }
            }
        }

        private async void MarkAsPaid_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var document = button?.DataContext as EasybillDocument;
            
            if (document?.Id == null)
                return;

            var result = MessageBox.Show(
                $"Möchten Sie Dokument '{document.Number}' als bezahlt markieren?\n\n" +
                $"Betrag: {document.TotalGrossDisplay}\n" +
                $"Datum: {DateTime.Now:dd.MM.yyyy}",
                "Als bezahlt markieren",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    StatusTextBlock.Text = $"Markiere Dokument {document.Number} als bezahlt...";

                    await easybillService.MarkDocumentAsPaidAsync(document.Id.Value);

                    MessageBox.Show(
                        $"Dokument {document.Number} wurde als bezahlt markiert!",
                        "Erfolg",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Refresh data
                    var selectedItem = TypeFilterComboBox.SelectedItem as ComboBoxItem;
                    var type = selectedItem?.Tag?.ToString();
                    await LoadDocumentsAsync(string.IsNullOrEmpty(type) ? null : type);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Fehler beim Markieren:\n\n{ex.Message}",
                        "Fehler",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    StatusTextBlock.Text = "Fehler";
                }
            }
        }

        private async void FinalizeDocument_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var document = button?.DataContext as EasybillDocument;

            if (document?.Id == null)
                return;

            var result = MessageBox.Show(
                $"Möchten Sie den Entwurf '{document.Number ?? "(ohne Nummer)"}' fertigstellen?\n\n" +
                $"Typ: {document.DisplayType}\n" +
                $"Betrag: {document.TotalGrossDisplay}\n\n" +
                "Das Dokument wird finalisiert und erhält eine fortlaufende Nummer.\n" +
                "Dieser Vorgang kann nicht rückgängig gemacht werden!",
                "Entwurf fertigstellen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    StatusTextBlock.Text = $"Stelle Entwurf {document.Number ?? document.Id.ToString()} fertig...";

                    var finalized = await easybillService.FinalizeDocumentAsync(document.Id.Value);

                    MessageBox.Show(
                        $"Dokument wurde erfolgreich fertiggestellt!\n\nNummer: {finalized.Number}",
                        "Fertiggestellt",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    var selectedItem = TypeFilterComboBox.SelectedItem as ComboBoxItem;
                    var type = selectedItem?.Tag?.ToString();
                    await LoadDocumentsAsync(string.IsNullOrEmpty(type) ? null : type);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Fehler beim Fertigstellen des Entwurfs:\n\n{ex.Message}",
                        "Fehler",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    StatusTextBlock.Text = "Fehler beim Fertigstellen";
                }
            }
        }

        private async void DeleteDocument_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var document = button?.DataContext as EasybillDocument;

            if (document?.Id == null)
                return;

            var result = MessageBox.Show(
                $"Möchten Sie Dokument '{document.Number}' wirklich löschen?\n\n" +
                $"Typ: {document.DisplayType}\n" +
                $"Betrag: {document.TotalGrossDisplay}\n\n" +
                "Diese Aktion kann nicht rückgängig gemacht werden!",
                "Dokument löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    StatusTextBlock.Text = $"Lösche Dokument {document.Number}...";

                    await easybillService.DeleteDocumentAsync(document.Id.Value);

                    MessageBox.Show(
                        $"Dokument {document.Number} wurde erfolgreich gelöscht!",
                        "Erfolg",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    documents.Remove(document);
                    DocumentCountTextBlock.Text = $"📄 {documents.Count} Dokument(e) geladen";
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

        private async void Convert_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var document = button?.DataContext as EasybillDocument;

            if (document?.Id == null)
                return;

            if (!ConvertDocumentDialog.HasConversions(document.Type))
            {
                MessageBox.Show(
                    $"Für '{document.DisplayType}' sind keine Konvertierungen verfügbar.",
                    "Nicht möglich",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dialog = new ConvertDocumentDialog(document);
            if (dialog.ShowDialog() != true)
                return;

            try
            {
                StatusTextBlock.Text = "Lade Dokumentdetails...";
                var fullDocument = await easybillService.GetDocumentAsync(document.Id.Value);

                StatusTextBlock.Text = $"Konvertiere zu {new EasybillDocument { Type = dialog.TargetType }.DisplayType}...";
                var converted = await easybillService.ConvertDocumentAsync(fullDocument, dialog.TargetType, dialog.CreateAsDraft);

                MessageBox.Show(
                    $"Dokument erfolgreich als {converted.DisplayType} erstellt!\n\n" +
                    $"Neue Nummer: {converted.Number ?? "(Entwurf)"}\n" +
                    (dialog.CreateAsDraft ? "Das Dokument wurde als Entwurf erstellt." : "Das Dokument wurde finalisiert."),
                    "Erfolg",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                var selectedItem = TypeFilterComboBox.SelectedItem as ComboBoxItem;
                var type = selectedItem?.Tag?.ToString();
                await LoadDocumentsAsync(string.IsNullOrEmpty(type) ? null : type);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Konvertieren:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                StatusTextBlock.Text = "Fehler beim Konvertieren";
            }
        }
    }
}
