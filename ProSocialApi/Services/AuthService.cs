// AUTHSERVICE.CS - Service d'authentification
// Implémente IAuthService : gère l'inscription et la connexion des utilisateurs.
// Utilise BCrypt pour le hachage des mots de passe et JWT pour les tokens.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProSocialApi.Data.Context;
using ProSocialApi.Data.Entities;
using ProSocialApi.DTOs.Auth;
using ProSocialApi.Services.Interfaces;

namespace ProSocialApi.Services;

/// <summary>
/// Service d'authentification - Gère l'inscription et la connexion.
/// Injecté via DI comme Scoped (une instance par requête HTTP).
/// </summary>
public class AuthService : IAuthService
{
    // DÉPENDANCES INJECTÉES
    /// <summary>
    /// Contexte de base de données pour accéder aux utilisateurs.
    /// </summary>
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Configuration de l'application pour récupérer les paramètres JWT.
    /// </summary>
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Constructeur avec injection de dépendances.
    /// Les dépendances sont fournies automatiquement par le container DI.
    /// </summary>
    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // MÉTHODES PUBLIQUES (Interface IAuthService)
    /// <summary>
    /// Inscrit un nouvel utilisateur.
    ///
    /// Processus :
    /// 1. Vérifie l'unicité de l'email (insensible à la casse)
    /// 2. Hash le mot de passe avec BCrypt
    /// 3. Crée l'utilisateur en base
    /// 4. Génère un token JWT
    /// </summary>
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        // Vérifier si l'email existe déjà (comparaison insensible à la casse)
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == registerDto.Email.ToLower());

        if (existingUser != null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Cet email est déjà utilisé"
            };
        }

        // Créer le nouvel utilisateur avec mot de passe hashé
        // BCrypt.HashPassword génère automatiquement un salt unique
        var user = new User
        {
            Email = registerDto.Email.ToLower(), // Stocké en minuscules pour cohérence
            Password = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            Headline = registerDto.Headline
        };

        // Ajouter et sauvegarder en base de données
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Générer le token JWT pour connexion automatique après inscription
        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Inscription réussie",
            Token = token,
            User = MapToUserInfo(user)
        };
    }

    /// <summary>
    /// Authentifie un utilisateur avec email et mot de passe.
    ///
    /// Sécurité : Le message d'erreur est générique ("Email ou mot de passe incorrect")
    /// pour ne pas révéler si l'email existe ou non (protection contre l'énumération).
    /// </summary>
    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        // Rechercher l'utilisateur par email (insensible à la casse)
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower());

        // Utilisateur non trouvé - message générique
        if (user == null)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Email ou mot de passe incorrect"
            };
        }

        // Vérifier le mot de passe avec BCrypt
        // BCrypt.Verify compare le mot de passe en clair avec le hash stocké
        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Email ou mot de passe incorrect" 
            };
        }

        // Authentification réussie - générer le token
        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Success = true,
            Message = "Connexion réussie",
            Token = token,
            User = MapToUserInfo(user)
        };
    }

    // MÉTHODES PRIVÉES
    /// <summary>
    /// Génère un token JWT pour un utilisateur authentifié.
    ///
    /// Structure du token :
    /// - sub (Subject) : ID de l'utilisateur (GUID)
    /// - email : Email de l'utilisateur
    /// - given_name : Prénom
    /// - family_name : Nom
    /// - jti : ID unique du token (pour invalidation potentielle)
    /// - exp : Date d'expiration
    /// </summary>
    /// <param name="user">Utilisateur pour lequel générer le token</param>
    /// <returns>Token JWT encodé en string</returns>
    private string GenerateJwtToken(User user)
    {
        // Récupérer les paramètres JWT depuis appsettings.json
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT Secret not configured");
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expiresInDays = int.Parse(_configuration["Jwt:ExpiresInDays"] ?? "7");

        // Créer la clé de signature à partir du secret
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Définir les claims (données) du token
        // JwtRegisteredClaimNames contient les noms de claims standards
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // Subject = User ID
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // ID unique du token
        };

        // Créer le token avec tous les paramètres
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expiresInDays), // Expiration en UTC
            signingCredentials: credentials
        );

        // Encoder le token en string Base64
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Convertit une entité User en UserInfoDto.
    /// Exclut les informations sensibles (mot de passe).
    /// </summary>
    private static UserInfoDto MapToUserInfo(User user)
    {
        return new UserInfoDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Headline = user.Headline,
            AvatarUrl = user.AvatarUrl
        };
    }
}
