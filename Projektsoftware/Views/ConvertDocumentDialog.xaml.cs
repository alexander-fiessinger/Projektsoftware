using Projektsoftware.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class ConvertDocumentDialog : Window
    {
        private static readonly Dictionary<string, (string Tag, string Display)[]> AllowedTargets = new()
        {
            ["OFFER"] =
            [
                ("ORDER_CONFIRMATION", "✓ Auftragsbestätigung"),
                ("INVOICE", "📄 Rechnung"),
                ("PROFORMA_INVOICE", "🧾 Proforma-Rechnung"),
                ("DELIVERY_NOTE", "📦 Lieferschein"),
            ],
            ["ORDER_CONFIRMATION"] =
            [
                ("INVOICE", "📄 Rechnung"),
                ("PROFORMA_INVOICE", "🧾 Proforma-Rechnung"),
                ("DELIVERY_NOTE", "📦 Lieferschein"),
            ],
            ["PROFORMA_INVOICE"] =
            [
                ("INVOICE", "📄 Rechnung"),
            ],
            ["DELIVERY_NOTE"] =
            [
                ("INVOICE", "📄 Rechnung"),
            ],
            ["INVOICE"] =
            [
                ("CREDIT", "💳 Gutschrift"),
            ],
        };

        public string TargetType { get; private set; } = string.Empty;
        public bool CreateAsDraft { get; private set; } = true;

        public static bool HasConversions(string? type) =>
            type != null && AllowedTargets.ContainsKey(type);

        public ConvertDocumentDialog(EasybillDocument sourceDocument)
        {
            InitializeComponent();

            SourceTypeTextBlock.Text = sourceDocument.DisplayType;
            SourceNumberTextBlock.Text = sourceDocument.Number ?? "–";

            var snapshot = sourceDocument.CustomerSnapshot;
            SourceCustomerTextBlock.Text = snapshot?.CompanyName
                ?? (snapshot != null ? $"{snapshot.FirstName} {snapshot.LastName}".Trim() : "–");

            if (AllowedTargets.TryGetValue(sourceDocument.Type ?? "", out var targets))
            {
                foreach (var (tag, display) in targets)
                    TargetTypeComboBox.Items.Add(new ComboBoxItem { Content = display, Tag = tag });

                TargetTypeComboBox.SelectedIndex = 0;
            }
            else
            {
                ConvertButton.IsEnabled = false;
                TargetTypeComboBox.IsEnabled = false;
            }
        }

        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            if (TargetTypeComboBox.SelectedItem is not ComboBoxItem selected || selected.Tag is not string tag)
                return;

            TargetType = tag;
            CreateAsDraft = IsDraftCheckBox.IsChecked == true;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
