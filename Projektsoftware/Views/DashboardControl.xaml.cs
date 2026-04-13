using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Projektsoftware.Views
{
    public partial class DashboardControl : UserControl
    {
        private static readonly CultureInfo euroFormat = new CultureInfo("de-DE");
        private List<EasybillDocument> _allDocuments = [];

        public event RoutedEventHandler? NewProjectClicked;
        public event RoutedEventHandler? DocumentSearchRefreshClicked;
        public event EventHandler<EasybillDocument>? DocumentSelected;
        public event RoutedEventHandler? NewTaskClicked;
        public event RoutedEventHandler? NewCustomerClicked;
        public event RoutedEventHandler? TimeTrackingClicked;
        public event RoutedEventHandler? CreateOfferClicked;
        public event RoutedEventHandler? CreateInvoiceClicked;
        public event RoutedEventHandler? CreateOrderConfirmationClicked;
        public event RoutedEventHandler? CreateDeliveryNoteClicked;
        public event RoutedEventHandler? CreateCreditNoteClicked;
        public event RoutedEventHandler? CreateDunningClicked;
        public event RoutedEventHandler? ShowDocumentsClicked;
        public event RoutedEventHandler? ManageCustomersClicked;
        public event RoutedEventHandler? ExportTimesClicked;
        public event RoutedEventHandler? RefreshFinancialClicked;
        public event RoutedEventHandler? ViewPurchasesClicked;
public event RoutedEventHandler? OpenInboxClicked;
public event RoutedEventHandler? InboxRefreshClicked;
        public DashboardControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Aktualisiert die personalisierte Begrüßung basierend auf Tageszeit und Benutzername
        /// </summary>
        public void UpdateGreeting(string username)
        {
            var hour = DateTime.Now.Hour;
            var greeting = hour switch
            {
                < 12 => "Guten Morgen",
                < 18 => "Guten Tag",
                _ => "Guten Abend"
            };
            DashboardGreetingText.Text = $"{greeting}, {username} 👋";
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

            UpdateFinancialStats(stats);
        }

        private void UpdateFinancialChart(DashboardStats stats)
        {
            if (!stats.IsFinancialDataLoaded || !stats.EasybillConfigured) return;

            var total = stats.TotalRevenuePaid + stats.OpenInvoicesAmount + stats.OverdueInvoicesAmount;
            const double maxBarWidth = 400.0;

            if (total <= 0) return;

            ChartBarPaid.Width    = maxBarWidth * (double)(stats.TotalRevenuePaid    / total);
            ChartBarOpen.Width    = maxBarWidth * (double)(stats.OpenInvoicesAmount  / total);
            ChartBarOverdue.Width = maxBarWidth * (double)(stats.OverdueInvoicesAmount / total);

            ChartLabelPaid.Text    = stats.TotalRevenuePaid.ToString("C0", euroFormat);
            ChartLabelOpen.Text    = stats.OpenInvoicesAmount.ToString("C0", euroFormat);
            ChartLabelOverdue.Text = stats.OverdueInvoicesAmount.ToString("C0", euroFormat);
        }

        private void UpdateBudgetChart(DashboardStats stats)
        {
            BudgetChartPanel.Children.Clear();

            var projects = stats.TopBudgetProjects;
            if (projects == null || projects.Count == 0)
            {
                BudgetChartPanel.Children.Add(new TextBlock
                {
                    Text = "Keine Projekte mit Budget vorhanden.",
                    FontSize = 12,
                    Foreground = (Brush)FindResource("TextSecondaryBrush")
                });
                return;
            }

            foreach (var p in projects)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                // Label
                var label = new TextBlock
                {
                    Text = p.ProjectName.Length > 20 ? p.ProjectName[..20] + "…" : p.ProjectName,
                    FontSize = 12, VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(label, 0);

                // Bar background
                var barTrack = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
                    CornerRadius = new CornerRadius(4),
                    Height = 18,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                var pct = (double)Math.Min(p.BudgetUsagePercent / 100m, 1.0m);
                var barColor = pct < 0.6 ? Color.FromRgb(0x16, 0xa3, 0x4a)
                             : pct < 0.85 ? Color.FromRgb(0xD9, 0x77, 0x06)
                             : Color.FromRgb(0xDC, 0x26, 0x26);
                var barFill = new Border
                {
                    Background = new SolidColorBrush(barColor),
                    CornerRadius = new CornerRadius(4),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 0 // set after render
                };
                var capturedFill = barFill;
                barTrack.SizeChanged += (s, e) =>
                    capturedFill.Width = Math.Max(0, e.NewSize.Width * pct);
                barTrack.Child = barFill;
                Grid.SetColumn(barTrack, 1);

                // Percent label
                var pctLabel = new TextBlock
                {
                    Text = $"{p.BudgetUsagePercent:F0}%",
                    FontSize = 12, FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(barColor)
                };
                Grid.SetColumn(pctLabel, 2);

                row.Children.Add(label);
                row.Children.Add(barTrack);
                row.Children.Add(pctLabel);
                BudgetChartPanel.Children.Add(row);
            }
        }

        public void UpdateFinancialStats(DashboardStats stats)
        {
            if (!stats.IsFinancialDataLoaded)
            {
                FinancialLoadingText.Text = "⏳ Lade Finanzdaten…";
                FinancialLoadingText.Visibility = Visibility.Visible;
                TotalRevenueText.Text = "–";
                ThisMonthRevenueText.Text = "–";
                OpenInvoicesText.Text = "–";
                OpenInvoicesCountText.Text = "";
                OverdueInvoicesText.Text = "–";
                OverdueInvoicesCountText.Text = "";
                DraftInvoicesText.Text = "–";
                OpenPurchaseOrdersText.Text = "–";
                UnpaidPurchaseInvoicesText.Text = "–";
                UnpaidPurchaseAmountText.Text = "";
                return;
            }

            FinancialLoadingText.Visibility = Visibility.Collapsed;

            // Einkauf-Kacheln: immer aus lokaler DB
            OpenPurchaseOrdersText.Text = stats.OpenPurchaseOrdersCount.ToString();
            UnpaidPurchaseInvoicesText.Text = stats.TotalPurchaseDocumentsCount.ToString();
            UnpaidPurchaseAmountText.Text = stats.TotalPurchaseDocumentsCount == 0
                ? "noch keine Belege"
                : $"{stats.SyncedPurchaseDocumentsCount} in Easybill synchronisiert";

            // Easybill-Rechnungskacheln
            if (!stats.EasybillConfigured)
            {
                FinancialLoadingText.Text = "ℹ️ Easybill nicht konfiguriert";
                FinancialLoadingText.Visibility = Visibility.Visible;
                TotalRevenueText.Text = "–";
                ThisMonthRevenueText.Text = "–";
                OpenInvoicesText.Text = "–";
                OpenInvoicesCountText.Text = "";
                OverdueInvoicesText.Text = "–";
                OverdueInvoicesCountText.Text = "";
                DraftInvoicesText.Text = "–";
                return;
            }

            TotalRevenueText.Text = stats.TotalRevenuePaid.ToString("C0", euroFormat);
            ThisMonthRevenueText.Text = stats.ThisMonthRevenue.ToString("C0", euroFormat);
            OpenInvoicesText.Text = stats.OpenInvoicesAmount.ToString("C0", euroFormat);
            OpenInvoicesCountText.Text = $"{stats.OpenInvoicesCount} Rechnung(en)";
            OverdueInvoicesText.Text = stats.OverdueInvoicesAmount.ToString("C0", euroFormat);
            OverdueInvoicesCountText.Text = $"{stats.OverdueInvoicesCount} Rechnung(en)";
            DraftInvoicesText.Text = stats.DraftInvoicesCount.ToString();

            UpdateFinancialChart(stats);
            UpdateBudgetChart(stats);
        }

        public void ShowEasybillError(string errorMessage)
        {
            FinancialLoadingText.Text = $"⚠️ Easybill-Fehler: {errorMessage}";
            FinancialLoadingText.Visibility = Visibility.Visible;
        }

public void UpdateInboxPreview(List<InboxEmail> emails, string statusText)
{
    InboxStatusText.Text = statusText;
    if (emails.Count > 0)
    {
        InboxPreviewGrid.ItemsSource = emails;
        InboxPreviewGrid.Visibility = Visibility.Visible;
        InboxEmptyText.Visibility = Visibility.Collapsed;
    }
    else
    {
        InboxPreviewGrid.Visibility = Visibility.Collapsed;
        InboxEmptyText.Visibility = Visibility.Visible;
        InboxEmptyText.Text = string.IsNullOrWhiteSpace(statusText)
            ? "Keine E-Mails gefunden"
            : statusText;
    }
}

public void UpdateDocuments(List<EasybillDocument> documents)
{
    _allDocuments = documents ?? [];
    foreach (var doc in _allDocuments)
    {
        if (string.IsNullOrEmpty(doc.CustomerDisplay))
        {
            var snap = doc.CustomerSnapshot;
            if (snap != null)
            {
                var name = !string.IsNullOrEmpty(snap.CompanyName)
                    ? snap.CompanyName
                    : $"{snap.FirstName} {snap.LastName}".Trim();
                doc.CustomerDisplay = !string.IsNullOrEmpty(snap.Number)
                    ? $"{snap.Number} – {name}" : name;
            }
        }
    }
    ApplyDocumentSearch();
}

public void SetDocSearchStatus(string message)
{
    DocSearchStatusText.Text = message;
}

private void ApplyDocumentSearch()
{
    if (DocSearchBox == null || DocTypeFilter == null || DocSearchResultsGrid == null) return;

    var query = DocSearchBox.Text.Trim();
    var typeFilter = (DocTypeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();

    var filtered = _allDocuments.AsEnumerable();

    if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "Alle Typen")
    {
        var apiType = typeFilter switch
        {
            "Rechnung" => "INVOICE",
            "Angebot" => "OFFER",
            "Bestellung" => "ORDER",
            "Lieferschein" => "DELIVERY_NOTE",
            "Gutschrift" => "CREDIT",
            "Mahnung" => "DUNNING",
            "Proforma-Rechnung" => "PROFORMA_INVOICE",
            _ => null
        };
        if (apiType != null)
            filtered = filtered.Where(d => d.Type == apiType);
    }

    if (!string.IsNullOrEmpty(query))
    {
        filtered = filtered.Where(d =>
            (d.Number?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
            (d.CustomerDisplay?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
            (d.CustomerSnapshot?.Number?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
            (d.Subject?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
            (d.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) == true));
    }

    var results = filtered.Take(100).ToList();
    var total = _allDocuments.Count;

    if (results.Count > 0 || !string.IsNullOrEmpty(query))
    {
        DocSearchResultsGrid.ItemsSource = results;
        DocSearchResultsGrid.Visibility = Visibility.Visible;
        DocSearchStatusText.Text = $"{results.Count} Treffer (von {total} Dokumenten)";
    }
    else if (total > 0)
    {
        DocSearchResultsGrid.Visibility = Visibility.Collapsed;
        DocSearchStatusText.Text = $"{total} Dokumente geladen – Suchbegriff eingeben";
    }
}

private void DocSearchRefresh_Click(object sender, RoutedEventArgs e)
    => DocumentSearchRefreshClicked?.Invoke(this, e);

private void DocSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    => ApplyDocumentSearch();

private void DocTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    => ApplyDocumentSearch();

private void DocSearchResultsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
{
    if (DocSearchResultsGrid.SelectedItem is EasybillDocument doc)
        DocumentSelected?.Invoke(this, doc);
}

private void NewProject_Click(object sender, RoutedEventArgs e) => NewProjectClicked?.Invoke(this, e);
private void NewTask_Click(object sender, RoutedEventArgs e) => NewTaskClicked?.Invoke(this, e);
private void NewCustomer_Click(object sender, RoutedEventArgs e) => NewCustomerClicked?.Invoke(this, e);
private void TimeTracking_Click(object sender, RoutedEventArgs e) => TimeTrackingClicked?.Invoke(this, e);
private void CreateOffer_Click(object sender, RoutedEventArgs e) => CreateOfferClicked?.Invoke(this, e);
private void CreateInvoice_Click(object sender, RoutedEventArgs e) => CreateInvoiceClicked?.Invoke(this, e);
private void ShowDocuments_Click(object sender, RoutedEventArgs e) => ShowDocumentsClicked?.Invoke(this, e);
private void ManageCustomers_Click(object sender, RoutedEventArgs e) => ManageCustomersClicked?.Invoke(this, e);
private void ExportTimes_Click(object sender, RoutedEventArgs e) => ExportTimesClicked?.Invoke(this, e);
private void CreateDeliveryNote_Click(object sender, RoutedEventArgs e) => CreateDeliveryNoteClicked?.Invoke(this, e);
private void CreateCreditNote_Click(object sender, RoutedEventArgs e) => CreateCreditNoteClicked?.Invoke(this, e);
private void CreateDunning_Click(object sender, RoutedEventArgs e) => CreateDunningClicked?.Invoke(this, e);
private void CreateOrderConfirmation_Click(object sender, RoutedEventArgs e) => CreateOrderConfirmationClicked?.Invoke(this, e);
private void RefreshFinancial_Click(object sender, RoutedEventArgs e) => RefreshFinancialClicked?.Invoke(this, e);
private void ViewPurchases_Click(object sender, RoutedEventArgs e) => ViewPurchasesClicked?.Invoke(this, e);
private void InboxOpen_Click(object sender, RoutedEventArgs e) => OpenInboxClicked?.Invoke(this, e);
        private void InboxRefresh_Click(object sender, RoutedEventArgs e) => InboxRefreshClicked?.Invoke(this, e);
        private void InboxPreviewGrid_DoubleClick(object sender, MouseButtonEventArgs e) => OpenInboxClicked?.Invoke(this, e);

        private void ToggleWidgetSettings_Click(object sender, RoutedEventArgs e)
        {
            WidgetSettingsPanel.Visibility = WidgetSettingsPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void WidgetToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (SectionKpi == null) return; // Not yet initialized
            SectionKpi.Visibility = WidgetKpi.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            SectionStatus.Visibility = WidgetStatus.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            SectionInbox.Visibility = WidgetInbox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            SectionFinance.Visibility = WidgetFinance.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            SectionDocSearch.Visibility = WidgetDocSearch.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            SectionQuickActions.Visibility = WidgetQuickActions.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            SectionDocs.Visibility = WidgetDocs.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            SectionCharts.Visibility = WidgetCharts.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            SectionActivity.Visibility = WidgetActivity.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            SectionDeadlines.Visibility = WidgetDeadlines.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateActivityFeed(List<Models.ActivityFeedItem> activities)
        {
            if (activities == null || activities.Count == 0)
            {
                ActivityEmptyText.Visibility = Visibility.Visible;
                ActivityFeedList.Visibility = Visibility.Collapsed;
            }
            else
            {
                ActivityEmptyText.Visibility = Visibility.Collapsed;
                ActivityFeedList.ItemsSource = activities;
                ActivityFeedList.Visibility = Visibility.Visible;
            }
        }

        public void UpdateDeadlines(List<Models.DeadlineItem> deadlines)
        {
            if (deadlines == null || deadlines.Count == 0)
            {
                DeadlinesEmptyText.Visibility = Visibility.Visible;
                DeadlinesList.Visibility = Visibility.Collapsed;
            }
            else
            {
                DeadlinesEmptyText.Visibility = Visibility.Collapsed;
                DeadlinesList.ItemsSource = deadlines;
                DeadlinesList.Visibility = Visibility.Visible;
            }
        }
    }
}
