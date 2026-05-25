using System.Text.Json;
using EventTicketSystem.Web.Models;

namespace EventTicketSystem.Web.Services;

public class CartService
{
    private const string CartKey = "ShoppingCart";

    public List<CartItem> GetCart(ISession session)
    {
        var json = session.GetString(CartKey);
        return string.IsNullOrEmpty(json)
            ? []
            : JsonSerializer.Deserialize<List<CartItem>>(json) ?? [];
    }

    public void SaveCart(ISession session, List<CartItem> cart) =>
        session.SetString(CartKey, JsonSerializer.Serialize(cart));

    public void AddToCart(ISession session, CartItem newItem)
    {
        var cart = GetCart(session);
        var existing = cart.FirstOrDefault(c => c.TicketTypeId == newItem.TicketTypeId);
        if (existing != null)
            existing.Quantity = Math.Min(existing.Quantity + newItem.Quantity, 10);
        else
            cart.Add(newItem);
        SaveCart(session, cart);
    }

    public void UpdateQuantity(ISession session, int ticketTypeId, int qty)
    {
        var cart = GetCart(session);
        var item = cart.FirstOrDefault(c => c.TicketTypeId == ticketTypeId);
        if (item != null)
        {
            item.Quantity = Math.Max(1, Math.Min(qty, 10));
            SaveCart(session, cart);
        }
    }

    public void RemoveFromCart(ISession session, int ticketTypeId)
    {
        var cart = GetCart(session);
        cart.RemoveAll(c => c.TicketTypeId == ticketTypeId);
        SaveCart(session, cart);
    }

    public void ClearCart(ISession session) => session.Remove(CartKey);

    public int GetCartCount(ISession session) => GetCart(session).Sum(c => c.Quantity);
}
