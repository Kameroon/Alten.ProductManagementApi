using System.ComponentModel.DataAnnotations;

namespace Alten.ProductManagementApi.DTOs;

public record WishlistItemDto(
        [Required] int ProductId
    );