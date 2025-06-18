using Alten.ProductManagementApi.Models;

namespace Alten.ProductManagementApi.Services.Interfaces;

public interface IUserService
{
    Task<User> CreateUserAsync(User user);
    Task<User?> GetUserByEmailAsync(string email);
    Task<bool> ValidateUserCredentialsAsync(string email, string password);
}
