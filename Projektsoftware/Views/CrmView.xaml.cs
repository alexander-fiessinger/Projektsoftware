using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Projektsoftware.Views
{
    public partial class CrmView : UserControl
    {
        private DatabaseService _db;
        private List<CrmContact> _allContacts = new();
        private List<CrmActivity> _allActivities = new();
        private List<CrmDeal> _allDeals = new();
        private List<Customer> _customers = new();
        private List<Employee> _employees = new();

        public CrmView()
        {
            InitializeComponent();
        }

        public async Task LoadAsync()
        {
            try
            {
                _db = new DatabaseService();
                _customers = await _db.GetAllCustomersAsync();
                _employees = await _db.GetAllEmployeesAsync();
                await RefreshContactsAsync();
                await RefreshActivitiesAsync();
                await RefreshDealsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CRM LoadAsync Fehler: {ex.Message}");
            }
        }

        #region Contacts

        private async Task RefreshContactsAsync()
        {
            bool includeInactive = ShowInactiveCheck.IsChecked == true;
            _allContacts = await _db.GetAllCrmContactsAsync(includeInactive);
            ApplyContactFilter();
            UpdateKpis();
        }

        private void ApplyContactFilter()
        {
            string search = ContactSearchBox.Text?.Trim().ToLower() ?? "";
            var filtered = string.IsNullOrEmpty(search)
                ? _allContacts
                : _allContacts.Where(c =>
                    c.DisplayName.ToLower().Contains(search) ||
                    c.Position?.ToLower().Contains(search) == true ||
                    c.CustomerName?.ToLower().Contains(search) == true ||
                    c.Email?.ToLower().Contains(search) == true).ToList();
            ContactsGrid.ItemsSource = filtered;
        }

        private void ContactSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyContactFilter();

        private async void ShowInactive_Changed(object sender, RoutedEventArgs e)
        {
            if (_db != null) await RefreshContactsAsync();
        }

        private void ContactsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ContactsGrid.SelectedItem is CrmContact contact)
                OpenContactDialog(contact);
        }

        private void AddContact_Click(object sender, RoutedEventArgs e) => OpenContactDialog(null);
        private void EditContact_Click(object sender, RoutedEventArgs e)
        {
            var contact = (sender as FrameworkElement)?.DataContext as CrmContact
                          ?? ContactsGrid.SelectedItem as CrmContact;
            if (contact != null) OpenContactDialog(contact);
        }

        private async void DeleteContact_Click(object sender, RoutedEventArgs e)
        {
            var contact = ContactsGrid.SelectedItem as CrmContact;
            if (contact == null) return;

            if (MessageBox.Show($"Kontakt '{contact.DisplayName}' deaktivieren?", "Kontakt deaktivieren",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _db.DeleteCrmContactAsync(contact.Id);
                await RefreshContactsAsync();
            }
        }

        private async void OpenContactDialog(CrmContact existing)
        {
            var dialog = new CrmContactDialog(_customers, existing) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                if (existing == null)
                    await _db.AddCrmContactAsync(dialog.Contact);
                else
                    await _db.UpdateCrmContactAsync(dialog.Contact);
                await RefreshContactsAsync();
            }
        }

        #endregion

        #region Activities

        private async Task RefreshActivitiesAsync()
        {
            _allActivities = await _db.GetAllCrmActivitiesAsync();
            ApplyActivityFilter();
            UpdateKpis();
        }

        private void ApplyActivityFilter()
        {
            bool showCompleted = ShowCompletedCheck.IsChecked == true;
            string search = ActivitySearchBox.Text?.Trim().ToLower() ?? "";

            var filtered = _allActivities
                .Where(a => showCompleted || !a.IsCompleted)
                .Where(a => string.IsNullOrEmpty(search) ||
                    a.Subject?.ToLower().Contains(search) == true ||
                    a.ContactName?.ToLower().Contains(search) == true ||
                    a.CustomerName?.ToLower().Contains(search) == true)
                .ToList();

            ActivitiesGrid.ItemsSource = filtered;
        }

        private void ActivitySearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyActivityFilter();

        private async void ShowCompleted_Changed(object sender, RoutedEventArgs e)
        {
            if (_db != null) await RefreshActivitiesAsync();
        }

        private void ActivitiesGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ActivitiesGrid.SelectedItem is CrmActivity activity)
                OpenActivityDialog(activity);
        }

        private void AddActivity_Click(object sender, RoutedEventArgs e) => OpenActivityDialog(null);
        private void EditActivity_Click(object sender, RoutedEventArgs e)
        {
            var activity = (sender as FrameworkElement)?.DataContext as CrmActivity
                           ?? ActivitiesGrid.SelectedItem as CrmActivity;
            if (activity != null) OpenActivityDialog(activity);
        }

        private async void CompleteActivity_Click(object sender, RoutedEventArgs e)
        {
            var activity = ActivitiesGrid.SelectedItem as CrmActivity;
            if (activity == null) return;
            activity.IsCompleted = true;
            activity.CompletedAt = DateTime.Now;
            await _db.UpdateCrmActivityAsync(activity);
            await RefreshActivitiesAsync();
        }

        private async void DeleteActivity_Click(object sender, RoutedEventArgs e)
        {
            var activity = ActivitiesGrid.SelectedItem as CrmActivity;
            if (activity == null) return;
            if (MessageBox.Show($"Aktivität '{activity.Subject}' löschen?", "Löschen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _db.DeleteCrmActivityAsync(activity.Id);
                await RefreshActivitiesAsync();
            }
        }

        private async void OpenActivityDialog(CrmActivity existing)
        {
            var currentUser = AuthenticationService.CurrentUser?.Username ?? "";
            var dialog = new CrmActivityDialog(_customers, _allContacts, existing) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                if (existing == null)
                {
                    dialog.Activity.CreatedBy = currentUser;
                    await _db.AddCrmActivityAsync(dialog.Activity);
                }
                else
                    await _db.UpdateCrmActivityAsync(dialog.Activity);
                await RefreshActivitiesAsync();
            }
        }

        #endregion

        #region Deals

        private async Task RefreshDealsAsync()
        {
            _allDeals = await _db.GetAllCrmDealsAsync();
            ApplyDealFilter();
            UpdateKpis();
            UpdatePipelineSummary();
        }

        private void ApplyDealFilter()
        {
            bool showClosed = ShowClosedDealsCheck.IsChecked == true;
            string search = DealSearchBox.Text?.Trim().ToLower() ?? "";

            var filtered = _allDeals
                .Where(d => showClosed || (d.Stage != DealStage.Won && d.Stage != DealStage.Lost))
                .Where(d => string.IsNullOrEmpty(search) ||
                    d.Title?.ToLower().Contains(search) == true ||
                    d.CustomerName?.ToLower().Contains(search) == true ||
                    d.AssignedTo?.ToLower().Contains(search) == true)
                .ToList();

            DealsGrid.ItemsSource = filtered;
        }

        private void DealSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyDealFilter();

        private async void ShowClosedDeals_Changed(object sender, RoutedEventArgs e)
        {
            if (_db != null) await RefreshDealsAsync();
        }

        private void DealsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DealsGrid.SelectedItem is CrmDeal deal)
                OpenDealDialog(deal);
        }

        private void AddDeal_Click(object sender, RoutedEventArgs e) => OpenDealDialog(null);
        private void EditDeal_Click(object sender, RoutedEventArgs e)
        {
            var deal = (sender as FrameworkElement)?.DataContext as CrmDeal
                       ?? DealsGrid.SelectedItem as CrmDeal;
            if (deal != null) OpenDealDialog(deal);
        }

        private async void DeleteDeal_Click(object sender, RoutedEventArgs e)
        {
            var deal = DealsGrid.SelectedItem as CrmDeal;
            if (deal == null) return;
            if (MessageBox.Show($"Deal '{deal.Title}' löschen?", "Löschen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _db.DeleteCrmDealAsync(deal.Id);
                await RefreshDealsAsync();
            }
        }

        private async void OpenDealDialog(CrmDeal existing)
        {
            var dialog = new CrmDealDialog(_customers, _allContacts, _employees, existing) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                if (existing == null)
                    await _db.AddCrmDealAsync(dialog.Deal);
                else
                    await _db.UpdateCrmDealAsync(dialog.Deal);
                await RefreshDealsAsync();
            }
        }

        private void UpdatePipelineSummary()
        {
            var active = _allDeals.Where(d => d.Stage != DealStage.Won && d.Stage != DealStage.Lost).ToList();
            PipelineLead.Text = $"{active.Count(d => d.Stage == DealStage.Lead)} Deals";
            PipelineQualified.Text = $"{active.Count(d => d.Stage == DealStage.Qualified)} Deals";
            PipelineProposal.Text = $"{active.Count(d => d.Stage == DealStage.Proposal)} Deals";
            PipelineNegotiation.Text = $"{active.Count(d => d.Stage == DealStage.Negotiation)} Deals";
            PipelineWon.Text = $"{_allDeals.Count(d => d.Stage == DealStage.Won)} Deals";
        }

        #endregion

        private void UpdateKpis()
        {
            KpiContacts.Text = _allContacts.Count(c => c.IsActive).ToString();
            KpiOpenActivities.Text = _allActivities.Count(a => !a.IsCompleted).ToString();
            KpiActiveDeals.Text = _allDeals.Count(d => d.Stage != DealStage.Won && d.Stage != DealStage.Lost).ToString();
            var pipelineValue = _allDeals
                .Where(d => d.Stage != DealStage.Won && d.Stage != DealStage.Lost)
                .Sum(d => d.WeightedValue);
            KpiPipelineValue.Text = $"{pipelineValue:N2} €";
        }
    }
}
