namespace Alten.ProductManagementApi.Repositories.Implementations;
using Alten.ProductManagementApi.Repositories.Interfaces;
using Dapper;
using ProductManagementApi.Models;
using System.Data;

public class ProductRepository : IProductRepository
{
    private readonly IDbConnection _dbConnection;

    public ProductRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        var sql = "SELECT Id, Code, Name, Description, Image, Category, Price, Quantity, InternalReference, ShellId, InventoryStatus, Rating, CreatedAt, UpdatedAt FROM Products;";
        return await _dbConnection.QueryAsync<Product>(sql);
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        var sql = "SELECT Id, Code, Name, Description, Image, Category, Price, Quantity, InternalReference, ShellId, InventoryStatus, Rating, CreatedAt, UpdatedAt FROM Products WHERE Id = @Id;";
        return await _dbConnection.QuerySingleOrDefaultAsync<Product>(sql, new { Id = id });
    }

    public async Task<int> AddProductAsync(Product product)
    {
        var sql = @"INSERT INTO Products (Code, Name, Description, Image, Category, Price, Quantity, InternalReference, ShellId, InventoryStatus, Rating, CreatedAt, UpdatedAt)
                        VALUES (@Code, @Name, @Description, @Image, @Category, @Price, @Quantity, @InternalReference, @ShellId, @InventoryStatus, @Rating, @CreatedAt, @UpdatedAt)
                        RETURNING Id;";
        return await _dbConnection.ExecuteScalarAsync<int>(sql, product);
    }

    public async Task<bool> UpdateProductAsync(Product product)
    {
        var sql = @"UPDATE Products SET
                        Code = @Code, Name = @Name, Description = @Description, Image = @Image, Category = @Category, Price = @Price, Quantity = @Quantity,
                        InternalReference = @InternalReference, ShellId = @ShellId, InventoryStatus = @InventoryStatus, Rating = @Rating, UpdatedAt = @UpdatedAt
                        WHERE Id = @Id;";
        var affectedRows = await _dbConnection.ExecuteAsync(sql, product);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var sql = "DELETE FROM Products WHERE Id = @Id;";
        var affectedRows = await _dbConnection.ExecuteAsync(sql, new { Id = id });
        return affectedRows > 0;
    }
}