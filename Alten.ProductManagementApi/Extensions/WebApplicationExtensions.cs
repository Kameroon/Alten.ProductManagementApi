using Alten.ProductManagementApi.DTOs;
using Alten.ProductManagementApi.Models;
using Alten.ProductManagementApi.Services.Implementations;
using Alten.ProductManagementApi.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Alten.ProductManagementApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication MapAuthenticationEndpoints(this WebApplication app)
    {
        app.MapPost("/account", async (RegisterRequest request, IUserService userService) =>
        {
            // La validation des attributs DataAnnotations sur le DTO sera gérée automatiquement par Minimal APIs
            // Si la validation échoue, un Results.BadRequest sera retourné automatiquement par le framework

            var existingUser = await userService.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Results.Conflict("User with this email already exists.");
            }

            var newUser = new User
            {
                Username = request.Username,
                Firstname = request.Firstname,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            var createdUser = await userService.CreateUserAsync(newUser);
            // Ne retourne pas le PasswordHash !
            return Results.Created($"/users/{createdUser.Id}", new { createdUser.Id, createdUser.Username, createdUser.Email });
        });

        app.MapPost("/token", async (LoginRequest request, IUserService userService, IConfiguration configuration) =>
        {
            // La validation des attributs DataAnnotations sur le DTO sera gérée automatiquement
            var user = await userService.GetUserByEmailAsync(request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            var jwtSettings = configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured."));

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // L'ID de l'utilisateur
                        new Claim(ClaimTypes.Email, user.Email),                 // L'email de l'utilisateur (utilisé pour la politique admin)
                        new Claim(ClaimTypes.Name, user.Username)                // Le nom d'utilisateur
                }),
                Expires = DateTime.UtcNow.AddHours(10), // Token valide 10 heures
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Results.Ok(new { Token = tokenString });
        });

        return app;
    }

    public static WebApplication MapProductEndpoints(this WebApplication app)
    {
        // GET all products
        app.MapGet("/products", async (IProductService productService) =>
        {
            var products = await productService.GetAllProductsAsync();
            return Results.Ok(products);
        }).RequireAuthorization(); // Requiert une authentification pour tous les accès aux produits

        // GET product by ID
        app.MapGet("/products/{id}", async (int id, IProductService productService) =>
        {
            var product = await productService.GetProductByIdAsync(id);
            return product != null ? Results.Ok(product) : Results.NotFound();
        }).RequireAuthorization();

        // POST a new product (Requires admin email claim)
        app.MapPost("/products", async (Product product, IProductService productService, ClaimsPrincipal user) =>
        {
            // Policy verification here instead of attribute because Minimal APIs policy cannot check claims directly
            if (!user.HasClaim(ClaimTypes.Email, "admin@admin.com"))
            {
                return Results.Forbid(); // 403 Forbidden
            }
            var createdProduct = await productService.CreateProductAsync(product);
            return Results.Created($"/products/{createdProduct.Id}", createdProduct);
        }).RequireAuthorization(); // Just requires authentication, claim check is manual

        // PUT to update product details (Requires admin email claim)
        app.MapPut("/products/{id}", async (int id, Product product, IProductService productService, ClaimsPrincipal user) =>
        {
            if (!user.HasClaim(ClaimTypes.Email, "admin@admin.com"))
            {
                return Results.Forbid();
            }
            if (id != product.Id) return Results.BadRequest("Product ID in path and body mismatch.");

            var updated = await productService.UpdateProductAsync(product);
            return updated ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();

        // DELETE a product (Requires admin email claim)
        app.MapDelete("/products/{id}", async (int id, IProductService productService, ClaimsPrincipal user) =>
        {
            if (!user.HasClaim(ClaimTypes.Email, "admin@admin.com"))
            {
                return Results.Forbid();
            }
            var deleted = await productService.DeleteProductAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();

        return app;
    }

    public static WebApplication MapCartEndpoints(this WebApplication app)
    {
        // GET /cart: Retrieve cart items for the authenticated user
        app.MapGet("/cart", async (ClaimsPrincipal user, ICartService cartService) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Results.Unauthorized();
            }
            var cartItems = await cartService.GetCartItemsByUserIdAsync(userId);
            return Results.Ok(cartItems);
        }).RequireAuthorization();

        // POST /cart: Add a product to the user's cart
        app.MapPost("/cart", async (CartItemDto cartItemDto, ClaimsPrincipal user, ICartService cartService) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Results.Unauthorized();
            }

            // Map DTO to Model
            var cartItem = new CartItem { UserId = userId, ProductId = cartItemDto.ProductId, Quantity = cartItemDto.Quantity, AddedAt = DateTime.UtcNow };
            var addedItem = await cartService.AddOrUpdateCartItemAsync(cartItem); // Assume service handles add or update
            return Results.Created($"/cart/{addedItem.ProductId}", addedItem);
        }).RequireAuthorization();

        // DELETE /cart/{productId}: Remove a product from the user's cart
        app.MapDelete("/cart/{productId}", async (int productId, ClaimsPrincipal user, ICartService cartService) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Results.Unauthorized();
            }
            var deleted = await cartService.RemoveCartItemAsync(userId, productId);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();

        return app;
    }

    public static WebApplication MapWishlistEndpoints(this WebApplication app)
    {
        // GET /wishlist: Retrieve wishlist items for the authenticated user
        app.MapGet("/wishlist", async (ClaimsPrincipal user, IWishlistService wishlistService) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Results.Unauthorized();
            }
            var wishlistItems = await wishlistService.GetWishlistItemsByUserIdAsync(userId);
            return Results.Ok(wishlistItems);
        }).RequireAuthorization();

        // POST /wishlist: Add a product to the user's wishlist
        app.MapPost("/wishlist", async (WishlistItemDto wishlistItemDto, ClaimsPrincipal user, IWishlistService wishlistService) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Results.Unauthorized();
            }

            // Map DTO to Model
            var wishlistItem = new WishlistItem { UserId = userId, ProductId = wishlistItemDto.ProductId, AddedAt = DateTime.UtcNow };
            var addedItem = await wishlistService.AddWishlistItemAsync(wishlistItem);
            return Results.Created($"/wishlist/{addedItem.ProductId}", addedItem);
        }).RequireAuthorization();

        // DELETE /wishlist/{productId}: Remove a product from the user's wishlist
        app.MapDelete("/wishlist/{productId}", async (int productId, ClaimsPrincipal user, IWishlistService wishlistService) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Results.Unauthorized();
            }
            var deleted = await wishlistService.RemoveWishlistItemAsync(userId, productId);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();

        return app;
    }
}