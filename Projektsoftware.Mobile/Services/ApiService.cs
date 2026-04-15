using System.Net.Http.Json;

namespace Projektsoftware.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private string? _token;

    public string? CurrentUser { get; private set; }
    public string? CurrentRole { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public ApiService(HttpClient http)
    {
        _http = http;
    }

    // ── Auth ─────────────────────────────────────────────────────

    public async Task<(bool Success, string? Error)> LoginAsync(string username, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login",
            new { Username = username, Password = password });

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (result is { Success: true })
        {
            _token = result.Token;
            CurrentUser = result.Username;
            CurrentRole = result.Role;
            return (true, null);
        }
        return (false, result?.Error ?? "Verbindungsfehler");
    }

    public void Logout()
    {
        _token = null;
        CurrentUser = null;
        CurrentRole = null;
    }

    // ── Dashboard ────────────────────────────────────────────────

    public async Task<DashboardDto?> GetDashboardAsync()
    {
        return await _http.GetFromJsonAsync<DashboardDto>("api/dashboard");
    }

    // ── Projects ─────────────────────────────────────────────────

    public async Task<List<ProjectDto>> GetProjectsAsync()
    {
        return await _http.GetFromJsonAsync<List<ProjectDto>>("api/projects") ?? [];
    }

    // ── Tasks ────────────────────────────────────────────────────

    public async Task<List<TaskDto>> GetTasksAsync(int? projectId = null)
    {
        var url = projectId.HasValue ? $"api/tasks?projectId={projectId}" : "api/tasks";
        return await _http.GetFromJsonAsync<List<TaskDto>>(url) ?? [];
    }

    public async Task<bool> UpdateTaskStatusAsync(int taskId, string newStatus)
    {
        var response = await _http.PatchAsJsonAsync($"api/tasks/{taskId}/status",
            new { Status = newStatus });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CreateTaskAsync(TaskCreateRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/tasks", request);
        return response.IsSuccessStatusCode;
    }
}

// ── DTOs (match API models) ──────────────────────────────────────

public record LoginResponse(bool Success, string? Token, string? Username, string? Role, string? Error);

public class DashboardDto
{
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int TotalTasks { get; set; }
    public int OpenTasks { get; set; }
    public int CompletedTasks { get; set; }
    public decimal TotalHoursLogged { get; set; }
    public int OverdueTasks { get; set; }
    public int ActiveEmployees { get; set; }
}

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "";
    public string ClientName { get; set; } = "";
    public decimal Budget { get; set; }
    public string Tags { get; set; } = "";
    public int ProgressPercent { get; set; }
}

public class TaskDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string AssignedTo { get; set; } = "";
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int EstimatedHours { get; set; }
    public int ActualHours { get; set; }

    public string DueDateDisplay => DueDate.HasValue ? $"Fällig: {DueDate.Value:dd.MM.yyyy}" : "";
    public string PriorityColor => Priority switch
    {
        "Kritisch" => "#C62828",
        "Hoch" => "#E65100",
        "Normal" => "#1565C0",
        "Niedrig" => "#2E7D32",
        _ => "#757575"
    };
    public string StatusColor => Status switch
    {
        "Offen" => "#E65100",
        "In Arbeit" => "#1565C0",
        "Blockiert" => "#C62828",
        "Erledigt" => "#2E7D32",
        _ => "#757575"
    };
}

public record TaskCreateRequest(
    int ProjectId, string Title, string Description, string AssignedTo,
    string Status, string Priority, DateTime? DueDate, int EstimatedHours);
