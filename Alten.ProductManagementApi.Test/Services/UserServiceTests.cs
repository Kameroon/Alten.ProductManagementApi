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
    private readonly IPasswordHasher _passwordHasher;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _passwordHasher = new PasswordHasher();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _userService = new UserService(_mockUserRepository.Object, _mockPasswordHasher.Object);
    }


    [Fact]
    public async Task AuthenticateAsync_ShouldReturnUser_WhenCredentialsAreValidAndUserIsActive()
    {
        // -- Arrange --
        var newUserRequest = new User
        {
            Email = "newuser@example.com",
            Username = "newuser",
            Firstname = "New",
            // -- PasswordHash est le mot de passe en CLAIR -- 
            PasswordHash = "plainpassword"
        };

        // -- Simule le hachage du mot de passe par le PasswordHasher -- 
        var hashedPassword = _passwordHasher.HashPassword(newUserRequest.PasswordHash);
        var expectedUserId = 5;

        // -- Retourne null (email non pris) -- 
        _mockUserRepository.Setup(repo => repo.GetUserByEmailAsync(newUserRequest.Email)).ReturnsAsync((User?)null);
        // -- HashPassword retourne le hachage -- 
        _mockPasswordHasher.Setup(hasher => hasher.HashPassword(newUserRequest.PasswordHash!)).Returns(hashedPassword);
        // -- AddUserAsync retourne une *nouvelle* instance de User -- 
        _mockUserRepository.Setup(repo => repo.AddUserAsync(It.IsAny<User>())).ReturnsAsync((User userArgument) =>
                           {
                               var userAfterAdd = new User
                               {
                                   Id = expectedUserId,
                                   Email = userArgument.Email,
                                   Username = userArgument.Username,
                                   Firstname = userArgument.Firstname,
                                   PasswordHash = userArgument.PasswordHash,
                                   IsActive = userArgument.IsActive,
                                   CreatedAt = userArgument.CreatedAt
                               };
                               return userAfterAdd;
                           });

        // -- Act --
        var createdUser = await _userService.CreateUserAsync(newUserRequest);

        // -- Assert --
        createdUser.Should().NotBeNull();
        createdUser.Id.Should().Be(expectedUserId);
        createdUser.Email.Should().Be(newUserRequest.Email);
        createdUser.PasswordHash.Should().Be(hashedPassword);
        createdUser.IsActive.Should().BeTrue();
        // -- Pour CreatedAt, doit être positif et pas 0. -- 
        createdUser.CreatedAt.Should().BePositive();

        _mockUserRepository.Verify(repo => repo.GetUserByEmailAsync(newUserRequest.Email), Times.Once);
        _mockUserRepository.Verify(repo => repo.AddUserAsync(It.Is<User>(u =>
            u.Email == newUserRequest.Email &&
            u.Username == newUserRequest.Username &&
            u.PasswordHash == hashedPassword &&
            u.IsActive == true
        )), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // -- Arrange -- 
        var email = "nonexistent@example.com";
        var password = "password123";

        _mockUserRepository.Setup(repo => repo.GetUserByEmailAsync(email)).ReturnsAsync((User?)null);

        // -- Act -- 
        var result = await _userService.ValidateUserCredentialsAsync(email, password);

        // -- Assert -- 
        result.User.Should().BeNull();
        _mockUserRepository.Verify(repo => repo.GetUserByEmailAsync(email), Times.Once);
        _mockPasswordHasher.Verify(hasher => hasher.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenPasswordIsInvalid()
    {
        // -- Arrange -- 
        var email = "test@example.com";
        var password = "invalid_password";
        var wrongPassword = "wrongPassword";
        var hashedPassword = _passwordHasher.HashPassword(wrongPassword);
        var user = new User { Id = 1, Email = email, PasswordHash = hashedPassword, IsActive = true };

        _mockUserRepository.Setup(repo => repo.GetUserByEmailAsync(email)).ReturnsAsync(user);
        _mockPasswordHasher.Setup(hasher => hasher.VerifyPassword(password, hashedPassword)).Returns(false);

        // -- Act -- 
        var result = await _userService.ValidateUserCredentialsAsync(email, password);

        // -- Assert -- 
        result.User.Should().BeNull();
        _mockUserRepository.Verify(repo => repo.GetUserByEmailAsync(email), Times.Once);
        _mockPasswordHasher.Verify(hasher => hasher.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenUserIsNotActive()
    {
        // -- Arrange -- 
        var email = "inactive@example.com";
        var password = "password123";
        var hashedPassword = _passwordHasher.HashPassword(password);
        var user = new User { Id = 1, Email = email, PasswordHash = hashedPassword, IsActive = false }; // Utilisateur inactif

        _mockUserRepository.Setup(repo => repo.GetUserByEmailAsync(email)).ReturnsAsync(user);
        _mockPasswordHasher.Setup(hasher => hasher.VerifyPassword(password, hashedPassword)).Returns(true);

        // -- Act -- 
        var result = await _userService.ValidateUserCredentialsAsync(email, password);

        // -- Assert -- 
        result.User.Should().BeNull();
        result.IsSuccess.Should().BeFalse();
        _mockUserRepository.Verify(repo => repo.GetUserByEmailAsync(email), Times.Once);
        // -- Verify que le hachage de mot de passe n'est jamais appelé si l'utilisateur n'est pas trouvé -- 
        _mockPasswordHasher.Verify(hasher => hasher.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // --- Tests pour RegisterAsync ---
    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenEmailIsNotTaken()
    {
        // -- Arrange --
        var newUserRequest = new User
        {
            Email = "newuser@example.com",
            Username = "newuser",
            Firstname = "New",
            PasswordHash = "plainpassword"
        };

        var hashedPassword = _passwordHasher.HashPassword(newUserRequest.PasswordHash);
        var expectedUserId = 5;

        // -- Retourne null (email non pris) -- 
        _mockUserRepository.Setup(repo => repo.GetUserByEmailAsync(newUserRequest.Email))
                           .ReturnsAsync((User?)null);

        _mockPasswordHasher.Setup(hasher => hasher.HashPassword(It.IsAny<string>())).Returns(hashedPassword);

        // -- Retourne une *nouvelle* instance de User --
        _mockUserRepository.Setup(repo => repo.AddUserAsync(It.IsAny<User>()))
                           .ReturnsAsync((User userArgument) =>
                           {
                               var userAfterAdd = new User
                               {
                                   Id = expectedUserId,
                                   Email = userArgument.Email,
                                   Username = userArgument.Username,
                                   Firstname = userArgument.Firstname,
                                   PasswordHash = userArgument.PasswordHash, 
                                   IsActive = userArgument.IsActive,         
                                   CreatedAt = userArgument.CreatedAt
                               };
                               return userAfterAdd;
                           });

        // -- Act --
        var createdUser = await _userService.CreateUserAsync(newUserRequest);

        // -- Assert --
        // -- Vérifie que les propriétés de l'objet User sont bien valorisé -- 
        createdUser.Should().NotBeNull();
        createdUser.Id.Should().Be(expectedUserId);
        createdUser.Email.Should().Be(newUserRequest.Email);
        createdUser.PasswordHash.Should().Be(hashedPassword);
        createdUser.IsActive.Should().BeTrue();
        createdUser.CreatedAt.Should().BePositive();

        // -- Vérifie que l'existence de l'email -- 
        _mockUserRepository.Verify(repo => repo.GetUserByEmailAsync(newUserRequest.Email), Times.Once);
        _mockUserRepository.Verify(repo => repo.AddUserAsync(It.Is<User>(u =>
            u.Email == newUserRequest.Email &&
            u.Username == newUserRequest.Username &&
            u.PasswordHash == hashedPassword &&
            u.IsActive == true &&
            u.CreatedAt > 0 // Vérifie que CreatedAt a été défini par le service avant d'appeler le repo
        )), Times.Once);
    }


    [Fact]
    public async Task RegisterAsync_ShouldReturnNull_WhenEmailIsAlreadyTaken()
    {
        // -- Arrange -- 
        var existingEmail = "existing@example.com";
        var newUserRequest = new User { Email = existingEmail, Username = "existing", PasswordHash = "password" };
        var existingUser = new User { Id = 1, Email = existingEmail }; // Un utilisateur existant

        // -- Le référentiel doit retourner un utilisateur existant pour l'e-mail donné -- 
        _mockUserRepository.Setup(repo => repo.GetUserByEmailAsync(existingEmail)).ReturnsAsync(existingUser);

        // -- Act & Assert -- 
        // -- On s'attend à ce qu'une InvalidOperationException soit levée -- 
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userService.CreateUserAsync(newUserRequest)
        );
    }
}
