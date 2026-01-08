// CONNECTIONDTO.CS - DTOs pour les connexions entre utilisateurs
// Ces DTOs définissent les structures de données pour les réponses liées
// aux connexions (relations entre utilisateurs). Ils incluent les infos
// de l'autre utilisateur pour un affichage direct côté client.

using ProSocialApi.Data.Entities;

namespace ProSocialApi.DTOs.Connections;

/// <summary>
/// DTO représentant une connexion entre l'utilisateur courant et un autre utilisateur.
/// Utilisé pour lister les connexions de l'utilisateur connecté.
///
/// Point de vue : Du point de vue de l'utilisateur connecté
/// - La propriété User contient les infos de l'AUTRE personne de la connexion
/// - Que l'utilisateur ait envoyé ou reçu la demande, User = l'autre
/// </summary>
public class ConnectionDto
{
    /// <summary>
    /// Identifiant unique de la connexion.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Statut de la connexion (Pending, Accepted, Rejected).
    /// Utilise directement l'enum de l'entité pour cohérence.
    /// </summary>
    public ConnectionStatus Status { get; set; }

    /// <summary>
    /// Date de création de la demande de connexion.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Informations sur l'autre utilisateur de la connexion.
    /// Contient les données nécessaires pour afficher la connexion
    /// dans une liste (nom, avatar, titre professionnel).
    /// </summary>
    public ConnectionUserDto User { get; set; } = null!;
}

/// <summary>
/// DTO allégé représentant un utilisateur dans le contexte d'une connexion.
/// Contient uniquement les informations nécessaires à l'affichage dans une liste.
///
/// Ce DTO est réutilisé par ConnectionDto et ConnectionRequestDto
/// pour éviter la duplication de code.
/// </summary>
public class ConnectionUserDto
{
    /// <summary>
    /// Identifiant unique de l'utilisateur.
    /// </summary>
    public Guid Id { get; set; }

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
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Titre professionnel.
    /// Exemple : "Développeur Full Stack | React & .NET"
    /// </summary>
    public string? Headline { get; set; }

    /// <summary>
    /// URL de l'avatar / photo de profil.
    /// </summary>
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// DTO représentant une demande de connexion en attente (REÇUE).
/// Utilisé par l'endpoint GET /api/connections/requests pour lister
/// les demandes que l'utilisateur connecté a reçues.
///
/// Différence avec ConnectionDto :
/// - Requester contient les infos de celui qui a ENVOYÉ la demande
/// - Pas de Status car ces demandes sont toujours "Pending"
/// </summary>
public class ConnectionRequestDto
{
    /// <summary>
    /// Identifiant unique de la demande de connexion.
    /// Utilisé pour accepter ou refuser la demande.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Date d'envoi de la demande.
    /// Permet de trier les demandes par ancienneté.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Informations sur l'utilisateur qui a envoyé la demande.
    /// L'utilisateur connecté peut voir qui veut se connecter avec lui
    /// et décider d'accepter ou refuser.
    /// </summary>
    public ConnectionUserDto Requester { get; set; } = null!;
}
