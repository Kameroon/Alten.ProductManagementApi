using System.ComponentModel.DataAnnotations;

namespace Alten.ProductManagementApi.DTOs;

public record RegisterRequest(
        [Required(ErrorMessage = "Username is required")] string Username,
        [Required(ErrorMessage = "Firstname is required")] string Firstname,
        [Required(ErrorMessage = "Email is required"), EmailAddress(ErrorMessage = "Invalid email format")] string Email,
        [Required(ErrorMessage = "Password is required"), MinLength(8, ErrorMessage = "Password must be at least 8 characters long")] string Password
    );