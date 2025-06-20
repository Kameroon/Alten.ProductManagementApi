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
        if (string.IsNullOrWhiteSpace(product.Name))
            throw new ArgumentException("Le nom du propduit ne être vide.", nameof(product.Name));

        product.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        product.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        product.Id = await _productRepository.AddProductAsync(product);
        return product;
    }

    public async Task<bool> UpdateProductAsync(Product product)
    {
        var existingProduct = await CheckExistingProduct(product.Id);
        if (existingProduct == null)
            return false;

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
        existingProduct.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); 

        return await _productRepository.UpdateProductAsync(existingProduct);
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("L'ID du produit doit être supérieur à zéro.", nameof(id));
        if (await CheckExistingProduct(id) == null)
            throw new KeyNotFoundException($"Aucun produit trouvé avec l'ID {id}.");

        return await _productRepository.DeleteProductAsync(id);
    }

    /// <summary>
    /// -- Checks if a product with the given ID exists in the repository.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private async Task<Product?> CheckExistingProduct(int id)
    {
        return await _productRepository.GetProductByIdAsync(id);
    }
}