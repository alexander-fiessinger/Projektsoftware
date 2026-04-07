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
            Loaded += async (s, e) => await LoadProjectsAsync();
        }

        private async System.Threading.Tasks.Task LoadProjectsAsync()
        {
            try
            {
                var db = new DatabaseService();
                var projects = await db.GetAllProjectsAsync();
                projects.Insert(0, new Project { Id = 0, Name = "— kein Projekt —" });
                ProjectComboBox.ItemsSource = projects;
                ProjectComboBox.SelectedIndex = 0;
            }
            catch
            {
                // Silently ignore; project assignment remains optional
            }
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
                    CreatedAt = DateTime.Now,
                    ProjectId = ProjectComboBox.SelectedValue is int pid && pid > 0 ? pid : (int?)null
                };

                var db = new DatabaseService();
                ticket.Id = await db.AddTicketAsync(ticket);

                CreatedTicket = ticket;

                // Automatische Bestätigungsmail senden (still im Hintergrund)
                _ = SendConfirmationEmailAsync(ticket);

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

        private static async System.Threading.Tasks.Task SendConfirmationEmailAsync(Ticket ticket)
        {
            try
            {
                var smtpConfig = TicketSmtpConfig.Load();
                if (!smtpConfig.IsConfigured) return;

                var subject = $"Ihr Ticket {ticket.TicketNumber} wurde erstellt – {ticket.Subject}";
                var body =
                    $"Sehr geehrte/r {ticket.CustomerName},\n\n" +
                    $"vielen Dank für Ihre Anfrage! Wir haben Ihr Support-Ticket erhalten.\n\n" +
                    $"────────────────────────────────\n" +
                    $"Ticket-Nummer : {ticket.TicketNumber}\n" +
                    $"Betreff       : {ticket.Subject}\n" +
                    $"Kategorie     : {ticket.CategoryText}\n" +
                    $"Priorität     : {ticket.PriorityText}\n" +
                    $"Erstellt am   : {ticket.CreatedAt:dd.MM.yyyy HH:mm}\n" +
                    $"────────────────────────────────\n\n" +
                    $"Wir werden uns schnellstmöglich bei Ihnen melden.\n\n" +
                    $"Mit freundlichen Grüßen\n" +
                    $"Ihr Support-Team";

                var emailService = new ExchangeEmailService(smtpConfig.ToExchangeConfig());
                await emailService.SendEmailAsync(ticket.CustomerEmail, subject, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bestätigungsmail konnte nicht gesendet werden: {ex.Message}");
            }
        }
    }
}
