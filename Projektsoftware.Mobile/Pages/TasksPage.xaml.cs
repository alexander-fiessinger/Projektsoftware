using Projektsoftware.Mobile.Services;

namespace Projektsoftware.Mobile.Pages;

public partial class TasksPage : ContentPage
{
    private readonly ApiService _api;
    private List<TaskDisplayModel> _tasks = [];

    public TasksPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTasksAsync();
    }

    private async Task LoadTasksAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            var tasks = await _api.GetTasksAsync();
            _tasks = tasks.Select(t => new TaskDisplayModel(t)).ToList();
            TasksList.ItemsSource = _tasks;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", $"Aufgaben konnten nicht geladen werden: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private async void OnChangeStatus(object? sender, EventArgs e)
    {
        TaskDisplayModel? task = null;

        if (sender is SwipeItem swipeItem)
            task = swipeItem.BindingContext as TaskDisplayModel;

        if (task is null) return;

        var statuses = new[] { "Offen", "In Arbeit", "Blockiert", "Erledigt" };
        var result = await DisplayActionSheet(
            $"Status für \"{task.Title}\"",
            "Abbrechen", null,
            statuses);

        if (result is null or "Abbrechen" || result == task.Status) return;

        var success = await _api.UpdateTaskStatusAsync(task.Id, result);
        if (success)
        {
            await LoadTasksAsync();
        }
        else
        {
            await DisplayAlert("Fehler", "Status konnte nicht geändert werden.", "OK");
        }
    }
}

public class TaskDisplayModel
{
    private readonly TaskDto _dto;

    public TaskDisplayModel(TaskDto dto) => _dto = dto;

    public int Id => _dto.Id;
    public string Title => _dto.Title;
    public string ProjectName => _dto.ProjectName;
    public string Status => _dto.Status;
    public string Priority => _dto.Priority;
    public string AssignedTo => string.IsNullOrEmpty(_dto.AssignedTo) ? "" : $"👤 {_dto.AssignedTo}";
    public bool HasAssignee => !string.IsNullOrEmpty(_dto.AssignedTo);
    public string DueDateDisplay => _dto.DueDateDisplay;

    public Color PriorityBackgroundColor => Color.FromArgb(_dto.PriorityColor);
    public Color StatusBackgroundColor => Color.FromArgb(_dto.StatusColor);
}
