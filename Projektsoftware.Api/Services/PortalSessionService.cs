namespace Projektsoftware.Api.Services;

/// <summary>
/// Hält den Login-Zustand eines Kundenportal-Kontos für die aktuelle Blazor-Circuit-Session.
/// Registriert als Scoped (pro SignalR-Verbindung) und getrennt von der Mitarbeiter-Auth
/// (<see cref="SessionService"/>).
/// </summary>
public class PortalSessionService
{
    public bool IsAuthenticated { get; private set; }
    public int UserId { get; private set; }
    public int? CustomerId { get; private set; }
    public string Email { get; private set; } = "";
    public string ContactName { get; private set; } = "";
    public decimal DiscountPercent { get; private set; }

    public void Login(int userId, int? customerId, string email, string contactName, decimal discountPercent)
    {
        IsAuthenticated = true;
        UserId = userId;
        CustomerId = customerId;
        Email = email;
        ContactName = contactName;
        DiscountPercent = discountPercent;
    }

    public void Logout()
    {
        IsAuthenticated = false;
        UserId = 0;
        CustomerId = null;
        Email = "";
        ContactName = "";
        DiscountPercent = 0;
    }
}
