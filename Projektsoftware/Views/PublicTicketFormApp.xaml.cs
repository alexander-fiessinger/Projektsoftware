using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;

namespace Projektsoftware.Views
{
    /// <summary>
    /// Öffentliches Ticket-Formular für Kunden (kann als eigenständige Anwendung gestartet werden)
    /// </summary>
    public partial class PublicTicketFormApp : Window
    {
        public PublicTicketFormApp()
        {
            InitializeComponent();
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateForm())
                    return;

                // Button deaktivieren während der Verarbeitung
                var submitButton = sender as System.Windows.Controls.Button;
                if (submitButton != null)
                {
                    submitButton.IsEnabled = false;
                    submitButton.Content = "⏳ Wird gesendet...";
                }

                var ticket = new Ticket
                {
                    CustomerName = CustomerNameTextBox.Text.Trim(),
                    CustomerEmail = CustomerEmailTextBox.Text.Trim(),
                    CustomerPhone = CustomerPhoneTextBox.Text.Trim(),
                    Subject = SubjectTextBox.Text.Trim(),
                    Description = DescriptionTextBox.Text.Trim(),
                    Category = (TicketCategory)CategoryComboBox.SelectedIndex,
                    Priority = (TicketPriority)PriorityComboBox.SelectedIndex,
                    Status = TicketStatus.New,
                    IpAddress = GetLocalIPAddress(),
                    UserAgent = $"{Environment.OSVersion} - {Environment.MachineName}",
                    CreatedAt = DateTime.Now
                };

                var db = new DatabaseService();
                ticket.Id = await db.AddTicketAsync(ticket);

                MessageBox.Show(
                    $"✅ Vielen Dank für Ihre Anfrage!\n\n" +
                    $"Ihre Support-Anfrage wurde erfolgreich übermittelt.\n\n" +
                    $"📋 Ticket-Nummer: {ticket.TicketNumber}\n" +
                    $"📧 Bestätigung an: {ticket.CustomerEmail}\n\n" +
                    $"Wir werden uns schnellstmöglich bei Ihnen melden.\n" +
                    $"In dringenden Fällen erreichen Sie uns auch telefonisch.",
                    "Anfrage erfolgreich gesendet",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Formular zurücksetzen
                ResetForm();

                if (submitButton != null)
                {
                    submitButton.IsEnabled = true;
                    submitButton.Content = "✉️ Anfrage absenden";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Fehler beim Senden der Anfrage\n\n" +
                    $"Es ist ein Fehler aufgetreten:\n{ex.Message}\n\n" +
                    $"Bitte versuchen Sie es erneut oder kontaktieren Sie uns telefonisch.",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                var submitButton = sender as System.Windows.Controls.Button;
                if (submitButton != null)
                {
                    submitButton.IsEnabled = true;
                    submitButton.Content = "✉️ Anfrage absenden";
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Möchten Sie das Formular wirklich schließen?\n\nAlle eingegebenen Daten gehen verloren.",
                "Abbrechen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Close();
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(CustomerNameTextBox.Text))
            {
                ShowValidationError("Bitte geben Sie Ihren Namen oder Firmennamen ein.", CustomerNameTextBox);
                return false;
            }

            if (string.IsNullOrWhiteSpace(CustomerEmailTextBox.Text))
            {
                ShowValidationError("Bitte geben Sie Ihre E-Mail-Adresse ein.", CustomerEmailTextBox);
                return false;
            }

            if (!IsValidEmail(CustomerEmailTextBox.Text.Trim()))
            {
                ShowValidationError("Bitte geben Sie eine gültige E-Mail-Adresse ein.", CustomerEmailTextBox);
                return false;
            }

            if (string.IsNullOrWhiteSpace(SubjectTextBox.Text))
            {
                ShowValidationError("Bitte geben Sie einen Betreff ein.", SubjectTextBox);
                return false;
            }

            if (SubjectTextBox.Text.Trim().Length < 5)
            {
                ShowValidationError("Der Betreff sollte mindestens 5 Zeichen lang sein.", SubjectTextBox);
                return false;
            }

            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                ShowValidationError("Bitte beschreiben Sie Ihr Anliegen.", DescriptionTextBox);
                return false;
            }

            if (DescriptionTextBox.Text.Trim().Length < 20)
            {
                ShowValidationError("Bitte beschreiben Sie Ihr Anliegen etwas ausführlicher (mindestens 20 Zeichen).", DescriptionTextBox);
                return false;
            }

            return true;
        }

        private void ShowValidationError(string message, System.Windows.Controls.Control control)
        {
            MessageBox.Show(message, "Eingabe erforderlich", MessageBoxButton.OK, MessageBoxImage.Warning);
            control.Focus();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        private void ResetForm()
        {
            CustomerNameTextBox.Clear();
            CustomerEmailTextBox.Clear();
            CustomerPhoneTextBox.Clear();
            SubjectTextBox.Clear();
            DescriptionTextBox.Clear();
            CategoryComboBox.SelectedIndex = 0;
            PriorityComboBox.SelectedIndex = 1;
        }
    }
}
