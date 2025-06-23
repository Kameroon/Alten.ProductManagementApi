using Alten.ProductManagementApi.Models;

namespace Alten.ProductManagementApi.Services.Interfaces;

public interface IWishlistService
{
    Task<IEnumerable<WishlistItem>> GetWishlistItemsByUserIdAsync(int userId);
    Task<WishlistItem> AddWishlistItemAsync(WishlistItem wishlistItem);
    Task<bool> RemoveWishlistItemAsync(int userId, int productId);
    Task<bool> ClearWishlistAsync(int userId);
}
