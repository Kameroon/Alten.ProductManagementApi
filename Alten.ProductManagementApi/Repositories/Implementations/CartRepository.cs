using Alten.ProductManagementApi.DTOs;
using Alten.ProductManagementApi.Models;
using Alten.ProductManagementApi.Repositories.Interfaces;
using Dapper;
using System.Data;

namespace Alten.ProductManagementApi.Repositories.Implementations;

public class CartRepository : ICartRepository
{
    private readonly IDbConnection _dbConnection;

    public CartRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<IEnumerable<CartItem>> GetCartItemsByUserIdAsync(int userId)
    {
        var sql = "SELECT Id, UserId, ProductId, Quantity, AddedAt FROM CartItems WHERE UserId = @UserId ORDER BY AddedAt ASC;";
        return await _dbConnection.QueryAsync<CartItem>(sql, new { UserId = userId });
    }

    public async Task<CartItem?> GetCartItemByIdAsync(int id)
    {
        var sql = "SELECT Id, UserId, ProductId, Quantity, AddedAt FROM CartItems WHERE Id = @Id;";
        return await _dbConnection.QuerySingleOrDefaultAsync<CartItem>(sql, new { Id = id });
    }

    public async Task<CartItem?> GetCartItemByUserIdAndProductIdAsync(int userId, int productId)
    {
        var sql = "SELECT Id, UserId, ProductId, Quantity, AddedAt FROM CartItems WHERE UserId = @UserId AND ProductId = @ProductId;";
        return await _dbConnection.QuerySingleOrDefaultAsync<CartItem>(sql, new { UserId = userId, ProductId = productId });
    }

    public async Task<CartItem> AddCartItemAsync(CartItem cartItem)
    {
        var sql = @"INSERT INTO CartItems (UserId, ProductId, Quantity, AddedAt)
                        VALUES (@UserId, @ProductId, @Quantity, @AddedAt)
                        RETURNING Id, UserId, ProductId, Quantity, AddedAt;";
        // Important: AddedAt from model should be DateTime.UtcNow before passing it here
        return await _dbConnection.QuerySingleAsync<CartItem>(sql, cartItem);
    }

    public async Task<bool> UpdateCartItemAsync(CartItem cartItem)
    {
        var sql = @"UPDATE CartItems SET
                        Quantity = @Quantity, AddedAt = @AddedAt
                        WHERE Id = @Id;"; // Or use UserId and ProductId in WHERE clause if Id is not known
        var affectedRows = await _dbConnection.ExecuteAsync(sql, cartItem);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteCartItemAsync(int userId, int productId)
    {
        var sql = "DELETE FROM CartItems WHERE UserId = @UserId AND ProductId = @ProductId;";
        var affectedRows = await _dbConnection.ExecuteAsync(sql, new { UserId = userId, ProductId = productId });
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAllCartItemsByUserIdAsync(int userId)
    {
        var sql = "DELETE FROM CartItems WHERE UserId = @UserId;";
        var affectedRows = await _dbConnection.ExecuteAsync(sql, new { UserId = userId });
        return affectedRows > 0;
    }
}