
using Alten.ProductManagementApi.Models;

namespace Alten.ProductManagementApi.Services.Interfaces;

public interface ICartService
{
    Task<IEnumerable<CartItem>> GetCartItemsByUserIdAsync(int userId);
    Task<CartItem?> GetCartItemByIdAsync(int id);
    Task<CartItem> AddOrUpdateCartItemAsync(CartItem cartItem);
    Task<bool> RemoveCartItemAsync(int userId, int productId);
    Task<bool> ClearCartAsync(int userId);
}
