﻿namespace Alten.ProductManagementApi.Helpers;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}
