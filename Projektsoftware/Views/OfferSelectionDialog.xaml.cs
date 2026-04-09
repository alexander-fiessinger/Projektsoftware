using Projektsoftware.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Projektsoftware.Views
{
    public partial class OfferSelectionDialog : Window
    {
        public EasybillDocument? SelectedOffer { get; private set; }

        public OfferSelectionDialog(List<EasybillDocument> offers)
        {
            InitializeComponent();
            OffersDataGrid.ItemsSource = offers;
        }

        private void OffersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectAndClose();
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            SelectAndClose();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SelectAndClose()
        {
            if (OffersDataGrid.SelectedItem is EasybillDocument offer)
            {
                SelectedOffer = offer;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Bitte wählen Sie ein Angebot aus.",
                    "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
