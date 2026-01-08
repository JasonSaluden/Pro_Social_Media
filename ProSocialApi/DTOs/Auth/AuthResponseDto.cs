// AUTHRESPONSEDTO.CS - DTOs pour les réponses d'authentification
// Ces DTOs définissent la structure des réponses renvoyées par les endpoints
// d'authentification (login et register). Ils contiennent le statut de
// l'opération, un message, et en cas de succès, le token JWT et les infos user.

namespace ProSocialApi.DTOs.Auth;

/// <summary>
/// DTO de réponse pour les opérations d'authentification.
/// Utilisé par les endpoints /api/auth/register et /api/auth/login.
///
/// Structure standardisée permettant au client de :
/// - Savoir si l'opération a réussi (Success)
/// - Afficher un message à l'utilisateur (Message)
/// - Récupérer le token JWT pour les requêtes authentifiées (Token)
/// - Obtenir les informations de l'utilisateur connecté (User)
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    /// Indique si l'opération a réussi.
    /// True = inscription/connexion réussie
    /// False = échec (email déjà utilisé, mauvais mot de passe, etc.)
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message descriptif du résultat de l'opération.
    /// Exemples :
    /// - "Inscription réussie"
    /// - "Connexion réussie"
    /// - "Email déjà utilisé"
    /// - "Email ou mot de passe incorrect"
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Token JWT (nullable - absent en cas d'échec).
    /// Ce token doit être envoyé dans le header Authorization des requêtes :
    /// Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
    ///
    /// Le token contient l'ID utilisateur (claim "sub") et a une durée de validité.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Informations de l'utilisateur authentifié (nullable - absent en cas d'échec).
    /// Permet au client d'afficher immédiatement le nom, avatar, etc.
    /// sans faire une requête supplémentaire.
    /// </summary>
    public UserInfoDto? User { get; set; }
}

/// <summary>
/// DTO contenant les informations essentielles d'un utilisateur.
/// Version allégée du profil utilisateur, utilisée dans les réponses d'auth.
///
/// Ne contient pas les informations sensibles (mot de passe) ni les
/// statistiques complètes (qui nécessitent des requêtes supplémentaires).
/// </summary>
public class UserInfoDto
{
    /// <summary>
    /// Identifiant unique de l'utilisateur (GUID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Adresse email de l'utilisateur.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Prénom de l'utilisateur.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Nom de famille de l'utilisateur.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Nom complet (prénom + nom).
    /// Propriété calculée pour faciliter l'affichage côté client.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Titre professionnel (optionnel).
    /// </summary>
    public string? Headline { get; set; }

    /// <summary>
    /// URL de l'avatar (optionnel).
    /// </summary>
    public string? AvatarUrl { get; set; }
}
