// REGISTERDTO.CS - DTO pour l'inscription d'un nouvel utilisateur
// DTO (Data Transfer Object) utilisé pour recevoir les données d'inscription
// depuis le client. Contient toutes les validations nécessaires pour garantir
// des données valides avant de créer un nouvel utilisateur en base.

using System.ComponentModel.DataAnnotations;

namespace ProSocialApi.DTOs.Auth;

/// <summary>
/// DTO pour la requête d'inscription (POST /api/auth/register).
/// Contient les informations nécessaires à la création d'un nouveau compte.
///
/// Les attributs de validation sont vérifiés automatiquement par ASP.NET Core
/// avant l'exécution du code du controller. Si une validation échoue,
/// une réponse 400 Bad Request est renvoyée avec les messages d'erreur.
/// </summary>
public class RegisterDto
{
    // INFORMATIONS D'AUTHENTIFICATION
    /// <summary>
    /// Adresse email de l'utilisateur (servira d'identifiant de connexion).
    /// - [Required] : Champ obligatoire
    /// - [EmailAddress] : Vérifie le format email (présence de @ et domaine)
    /// </summary>
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mot de passe choisi par l'utilisateur.
    /// - [Required] : Champ obligatoire
    /// - [MinLength(6)] : Minimum 6 caractères pour la sécurité
    ///
    /// Note : Le mot de passe sera hashé avec BCrypt avant stockage.
    /// Il n'est jamais stocké en clair en base de données.
    /// </summary>
    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation du mot de passe (doit être identique à Password).
    /// - [Required] : Champ obligatoire
    /// - [Compare] : Vérifie que la valeur est identique à Password
    ///
    /// Cette vérification évite les erreurs de saisie lors de l'inscription.
    /// </summary>
    [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
    [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // INFORMATIONS DE PROFIL
    /// <summary>
    /// Prénom de l'utilisateur.
    /// - [Required] : Champ obligatoire
    /// - [MaxLength(100)] : Limite pour éviter les abus et optimiser le stockage
    /// </summary>
    [Required(ErrorMessage = "Le prénom est requis")]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Nom de famille de l'utilisateur.
    /// </summary>
    [Required(ErrorMessage = "Le nom est requis")]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Titre professionnel (optionnel).
    /// Exemple : "Développeur Full Stack | React & .NET"
    /// Peut être renseigné plus tard via la mise à jour du profil.
    /// </summary>
    [MaxLength(255)]
    public string? Headline { get; set; }
}
