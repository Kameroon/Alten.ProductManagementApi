using Alten.ProductManagementApi.Models;

namespace Alten.ProductManagementApi.Helpers;

public interface IJwtHelper
{
    string GenerateToken(User user);
}