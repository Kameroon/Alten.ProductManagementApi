﻿using Alten.ProductManagementApi.Models;

namespace Alten.ProductManagementApi.Repositories.Interfaces;

public interface IWishlistRepository
{
    Task<IEnumerable<WishlistItem>> GetWishlistItemsByUserIdAsync(int userId);
    Task<WishlistItem?> GetWishlistItemByUserIdAndProductIdAsync(int userId, int productId); 
    Task<WishlistItem> AddWishlistItemAsync(WishlistItem wishlistItem);
    Task<bool> DeleteWishlistItemAsync(int userId, int productId);
    Task<bool> DeleteAllWishlistItemsByUserIdAsync(int userId); 
}