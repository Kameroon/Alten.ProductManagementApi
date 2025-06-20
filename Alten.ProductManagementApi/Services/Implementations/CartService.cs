using Alten.ProductManagementApi.Models;
using Alten.ProductManagementApi.Repositories.Interfaces;
using Alten.ProductManagementApi.Services.Interfaces;

namespace Alten.ProductManagementApi.Services.Implementations;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository; 

    public CartService(ICartRepository cartRepository, IProductRepository productRepository)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<CartItem>> GetCartItemsByUserIdAsync(int userId)
    {
        return await _cartRepository.GetCartItemsByUserIdAsync(userId);
    }

    public async Task<CartItem?> GetCartItemByIdAsync(int id)
    {
        return await _cartRepository.GetCartItemByIdAsync(id);
    }

    private async Task<Product?> CheckExistingProduct(int id)
    {
        return await _productRepository.GetProductByIdAsync(id);
    }

    public async Task<CartItem> AddOrUpdateCartItemAsync(CartItem cartItem)
    {
        if (cartItem.Quantity <= 0)
            throw new ArgumentException("La quantité ne peut être inférieure à 0.", nameof(cartItem.Quantity));

        var product = await _productRepository.GetProductByIdAsync(cartItem.ProductId);
        if (product == null)
            throw new InvalidOperationException($" Aucun Product n'a été trouvé avec l'ID : {cartItem.ProductId}.");

        var existingCartItem = await _cartRepository.GetCartItemByUserIdAndProductIdAsync(cartItem.UserId, cartItem.ProductId);

        if (existingCartItem == null)
        {
            cartItem.AddedAt = DateTime.UtcNow;
            
            if (product.Quantity < cartItem.Quantity)
                throw new InvalidOperationException($"Il n'a y pas acces de stock le produit {product.Name} à la quantité : {product.Quantity}");

            return await _cartRepository.AddCartItemAsync(cartItem);
        }
        else
        {
            existingCartItem.Quantity += cartItem.Quantity;
            existingCartItem.AddedAt = DateTime.UtcNow;

            // -- Vérifier que la quantité totale ne dépasse pas le stock
            if (product.Quantity < existingCartItem.Quantity)
                throw new InvalidOperationException($"Il n'a y pas acces de stock le produit {product.Name} à la quantité : {product.Quantity}");

            await _cartRepository.UpdateCartItemAsync(existingCartItem);
            return existingCartItem;
        }
    }

    public async Task<bool> RemoveCartItemAsync(int userId, int productId)
    {
        if (await CheckExistingProduct(productId) == null)
            throw new KeyNotFoundException($"Aucun produit trouvé avec l'ID {productId}.");

        return await _cartRepository.DeleteCartItemAsync(userId, productId);
    }

    public async Task<bool> ClearCartAsync(int userId)
    {
        return await _cartRepository.DeleteAllCartItemsByUserIdAsync(userId);
    }
}