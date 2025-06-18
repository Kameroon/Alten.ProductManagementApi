using Alten.ProductManagementApi.Helpers;
using Alten.ProductManagementApi.Models;
using Alten.ProductManagementApi.Repositories.Interfaces;
using Alten.ProductManagementApi.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Alten.ProductManagementApi.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<User> CreateUserAsync(User user)
    {
        // Vérifie si l'utilisateur existe déjà par email avant de le créer
        var existingUser = await _userRepository.GetUserByEmailAsync(user.Email);
        if (existingUser != null)
        {
            // Tu pourrais lancer une exception spécifique ou retourner null/un objet d'erreur
            throw new InvalidOperationException($"User with email '{user.Email}' already exists.");
        }

        // Le hachage du mot de passe doit être fait ici dans la couche service
        // avant de passer l'utilisateur au repository pour le sauvegarder.
        // Le PasswordHash de l'objet User passé ici doit être le mot de passe en clair du DTO
        // ou tu devras passer le mot de passe en clair directement à cette méthode.
        // Assure-toi que ton endpoint appelle cette méthode avec le mot de passe HASHÉ
        // ou ajoute le hachage ici si ton DTO de création d'utilisateur contient le mot de passe en clair.
        // Pour l'instant, je suppose que l'objet User reçu ici a déjà son PasswordHash défini
        // par l'endpoint (comme dans l'exemple WebApplicationExtensions.MapAuthenticationEndpoints).
        // Si le DTO est passé au service, le hachage se ferait comme ceci :
        // user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password); // si User avait une prop Password

        // Pour simplifier et suivre le DTO RegisterRequest -> User model,
        // on suppose que l'endpoint a déjà haché le mot de passe avant de créer l'objet User.
        // Si tu passes le DTO au service, tu feras le hachage ici.
        // Exemple : public async Task<User> CreateUserAsync(RegisterRequest request) { ... user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password); ... }

        // Hachage du mot de passe
        user.PasswordHash = _passwordHasher.HashPassword(user.PasswordHash);

        // Définition de IsActive et CreatedAt juste avant l'ajout
        user.IsActive = true; // <-- Définition explicite à TRUE
        user.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // <-- Définition à la date du jour (timestamp Unix)

        // Le repository va insérer l'utilisateur et retourner l'objet User avec son ID généré.
        return await _userRepository.AddUserAsync(user);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userRepository.GetUserByEmailAsync(email);
    }

    public async Task<bool> ValidateUserCredentialsAsync(string email, string password)
    {
        var user = await _userRepository.GetUserByEmailAsync(email);
        if (user == null)
        {
            return false; // Utilisateur non trouvé
        }

        // Vérifie le mot de passe haché
        // BCrypt.Net.BCrypt.Verify(plainTextPassword, hashedPassword)
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }
}