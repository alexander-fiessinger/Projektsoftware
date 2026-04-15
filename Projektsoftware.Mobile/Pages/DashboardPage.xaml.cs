using Projektsoftware.Mobile.Services;

namespace Projektsoftware.Mobile.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly ApiService _api;

    public DashboardPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        GreetingLabel.Text = $"Hallo, {_api.CurrentUser ?? "Benutzer"} 👋";
        await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        StatsGrid.IsVisible = false;

        try
        {
            var data = await _api.GetDashboardAsync();
            if (data is not null)
            {
                TotalProjectsLabel.Text = data.TotalProjects.ToString();
                ActiveProjectsLabel.Text = data.ActiveProjects.ToString();
                CompletedProjectsLabel.Text = data.CompletedProjects.ToString();
                TotalTasksLabel.Text = data.TotalTasks.ToString();
                OpenTasksLabel.Text = data.OpenTasks.ToString();
                CompletedTasksLabel.Text = data.CompletedTasks.ToString();
                OverdueTasksLabel.Text = data.OverdueTasks.ToString();
                HoursLabel.Text = data.TotalHoursLogged.ToString("F0");
                EmployeesLabel.Text = data.ActiveEmployees.ToString();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", $"Dashboard konnte nicht geladen werden: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            StatsGrid.IsVisible = true;
        }
    }
}
