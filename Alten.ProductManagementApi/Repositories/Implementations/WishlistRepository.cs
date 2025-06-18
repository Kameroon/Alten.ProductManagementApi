using Alten.ProductManagementApi.Models;
using Alten.ProductManagementApi.Repositories.Interfaces;
using Dapper;
using System.Data;

namespace Alten.ProductManagementApi.Repositories.Implementations;

public class WishlistRepository : IWishlistRepository
{
    private readonly IDbConnection _dbConnection;

    public WishlistRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<IEnumerable<WishlistItem>> GetWishlistItemsByUserIdAsync(int userId)
    {
        var sql = "SELECT Id, UserId, ProductId, AddedAt FROM WishlistItems WHERE UserId = @UserId ORDER BY AddedAt ASC;";
        return await _dbConnection.QueryAsync<WishlistItem>(sql, new { UserId = userId });
    }

    public async Task<WishlistItem?> GetWishlistItemByIdAsync(int id)
    {
        var sql = "SELECT Id, UserId, ProductId, AddedAt FROM WishlistItems WHERE Id = @Id;";
        return await _dbConnection.QuerySingleOrDefaultAsync<WishlistItem>(sql, new { Id = id });
    }

    public async Task<WishlistItem?> GetWishlistItemByUserIdAndProductIdAsync(int userId, int productId)
    {
        var sql = "SELECT Id, UserId, ProductId, AddedAt FROM WishlistItems WHERE UserId = @UserId AND ProductId = @ProductId;";
        return await _dbConnection.QuerySingleOrDefaultAsync<WishlistItem>(sql, new { UserId = userId, ProductId = productId });
    }

    public async Task<WishlistItem> AddWishlistItemAsync(WishlistItem wishlistItem)
    {
        var sql = @"INSERT INTO WishlistItems (UserId, ProductId, AddedAt)
                        VALUES (@UserId, @ProductId, @AddedAt)
                        RETURNING Id, UserId, ProductId, AddedAt;";
        return await _dbConnection.QuerySingleAsync<WishlistItem>(sql, wishlistItem);
    }

    public async Task<bool> DeleteWishlistItemAsync(int userId, int intProductId)
    {
        var sql = "DELETE FROM WishlistItems WHERE UserId = @UserId AND ProductId = @ProductId;";
        var affectedRows = await _dbConnection.ExecuteAsync(sql, new { UserId = userId, ProductId = intProductId });
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAllWishlistItemsByUserIdAsync(int userId)
    {
        var sql = "DELETE FROM WishlistItems WHERE UserId = @UserId;";
        var affectedRows = await _dbConnection.ExecuteAsync(sql, new { UserId = userId });
        return affectedRows > 0;
    }
}
