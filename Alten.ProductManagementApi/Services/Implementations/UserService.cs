using Alten.ProductManagementApi.Helpers;
using Alten.ProductManagementApi.Models;
using Alten.ProductManagementApi.Repositories.Interfaces;
using Alten.ProductManagementApi.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Alten.ProductManagementApi.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<User> CreateUserAsync(User user)
    {
        var existingUser = await _userRepository.GetUserByEmailAsync(user.Email);
        if (existingUser != null)
            throw new InvalidOperationException($"L'adresse email '{user.Email}' existe déjà.");

        user.PasswordHash = _passwordHasher.HashPassword(user.PasswordHash);

        user.IsActive = true; 
        user.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // <-- (timestamp Unix)

        return await _userRepository.AddUserAsync(user);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userRepository.GetUserByEmailAsync(email);
    }

    public async Task<(bool IsSuccess, User? User)> ValidateUserCredentialsAsync(string email, string password)
    {
       var user = await _userRepository.GetUserByEmailAsync(email);
        if (user == null || !user.IsActive)
            return (false, null);

        var isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        return (isValid, isValid ? user : null);
    }
}