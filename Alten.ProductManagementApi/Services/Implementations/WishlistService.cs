using Alten.ProductManagementApi.Models;
using Alten.ProductManagementApi.Repositories.Implementations;
using Alten.ProductManagementApi.Repositories.Interfaces;
using Alten.ProductManagementApi.Services.Interfaces;

namespace Alten.ProductManagementApi.Services.Implementations;

public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IProductRepository _productRepository; 

    public WishlistService(IWishlistRepository wishlistRepository, IProductRepository productRepository)
    {
        _wishlistRepository = wishlistRepository;
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<WishlistItem>> GetWishlistItemsByUserIdAsync(int userId)
    {
        return await _wishlistRepository.GetWishlistItemsByUserIdAsync(userId);
    }

    //public async Task<WishlistItem?> GetWishlistItemByIdAsync(int id)
    //{
    //    return await _wishlistRepository.GetWishlistItemByIdAsync(id);
    //}

    public async Task<WishlistItem> AddWishlistItemAsync(WishlistItem wishlistItem)
    {
        if (await CheckExistingProduct(wishlistItem.ProductId) == null)
            throw new KeyNotFoundException($"Aucun produit trouvé avec l'ID {wishlistItem.ProductId}.");

        var existingWishlistItem = await CheckExistingUserIdAndProductIdInWishlistItem(wishlistItem.UserId, wishlistItem.ProductId);
        if (existingWishlistItem != null)
            return existingWishlistItem;

        wishlistItem.AddedAt = DateTime.UtcNow;
        return await _wishlistRepository.AddWishlistItemAsync(wishlistItem);
    }

    public async Task<bool> RemoveWishlistItemAsync(int userId, int productId)
    {
        var existingWishlistItem = await CheckExistingUserIdAndProductIdInWishlistItem(userId, productId);
        if (existingWishlistItem != null)
            return false;

        return await _wishlistRepository.DeleteWishlistItemAsync(userId, productId);
    }

    private async Task<WishlistItem?> CheckExistingUserIdAndProductIdInWishlistItem(int userId, int productId)
        => await _wishlistRepository.GetWishlistItemByUserIdAndProductIdAsync(userId, productId);

    public async Task<bool> ClearWishlistAsync(int userId) => await _wishlistRepository.DeleteAllWishlistItemsByUserIdAsync(userId);

    /// <summary>
    /// -- Checks if a product with the given ID exists in the repository.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private async Task<Product?> CheckExistingProduct(int id) => await _productRepository.GetProductByIdAsync(id);
}
