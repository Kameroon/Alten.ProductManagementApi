using Alten.ProductManagementApi.Helpers; 
using Alten.ProductManagementApi.Models;
using Alten.ProductManagementApi.Repositories.Interfaces; 
using Alten.ProductManagementApi.Services;
using Alten.ProductManagementApi.Services.Implementations;
using Alten.ProductManagementApi.Services.Interfaces;
using Alten.ProductManagementApi.Test.Services;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Alten.ProductManagementApi.Tests.Services;
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();

        _userService = new UserService(_mockUserRepository.Object, _mockPasswordHasher.Object);
    }

    // --- Tests pour AuthenticateAsync ---
    [Fact]
    public async Task AuthenticateAsync_ShouldReturnUser_WhenCredentialsAreValidAndUserIsActive()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password123";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User { Id = 1, Email = email, PasswordHash = hashedPassword, IsActive = true };

        // Configure le mock UserRepository pour retourner l'utilisateur
        _mockUserRepository.Setup(repo => repo.GetUserByEmailAsync(email))
                           .ReturnsAsync(user);

        // Configure le mock PasswordHasher pour qu'il retourne true quand VerifyPassword est appelé
        // avec N'IMPORTE QUEL string comme mot de passe et N'IMPORTE QUEL string comme hachage.
        // Ceci est une étape de débogage pour s'assurer que la méthode est appelée.
        _mockPasswordHasher.Setup(hasher => hasher.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(true);

        // Act
        var result = await _userService.ValidateUserCredentialsAsync(email, password);

        // Assert
        result.User.Should().BeEquivalentTo(user);
        _mockUserRepository.Verify(repo => repo.GetUserByEmailAsync(email), Times.Once);

        _mockPasswordHasher.Verify(hasher => hasher.VerifyPassword(password, hashedPassword), Times.Once);
        //// Vérifie que VerifyPassword a été appelé au moins une fois avec n'importe quels arguments.
        //_mockPasswordHasher.Verify(hasher => hasher.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        //_mockUserRepository.Setup(repo => repo.GetUserByEmailAsync(email))
        //                   .ReturnsAsync(user);
        //_mockPasswordHasher.Setup(hasher => hasher.VerifyPassword(password, hashedPassword))
        //                   .Returns(true); // Le mot de passe correspond

        //// Act
        //var result = await _userService.ValidateUserCredentialsAsync(email, password);

        //// Assert
        //result.User.Should().BeEquivalentTo(user);
        //_mockUserRepository.Verify(repo => repo.GetUserByEmailAsync(email), Times.Once); 
        //_mockPasswordHasher.Verify(hasher => hasher.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "password123";

        _mockUserRepository.Setup(repo => repo.GetUserByEmailAsync(email))
                           .ReturnsAsync((User?)null); // Simule utilisateur non trouvé

        // Act
        var result = await _userService.ValidateUserCredentialsAsync(email, password);

        // Assert
        result.User.Should().BeNull();
        _mockUserRepository.Verify(repo => repo.GetUserByEmailAsync(email), Times.Once);
        // Verify que le hachage de mot de passe n'est jamais appelé si l'utilisateur n'est pas trouvé
        _mockPasswordHasher.Verify(hasher => hasher.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenPasswordIsInvalid()
    {
        // Arrange
        var email = "test@example.com";
        var password = "invalid_password";
        var hashedPassword = "hashed_password_from_hasher";
        var user = new User { Id = 1, Email = email, PasswordHash = hashedPassword, IsActive = true };

        _mockUserRepository.Setup(repo => repo.GetUserByEmailAsync(email))
                           .ReturnsAsync(user);
        _mockPasswordHasher.Setup(hasher => hasher.VerifyPassword(password, hashedPassword))
                           .Returns(false); // Simule mot de passe incorrect

        // Act
        var result = await _userService.ValidateUserCredentialsAsync(email, password);

        // Assert
        result.Should().BeNull();
        _mockUserRepository.Verify(repo => repo.GetUserByEmailAsync(email), Times.Once);
        _mockPasswordHasher.Verify(hasher => hasher.VerifyPassword(password, hashedPassword), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenUserIsNotActive()
    {
        // Arrange
        var email = "inactive@example.com";
        var password = "password123";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User { Id = 1, Email = email, PasswordHash = hashedPassword, IsActive = false }; // Utilisateur inactif

        _mockUserRepository.Setup(repo => repo.GetUserByEmailAsync(email))
                           .ReturnsAsync(user);
        _mockPasswordHasher.Setup(hasher => hasher.VerifyPassword(password, hashedPassword))
                           .Returns(true);

        // Act
        var result = await _userService.ValidateUserCredentialsAsync(email, password);

        // Assert
        result.User.Should().BeNull();
        result.IsSuccess.Should().BeFalse();
        _mockUserRepository.Verify(repo => repo.GetUserByEmailAsync(email), Times.Once);
        // Verify que le hachage de mot de passe n'est jamais appelé si l'utilisateur n'est pas trouvé
        _mockPasswordHasher.Verify(hasher => hasher.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // --- Tests pour RegisterAsync ---
    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenEmailIsNotTaken()
    {
        // Arrange
        var newUserRequest = new User { Email = "newuser@example.com", Username = "newuser", Firstname = "New", PasswordHash = "plainpassword" };
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newUserRequest.PasswordHash);
        var expectedUserId = 5;

        _mockUserRepository.Setup(repo => repo.GetUserByEmailAsync(newUserRequest.Email))
                           .ReturnsAsync((User?)null); // Email non pris
        _mockPasswordHasher.Setup(hasher => hasher.HashPassword(newUserRequest.PasswordHash!))
                           .Returns(hashedPassword);
        //_mockUserRepository.Setup(repo => repo.AddUserAsync(It.IsAny<User>()))
        //                   .ReturnsAsync(expectedUserId);

        // Act
        var createdUser = await _userService.CreateUserAsync(newUserRequest);

        // Assert
        createdUser.Should().NotBeNull();
        createdUser.Id.Should().Be(expectedUserId);
        createdUser.Email.Should().Be(newUserRequest.Email);
        createdUser.PasswordHash.Should().Be(hashedPassword); 
        createdUser.IsActive.Should().BeTrue(); 
        createdUser.CreatedAt.Should().BePositive();

        _mockUserRepository.Verify(repo => repo.GetUserByEmailAsync(newUserRequest.Email), Times.Once);
        _mockPasswordHasher.Verify(hasher => hasher.HashPassword(newUserRequest.PasswordHash!), Times.Once);
        _mockUserRepository.Verify(repo => repo.AddUserAsync(It.Is<User>(u =>
            u.Email == newUserRequest.Email && u.PasswordHash == hashedPassword && u.IsActive == true
        )), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnNull_WhenEmailIsAlreadyTaken()
    {
        // Arrange
        var existingEmail = "existing@example.com";
        var newUserRequest = new User { Email = existingEmail, Username = "existing", PasswordHash = "password" };
        var existingUser = new User { Id = 1, Email = existingEmail }; // Un utilisateur existant

        // Le référentiel doit retourner un utilisateur existant pour l'e-mail donné
        _mockUserRepository.Setup(repo => repo.GetUserByEmailAsync(existingEmail))
                           .ReturnsAsync(existingUser); // <-- C'EST LA CLÉ : Retourne un utilisateur !

        // Act & Assert
        // On s'attend à ce qu'une InvalidOperationException soit levée
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userService.CreateUserAsync(newUserRequest)
        );
    }
}
