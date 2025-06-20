using Alten.ProductManagementApi.DTOs;
using Alten.ProductManagementApi.Helpers;
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

    #region -- TO DELETED --
    private static async Task<IResult> CryptPassword()
    {
        try
        {
            List<string> passwords = new List<string>
            {
                "adminpassword",
                "Securep@ss!",
                "!Strong@3456"
            };

            foreach (var password in passwords)
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
                Console.WriteLine("Mot de passe haché: " + hashedPassword);
            }

            return Results.Ok();
        }
        catch (Exception exception)
        {
            return Results.Problem(exception.Message);
        }
    }
    #endregion

    /// <summary>
    /// -- Maps endpoints for user authentication and registration.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static WebApplication MapAuthenticationEndpoints(this WebApplication app)
    {
        // TO DELETED
        app.MapGet("/api/CryptPassword", CryptPassword);

        app.MapPost("/account", async (RegisterRequest request, IUserService userService) =>
        {
            var existingUser = await userService.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
                return Results.Conflict("Une utilisateur existe deja avec cet email.");

            var newUser = new User
            {
                Username = request.Username,
                Firstname = request.Firstname,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            var createdUser = await userService.CreateUserAsync(newUser);
           
            return Results.Created($"/users/{createdUser.Id}", new { createdUser.Id, createdUser.Username, createdUser.Email });
        });

        app.MapPost("/token", async (LoginRequest request, IUserService userService, IConfiguration configuration, IJwtHelper jwtHelper) =>
        {
            var user = await userService.GetUserByEmailAsync(request.Email);
            if (user == null)
                return Results.Unauthorized();

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Results.Unauthorized();

            string newToken = jwtHelper.GenerateToken(user);
            return Results.Ok(new { Token = newToken });
        });

        return app;
    }

    /// <summary>
    /// -- Maps endpoints for managing products.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication MapProductEndpoints(this WebApplication app)
    {
        // GET all products
        app.MapGet("/products", async (IProductService productService) =>
        {
            var products = await productService.GetAllProductsAsync();
            return Results.Ok(products);
        }).RequireAuthorization(); // 

        // GET product by ID
        app.MapGet("/products/{id}", async (int id, IProductService productService) =>
        {
            var product = await productService.GetProductByIdAsync(id);
            return product != null ? Results.Ok(product) : Results.NotFound();
        }).RequireAuthorization();

        // POST a new product (Requires admin email claim)
        app.MapPost("/product", async (Product product, IProductService productService, ClaimsPrincipal user) =>
        {          
            if (!user.HasClaim(ClaimTypes.Email, "admin@admin.com"))
                return Results.Forbid();

            var createdProduct = await productService.CreateProductAsync(product);
            return Results.Created($"/products/{createdProduct.Id}", createdProduct);
        }).RequireAuthorization(); 
        
        // PUT to update product details (Requires admin email claim)
        app.MapPut("/products/{id}", async (int id, Product product, IProductService productService, ClaimsPrincipal user) =>
        {
            if (!user.HasClaim(ClaimTypes.Email, "admin@admin.com"))
                return Results.Forbid();

            if (id != product.Id) 
                return Results.BadRequest("Product ID in path and body mismatch.");

            var updated = await productService.UpdateProductAsync(product);
            return updated ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();

        // DELETE a product (Requires admin email claim)
        app.MapDelete("/products/{id}", async (int id, IProductService productService, ClaimsPrincipal user) =>
        {
            if (!user.HasClaim(ClaimTypes.Email, "admin@admin.com"))
                return Results.Forbid();

            var deleted = await productService.DeleteProductAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();

        return app;
    }

    /// <summary>
    /// -- Maps endpoints for managing the user's cart.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication MapCartEndpoints(this WebApplication app)
    {
        // GET /cart: Retrieve cart items for the authenticated user
        app.MapGet("/cart", async (ClaimsPrincipal user, ICartService cartService) =>
        {
            var (isAuthorized, userId) = CheckIfUserIsAuthorized(user);
            if (!isAuthorized)
                return Results.Unauthorized();

            var cartItems = await cartService.GetCartItemsByUserIdAsync(userId);
            return Results.Ok(cartItems);
        }).RequireAuthorization();

        // POST /cart: Add a product to the user's cart
        app.MapPost("/cart", async (CartItemDto cartItemDto, ClaimsPrincipal user, ICartService cartService) =>
        {
            var (isAuthorized, userId) = CheckIfUserIsAuthorized(user);
            if (!isAuthorized)
                return Results.Unauthorized();

            var cartItem = new CartItem { UserId = userId, ProductId = cartItemDto.ProductId, Quantity = cartItemDto.Quantity, AddedAt = DateTime.UtcNow };
            var addedItem = await cartService.AddOrUpdateCartItemAsync(cartItem); 
            return Results.Created($"/cart/{addedItem.ProductId}", addedItem);
        }).RequireAuthorization();

        // DELETE /cart/{productId}: Remove a product from the user's cart
        app.MapDelete("/cart/{productId}", async (int productId, ClaimsPrincipal user, ICartService cartService) =>
        {
            var (isAuthorized, userId) = CheckIfUserIsAuthorized(user);
            if (!isAuthorized)
                return Results.Unauthorized();

            var deleted = await cartService.RemoveCartItemAsync(userId, productId);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();

        return app;
    }

    /// <summary>
    /// -- Maps endpoints for managing the user's wishlist.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication MapWishlistEndpoints(this WebApplication app)
    {
        // GET /wishlist: Retrieve wishlist items for the authenticated user
        app.MapGet("/wishlist", async (ClaimsPrincipal user, IWishlistService wishlistService) =>
        {
            var (isAuthorized, userId) = CheckIfUserIsAuthorized(user);
            if (!isAuthorized)
                return Results.Unauthorized();

            var wishlistItems = await wishlistService.GetWishlistItemsByUserIdAsync(userId);
            return Results.Ok(wishlistItems);
        }).RequireAuthorization();

        // POST /wishlist: Add a product to the user's wishlist
        app.MapPost("/wishlist", async (WishlistItemDto wishlistItemDto, ClaimsPrincipal user, IWishlistService wishlistService) =>
        {
            var (isAuthorized, userId) = CheckIfUserIsAuthorized(user);
            if (!isAuthorized)
                return Results.Unauthorized();

            var wishlistItem = new WishlistItem { UserId = userId, ProductId = wishlistItemDto.ProductId, AddedAt = DateTime.UtcNow };
            var addedItem = await wishlistService.AddWishlistItemAsync(wishlistItem);
            return Results.Created($"/wishlist/{addedItem.ProductId}", addedItem);
        }).RequireAuthorization();

        // DELETE /wishlist/{productId}: Remove a product from the user's wishlist
        app.MapDelete("/wishlist/{productId}", async (int productId, ClaimsPrincipal user, IWishlistService wishlistService) =>
        {
            var (isAuthorized, userId) = CheckIfUserIsAuthorized(user);
            if (!isAuthorized)
                return Results.Unauthorized();

            var deleted = await wishlistService.RemoveWishlistItemAsync(userId, productId);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();

        return app;
    }

    /// <summary>
    /// -- Checks if the user is authorized and retrieves their user ID.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    private static (bool IsAuthorized, int UserId) CheckIfUserIsAuthorized(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return (false, 0);

        return (true, userId);
    }

}