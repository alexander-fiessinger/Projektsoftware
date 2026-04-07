using Projektsoftware.Models;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class EasybillSendEmailDialog : Window
    {
        public string To { get; private set; } = string.Empty;
        public string Cc { get; private set; } = string.Empty;
        public string Bcc { get; private set; } = string.Empty;
        public string EmailSubject { get; private set; } = string.Empty;
        public string Message { get; private set; } = string.Empty;

        public EasybillSendEmailDialog(EasybillDocument document, string customerEmail = "")
        {
            InitializeComponent();

            DocumentInfoTextBlock.Text = $"Dokument: {document.DisplayType} {document.Number}";
            SubjectTextBox.Text = $"{document.DisplayType} {document.Number}";
            MessageTextBox.Text = $"Sehr geehrte Damen und Herren,\n\nanbei erhalten Sie {document.DisplayType} {document.Number}.\n\nMit freundlichen Grüßen";

            if (!string.IsNullOrEmpty(customerEmail))
                ToTextBox.Text = customerEmail;

            ToTextBox.Focus();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            // Validierung
            if (string.IsNullOrWhiteSpace(ToTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie eine E-Mail-Adresse ein!", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                ToTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(SubjectTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie einen Betreff ein!", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                SubjectTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie eine Nachricht ein!", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                MessageTextBox.Focus();
                return;
            }

            To = ToTextBox.Text.Trim();
            Cc = string.IsNullOrWhiteSpace(CcTextBox.Text) ? string.Empty : CcTextBox.Text.Trim();
            Bcc = string.IsNullOrWhiteSpace(BccTextBox.Text) ? string.Empty : BccTextBox.Text.Trim();
            EmailSubject = SubjectTextBox.Text.Trim();
            Message = MessageTextBox.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
