namespace Projektsoftware.Api.Services;

/// <summary>
/// Hält den Login-Zustand für die aktuelle Blazor-Circuit-Session.
/// Registriert als Scoped (pro SignalR-Verbindung).
/// Berechtigungen werden beim Login aus der DB geladen (wie Desktop PermissionService).
/// </summary>
public class SessionService
{
    public bool IsAuthenticated { get; private set; }
    public int UserId { get; private set; }
    public string Username { get; private set; } = "";
    public string Role { get; private set; } = "";

    private readonly HashSet<string> _allowedModules = new(StringComparer.OrdinalIgnoreCase);
    private bool _isAdmin;

    public void Login(int userId, string username, string role, List<string> permissions)
    {
        IsAuthenticated = true;
        UserId = userId;
        Username = username;
        Role = role;
        _isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        _allowedModules.Clear();
        foreach (var p in permissions)
            _allowedModules.Add(p);
    }

    /// <summary>
    /// Erstellt einen serialisierbaren Snapshot des aktuellen Login-Zustands,
    /// damit die Session über einen Seiten-Reload hinweg wiederhergestellt werden kann.
    /// </summary>
    public SessionSnapshot? CreateSnapshot()
    {
        if (!IsAuthenticated) return null;
        return new SessionSnapshot
        {
            UserId = UserId,
            Username = Username,
            Role = Role,
            Permissions = _allowedModules.ToList()
        };
    }

    /// <summary>
    /// Stellt den Login-Zustand aus einem zuvor gespeicherten Snapshot wieder her
    /// (z. B. nach einem Reload aus dem verschlüsselten Browser-Storage).
    /// </summary>
    public void Restore(SessionSnapshot snapshot)
    {
        if (snapshot is null || snapshot.UserId <= 0) return;
        Login(snapshot.UserId, snapshot.Username, snapshot.Role, snapshot.Permissions ?? new List<string>());
    }

    /// <summary>
    /// Prüft ob der aktuelle Benutzer Zugriff auf ein Modul hat.
    /// Admins haben immer Zugriff; Dashboard ist immer sichtbar.
    /// </summary>
    public bool HasAccess(string moduleKey)
    {
        if (_isAdmin) return true;
        if (string.Equals(moduleKey, "dashboard", StringComparison.OrdinalIgnoreCase)) return true;
        return _allowedModules.Contains(moduleKey);
    }

    public void Logout()
    {
        IsAuthenticated = false;
        UserId = 0;
        Username = "";
        Role = "";
        _isAdmin = false;
        _allowedModules.Clear();
    }
}

/// <summary>
/// Serialisierbarer Snapshot des Login-Zustands für die Wiederherstellung nach Reload.
/// </summary>
public class SessionSnapshot
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string Role { get; set; } = "";
    public List<string> Permissions { get; set; } = new();
}