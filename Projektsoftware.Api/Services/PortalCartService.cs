using Projektsoftware.Api.Models;

namespace Projektsoftware.Api.Services;

/// <summary>
/// Hält den Warenkorb eines Portal-Kunden für die aktuelle Blazor-Circuit-Session.
/// Registriert als Scoped (pro SignalR-Verbindung), analog zu <see cref="PortalSessionService"/>.
/// </summary>
public class PortalCartService
{
    private readonly List<PortalCartItemDto> _items = new();

    public IReadOnlyList<PortalCartItemDto> Items => _items;

    public int TotalQuantity => _items.Sum(i => i.Quantity);

    public bool IsEmpty => _items.Count == 0;

    public decimal TotalNet => Math.Round(_items.Sum(i => i.LineNet), 2);

    public decimal TotalGross => Math.Round(_items.Sum(i => i.LineGross), 2);

    /// <summary>Benachrichtigt UI-Komponenten (z. B. Header-Badge) über Änderungen am Warenkorb.</summary>
    public event Action? OnChange;

    /// <summary>
    /// Fügt einen Artikel hinzu oder erhöht die Menge, falls er bereits enthalten ist.
    /// Der übergebene Nettopreis ist bereits der kundenspezifische Rabattpreis.
    /// </summary>
    public void Add(int productId, string number, string name, string unit, decimal netPrice, int vatPercent, int quantity = 1)
    {
        if (quantity < 1) quantity = 1;

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            _items.Add(new PortalCartItemDto
            {
                ProductId = productId,
                Number = number,
                Name = name,
                Unit = string.IsNullOrWhiteSpace(unit) ? "Stück" : unit,
                NetPrice = netPrice,
                VatPercent = vatPercent,
                Quantity = quantity
            });
        }
        OnChange?.Invoke();
    }

    public void SetQuantity(int productId, int quantity)
    {
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing == null) return;

        if (quantity < 1)
            _items.Remove(existing);
        else
            existing.Quantity = quantity;

        OnChange?.Invoke();
    }

    public void Remove(int productId)
    {
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing != null)
        {
            _items.Remove(existing);
            OnChange?.Invoke();
        }
    }

    public void Clear()
    {
        if (_items.Count == 0) return;
        _items.Clear();
        OnChange?.Invoke();
    }

    /// <summary>Erstellt eine Kopie der Warenkorbpositionen für den Bestellabschluss.</summary>
    public List<PortalCartItemDto> Snapshot() => _items
        .Select(i => new PortalCartItemDto
        {
            ProductId = i.ProductId,
            Number = i.Number,
            Name = i.Name,
            Unit = i.Unit,
            NetPrice = i.NetPrice,
            VatPercent = i.VatPercent,
            Quantity = i.Quantity
        })
        .ToList();
}
