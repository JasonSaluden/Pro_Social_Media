// IAUTHSERVICE.CS - Interface du service d'authentification
// Définit le contrat pour les opérations d'authentification :
// - Inscription de nouveaux utilisateurs
// - Connexion des utilisateurs existants
//
// L'utilisation d'une interface permet :
// - L'injection de dépendances (DI)
// - Les tests unitaires avec des mocks
// - Le respect du principe d'inversion de dépendances (SOLID)

using ProSocialApi.DTOs.Auth;

namespace ProSocialApi.Services.Interfaces;

/// <summary>
/// Interface pour le service d'authentification.
/// Gère l'inscription et la connexion des utilisateurs.
///
/// Implémentation : AuthService
/// Enregistrement DI : AddScoped&lt;IAuthService, AuthService&gt;()
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Inscrit un nouvel utilisateur dans le système.
    ///
    /// Processus :
    /// 1. Vérifie que l'email n'est pas déjà utilisé
    /// 2. Hash le mot de passe avec BCrypt
    /// 3. Crée l'utilisateur en base de données
    /// 4. Génère un token JWT
    /// 5. Retourne le token et les infos utilisateur
    /// </summary>
    /// <param name="registerDto">Données d'inscription (email, password, nom, etc.)</param>
    /// <returns>
    /// AuthResponseDto avec :
    /// - Success = true + Token + User si inscription réussie
    /// - Success = false + Message d'erreur sinon
    /// </returns>
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);

    /// <summary>
    /// Authentifie un utilisateur avec son email et mot de passe.
    ///
    /// Processus :
    /// 1. Recherche l'utilisateur par email
    /// 2. Vérifie le mot de passe avec BCrypt
    /// 3. Génère un token JWT si valide
    /// 4. Retourne le token et les infos utilisateur
    ///
    /// Sécurité : En cas d'échec, un message générique est retourné
    /// ("Email ou mot de passe incorrect") pour ne pas révéler
    /// si l'email existe ou non dans le système.
    /// </summary>
    /// <param name="loginDto">Identifiants de connexion (email, password)</param>
    /// <returns>
    /// AuthResponseDto avec :
    /// - Success = true + Token + User si connexion réussie
    /// - Success = false + Message d'erreur sinon
    /// </returns>
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
}
