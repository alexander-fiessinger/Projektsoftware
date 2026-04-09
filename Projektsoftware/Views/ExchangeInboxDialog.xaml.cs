using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class ExchangeInboxDialog : Window
    {
        private readonly DatabaseService _db;
        private readonly EwsConfig _config;
        private List<Customer> _customers = new();
        private List<InboxEmail> _emails = new();

        public ExchangeInboxDialog()
        {
            InitializeComponent();
            _db = new DatabaseService();
            _config = EwsConfig.Load();
            Loaded += async (s, e) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                StatusTextBlock.Text = "⏳ Lade Kundendaten...";
                _customers = await _db.GetAllCustomersAsync();
                await FetchEmailsAsync();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"❌ Fehler: {ex.Message}";
            }
        }

        private async Task FetchEmailsAsync()
        {
            if (!_config.IsConfigured)
            {
                StatusTextBlock.Text = "⚠ Exchange EWS nicht konfiguriert. Bitte unter Einstellungen → Exchange Posteingang → EWS-Konfiguration einrichten.";
                return;
            }

            StatusTextBlock.Text = "⏳ Lade E-Mails aus dem Posteingang...";
            try
            {
                var service = new EwsService(_config);
                _emails = await service.FetchInboxEmailsAsync(_customers);
                EmailsDataGrid.ItemsSource = null;
                EmailsDataGrid.ItemsSource = _emails;

                int unread = _emails.FindAll(m => !m.IsRead).Count;
                int matched = _emails.FindAll(m => m.MatchedCustomer != null).Count;
                StatusTextBlock.Text =
                    $"✅ {_emails.Count} E-Mails geladen  |  " +
                    $"{unread} ungelesen  |  " +
                    $"{matched} Kunden zugeordnet  |  " +
                    $"Stand: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"❌ Fehler beim Laden: {ex.Message}";
                MessageBox.Show(
                    $"Fehler beim Abrufen der E-Mails:\n\n{ex.Message}",
                    "IMAP-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await FetchEmailsAsync();
        }

        private void EmailsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview(EmailsDataGrid.SelectedItem as InboxEmail);
        }

        private void UpdatePreview(InboxEmail? email)
        {
            if (email == null)
            {
                PreviewSubjectText.Text = string.Empty;
                PreviewFromText.Text = string.Empty;
                PreviewDateText.Text = string.Empty;
                PreviewCustomerText.Text = string.Empty;
                BodyTextBox.Text = string.Empty;
                return;
            }

            PreviewSubjectText.Text = email.Subject;
            PreviewFromText.Text = $"Von: {email.FromDisplay}";
            PreviewDateText.Text = $"Datum: {email.Date:dd.MM.yyyy HH:mm}";
            PreviewCustomerText.Text = email.MatchedCustomer != null
                ? $"✅ Kunde: {email.MatchedCustomer.DisplayName}"
                : "⚠ Kein Kunde automatisch zugeordnet";
            BodyTextBox.Text = email.Body;
        }

        private void CreateTicket_Click(object sender, RoutedEventArgs e)
        {
            if (EmailsDataGrid.SelectedItem is not InboxEmail email)
            {
                MessageBox.Show("Bitte wählen Sie zunächst eine E-Mail aus.",
                    "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var form = new TicketFormWindow(email, email.MatchedCustomer) { Owner = this };
            if (form.ShowDialog() == true)
            {
                MessageBox.Show(
                    $"Ticket wurde erfolgreich angelegt!\n\nTicket-Nr.: {form.CreatedTicket?.TicketNumber}",
                    "Ticket erstellt", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AssignCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (EmailsDataGrid.SelectedItem is not InboxEmail email)
            {
                MessageBox.Show("Bitte wählen Sie zunächst eine E-Mail aus.",
                    "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var picker = new CustomerPickerDialog(_customers) { Owner = this };
            if (picker.ShowDialog() == true && picker.SelectedCustomer != null)
            {
                email.MatchedCustomer = picker.SelectedCustomer;
                EmailsDataGrid.Items.Refresh();
                UpdatePreview(email);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
