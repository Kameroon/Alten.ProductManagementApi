using System.ComponentModel.DataAnnotations;

namespace Alten.ProductManagementApi.DTOs;

public record CartItemDto(
        [Required] int ProductId,
        [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")] int Quantity
    );
