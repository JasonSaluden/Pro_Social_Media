// LOGINDTO.CS - DTO pour la connexion d'un utilisateur
// DTO utilisé pour recevoir les identifiants de connexion depuis le client.
// Simple mais essentiel : email + mot de passe.

using System.ComponentModel.DataAnnotations;

namespace ProSocialApi.DTOs.Auth;

/// <summary>
/// DTO pour la requête de connexion (POST /api/auth/login).
/// Contient les identifiants nécessaires pour authentifier un utilisateur.
///
/// En cas de succès, le service retourne un JWT token permettant d'accéder
/// aux endpoints protégés de l'API.
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Adresse email de l'utilisateur (identifiant de connexion).
    /// - [Required] : Champ obligatoire
    /// - [EmailAddress] : Vérifie le format email
    ///
    /// L'email doit correspondre à un compte existant en base de données.
    /// </summary>
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mot de passe de l'utilisateur.
    /// - [Required] : Champ obligatoire
    ///
    /// Le mot de passe sera comparé au hash BCrypt stocké en base.
    /// En cas d'échec, un message générique est retourné (sécurité).
    /// </summary>
    [Required(ErrorMessage = "Le mot de passe est requis")]
    public string Password { get; set; } = string.Empty;
}
