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
    public long? EasybillCustomerId { get; set; }
    public long? EasybillProjectId { get; set; }
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

// ── Kundenportal ────────────────────────────────────────────────────

/// <summary>
/// Artikel aus dem lokalen Katalog mit bereits angewandtem Kundenrabatt für die Portal-Preisliste.
/// </summary>
public class PortalProductDto
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Unit { get; set; } = "Stück";
    public decimal ListNetPrice { get; set; }
    public int VatPercent { get; set; } = 19;
    public decimal DiscountPercent { get; set; }
    public decimal NetPrice => Math.Round(ListNetPrice * (1 - DiscountPercent / 100m), 2);
    public decimal GrossPrice => Math.Round(NetPrice * (1 + VatPercent / 100m), 2);
    public bool HasDiscount => DiscountPercent > 0;
}

/// <summary>
/// Ergebnis eines Portal-Logins.
/// </summary>
public record PortalLoginResponse(
    bool Success,
    int UserId,
    int? CustomerId,
    string Email,
    string ContactName,
    decimal DiscountPercent,
    string? Error);

/// <summary>
/// Ergebnis einer Portal-Registrierung.
/// </summary>
public record PortalRegisterResponse(
    bool Success,
    string? Error);

/// <summary>
/// Registrierungsdaten aus dem Neukundenformular des Kundenportals.
/// Legt zusätzlich zum Portal-Konto automatisch einen Kunden an.
/// </summary>
public record PortalRegisterRequest(
    string Email,
    string Password,
    string CompanyName,
    string FirstName,
    string LastName,
    string Phone,
    string Street,
    string ZipCode,
    string City,
    string Country,
    string VatId);

// ── Kundenportal: Webshop / Bestellungen ────────────────────────────

/// <summary>
/// Zahlungsart einer Portal-Bestellung. Standard ist immer Vorkasse;
/// Rechnung wird erst nach den ersten Bestellungen je nach Bonität freigeschaltet.
/// </summary>
public enum PortalPaymentMethod
{
    Prepayment = 0, // Vorkasse
    Invoice = 1     // Auf Rechnung
}

/// <summary>
/// Ein einzelner Warenkorb-/Bestellposten im Kundenportal.
/// </summary>
public class PortalCartItemDto
{
    public int ProductId { get; set; }
    public string Number { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "Stück";
    public decimal NetPrice { get; set; }
    public int VatPercent { get; set; } = 19;
    public int Quantity { get; set; } = 1;

    public decimal LineNet => Math.Round(NetPrice * Quantity, 2);
    public decimal LineGross => Math.Round(LineNet * (1 + VatPercent / 100m), 2);
}

/// <summary>
/// Bonitäts-/Bestellstatus eines Portal-Kunden, steuert die verfügbaren Zahlungsarten.
/// </summary>
public class PortalCustomerStatusDto
{
    public int CompletedOrderCount { get; set; }
    public bool InvoiceAllowed { get; set; }

    /// <summary>Rechnungskauf erst ab 2 Bestellungen und nur bei freigegebener Bonität.</summary>
    public bool CanPayByInvoice => CompletedOrderCount >= 2 && InvoiceAllowed;
}

/// <summary>
/// Bestellanfrage aus dem Portal-Warenkorb.
/// </summary>
public record PortalCheckoutRequest(
    int UserId,
    int? CustomerId,
    List<PortalCartItemDto> Items,
    PortalPaymentMethod PaymentMethod,
    string Note);

/// <summary>
/// Ergebnis eines Bestellabschlusses.
/// </summary>
public record PortalCheckoutResponse(
    bool Success,
    string? OrderNumber,
    string? Error);

/// <summary>
/// Bestellung für Anzeige im Portal (Kundensicht) und in WPF (Sachbearbeiter).
/// </summary>
public class PortalOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public decimal TotalNet { get; set; }
    public decimal TotalGross { get; set; }
    public PortalPaymentMethod PaymentMethod { get; set; }
    public int Status { get; set; } // 0=Neu, 1=In Bearbeitung, 2=Erledigt, 3=Storniert
    public string Note { get; set; } = "";
    public List<PortalCartItemDto> Items { get; set; } = new();

    public string PaymentMethodText => PaymentMethod == PortalPaymentMethod.Invoice ? "Auf Rechnung" : "Vorkasse";
    public string StatusText => Status switch
    {
        0 => "🆕 Neu",
        1 => "⏳ In Bearbeitung",
        2 => "✅ Erledigt",
        3 => "❌ Storniert",
        _ => "Unbekannt"
    };
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
    public int? CustomerId { get; set; }
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

// ── Sales / Leads ───────────────────────────────────────────────────

public class SalesLeadDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string ContactCompany { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string Source { get; set; } = "";
    public int Status { get; set; }
    public DateTime LeadDate { get; set; } = DateTime.Today;
    public string Notes { get; set; } = "";
    public bool HasFile { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

// ── Audit-Log ───────────────────────────────────────────────────────

public class AuditLogDto
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string UserName { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Action { get; set; } = "";
    public string Details { get; set; } = "";
}

// ── Globale Suche ───────────────────────────────────────────────────

public class SearchResultDto
{
    public string Type { get; set; } = "";
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
}

// ── Analytics / Berichte ────────────────────────────────────────────

public class AnalyticsDto
{
    // Tickets
    public int TicketsTotal { get; set; }
    public int TicketsOpen { get; set; }
    public int TicketsResolved { get; set; }
    public double TicketResolveRate { get; set; }

    // Leads / Vertrieb
    public int LeadsTotal { get; set; }
    public int LeadsNew { get; set; }
    public int LeadsInProgress { get; set; }
    public int LeadsWon { get; set; }
    public int LeadsLost { get; set; }
    public double LeadConversionRate { get; set; }

    // Projekte (Status -> Anzahl)
    public List<AnalyticsBucket> ProjectsByStatus { get; set; } = [];

    // Aufgaben
    public int TasksTotal { get; set; }
    public int TasksOpen { get; set; }
    public int TasksDone { get; set; }
    public int TasksOverdue { get; set; }
}

public class AnalyticsBucket
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
}

// ── Benutzerverwaltung ──────────────────────────────────────────────

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public string EmployeeName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}

// ── Budget-Tracking ─────────────────────────────────────────────────

public class ProjectBudgetDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public decimal TotalPlannedBudget { get; set; }
    public decimal TotalActualBudget { get; set; }
    public decimal TotalPlannedHours { get; set; }
    public decimal TotalActualHours { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime LastUpdated { get; set; }

    public decimal RemainingBudget => TotalPlannedBudget - TotalActualBudget;

    public decimal BudgetUtilizationPercentage =>
        TotalPlannedBudget == 0 ? 0 : Math.Round(TotalActualBudget / TotalPlannedBudget * 100m, 1);

    public string BudgetStatusColor
    {
        get
        {
            var p = BudgetUtilizationPercentage;
            if (p <= 75) return "#16a34a";
            if (p <= 90) return "#ca8a04";
            if (p <= 100) return "#ea580c";
            return "#dc2626";
        }
    }
}

public class BudgetEntryDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal? PlannedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public decimal? CostPerHour { get; set; }
    public DateTime EntryDate { get; set; } = DateTime.Now;
    public string? Notes { get; set; }

    public decimal Variance => ActualAmount - PlannedAmount;

    public decimal VariancePercentage =>
        PlannedAmount == 0 ? 0 : Math.Round(Variance / PlannedAmount * 100m, 1);
}

public class CategoryBreakdownDto
{
    public string Category { get; set; } = string.Empty;
    public decimal PlannedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance => ActualAmount - PlannedAmount;
}

public class BudgetOverviewDto
{
    public int ProjectId { get; set; }
    public ProjectBudgetDto Budget { get; set; } = new();
    public List<CategoryBreakdownDto> CategoryBreakdown { get; set; } = new();
    public decimal TotalVariance { get; set; }
    public decimal VariancePercentage { get; set; }
    public bool IsOverBudget { get; set; }
}

// ── SLA-Monitoring ──────────────────────────────────────────────────

public class SlaStatusDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string TicketSubject { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public int Priority { get; set; }
    public int Status { get; set; }
    public int? SlaRuleId { get; set; }
    public DateTime? FirstResponseDue { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolutionDue { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool IsBreached { get; set; }
    public string? BreachType { get; set; }
    public int EscalationLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string PriorityText => Priority switch { 0 => "Niedrig", 1 => "Mittel", 2 => "Hoch", 3 => "Kritisch", _ => "Mittel" };

    public string Health
    {
        get
        {
            if (IsBreached || (ResolutionDue.HasValue && DateTime.Now > ResolutionDue.Value)) return "Breached";
            if (ResolutionDue.HasValue && DateTime.Now > ResolutionDue.Value.AddHours(-1)) return "Warning";
            return "Healthy";
        }
    }

    public string HealthColor => Health switch
    {
        "Breached" => "#dc2626",
        "Warning" => "#ea580c",
        _ => "#16a34a"
    };

    public string TimeRemaining
    {
        get
        {
            if (!ResolutionDue.HasValue) return "–";
            var diff = ResolutionDue.Value - DateTime.Now;
            if (diff.TotalMinutes < 0) return $"überfällig seit {FormatSpan(diff.Negate())}";
            return $"noch {FormatSpan(diff)}";
        }
    }

    private static string FormatSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours} h {ts.Minutes} min";
        return $"{ts.Minutes} min";
    }
}

public class SlaSummaryDto
{
    public int TotalTickets { get; set; }
    public int HealthyTickets { get; set; }
    public int WarningTickets { get; set; }
    public int BreachedTickets { get; set; }
    public decimal BreachPercentage { get; set; }
}

// ── Follow-up-Erinnerungen ──────────────────────────────────────────

public class FollowUpReminderDto
{
    public int Id { get; set; }
    public int? LeadId { get; set; }
    public string LeadTitle { get; set; } = "";
    public string ContactName { get; set; } = "";
    public DateTime DueDate { get; set; }
    public string Note { get; set; } = "";
    public bool Completed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsOverdue => !Completed && DueDate.Date < DateTime.Now.Date;
    public bool IsDueToday => !Completed && DueDate.Date == DateTime.Now.Date;

    public string StatusColor =>
        Completed ? "#16a34a" : IsOverdue ? "#dc2626" : IsDueToday ? "#ea580c" : "#2563eb";
}

// ── MwSt-Ermittlung ─────────────────────────────────────────────────

public class VatResultDto
{
    public string Scenario { get; set; } = "";
    public int VatPercent { get; set; }
    public string DisplayText { get; set; } = "";
    public string LegalNotice { get; set; } = "";
    public string DocumentSuffix { get; set; } = "";
    public string InfoColor { get; set; } = "#2196F3";
    public string? CustomerCountry { get; set; }
    public string? CustomerVatId { get; set; }
    public bool IsTaxFree => VatPercent == 0;
}

// ── Projektvorlagen ─────────────────────────────────────────────────

public class TemplateTaskDto
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Priority { get; set; } = "Normal";
    public int DueAfterDays { get; set; } = 7;
}

public class ProjectTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int DefaultDurationDays { get; set; } = 30;
    public List<TemplateTaskDto> Tasks { get; set; } = new();
}

// ── KPI-Dashboard ───────────────────────────────────────────────────

public class DashboardKpiDto
{
    public string KpiType { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal CurrentValue { get; set; }
    public string Unit { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "Green";

    public string ColorHex => Color switch
    {
        "Red" => "#dc2626",
        "Orange" => "#ea580c",
        "Yellow" => "#ca8a04",
        _ => "#16a34a"
    };

    public string ValueText => CurrentValue == Math.Floor(CurrentValue)
        ? $"{CurrentValue:0} {Unit}".Trim()
        : $"{CurrentValue:0.0} {Unit}".Trim();
}

// ── Lead-Statistik ──────────────────────────────────────────────────

public class LeadStatisticsDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public int LostLeads { get; set; }
    public int ActiveLeads { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageDealValue { get; set; }

    public string PeriodName => $"{PeriodStart:dd.MM.yyyy} – {PeriodEnd:dd.MM.yyyy}";
    public string ConversionRateFormatted => $"{ConversionRate:N1}%";
    public List<LeadSourceStatDto> Sources { get; set; } = new();
}

public class LeadSourceStatDto
{
    public string SourceName { get; set; } = "";
    public int TotalLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public decimal ConversionRate { get; set; }
    public string ConversionRateFormatted => $"{ConversionRate:N1}%";
}

// ── Fälligkeits-Warnungen ───────────────────────────────────────────

public class DueDateWarningDto
{
    public string EntityType { get; set; } = "";
    public int EntityId { get; set; }
    public string EntityTitle { get; set; } = "";
    public DateTime DueDate { get; set; }
    public string AssignedTo { get; set; } = "";
    public string Priority { get; set; } = "";
    public string WarningLevel { get; set; } = "";

    public string WarningColor => WarningLevel switch
    {
        "Overdue" => "#dc2626",
        "DueToday" => "#ea580c",
        "DueTomorrow" => "#ca8a04",
        _ => "#2563eb"
    };

    public string WarningText => WarningLevel switch
    {
        "Overdue" => "Überfällig",
        "DueToday" => "Heute fällig",
        "DueTomorrow" => "Morgen fällig",
        _ => "Diese Woche"
    };

    public string EntityIcon => EntityType switch
    {
        "Task" => "✅",
        "Ticket" => "🎫",
        "Lead" => "💼",
        "Project" => "📁",
        _ => "📌"
    };
}

// ── Sales-Kalender ──────────────────────────────────────────────────

public class SalesAppointmentDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string ContactCompany { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public DateTime AppointmentDate { get; set; } = DateTime.Today.AddHours(9);
    public DateTime AppointmentEnd { get; set; } = DateTime.Today.AddHours(10);
    public string Location { get; set; } = "";
    public string Notes { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string ICalUid { get; set; } = "";
    public string WebexJoinLink { get; set; } = "";
    public int RsvpStatus { get; set; }

    public string RsvpIcon => RsvpStatus switch
    {
        1 => "✅",
        2 => "❌",
        3 => "❓",
        _ => "⏳"
    };

    public string RsvpText => RsvpStatus switch
    {
        1 => "Angenommen",
        2 => "Abgesagt",
        3 => "Vielleicht",
        _ => "Ausstehend"
    };

    public string RsvpColor => RsvpStatus switch
    {
        1 => "var(--success)",
        2 => "var(--danger)",
        3 => "var(--warning)",
        _ => "var(--gray)"
    };

    public string DurationText
    {
        get
        {
            var d = AppointmentEnd - AppointmentDate;
            return d.TotalHours >= 1
                ? $"{(int)d.TotalHours}h {d.Minutes:00}min"
                : $"{d.Minutes} min";
        }
    }

    public bool IsPast => AppointmentEnd < DateTime.Now;
}

// ── Lieferanten-Bewertung ───────────────────────────────────────────

public class SupplierRatingDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = "";
    public DateTime RatingDate { get; set; } = DateTime.Now;
    public int QualityRating { get; set; } = 5;
    public int DeliveryRating { get; set; } = 5;
    public int PriceRating { get; set; } = 5;
    public int ServiceRating { get; set; } = 5;
    public int CommunicationRating { get; set; } = 5;
    public decimal OverallRating { get; set; } = 5.0m;
    public string ReviewText { get; set; } = "";
    public string Pros { get; set; } = "";
    public string Cons { get; set; } = "";
    public bool WouldRecommend { get; set; } = true;
    public string RatedBy { get; set; } = "";
    public string OrderReference { get; set; } = "";

    public string StarsDisplay => new string('⭐', (int)Math.Round(OverallRating));

    public string RatingColor =>
        OverallRating >= 4.0m ? "#16a34a" :
        OverallRating >= 3.0m ? "#ca8a04" :
        OverallRating >= 2.0m ? "#ea580c" : "#dc2626";

    public void CalculateOverall() =>
        OverallRating = (QualityRating + DeliveryRating + PriceRating + ServiceRating + CommunicationRating) / 5.0m;
}

// ── Ausgaben-Analyse ────────────────────────────────────────────────

public class ExpenseAnalysisDto
{
    public decimal TotalExpenses { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<ExpenseBySupplierDto> BySupplier { get; set; } = new();
    public List<ExpenseByMonthDto> ByMonth { get; set; } = new();

    public string PeriodName => $"{PeriodStart:dd.MM.yyyy} – {PeriodEnd:dd.MM.yyyy}";
}

public class ExpenseBySupplierDto
{
    public string SupplierName { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public int OrderCount { get; set; }
    public decimal Percentage { get; set; }
}

public class ExpenseByMonthDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalAmount { get; set; }
    public int OrderCount { get; set; }
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
}
