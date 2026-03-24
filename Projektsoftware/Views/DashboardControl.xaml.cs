using Projektsoftware.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class DashboardControl : UserControl
    {
        private static readonly CultureInfo euroFormat = new CultureInfo("de-DE");

        public event RoutedEventHandler NewProjectClicked;
        public event RoutedEventHandler NewTaskClicked;
        public event RoutedEventHandler NewCustomerClicked;
        public event RoutedEventHandler TimeTrackingClicked;
        public event RoutedEventHandler CreateOfferClicked;
        public event RoutedEventHandler CreateInvoiceClicked;
        public event RoutedEventHandler CreateOrderConfirmationClicked;
        public event RoutedEventHandler CreateDeliveryNoteClicked;
        public event RoutedEventHandler CreateCreditNoteClicked;
        public event RoutedEventHandler CreateDunningClicked;
        public event RoutedEventHandler ShowDocumentsClicked;
        public event RoutedEventHandler ManageCustomersClicked;
        public event RoutedEventHandler ShowProductsClicked;
        public event RoutedEventHandler ExportTimesClicked;

        public DashboardControl()
        {
            InitializeComponent();
        }

        public void UpdateStats(DashboardStats stats)
        {
            TotalProjectsText.Text = stats.TotalProjects.ToString();
            ActiveProjectsText.Text = $"{stats.ActiveProjects} aktiv • {stats.CompletedProjects} abgeschlossen";

            TotalTasksText.Text = stats.TotalTasks.ToString();
            OpenTasksText.Text = $"{stats.OpenTasks} offen • {stats.CompletedTasks} erledigt";

            TotalHoursText.Text = stats.TotalHoursLogged.ToString("F1", euroFormat);

            TotalBudgetText.Text = stats.TotalBudget.ToString("C0", euroFormat);

            OverdueTasksText.Text = $"{stats.OverdueTasks} überfällige Aufgaben";
            OpenTasksDetailText.Text = $"{stats.OpenTasks} offene Aufgaben";

            UpcomingMeetingsText.Text = $"{stats.UpcomingMeetings} Meetings (nächste 7 Tage)";
            ActiveEmployeesText.Text = $"{stats.ActiveEmployees} aktive Mitarbeiter";
        }

        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            NewProjectClicked?.Invoke(this, e);
        }

        private void NewTask_Click(object sender, RoutedEventArgs e)
        {
            NewTaskClicked?.Invoke(this, e);
        }

        private void NewCustomer_Click(object sender, RoutedEventArgs e)
        {
            NewCustomerClicked?.Invoke(this, e);
        }

        private void TimeTracking_Click(object sender, RoutedEventArgs e)
        {
            TimeTrackingClicked?.Invoke(this, e);
        }

        private void CreateOffer_Click(object sender, RoutedEventArgs e)
        {
            CreateOfferClicked?.Invoke(this, e);
        }

        private void CreateInvoice_Click(object sender, RoutedEventArgs e)
        {
            CreateInvoiceClicked?.Invoke(this, e);
        }

        private void ShowDocuments_Click(object sender, RoutedEventArgs e)
        {
            ShowDocumentsClicked?.Invoke(this, e);
        }

        private void ManageCustomers_Click(object sender, RoutedEventArgs e)
        {
            ManageCustomersClicked?.Invoke(this, e);
        }

        private void ShowProducts_Click(object sender, RoutedEventArgs e)
        {
            ShowProductsClicked?.Invoke(this, e);
        }

        private void ExportTimes_Click(object sender, RoutedEventArgs e)
        {
            ExportTimesClicked?.Invoke(this, e);
        }

        private void CreateDeliveryNote_Click(object sender, RoutedEventArgs e)
        {
            CreateDeliveryNoteClicked?.Invoke(this, e);
        }

        private void CreateCreditNote_Click(object sender, RoutedEventArgs e)
        {
            CreateCreditNoteClicked?.Invoke(this, e);
        }

        private void CreateDunning_Click(object sender, RoutedEventArgs e)
        {
            CreateDunningClicked?.Invoke(this, e);
        }

        private void CreateOrderConfirmation_Click(object sender, RoutedEventArgs e)
        {
            CreateOrderConfirmationClicked?.Invoke(this, e);
        }
    }
}
