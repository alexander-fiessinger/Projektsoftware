using Microsoft.Win32;
using Projektsoftware.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class SalesLeadDialog : Window
    {
        public SalesLead Result { get; private set; }
        public byte[]? SelectedFileBytes { get; private set; }

        public SalesLeadDialog(SalesLead? existing = null)
        {
            InitializeComponent();
            LeadDatePicker.SelectedDate = DateTime.Today;

            if (existing != null)
            {
                TitleText.Text = "📋 Lead bearbeiten";
                TitleBox.Text = existing.Title;
                ContactNameBox.Text = existing.ContactName;
                ContactCompanyBox.Text = existing.ContactCompany;
                ContactEmailBox.Text = existing.ContactEmail;
                ContactPhoneBox.Text = existing.ContactPhone;
                SourceBox.Text = existing.Source;
                NotesBox.Text = existing.Notes;
                LeadDatePicker.SelectedDate = existing.LeadDate;

                foreach (ComboBoxItem item in StatusCombo.Items)
                    if (item.Tag?.ToString() == ((int)existing.Status).ToString())
                    {
                        StatusCombo.SelectedItem = item;
                        break;
                    }

                if (!string.IsNullOrEmpty(existing.OriginalFileName))
                {
                    SelectedFileText.Text = $"📎 {existing.OriginalFileName} (bereits gespeichert)";
                    SelectedFileText.FontStyle = FontStyles.Normal;
                    SelectedFileText.Foreground = System.Windows.Media.Brushes.DimGray;
                }

                Result = existing;
            }
            else
            {
                Result = new SalesLead { LeadDate = DateTime.Today };
            }
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Leadbogen auswählen",
                Filter = "Alle Belege|*.pdf;*.jpg;*.jpeg;*.png;*.tiff;*.bmp|" +
                         "PDF-Dateien|*.pdf|" +
                         "Bilder|*.jpg;*.jpeg;*.png;*.tiff;*.bmp|" +
                         "Alle Dateien|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                SelectedFileBytes = File.ReadAllBytes(dlg.FileName);
                Result.OriginalFileName = Path.GetFileName(dlg.FileName);
                SelectedFileText.Text = $"📎 {Result.OriginalFileName}";
                SelectedFileText.FontStyle = FontStyles.Normal;
                SelectedFileText.Foreground = System.Windows.Media.Brushes.DimGray;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text))
            {
                MessageBox.Show("Bitte eine Bezeichnung eingeben.", "Pflichtfeld",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Result.Title = TitleBox.Text.Trim();
            Result.ContactName = ContactNameBox.Text.Trim();
            Result.ContactCompany = ContactCompanyBox.Text.Trim();
            Result.ContactEmail = ContactEmailBox.Text.Trim();
            Result.ContactPhone = ContactPhoneBox.Text.Trim();
            Result.Source = SourceBox.Text.Trim();
            Result.Notes = NotesBox.Text.Trim();
            Result.LeadDate = LeadDatePicker.SelectedDate ?? DateTime.Today;

            if (StatusCombo.SelectedItem is ComboBoxItem selected &&
                int.TryParse(selected.Tag?.ToString(), out int statusVal))
                Result.Status = (LeadStatus)statusVal;

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
