namespace Projektsoftware.Api.Models;

// ── Authentication ──────────────────────────────────────────────────

public record LoginRequest(string Username, string Password);

public record LoginResponse(bool Success, string? Token, string? Username, string? Role, int UserId, string? Error);

// ── Dashboard ───────────────────────────────────────────────────────

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
    public int TotalCustomers { get; set; }
    public int OpenTickets { get; set; }
    public int CrmDeals { get; set; }
    public int UpcomingMeetings { get; set; }
    public int TotalSuppliers { get; set; }
}

// ── Projects ────────────────────────────────────────────────────────

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "Aktiv";
    public string ClientName { get; set; } = "";
    public decimal Budget { get; set; }
    public string Tags { get; set; } = "";
    public int ProgressPercent { get; set; }
}

// ── Tasks ───────────────────────────────────────────────────────────

public class TaskDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string AssignedTo { get; set; } = "";
    public string Status { get; set; } = "Offen";
    public string Priority { get; set; } = "Normal";
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int EstimatedHours { get; set; }
    public int ActualHours { get; set; }
}

public record TaskStatusUpdateRequest(string Status);

public record TaskCreateRequest(
    int ProjectId,
    string Title,
    string Description,
    string AssignedTo,
    string Status,
    string Priority,
    DateTime? DueDate,
    int EstimatedHours);

// ── Employees ───────────────────────────────────────────────────────

public class EmployeeDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Position { get; set; } = "";
    public string Department { get; set; } = "";
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime HireDate { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}

// ── Time Entries ────────────────────────────────────────────────────

public class TimeEntryDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public DateTime Date { get; set; }
    public TimeSpan Duration { get; set; }
    public string Description { get; set; } = "";
    public string Activity { get; set; } = "";
}

public record TimeEntryCreateRequest(
    int ProjectId,
    string EmployeeName,
    DateTime Date,
    double DurationHours,
    string Description,
    string Activity);

// ── Customers ───────────────────────────────────────────────────────

public class CustomerDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Street { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "Deutschland";
    public string Note { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string DisplayName => !string.IsNullOrEmpty(CompanyName) ? CompanyName : $"{FirstName} {LastName}".Trim();
}

// ── Tickets ─────────────────────────────────────────────────────────

public class TicketDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Description { get; set; } = "";
    public int Priority { get; set; }
    public int Status { get; set; }
    public int Category { get; set; }
    public string AssignedToEmployeeName { get; set; } = "";
    public string Resolution { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public string PriorityText => Priority switch { 0 => "Niedrig", 1 => "Mittel", 2 => "Hoch", 3 => "Kritisch", _ => "Mittel" };
    public string StatusText => Status switch { 0 => "Neu", 1 => "Offen", 2 => "In Bearbeitung", 3 => "Warten", 4 => "Gelöst", 5 => "Geschlossen", _ => "Neu" };
    public string CategoryText => Category switch { 0 => "Allgemein", 1 => "Bug", 2 => "Feature", 3 => "Support", _ => "Allgemein" };
}

// ── Meeting Protocols ───────────────────────────────────────────────

public class MeetingProtocolDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime MeetingDate { get; set; }
    public string Location { get; set; } = "";
    public string Participants { get; set; } = "";
    public string Agenda { get; set; } = "";
    public string Discussion { get; set; } = "";
    public string Decisions { get; set; } = "";
    public string ActionItems { get; set; } = "";
}

// ── CRM Contacts ────────────────────────────────────────────────────

public class CrmContactDto
{
    public int Id { get; set; }
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Position { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string DisplayName => $"{FirstName} {LastName}".Trim();
}

// ── CRM Activities ──────────────────────────────────────────────────

public class CrmActivityDto
{
    public int Id { get; set; }
    public string ContactName { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public int Type { get; set; }
    public string Subject { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public string TypeText => Type switch { 0 => "📝 Notiz", 1 => "📞 Anruf", 2 => "✉️ E-Mail", 3 => "🤝 Meeting", 4 => "✓ Aufgabe", _ => "Aktivität" };
    public string StatusText => IsCompleted ? "✅ Erledigt" : (DueDate.HasValue && DueDate.Value < DateTime.Now ? "⚠️ Überfällig" : "⏳ Offen");
}

// ── CRM Deals ───────────────────────────────────────────────────────

public class CrmDealDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal Value { get; set; }
    public int Stage { get; set; }
    public int Probability { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public string Notes { get; set; } = "";
    public string AssignedTo { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public string StageText => Stage switch { 0 => "🔵 Lead", 1 => "🟡 Qualifiziert", 2 => "🟠 Angebot", 3 => "🔴 Verhandlung", 4 => "✅ Gewonnen", 5 => "❌ Verloren", _ => "Unbekannt" };
    public decimal WeightedValue => Value * Probability / 100m;
}

// ── Meetings ────────────────────────────────────────────────────────

public class MeetingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Location { get; set; }
    public string? Participants { get; set; }
    public string? ProjectName { get; set; }
    public DateTime CreatedAt { get; set; }

    // Webex
    public bool IsWebexMeeting { get; set; }
    public string? WebexMeetingId { get; set; }
    public string? WebexJoinLink { get; set; }
    public string? WebexHostKey { get; set; }
    public string? WebexPassword { get; set; }
    public string? WebexSipAddress { get; set; }

    public string DisplayTime => $"{StartTime:HH:mm} – {EndTime:HH:mm}";
    public string DisplayDate => StartTime.ToString("dd.MM.yyyy");
    public string DurationText
    {
        get
        {
            var d = EndTime - StartTime;
            return d.TotalHours >= 1 ? $"{(int)d.TotalHours}h {d.Minutes:D2}m" : $"{d.Minutes}m";
        }
    }
    public bool IsToday => StartTime.Date == DateTime.Today;
    public bool IsPast => EndTime < DateTime.Now;
    public string StatusText => IsPast ? "Vergangen" : (StartTime <= DateTime.Now && EndTime >= DateTime.Now ? "Läuft" : "Geplant");
}

public class WebexUserSettings
{
    public int UserId { get; set; }
    public string AccessToken { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime TokenExpiry { get; set; } = DateTime.MinValue;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(AccessToken);
    public bool CanRefresh => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret) && !string.IsNullOrWhiteSpace(RefreshToken);
    public bool IsTokenExpired => TokenExpiry != DateTime.MinValue && DateTime.UtcNow >= TokenExpiry.AddMinutes(-5);
}

// ── Suppliers ───────────────────────────────────────────────────────

public class SupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ContactPerson { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "Deutschland";
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string DisplayAddress => $"{ZipCode} {City}".Trim();
}

// ── Purchase Orders ─────────────────────────────────────────────────

public class PurchaseOrderDto
{
    public int Id { get; set; }
    public string SupplierName { get; set; } = "";
    public string OrderNumber { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public DateTime? DeliveryDateExpected { get; set; }
    public string Status { get; set; } = "Offen";
    public decimal TotalNet { get; set; }
    public decimal TotalGross { get; set; }
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public string StatusDisplay => Status switch { "Offen" => "🟡 Offen", "Bestellt" => "🔵 Bestellt", "Teilweise geliefert" => "🟠 Teilw. geliefert", "Geliefert" => "🟢 Geliefert", "Storniert" => "🔴 Storniert", _ => Status };
    public string TotalNetDisplay => TotalNet.ToString("N2") + " €";
    public string TotalGrossDisplay => TotalGross.ToString("N2") + " €";
}

// ── Notifications ───────────────────────────────────────────────────

public class NotificationDto
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "Info";
    public DateTime Timestamp { get; set; }
}
