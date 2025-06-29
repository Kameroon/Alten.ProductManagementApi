﻿using Alten.ProductManagementApi.Models;

namespace Alten.ProductManagementApi.Services.Interfaces;

public interface IUserService
{
    Task<User> CreateUserAsync(User user);
    //Task<User?> GetUserByIdAsync(int id);
    Task<User?> GetUserByEmailAsync(string email);
    Task<(bool IsSuccess, User? User)> ValidateUserCredentialsAsync(string email, string password);
}
