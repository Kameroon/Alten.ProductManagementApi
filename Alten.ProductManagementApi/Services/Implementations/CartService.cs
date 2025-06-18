using Alten.ProductManagementApi.Models;
using Alten.ProductManagementApi.Repositories.Interfaces;
using Alten.ProductManagementApi.Services.Interfaces;

namespace Alten.ProductManagementApi.Services.Implementations;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository; // Nécessaire pour vérifier l'existence du produit

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

    public async Task<CartItem> AddOrUpdateCartItemAsync(CartItem cartItem)
    {
        if (cartItem.Quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(cartItem.Quantity));
        }

        // Vérifier si le produit existe et est en stock (logique métier)
        var product = await _productRepository.GetProductByIdAsync(cartItem.ProductId);
        if (product == null)
        {
            throw new InvalidOperationException($"Product with ID {cartItem.ProductId} not found.");
        }

        var existingCartItem = await _cartRepository.GetCartItemByUserIdAndProductIdAsync(cartItem.UserId, cartItem.ProductId);

        if (existingCartItem == null)
        {
            // Nouvel article dans le panier
            cartItem.AddedAt = DateTime.UtcNow;
            // Optionnel: Vérifier que la quantité demandée ne dépasse pas le stock initial
            // if (product.Quantity < cartItem.Quantity)
            // {
            //     throw new InvalidOperationException($"Not enough stock for product {product.Name}. Available: {product.Quantity}, Requested: {cartItem.Quantity}");
            // }
            return await _cartRepository.AddCartItemAsync(cartItem);
        }
        else
        {
            // Mettre à jour la quantité de l'article existant
            existingCartItem.Quantity += cartItem.Quantity;
            existingCartItem.AddedAt = DateTime.UtcNow; // Mise à jour du "dernier ajout"

            // Optionnel: Vérifier que la quantité totale ne dépasse pas le stock
            // if (product.Quantity < existingCartItem.Quantity)
            // {
            //     throw new InvalidOperationException($"Not enough stock for product {product.Name}. Available: {product.Quantity}, Total in cart: {existingCartItem.Quantity}");
            // }

            await _cartRepository.UpdateCartItemAsync(existingCartItem);
            return existingCartItem;
        }
    }

    public async Task<bool> RemoveCartItemAsync(int userId, int productId)
    {
        // Tu peux ajouter une vérification d'existence ici si tu veux être sûr,
        // bien que le repository se charge de retourner false si rien n'est supprimé.
        return await _cartRepository.DeleteCartItemAsync(userId, productId);
    }

    public async Task<bool> ClearCartAsync(int userId)
    {
        return await _cartRepository.DeleteAllCartItemsByUserIdAsync(userId);
    }
}