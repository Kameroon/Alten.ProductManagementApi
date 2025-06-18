using Alten.ProductManagementApi.Models;

namespace Alten.ProductManagementApi.Repositories.Interfaces;

public interface IWishlistRepository
{
    Task<IEnumerable<WishlistItem>> GetWishlistItemsByUserIdAsync(int userId);
    Task<WishlistItem?> GetWishlistItemByIdAsync(int id);
    Task<WishlistItem?> GetWishlistItemByUserIdAndProductIdAsync(int userId, int productId); // Pour vérifier les doublons
    Task<WishlistItem> AddWishlistItemAsync(WishlistItem wishlistItem);
    Task<bool> DeleteWishlistItemAsync(int userId, int productId);
    Task<bool> DeleteAllWishlistItemsByUserIdAsync(int userId); // Pour vider la liste d'envies
}