using Projektsoftware.Models;
using System;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class SalesAppointmentDialog : Window
    {
        public SalesAppointment Result { get; private set; }
        public bool SendInvite   => SendInviteCheck.IsChecked == true;
        public bool CreateWebex  => CreateWebexCheck.IsChecked == true;

        public SalesAppointmentDialog(SalesAppointment? existing = null)
        {
            InitializeComponent();

            if (existing != null)
            {
                TitleText.Text = "📅 Sales-Termin bearbeiten";
                TitleBox.Text = existing.Title;
                DatePicker.SelectedDate = existing.AppointmentDate.Date;
                StartTimeBox.Text = existing.AppointmentDate.ToString("HH:mm");
                EndTimeBox.Text   = existing.AppointmentEnd.ToString("HH:mm");
                ContactNameBox.Text  = existing.ContactName;
                ContactEmailBox.Text = existing.ContactEmail;
                CompanyBox.Text   = existing.ContactCompany;
                PhoneBox.Text     = existing.ContactPhone;
                LocationBox.Text  = existing.Location;
                NotesBox.Text     = existing.Notes;
                Result = existing;
            }
            else
            {
                DatePicker.SelectedDate = DateTime.Today;
                Result = new SalesAppointment { CreatedAt = DateTime.Now };
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text))
            { MessageBox.Show("Bitte Betreff eingeben.", "Pflichtfeld", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (string.IsNullOrWhiteSpace(ContactNameBox.Text))
            { MessageBox.Show("Bitte Kontaktperson eingeben.", "Pflichtfeld", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var emailAddresses = ContactEmailBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (emailAddresses.Length == 0 || emailAddresses.Any(m => !m.Contains('@')))
            { MessageBox.Show("Bitte gültige E-Mail-Adresse(n) eingeben (mehrere kommagetrennt).", "Pflichtfeld", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (DatePicker.SelectedDate == null)
            { MessageBox.Show("Bitte Datum wählen.", "Pflichtfeld", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            if (!TimeSpan.TryParse(StartTimeBox.Text, out var start))
            { MessageBox.Show("Ungültige Startzeit. Format: HH:mm", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (!TimeSpan.TryParse(EndTimeBox.Text, out var end))
            { MessageBox.Show("Ungültige Endzeit. Format: HH:mm", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            var date = DatePicker.SelectedDate!.Value.Date;

            Result.Title           = TitleBox.Text.Trim();
            Result.ContactName     = ContactNameBox.Text.Trim();
            Result.ContactEmail    = ContactEmailBox.Text.Trim();
            Result.ContactCompany  = CompanyBox.Text.Trim();
            Result.ContactPhone    = PhoneBox.Text.Trim();
            Result.Location        = LocationBox.Text.Trim();
            Result.Notes           = NotesBox.Text.Trim();
            Result.AppointmentDate = DateTime.SpecifyKind(date + start, DateTimeKind.Local);
            Result.AppointmentEnd  = DateTime.SpecifyKind(date + end,   DateTimeKind.Local);

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
