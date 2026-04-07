using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class TicketEmailDialog : Window
    {
        private readonly Ticket _ticket;

        public TicketEmailDialog(Ticket ticket)
        {
            InitializeComponent();
            _ticket = ticket;

            TicketInfoTextBlock.Text = $"Ticket {ticket.TicketNumber}: {ticket.Subject}";
            ToTextBox.Text = ticket.CustomerEmail;
            SubjectTextBox.Text = $"Re: Ticket {ticket.TicketNumber} – {ticket.Subject}";
            MessageTextBox.Text = BuildDefaultMessage(ticket);
        }

        private static string BuildDefaultMessage(Ticket ticket) =>
            $"Sehr geehrte/r {ticket.CustomerName},\n\n" +
            $"vielen Dank für Ihre Anfrage zu Ticket {ticket.TicketNumber}.\n\n" +
            $"\n\n" +
            $"Mit freundlichen Grüßen";

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ToTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie eine E-Mail-Adresse ein.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                ToTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(SubjectTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie einen Betreff ein.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                SubjectTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie eine Nachricht ein.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                MessageTextBox.Focus();
                return;
            }

            var smtpConfig = TicketSmtpConfig.Load();
            if (!smtpConfig.IsConfigured)
            {
                MessageBox.Show(
                    "Das Ticket-SMTP-Konto ist nicht konfiguriert.\nBitte zuerst unter Einstellungen → Ticket-SMTP konfigurieren.",
                    "Ticket-SMTP nicht konfiguriert",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            SendButton.IsEnabled = false;
            SendButton.Content = "⏳ Wird gesendet...";

            try
            {
                var emailService = new ExchangeEmailService(smtpConfig.ToExchangeConfig());
                await emailService.SendEmailAsync(
                    ToTextBox.Text.Trim(),
                    SubjectTextBox.Text.Trim(),
                    MessageTextBox.Text.Trim(),
                    string.IsNullOrWhiteSpace(CcTextBox.Text) ? null : CcTextBox.Text.Trim());

                MessageBox.Show(
                    $"✅ E-Mail erfolgreich an {ToTextBox.Text.Trim()} gesendet!",
                    "E-Mail gesendet",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Senden der E-Mail:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                SendButton.IsEnabled = true;
                SendButton.Content = "📧 Senden";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
