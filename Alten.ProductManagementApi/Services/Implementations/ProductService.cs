using Alten.ProductManagementApi.Models;
using Alten.ProductManagementApi.Repositories.Interfaces;
using Alten.ProductManagementApi.Services.Interfaces;

namespace Alten.ProductManagementApi.Services.Implementations;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await _productRepository.GetAllProductsAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _productRepository.GetProductByIdAsync(id);
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        // Exemple de logique métier: s'assurer que le nom n'est pas vide
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            throw new ArgumentException("Product name cannot be empty.", nameof(product.Name));
        }
        // Définit les timestamps avant l'ajout
        product.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        product.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Le repository ajoute le produit et retourne l'ID généré
        product.Id = await _productRepository.AddProductAsync(product);
        return product;
    }

    public async Task<bool> UpdateProductAsync(Product product)
    {
        // Vérifie si le produit existe avant de tenter la mise à jour
        var existingProduct = await _productRepository.GetProductByIdAsync(product.Id);
        if (existingProduct == null)
        {
            return false; // Produit non trouvé
        }

        // Applique les mises à jour (tu peux choisir de ne mettre à jour que certaines propriétés)
        existingProduct.Code = product.Code;
        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Image = product.Image;
        existingProduct.Category = product.Category;
        existingProduct.Price = product.Price;
        existingProduct.Quantity = product.Quantity;
        existingProduct.InternalReference = product.InternalReference;
        existingProduct.ShellId = product.ShellId;
        existingProduct.InventoryStatus = product.InventoryStatus;
        existingProduct.Rating = product.Rating;
        existingProduct.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // Met à jour le timestamp

        return await _productRepository.UpdateProductAsync(existingProduct);
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        // Ici, tu pourrais ajouter des vérifications supplémentaires, par exemple si le produit est en stock ou dans un panier actif
        return await _productRepository.DeleteProductAsync(id);
    }
}