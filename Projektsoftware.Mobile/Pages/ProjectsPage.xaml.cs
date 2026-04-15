using System.Globalization;
using Projektsoftware.Mobile.Services;

namespace Projektsoftware.Mobile.Pages;

public partial class ProjectsPage : ContentPage
{
    private readonly ApiService _api;

    public ProjectsPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProjectsAsync();
    }

    private async Task LoadProjectsAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            var projects = await _api.GetProjectsAsync();
            ProjectsList.ItemsSource = projects.Select(p => new ProjectDisplayModel(p)).ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", $"Projekte konnten nicht geladen werden: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }
}

public class ProjectDisplayModel
{
    private readonly ProjectDto _dto;

    public ProjectDisplayModel(ProjectDto dto) => _dto = dto;

    public string Name => _dto.Name;
    public string Status => _dto.Status;
    public string ClientName => _dto.ClientName;
    public double ProgressDecimal => _dto.ProgressPercent / 100.0;
    public string ProgressText => $"{_dto.ProgressPercent}%";
    public string DateRange =>
        _dto.EndDate.HasValue
            ? $"{_dto.StartDate:dd.MM.yyyy} – {_dto.EndDate.Value:dd.MM.yyyy}"
            : $"Ab {_dto.StartDate:dd.MM.yyyy}";
}

public class StringToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrEmpty(s);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
