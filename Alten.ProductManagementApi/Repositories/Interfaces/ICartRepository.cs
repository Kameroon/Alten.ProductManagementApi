
using Alten.ProductManagementApi.Models;

namespace Alten.ProductManagementApi.Repositories.Interfaces;

public interface ICartRepository
{
    Task<IEnumerable<CartItem>> GetCartItemsByUserIdAsync(int userId);
    Task<CartItem?> GetCartItemByIdAsync(int id);
    Task<CartItem?> GetCartItemByUserIdAndProductIdAsync(int userId, int productId);
    Task<CartItem> AddCartItemAsync(CartItem cartItem); // Retourne l'objet complet avec l'ID généré
    Task<bool> UpdateCartItemAsync(CartItem cartItem);
    Task<bool> DeleteCartItemAsync(int userId, int productId);
    Task<bool> DeleteAllCartItemsByUserIdAsync(int userId);
}