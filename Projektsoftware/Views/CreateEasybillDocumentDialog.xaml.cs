using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Projektsoftware.Views
{
    public partial class CreateEasybillDocumentDialog : Window
    {
        private readonly EasybillService easybillService;
        private List<EasybillCustomer>? customers;
        private List<EasybillProject>? projects;
        private ObservableCollection<EasybillDocumentItem> items;
        private VatResult? currentVatResult;
        private EasybillDocument? editDocument; // != null im Edit-Modus

        public EasybillDocument CreatedDocument { get; private set; } = null!;

        // Neu erstellen
        public CreateEasybillDocumentDialog(string documentType = "INVOICE")
        {
            InitializeComponent();
            easybillService = new EasybillService();
            items = new ObservableCollection<EasybillDocumentItem>();
            ItemsDataGrid.ItemsSource = items;
            items.CollectionChanged += (s, e) => UpdateTotals();
            SetDocumentType(documentType);
            Loaded += async (s, e) => await LoadDataAsync();
        }

        // Bearbeiten
        public CreateEasybillDocumentDialog(EasybillDocument document)
        {
            InitializeComponent();
            easybillService = new EasybillService();
            editDocument = document;
            items = new ObservableCollection<EasybillDocumentItem>();
            ItemsDataGrid.ItemsSource = items;
            items.CollectionChanged += (s, e) => UpdateTotals();
            CreateButton.Content = "💾 Speichern";
            Title = "📄 Dokument bearbeiten";
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private void SetDocumentType(string type)
        {
            foreach (ComboBoxItem item in TypeComboBox.Items)
            {
                if (item.Tag?.ToString() == type)
                {
                    TypeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                CreateButton.IsEnabled = false;

                // Lade Kunden
                customers = await easybillService.GetAllCustomersAsync();

                if (!customers.Any())
                {
                    MessageBox.Show(
                        "Es sind keine Kunden vorhanden.\nBitte erstellen Sie zuerst einen Kunden in Easybill.",
                        "Keine Kunden",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    Close();
                    return;
                }

                CustomerComboBox.ItemsSource = customers;
                CustomerComboBox.SelectedIndex = 0;

                // Lade Projekte
                projects = await easybillService.GetAllProjectsAsync();

                var projectItems = new List<object> { new { Id = (long?)null, Name = "(kein Projekt)" } };
                projectItems.AddRange(projects.Select(p => new { Id = (long?)p.Id, Name = p.Name }));

                ProjectComboBox.ItemsSource = projectItems;
                ProjectComboBox.SelectedIndex = 0;

                // Edit-Modus: Felder mit vorhandenen Werten befüllen
                if (editDocument != null)
                {
                    SetDocumentType(editDocument.Type ?? "INVOICE");
                    TypeComboBox.IsEnabled = false; // Typ nicht änderbar

                    var customer = customers.FirstOrDefault(c => c.Id == editDocument.CustomerId);
                    if (customer != null)
                        CustomerComboBox.SelectedItem = customer;

                    if (editDocument.ProjectId.HasValue)
                    {
                        foreach (var p in projectItems)
                        {
                            var val = p.GetType().GetProperty("Id")?.GetValue(p);
                            if (val is long lid && lid == editDocument.ProjectId.Value)
                            {
                                ProjectComboBox.SelectedItem = p;
                                break;
                            }
                        }
                    }

                    if (DateTime.TryParse(editDocument.DocumentDate, out var docDate))
                        DocumentDatePicker.SelectedDate = docDate;

                    TitleTextBox.Text       = editDocument.Title ?? "";
                    SubjectTextBox.Text     = editDocument.Subject ?? "";
                    TextTextBox.Text        = editDocument.TextPrefix ?? "";
                    TextSuffixTextBox.Text  = editDocument.Text ?? "";
                    DueInDaysTextBox.Text   = (editDocument.DueInDays ?? 14).ToString();
                    IsDraftCheckBox.IsChecked = editDocument.IsDraft;

                    if (!string.IsNullOrEmpty(editDocument.Currency))
                    {
                        foreach (ComboBoxItem ci in CurrencyComboBox.Items)
                            if (ci.Tag?.ToString() == editDocument.Currency) { CurrencyComboBox.SelectedItem = ci; break; }
                    }

                    if (editDocument.Items != null)
                        foreach (var item in editDocument.Items)
                            items.Add(new EasybillDocumentItem
                            {
                                Id              = item.Id,
                                Number          = item.Number,
                                Description     = item.Description,
                                Quantity        = item.Quantity,
                                Unit            = item.Unit,
                                Type            = item.Type,
                                Position        = item.Position,
                                SinglePriceNet  = item.SinglePriceNet,
                                SinglePriceGross= item.SinglePriceGross,
                                VatPercent      = item.VatPercent,
                                Discount        = item.Discount,
                                DiscountType    = item.DiscountType,
                                PositionId      = item.PositionId,
                            });

                    UpdateTotals();
                }

                CreateButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Laden der Daten:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
            }
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HeaderTextBlock == null) return;

            var selectedItem = TypeComboBox.SelectedItem as ComboBoxItem;
            var content = selectedItem?.Content?.ToString();

            if (!string.IsNullOrEmpty(content))
            {
                // Entferne Emoji
                var documentType = content.Replace("📄", "").Replace("📋", "")
                    .Replace("✓", "").Replace("📦", "").Replace("💳", "").Trim();
                HeaderTextBlock.Text = $"{documentType} erstellen";
            }
        }

        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCustomer = CustomerComboBox.SelectedItem as EasybillCustomer;
            currentVatResult = VatService.DetermineVat(selectedCustomer);

            if (currentVatResult != null && currentVatResult.Scenario != VatScenario.Inland)
            {
                VatInfoBorder.Visibility = Visibility.Visible;
                VatInfoBorder.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(currentVatResult.InfoColor));
                VatInfoTextBlock.Text = currentVatResult.DisplayText;
                VatDetailTextBlock.Text = currentVatResult.LegalNotice;

                // Schlusstext automatisch setzen wenn leer oder Standard
                if (string.IsNullOrWhiteSpace(TextSuffixTextBox.Text) && !string.IsNullOrEmpty(currentVatResult.DocumentSuffix))
                {
                    TextSuffixTextBox.Text = currentVatResult.DocumentSuffix;
                }
            }
            else
            {
                VatInfoBorder.Visibility = Visibility.Collapsed;
                // Schlusstext zurücksetzen wenn es ein automatischer war
                if (currentVatResult != null && string.IsNullOrEmpty(currentVatResult.DocumentSuffix))
                {
                    // Nur leeren wenn der aktuelle Text ein steuerrechtlicher Text war
                    var currentSuffix = TextSuffixTextBox.Text;
                    if (currentSuffix.Contains("Reverse Charge") ||
                        currentSuffix.Contains("§4 Nr. 1") ||
                        currentSuffix.Contains("Ausfuhrlieferung") ||
                        currentSuffix.Contains("§19 UStG"))
                    {
                        TextSuffixTextBox.Text = "";
                    }
                }
            }

            // Bestehende Positionen aktualisieren
            if (items.Count > 0 && currentVatResult != null)
            {
                var result = MessageBox.Show(
                    $"Der MwSt-Satz hat sich geändert ({currentVatResult.VatPercent}% - {currentVatResult.DisplayText}).\n\n" +
                    "Sollen die bestehenden Positionen aktualisiert werden?",
                    "MwSt aktualisieren",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    foreach (var item in items)
                    {
                        item.VatPercent = currentVatResult.VatPercent;
                        if (item.SinglePriceNet.HasValue)
                        {
                            var netTotal = item.SinglePriceNet.Value * item.Quantity;
                            var vatAmount = netTotal * (item.VatPercent / 100m);
                            item.TotalPriceNet = netTotal;
                            item.TotalPriceGross = netTotal + vatAmount;
                            item.SinglePriceGross = item.SinglePriceNet.Value + (item.SinglePriceNet.Value * item.VatPercent / 100m);
                        }
                    }
                    UpdateTotals();
                }
            }
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddDocumentItemDialog(currentVatResult);
            if (dialog.ShowDialog() == true && dialog.Item != null)
            {
                dialog.Item.Position = items.Count + 1;
                items.Add(dialog.Item);
                UpdateTotals();
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.DataContext as EasybillDocumentItem;

            if (item != null)
            {
                items.Remove(item);

                // Positionen neu nummerieren
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].Position = i + 1;
                }

                UpdateTotals();
            }
        }

        private void UpdateTotals()
        {
            decimal netTotal = 0;
            decimal vatTotal = 0;

            foreach (var item in items)
            {
                if (item.SinglePriceNet.HasValue && item.Quantity > 0)
                {
                    var itemNetTotal = item.SinglePriceNet.Value * item.Quantity;
                    item.TotalPriceNet = itemNetTotal;

                    var itemVatAmount = itemNetTotal * (item.VatPercent / 100m);
                    item.TotalPriceGross = itemNetTotal + itemVatAmount;

                    netTotal += itemNetTotal;
                    vatTotal += itemVatAmount;
                }
            }

            var grossTotal = netTotal + vatTotal;

            NetTotalTextBlock.Text = $"{netTotal:N2} €";
            VatTotalTextBlock.Text = $"{vatTotal:N2} €";
            GrossTotalTextBlock.Text = $"{grossTotal:N2} €";

            // DataGrid aktualisieren
            ItemsDataGrid.Items.Refresh();
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validierung
                if (CustomerComboBox.SelectedItem == null)
                {
                    MessageBox.Show(
                        "Bitte wählen Sie einen Kunden aus.",
                        "Validierung",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (DocumentDatePicker.SelectedDate == null)
                {
                    MessageBox.Show(
                        "Bitte wählen Sie ein Dokumentdatum aus.",
                        "Validierung",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Zahlungsziel validieren
                if (!int.TryParse(DueInDaysTextBox.Text, out int dueInDays) || dueInDays < 0)
                {
                    MessageBox.Show(
                        "Bitte geben Sie ein gültiges Zahlungsziel ein (0 oder mehr Tage).",
                        "Validierung",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (items.Count == 0)
                {
                    var result = MessageBox.Show(
                        "Sie haben keine Positionen hinzugefügt.\n\nMöchten Sie trotzdem fortfahren?",
                        "Keine Positionen",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                        return;
                }

                CreateButton.IsEnabled = false;

                var selectedCustomer = CustomerComboBox.SelectedItem as EasybillCustomer;
                var selectedProjectItem = ProjectComboBox.SelectedItem;
                var projectId = selectedProjectItem?.GetType().GetProperty("Id")?.GetValue(selectedProjectItem) as long?;
                var selectedType = (TypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                var selectedCurrency = (CurrencyComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();

                var document = new EasybillDocument
                {
                    Type       = selectedType,
                    CustomerId = selectedCustomer!.Id,
                    ProjectId  = projectId,
                    DocumentDate  = DocumentDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd"),
                    Title      = string.IsNullOrWhiteSpace(TitleTextBox.Text) ? null : TitleTextBox.Text,
                    Subject    = string.IsNullOrWhiteSpace(SubjectTextBox.Text) ? null : SubjectTextBox.Text,
                    TextPrefix = string.IsNullOrWhiteSpace(TextTextBox.Text) ? null : TextTextBox.Text,
                    Text       = string.IsNullOrWhiteSpace(TextSuffixTextBox.Text) ? null : TextSuffixTextBox.Text,
                    Currency   = selectedCurrency,
                    DueInDays  = dueInDays,
                    IsDraft    = IsDraftCheckBox.IsChecked == true,
                    Items      = items.ToArray()
                };

                var typeName = (TypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()
                    ?.Replace("📄", "").Replace("📋", "").Replace("✓", "")
                    .Replace("📦", "").Replace("💳", "").Trim() ?? "Dokument";

                if (editDocument != null)
                {
                    // Bearbeiten: PUT /documents/{id}
                    CreatedDocument = await easybillService.UpdateDocumentAsync(editDocument.Id!.Value, document);

                    // Wenn nicht als Entwurf gewünscht und das Dokument noch ein Entwurf ist,
                    // abschließen, damit Easybill eine Nummer vergibt.
                    if (!document.IsDraft && CreatedDocument.IsDraft && CreatedDocument.Id.HasValue)
                    {
                        CreatedDocument = await easybillService.FinalizeDocumentAsync(CreatedDocument.Id.Value);
                    }

                    MessageBox.Show(
                        $"{typeName} erfolgreich gespeichert!\n\n" +
                        $"Nummer: {CreatedDocument.Number ?? "(Entwurf)"}\n" +
                        $"Status: {CreatedDocument.DisplayStatus}\n" +
                        $"Kunde: {selectedCustomer.DisplayName}",
                        "Gespeichert",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    // Neu erstellen: POST /documents (Easybill legt immer einen Entwurf an)
                    CreatedDocument = await easybillService.CreateDocumentAsync(document);

                    // Wenn nicht als Entwurf gewünscht, Dokument abschließen → Easybill vergibt die Nummer.
                    if (!document.IsDraft && CreatedDocument.IsDraft && CreatedDocument.Id.HasValue)
                    {
                        CreatedDocument = await easybillService.FinalizeDocumentAsync(CreatedDocument.Id.Value);
                    }

                    MessageBox.Show(
                        $"{typeName} erfolgreich erstellt!\n\n" +
                        $"Nummer: {CreatedDocument.Number ?? "(Entwurf)"}\n" +
                        $"Status: {CreatedDocument.DisplayStatus}\n" +
                        $"Kunde: {selectedCustomer.DisplayName}\n" +
                        $"Positionen: {items.Count}\n" +
                        $"Gesamtbetrag: {CreatedDocument.TotalGross:N2} {selectedCurrency}",
                        "Erfolg",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Speichern des Dokuments:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                CreateButton.IsEnabled = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
