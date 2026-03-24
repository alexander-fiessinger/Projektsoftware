using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class TicketFormWindow : Window
    {
        public Ticket? CreatedTicket { get; private set; }

        public TicketFormWindow()
        {
            InitializeComponent();
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateForm())
                    return;

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
                    UserAgent = Environment.OSVersion.ToString(),
                    CreatedAt = DateTime.Now
                };

                var db = new DatabaseService();
                ticket.Id = await db.AddTicketAsync(ticket);

                CreatedTicket = ticket;

                MessageBox.Show(
                    $"Vielen Dank!\n\n" +
                    $"Ihr Support-Ticket wurde erfolgreich erstellt.\n" +
                    $"Ticket-Nummer: {ticket.TicketNumber}\n\n" +
                    $"Wir werden uns schnellstmöglich bei Ihnen melden.",
                    "Ticket erstellt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Erstellen des Tickets:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(CustomerNameTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie Ihren Namen ein.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                CustomerNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(CustomerEmailTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie Ihre E-Mail-Adresse ein.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                CustomerEmailTextBox.Focus();
                return false;
            }

            if (!IsValidEmail(CustomerEmailTextBox.Text.Trim()))
            {
                MessageBox.Show("Bitte geben Sie eine gültige E-Mail-Adresse ein.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                CustomerEmailTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(SubjectTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie einen Betreff ein.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                SubjectTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                MessageBox.Show("Bitte beschreiben Sie Ihr Anliegen.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                DescriptionTextBox.Focus();
                return false;
            }

            return true;
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
    }
}
