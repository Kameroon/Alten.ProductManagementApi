﻿using Alten.ProductManagementApi.Models;
using Alten.ProductManagementApi.Repositories.Interfaces;
using Alten.ProductManagementApi.Services.Implementations;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alten.ProductManagementApi.Test.Services;

public class CartServiceTests
{
    private readonly Mock<ICartRepository> _mockCartRepository;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly CartService _cartService;

    public CartServiceTests()
    {
        _mockCartRepository = new Mock<ICartRepository>();
        _mockProductRepository = new Mock<IProductRepository>();
        _cartService = new CartService(_mockCartRepository.Object, _mockProductRepository.Object);
    }

    // --- Tests pour GetCartItemsByUserIdAsync ---
    [Fact]
    public async Task GetCartItemsByUserIdAsync_ShouldReturnCartItems_WhenCartExists()
    {
        // -- Arrange -- 
        var userId = 1;
        var cartItems = new List<CartItem>
            {
                new CartItem { Id = 1, UserId = userId, ProductId = 10, Quantity = 2, AddedAt = DateTime.UtcNow.AddMinutes(-5) },
                new CartItem { Id = 2, UserId = userId, ProductId = 20, Quantity = 1, AddedAt = DateTime.UtcNow.AddMinutes(-10) }
            };

        _mockCartRepository.Setup(repo => repo.GetCartItemsByUserIdAsync(userId))
                           .ReturnsAsync(cartItems);

        // -- Act --  
        var result = await _cartService.GetCartItemsByUserIdAsync(userId);

        // Assert
        result.Should().BeEquivalentTo(cartItems);
        _mockCartRepository.Verify(repo => repo.GetCartItemsByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetCartItemsByUserIdAsync_ShouldReturnEmptyList_WhenCartIsEmpty()
    {
        // -- Arrange -- 
        var userId = 1;
        _mockCartRepository.Setup(repo => repo.GetCartItemsByUserIdAsync(userId))
                           .ReturnsAsync(new List<CartItem>());

        // -- Act -- 
        var result = await _cartService.GetCartItemsByUserIdAsync(userId);

        // -- Assert -- 
        result.Should().BeEmpty();
        _mockCartRepository.Verify(repo => repo.GetCartItemsByUserIdAsync(userId), Times.Once);
    }

    // --- Tests pour GetCartItemByIdAsync ---
    [Fact]
    public async Task GetCartItemByIdAsync_ShouldReturnCartItem_WhenItemExists()
    {
        // -- Arrange
        var cartItemId = 1;
        var cartItem = new CartItem { Id = cartItemId, UserId = 1, ProductId = 10, Quantity = 1, AddedAt = DateTime.UtcNow };

        _mockCartRepository.Setup(repo => repo.GetCartItemByIdAsync(cartItemId))
                           .ReturnsAsync(cartItem);

        // Act -- 
        var result = await _cartService.GetCartItemByIdAsync(cartItemId);

        // -- Assert -- 
        result.Should().BeEquivalentTo(cartItem);
        _mockCartRepository.Verify(repo => repo.GetCartItemByIdAsync(cartItemId), Times.Once);
    }

    [Fact]
    public async Task GetCartItemByIdAsync_ShouldReturnNull_WhenItemDoesNotExist()
    {
        // -- Arrange -- 
        var cartItemId = 999;
        _mockCartRepository.Setup(repo => repo.GetCartItemByIdAsync(cartItemId))
                           .ReturnsAsync((CartItem?)null);

        // -- Act -- 
        var result = await _cartService.GetCartItemByIdAsync(cartItemId);

        // -- Assert -- 
        result.Should().BeNull();
        _mockCartRepository.Verify(repo => repo.GetCartItemByIdAsync(cartItemId), Times.Once);
    }

    // --- Tests pour AddOrUpdateCartItemAsync ---
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task AddOrUpdateCartItemAsync_ShouldThrowArgumentException_WhenQuantityIsZeroOrNegative(int quantity)
    {
        // Arrange -- 
        var cartItem = new CartItem { UserId = 1, ProductId = 10, Quantity = quantity };

        // -- Act & Assert -- 
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _cartService.AddOrUpdateCartItemAsync(cartItem));
        exception.ParamName.Should().Be(nameof(cartItem.Quantity));
        exception.Message.Should().Contain("La quantité ne peut être inférieure à 0.");

        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(It.IsAny<int>()), Times.Never);
        _mockCartRepository.Verify(repo => repo.GetCartItemByUserIdAndProductIdAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _mockCartRepository.Verify(repo => repo.AddCartItemAsync(It.IsAny<CartItem>()), Times.Never);
        _mockCartRepository.Verify(repo => repo.UpdateCartItemAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task AddOrUpdateCartItemAsync_ShouldThrowInvalidOperationException_WhenProductDoesNotExist()
    {
        // -- Arrange -- 
        var cartItem = new CartItem { UserId = 1, ProductId = 99, Quantity = 1 };
        _mockProductRepository.Setup(repo => repo.GetProductByIdAsync(cartItem.ProductId))
                              .ReturnsAsync((Product?)null);

        // -- Act & Assert -- 
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _cartService.AddOrUpdateCartItemAsync(cartItem));
        exception.Message.Should().Contain($"Aucun Product n'a été trouvé avec l'ID : {cartItem.ProductId}.");

        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(cartItem.ProductId), Times.Once);
        _mockCartRepository.Verify(repo => repo.GetCartItemByUserIdAndProductIdAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _mockCartRepository.Verify(repo => repo.AddCartItemAsync(It.IsAny<CartItem>()), Times.Never);
        _mockCartRepository.Verify(repo => repo.UpdateCartItemAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task AddOrUpdateCartItemAsync_ShouldAddItem_WhenProductExistsAndNotInCartAndEnoughStock()
    {
        // -- Arrange -- 
        var userId = 1;
        var productId = 10;
        var quantity = 2;
        var product = new Product { Id = productId, Name = "Test Product", Quantity = 10, InventoryStatus = "INSTOCK" }; 
        var newCartItem = new CartItem { UserId = userId, ProductId = productId, Quantity = quantity };
        var expectedAddedItem = new CartItem { Id = 50, UserId = userId, ProductId = productId, Quantity = quantity };

        _mockProductRepository.Setup(repo => repo.GetProductByIdAsync(productId))
                              .ReturnsAsync(product);
        _mockCartRepository.Setup(repo => repo.GetCartItemByUserIdAndProductIdAsync(userId, productId))
                           .ReturnsAsync((CartItem?)null); 
        _mockCartRepository.Setup(repo => repo.AddCartItemAsync(It.IsAny<CartItem>()))
                           .ReturnsAsync((CartItem ci) =>
                           {
                               ci.Id = expectedAddedItem.Id;
                               return ci;
                           });

        // -- Act -- 
        var result = await _cartService.AddOrUpdateCartItemAsync(newCartItem);

        // -- Assert -- 
        result.Should().NotBeNull();
        result.Id.Should().Be(expectedAddedItem.Id);
        result.Quantity.Should().Be(quantity);
        result.UserId.Should().Be(userId);
        result.ProductId.Should().Be(productId);
        result.AddedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(productId), Times.Once);
        _mockCartRepository.Verify(repo => repo.GetCartItemByUserIdAndProductIdAsync(userId, productId), Times.Once);
        _mockCartRepository.Verify(repo => repo.AddCartItemAsync(It.Is<CartItem>(ci =>
            ci.UserId == userId && ci.ProductId == productId && ci.Quantity == quantity && ci.AddedAt != default
        )), Times.Once);
        _mockCartRepository.Verify(repo => repo.UpdateCartItemAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task AddOrUpdateCartItemAsync_ShouldThrowInvalidOperationException_WhenAddingItemAndNotEnoughStock()
    {
        // -- Arrange -- 
        var userId = 1;
        var productId = 10;
        var quantity = 10;
        var product = new Product { Id = productId, Name = "Low Stock Product", Quantity = 5, InventoryStatus = "LOWSTOCK" }; // Stock insuffisant
        var newCartItem = new CartItem { UserId = userId, ProductId = productId, Quantity = quantity };

        _mockProductRepository.Setup(repo => repo.GetProductByIdAsync(productId))
                              .ReturnsAsync(product);
        _mockCartRepository.Setup(repo => repo.GetCartItemByUserIdAndProductIdAsync(userId, productId))
                           .ReturnsAsync((CartItem?)null);

        // -- Act & Assert -- 
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _cartService.AddOrUpdateCartItemAsync(newCartItem));
        exception.Message.Should().Contain($"Il n'a y pas acces de stock le produit {product.Name} à la quantité : {product.Quantity}");

        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(productId), Times.Once);
        _mockCartRepository.Verify(repo => repo.GetCartItemByUserIdAndProductIdAsync(userId, productId), Times.Once);
        _mockCartRepository.Verify(repo => repo.AddCartItemAsync(It.IsAny<CartItem>()), Times.Never);
        _mockCartRepository.Verify(repo => repo.UpdateCartItemAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task AddOrUpdateCartItemAsync_ShouldUpdateItemQuantity_WhenProductExistsAndAlreadyInCartAndEnoughStock()
    {
        // -- Arrange -- 
        var userId = 1;
        var productId = 10;
        var initialQuantityInCart = 2;
        var quantityToAdd = 3;
        var totalExpectedQuantity = initialQuantityInCart + quantityToAdd;
        var product = new Product { Id = productId, Name = "Test Product", Quantity = 10, InventoryStatus = "INSTOCK" };
        var existingCartItem = new CartItem { Id = 100, UserId = userId, ProductId = productId, Quantity = initialQuantityInCart, AddedAt = DateTime.UtcNow.AddHours(-1) };
        var incomingCartItem = new CartItem { UserId = userId, ProductId = productId, Quantity = quantityToAdd };

        _mockProductRepository.Setup(repo => repo.GetProductByIdAsync(productId))
                              .ReturnsAsync(product);
        _mockCartRepository.Setup(repo => repo.GetCartItemByUserIdAndProductIdAsync(userId, productId))
                           .ReturnsAsync(existingCartItem);
       
        // -- Act -- 
        var result = await _cartService.AddOrUpdateCartItemAsync(incomingCartItem);

        // -- Assert -- 
        result.Should().NotBeNull();
        result.Id.Should().Be(existingCartItem.Id);
        result.Quantity.Should().Be(totalExpectedQuantity);
        result.UserId.Should().Be(userId);
        result.ProductId.Should().Be(productId);
       
        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(productId), Times.Once);
        _mockCartRepository.Verify(repo => repo.GetCartItemByUserIdAndProductIdAsync(userId, productId), Times.Once);
        _mockCartRepository.Verify(repo => repo.AddCartItemAsync(It.IsAny<CartItem>()), Times.Never);
        _mockCartRepository.Verify(repo => repo.UpdateCartItemAsync(It.Is<CartItem>(ci =>
            ci.Id == existingCartItem.Id && ci.Quantity == totalExpectedQuantity && ci.AddedAt != default
        )), Times.Once);
    }

    [Fact]
    public async Task AddOrUpdateCartItemAsync_ShouldThrowInvalidOperationException_WhenUpdatingItemAndNotEnoughStock()
    {
        // -- Arrange -- 
        var userId = 1;
        var productId = 10;
        var initialQuantityInCart = 8;
        var quantityToAdd = 3;
        var product = new Product { Id = productId, Name = "Very Low Stock Product", Quantity = 10, InventoryStatus = "LOWSTOCK" };
        var existingCartItem = new CartItem { Id = 100, UserId = userId, ProductId = productId, Quantity = initialQuantityInCart, AddedAt = DateTime.UtcNow.AddHours(-1) };
        var incomingCartItem = new CartItem { UserId = userId, ProductId = productId, Quantity = quantityToAdd };

        _mockProductRepository.Setup(repo => repo.GetProductByIdAsync(productId))
                              .ReturnsAsync(product);
        _mockCartRepository.Setup(repo => repo.GetCartItemByUserIdAndProductIdAsync(userId, productId))
                           .ReturnsAsync(existingCartItem);

        // -- Act & Assert -- 
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _cartService.AddOrUpdateCartItemAsync(incomingCartItem));
        exception.Message.Should().Contain($"Il n'a y pas acces de stock le produit {product.Name} à la quantité : {product.Quantity}");

        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(productId), Times.Once);
        _mockCartRepository.Verify(repo => repo.GetCartItemByUserIdAndProductIdAsync(userId, productId), Times.Once);
        _mockCartRepository.Verify(repo => repo.AddCartItemAsync(It.IsAny<CartItem>()), Times.Never);
        _mockCartRepository.Verify(repo => repo.UpdateCartItemAsync(It.IsAny<CartItem>()), Times.Never);
    }

    // --- Tests pour RemoveCartItemAsync ---
    [Fact]
    public async Task RemoveCartItemAsync_ShouldReturnTrue_WhenProductExistsAndItemIsRemoved()
    {
        // -- Arrange -- 
        var userId = 1;
        var productId = 10;
        var product = new Product { Id = productId, Name = "Test Product" }; 

        _mockProductRepository.Setup(repo => repo.GetProductByIdAsync(productId)).ReturnsAsync(product);
        _mockCartRepository.Setup(repo => repo.DeleteCartItemAsync(userId, productId)).ReturnsAsync(true);

        // -- Act -- 
        var result = await _cartService.RemoveCartItemAsync(userId, productId);

        // -- Assert -- 
        result.Should().BeTrue();
        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(productId), Times.Once);
        _mockCartRepository.Verify(repo => repo.DeleteCartItemAsync(userId, productId), Times.Once);
    }

    [Fact]
    public async Task RemoveCartItemAsync_ShouldThrowKeyNotFoundException_WhenProductDoesNotExist()
    {
        // -- Arrange -- 
        var userId = 1;
        var productId = 999; 
        _mockProductRepository.Setup(repo => repo.GetProductByIdAsync(productId)).ReturnsAsync((Product?)null);

        // -- Act & Assert -- 
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _cartService.RemoveCartItemAsync(userId, productId));
        exception.Message.Should().Contain($"Aucun produit trouvé avec l'ID {productId}.");

        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(productId), Times.Once);
        _mockCartRepository.Verify(repo => repo.DeleteCartItemAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RemoveCartItemAsync_ShouldReturnFalse_WhenProductExistsButItemIsNotInCart()
    {
        // -- Arrange -- 
        var userId = 1;
        var productId = 10;
        var product = new Product { Id = productId, Name = "Test Product" }; 

        _mockProductRepository.Setup(repo => repo.GetProductByIdAsync(productId)).ReturnsAsync(product);
        _mockCartRepository.Setup(repo => repo.DeleteCartItemAsync(userId, productId)).ReturnsAsync(false);

        // -- Act -- 
        var result = await _cartService.RemoveCartItemAsync(userId, productId);

        // -- Assert -- 
        result.Should().BeFalse();
        _mockProductRepository.Verify(repo => repo.GetProductByIdAsync(productId), Times.Once);
        _mockCartRepository.Verify(repo => repo.DeleteCartItemAsync(userId, productId), Times.Once);
    }

    // --- Tests pour ClearCartAsync ---
    [Fact]
    public async Task ClearCartAsync_ShouldReturnTrue_WhenCartIsClearedSuccessfully()
    {
        // -- Arrange -- 
        var userId = 1;
        _mockCartRepository.Setup(repo => repo.DeleteAllCartItemsByUserIdAsync(userId)).ReturnsAsync(true);

        // -- Act -- 
        var result = await _cartService.ClearCartAsync(userId);

        // -- Assert -- 
        result.Should().BeTrue();
        _mockCartRepository.Verify(repo => repo.DeleteAllCartItemsByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task ClearCartAsync_ShouldReturnFalse_WhenCartIsEmptyOrClearFails()
    {
        // -- Arrange -- 
        var userId = 1;
        _mockCartRepository.Setup(repo => repo.DeleteAllCartItemsByUserIdAsync(userId)).ReturnsAsync(false);

        // -- Act -- 
        var result = await _cartService.ClearCartAsync(userId);

        // -- Assert -- 
        result.Should().BeFalse();
        _mockCartRepository.Verify(repo => repo.DeleteAllCartItemsByUserIdAsync(userId), Times.Once);
    }
}
