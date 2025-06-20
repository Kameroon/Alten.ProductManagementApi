using Alten.ProductManagementApi.Models;
using Alten.ProductManagementApi.Repositories.Interfaces;
using Dapper;
using System.Data;

namespace Alten.ProductManagementApi.Repositories.Implementations;

public class UserRepository : IUserRepository
{
    private readonly IDbConnection _dbConnection;

    public UserRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<User> AddUserAsync(User user)
    {
        var sql = @"INSERT INTO Users (Username, Firstname, Email, PasswordHash, IsActive, CreatedAt)
                        VALUES (@Username, @Firstname, @Email, @PasswordHash, @IsActive, @CreatedAt)
                        RETURNING Id, Username, Firstname, Email, PasswordHash, IsActive, CreatedAt;"; 
        return await _dbConnection.QuerySingleAsync<User>(sql, user);
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        var sql = "SELECT Id, Username, Firstname, Email, PasswordHash, isactive FROM Users WHERE Id = @Id;";
        return await _dbConnection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var sql = "SELECT Id, Username, Firstname, Email, PasswordHash, isactive FROM Users WHERE Email = @Email;";
        return await _dbConnection.QuerySingleOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        var sql = @"UPDATE Users SET
                        Username = @Username, Firstname = @Firstname, Email = @Email, PasswordHash = @PasswordHash
                        WHERE Id = @Id;";
        var affectedRows = await _dbConnection.ExecuteAsync(sql, user);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var sql = "DELETE FROM Users WHERE Id = @Id;";
        var affectedRows = await _dbConnection.ExecuteAsync(sql, new { Id = id });
        return affectedRows > 0;
    }
}