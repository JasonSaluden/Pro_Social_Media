// USERDTO.CS - DTO pour l'affichage d'un profil utilisateur
// DTO utilisé pour renvoyer les informations d'un profil utilisateur.
// Contient les informations publiques du profil ainsi que des statistiques
// (nombre de connexions, nombre de posts).

namespace ProSocialApi.DTOs.Users;

/// <summary>
/// DTO représentant le profil complet d'un utilisateur.
/// Utilisé par les endpoints :
/// - GET /api/users/me (profil de l'utilisateur connecté)
/// - GET /api/users/{id} (profil d'un autre utilisateur)
///
/// Différences avec l'entité User :
/// - Pas de mot de passe (sécurité !)
/// - Inclut des statistiques calculées (ConnectionsCount, PostsCount)
/// - Pas de propriétés de navigation EF Core
/// </summary>
public class UserDto
{
    // IDENTIFICATION
    /// <summary>
    /// Identifiant unique de l'utilisateur.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Adresse email de l'utilisateur.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    // INFORMATIONS DE PROFIL
    /// <summary>
    /// Prénom de l'utilisateur.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Nom de famille de l'utilisateur.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Nom complet (propriété calculée).
    /// Pratique pour l'affichage direct côté client.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Titre professionnel (ex: "Développeur Full Stack").
    /// Affiché sous le nom dans les cartes de profil.
    /// </summary>
    public string? Headline { get; set; }

    /// <summary>
    /// Biographie / À propos de l'utilisateur.
    /// Texte libre décrivant le parcours et les compétences.
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// URL de la photo de profil.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Date de création du compte.
    /// Permet d'afficher "Membre depuis..."
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // STATISTIQUES
    // Ces valeurs sont calculées par le service à partir des relations.
    // Elles ne sont pas stockées directement sur l'entité User.
    /// <summary>
    /// Nombre de connexions acceptées de l'utilisateur.
    /// Équivalent du nombre de "contacts" sur LinkedIn.
    /// </summary>
    public int ConnectionsCount { get; set; }

    /// <summary>
    /// Nombre de posts publiés par l'utilisateur.
    /// Indicateur d'activité sur le réseau.
    /// </summary>
    public int PostsCount { get; set; }
}
