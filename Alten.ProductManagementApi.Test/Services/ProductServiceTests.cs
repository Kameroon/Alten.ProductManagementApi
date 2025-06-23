using Alten.ProductManagementApi.Models; 
using Alten.ProductManagementApi.Repositories.Interfaces; 
using Alten.ProductManagementApi.Services.Implementations;
using Alten.ProductManagementApi.Services.Interfaces; 
using FluentAssertions;
using Moq;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Alten.ProductManagementApi.Test.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _mockProductRepository = new Mock<IProductRepository>();
        _productService = new ProductService(_mockProductRepository.Object);
    }

    // --- Tests pour GetAllProductsAsync ---
    [Fact]
    public async Task GetAllProductsAsync_ShouldReturnAllProducts()
    {
        // --- Arrange --- 
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Product A" },
            new Product { Id = 2, Name = "Product B" }
        };
        _mockProductRepository.Setup(repo => repo.GetAllProductsAsync()).ReturnsAsync(products);

        // --- Act --- 
        var result = await _productService.GetAllProductsAsync();

        // --- Assert --- 
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(products);
        _mockProductRepository.Verify(repo => repo.GetAllProductsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllProductsAsync_ShouldReturnEmptyList_WhenNoProductsExist()
    {
        // --- Arrange --- 
        _mockProductRepository.Setup(repo => repo.GetAllProductsAsync()).ReturnsAsync(new List<Product>());

        // --- Act --- 
        var result = await _productService.GetAllProductsAsync();

        // --- Assert --- 
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockProductRepository.Verify(repo => repo.GetAllProductsAsync(), Times.Once);
    }

    // --- Tests pour GetProductByIdAsync ---
    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnProduct_WhenProductExists()
    {
        // --- Arrange --- 
        var productId = 1;
        var product = new Product { Id = productId, Name = "Test Product" };
        _mockProductRepository.Setup(repo => repo.GetProductByIdAsync(productId)).ReturnsAsync(product);

        // --- Act --- 
        var result = await _productService.GetProductByIdAsync(productId);

        // --- Assert --- 
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(product);
        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(productId), Times.Once);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        // --- Arrange --- 
        var productId = 99; 
        _mockProductRepository.Setup(repo => repo.GetProductByIdAsync(productId)).ReturnsAsync((Product?)null);

        // --- Act --- 
        var result = await _productService.GetProductByIdAsync(productId);

        // --- Assert --- 
        result.Should().BeNull();
        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(productId), Times.Once);
    }

    // --- Tests pour CreateProductAsync ---
    [Fact]
    public async Task CreateProductAsync_ShouldCreateProduct_WhenNameIsValid()
    {
        // -- Arrange --
        var productInput = new Product
        {
            Name = "New Gadget",
            Description = "A very useful gadget.",
            Price = 29.99m,
            Quantity = 100,
            Code = "C123",
            Image = "new_image.jpg",
            Category = "Electronics",
            InternalReference = "IRef1",
            ShellId = 111, 
            InventoryStatus = "InStock",
            Rating = 4.5m
        };
        var expectedProductId = 10;

        // -- Configuration du mock pour AddProductAsync pour renvoyer l'ID du produit créé --
        _mockProductRepository.Setup(repo => repo.AddProductAsync(It.IsAny<Product>()))
                              .ReturnsAsync((Product productArgument) =>
                              {
                                  productArgument.Id = expectedProductId;
                                  return expectedProductId;
                              });

        // -- Act --
        var createdProduct = await _productService.CreateProductAsync(productInput);

        // -- Assert --
        createdProduct.Should().NotBeNull();
        createdProduct.Id.Should().Be(expectedProductId);
        createdProduct.Name.Should().Be(productInput.Name);
        createdProduct.Description.Should().Be(productInput.Description);
        createdProduct.Price.Should().Be(productInput.Price);
        createdProduct.Quantity.Should().Be(productInput.Quantity);
        createdProduct.Code.Should().Be(productInput.Code);
        createdProduct.Image.Should().Be(productInput.Image);
        createdProduct.Category.Should().Be(productInput.Category);
        createdProduct.InternalReference.Should().Be(productInput.InternalReference);
        createdProduct.ShellId.Should().Be(productInput.ShellId);
        createdProduct.InventoryStatus.Should().Be(productInput.InventoryStatus);
        createdProduct.Rating.Should().Be(productInput.Rating);
        createdProduct.CreatedAt.Should().BePositive();
        createdProduct.UpdatedAt.Should().Be(0);

        _mockProductRepository.Verify(repo => repo.AddProductAsync(It.Is<Product>(p =>
            p.Name == productInput.Name &&
            p.Description == productInput.Description &&
            p.Price == productInput.Price &&
            p.Quantity == productInput.Quantity &&
            p.Code == productInput.Code &&
            p.Image == productInput.Image &&
            p.Category == productInput.Category &&
            p.InternalReference == productInput.InternalReference &&
            p.ShellId == productInput.ShellId &&
            p.InventoryStatus == productInput.InventoryStatus &&
            p.Rating == productInput.Rating &&
            p.CreatedAt > 0 &&
            p.Id > 0
        )), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldThrowArgumentException_WhenNameIsIsNullOrWhiteSpace()
    {
        //  -- Arrange -- 
        var productWithEmptyName = new Product { Name = "", Description = "...", Price = 10.0m };
        var productWithNullName = new Product { Name = null!, Description = "...", Price = 10.0m };

        // -- Act & Assert pour le nom vide -- 
        Func<Task> actEmpty = async () => await _productService.CreateProductAsync(productWithEmptyName);
        await actEmpty.Should().ThrowAsync<ArgumentException>()
                      .WithMessage("Le nom du propduit ne être vide. (Parameter 'Name')");

        // -- Act & Assert pour le nom nul
        Func<Task> actNull = async () => await _productService.CreateProductAsync(productWithNullName);
        await actNull.Should().ThrowAsync<ArgumentException>()
                      .WithMessage("Le nom du propduit ne être vide. (Parameter 'Name')");

        // -- Vérifiez que AddProductAsync n'a jamais été appelé -- 
        _mockProductRepository.Verify(repo => repo.AddProductAsync(It.IsAny<Product>()), Times.Never);
    }

    // --- Tests pour UpdateProductAsync ---
    [Fact]
    public async Task UpdateProductAsync_ShouldReturnTrue_WhenProductExistsAndIsUpdated()
    {
        // --- Arrange --- 
        var productId = 3;
        var existingProduct = new Product { Id = productId, Name = "Old Name", UpdatedAt = 1750621588 };
        var updatedProductRequest = new Product
        {
            Id = productId,
            Name = "New Name",
            Code = "C123",
            Description = "Updated desc",
            Price = 50.0m,
            Quantity = 10,
            Image = "new_image.jpg",
            Category = "Electronics",
            InternalReference = "IRef1",
            ShellId = 0,
            InventoryStatus = "InStock",
            Rating = 4.5m
        };

        _mockProductRepository.Setup(repo => repo.GetProductByIdAsync(productId)).ReturnsAsync(existingProduct);
        _mockProductRepository.Setup(repo => repo.UpdateProductAsync(It.IsAny<Product>())).ReturnsAsync(true);

        // --- Act --- 
        var result = await _productService.UpdateProductAsync(updatedProductRequest);

        // --- Assert --- 
        result.Should().BeTrue();
        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(productId), Times.Once);

        // --- Vérifier que UpdateProductAsync a été appelé avec les bonnes valeurs --- 
        _mockProductRepository.Verify(repo => repo.UpdateProductAsync(It.Is<Product>(p =>
               p.Id == productId &&
               p.Name == updatedProductRequest.Name &&
               p.Description == updatedProductRequest.Description &&
               p.Price == updatedProductRequest.Price &&
               p.Quantity == updatedProductRequest.Quantity &&
               p.Image == updatedProductRequest.Image &&
               p.Category == updatedProductRequest.Category &&
               p.Code == updatedProductRequest.Code &&
               p.InternalReference == updatedProductRequest.InternalReference &&
               p.ShellId == updatedProductRequest.ShellId &&
               p.InventoryStatus == updatedProductRequest.InventoryStatus &&
               p.Rating == updatedProductRequest.Rating &&
               p.UpdatedAt >= existingProduct.UpdatedAt
           )), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldReturnFalse_WhenProductDoesNotExist()
    {
        // --- Arrange --- 
        var productId = 99;
        var updatedProductRequest = new Product { Id = productId, Name = "Non Existent" };

        _mockProductRepository.Setup(repo => repo.GetProductByIdAsync(productId)).ReturnsAsync((Product?)null);

        // --- Act --- 
        var result = await _productService.UpdateProductAsync(updatedProductRequest);

        // --- Assert --- 
        result.Should().BeFalse();
        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(productId), Times.Once);        
        _mockProductRepository.Verify(repo => repo.UpdateProductAsync(It.IsAny<Product>()), Times.Never);
    }

    // --- Tests pour DeleteProductAsync ---
    [Fact]
    public async Task DeleteProductAsync_ShouldReturnTrue_WhenProductExists()
    {
        // --- Arrange --- 
        var productId = 1;
        _mockProductRepository.Setup(repo => repo.GetProductByIdAsync(productId)).ReturnsAsync(new Product { Id = productId });
        _mockProductRepository.Setup(repo => repo.DeleteProductAsync(productId)).ReturnsAsync(true);

        // --- Act --- 
        var result = await _productService.DeleteProductAsync(productId);

        // --- Assert --- 
        result.Should().BeTrue();
        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(productId), Times.Once);
        _mockProductRepository.Verify(repo => repo.DeleteProductAsync(productId), Times.Once);
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldThrowArgumentException_WhenIdIsZeroOrLess()
    {
        // --- Arrange --- 
        var invalidId = 0;

        //  --- Act --- 
        Func<Task> act = async () => await _productService.DeleteProductAsync(invalidId);

        // --- Assert --- 
        await act.Should().ThrowAsync<ArgumentException>()
           .WithMessage("L'ID du produit doit être supérieur à zéro. (Parameter 'id')");

        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(It.IsAny<int>()), Times.Never);
        _mockProductRepository.Verify(repo => repo.DeleteProductAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldThrowKeyNotFoundException_WhenProductDoesNotExist()
    {
        // --- Arrange --- 
        var nonExistentId = 99;
        _mockProductRepository.Setup(repo => repo.GetProductByIdAsync(nonExistentId)).ReturnsAsync((Product?)null);

        // Act --- 
        Func<Task> act = async () => await _productService.DeleteProductAsync(nonExistentId);

        // --- Assert --- 
        await act.Should().ThrowAsync<KeyNotFoundException>()
                      .WithMessage($"Aucun produit trouvé avec l'ID {nonExistentId}.");

        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(nonExistentId), Times.Once);
        _mockProductRepository.Verify(repo => repo.DeleteProductAsync(It.IsAny<int>()), Times.Never);
    }

}

