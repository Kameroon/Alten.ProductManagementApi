using Alten.ProductManagementApi.Models;
using Alten.ProductManagementApi.Repositories.Interfaces;
using Alten.ProductManagementApi.Services.Interfaces;

namespace Alten.ProductManagementApi.Services.Implementations;

public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IProductRepository _productRepository; // Nécessaire pour vérifier l'existence du produit

    public WishlistService(IWishlistRepository wishlistRepository, IProductRepository productRepository)
    {
        _wishlistRepository = wishlistRepository;
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<WishlistItem>> GetWishlistItemsByUserIdAsync(int userId)
    {
        return await _wishlistRepository.GetWishlistItemsByUserIdAsync(userId);
    }

    public async Task<WishlistItem?> GetWishlistItemByIdAsync(int id)
    {
        return await _wishlistRepository.GetWishlistItemByIdAsync(id);
    }

    public async Task<WishlistItem> AddWishlistItemAsync(WishlistItem wishlistItem)
    {
        // 1. Vérifier si le produit existe
        var product = await _productRepository.GetProductByIdAsync(wishlistItem.ProductId);
        if (product == null)
        {
            throw new InvalidOperationException($"Product with ID {wishlistItem.ProductId} not found.");
        }

        // 2. Vérifier si l'article existe déjà dans la liste d'envies de l'utilisateur
        var existingWishlistItem = await _wishlistRepository.GetWishlistItemByUserIdAndProductIdAsync(wishlistItem.UserId, wishlistItem.ProductId);

        if (existingWishlistItem != null)
        {
            // Si l'article existe déjà, tu peux choisir de ne rien faire, ou de renvoyer l'existant.
            // Ici, nous allons simplement retourner l'article existant, indiquant qu'il est déjà là.
            // Tu pourrais aussi lancer une exception comme Conflict si c'est une erreur métier de l'ajouter deux fois.
            return existingWishlistItem;
        }

        // 3. Ajouter le nouvel article à la liste d'envies
        wishlistItem.AddedAt = DateTime.UtcNow;
        return await _wishlistRepository.AddWishlistItemAsync(wishlistItem);
    }

    public async Task<bool> RemoveWishlistItemAsync(int userId, int productId)
    {
        return await _wishlistRepository.DeleteWishlistItemAsync(userId, productId);
    }

    public async Task<bool> ClearWishlistAsync(int userId)
    {
        return await _wishlistRepository.DeleteAllWishlistItemsByUserIdAsync(userId);
    }
}
