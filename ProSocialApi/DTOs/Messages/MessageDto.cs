// MESSAGEDTO.CS - DTOs pour la messagerie (conversations et messages)
// Ces DTOs définissent les structures de données pour le système de
// messagerie privée. Ils sont conçus pour fonctionner avec MongoDB
// où les messages sont imbriqués dans les conversations.

using System.ComponentModel.DataAnnotations;

namespace ProSocialApi.DTOs.Messages;

/// <summary>
/// DTO représentant une conversation dans la liste des conversations.
/// Version allégée montrant uniquement le dernier message.
/// Utilisé par GET /api/conversations.
/// </summary>
public class ConversationDto
{
    /// <summary>
    /// Identifiant unique de la conversation (ObjectId MongoDB).
    /// Format : 24 caractères hexadécimaux.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Liste des participants à la conversation.
    /// Dans une conversation 1-to-1, contient 2 participants.
    /// </summary>
    public List<ParticipantDto> Participants { get; set; } = new();

    /// <summary>
    /// Dernier message de la conversation (pour l'aperçu).
    /// Null si la conversation n'a pas encore de messages.
    /// </summary>
    public MessageDto? LastMessage { get; set; }

    /// <summary>
    /// Date du dernier message.
    /// Utilisé pour trier les conversations (plus récentes en premier).
    /// </summary>
    public DateTime? LastMessageAt { get; set; }

    /// <summary>
    /// Date de création de la conversation.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO représentant une conversation avec tous ses messages.
/// Utilisé par GET /api/conversations/{id} pour afficher l'historique complet.
/// </summary>
public class ConversationDetailDto
{
    /// <summary>
    /// Identifiant unique de la conversation.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Liste des participants à la conversation.
    /// </summary>
    public List<ParticipantDto> Participants { get; set; } = new();

    /// <summary>
    /// Liste complète des messages (du plus ancien au plus récent).
    /// Attention : Pour de longues conversations, envisager la pagination.
    /// </summary>
    public List<MessageDto> Messages { get; set; } = new();

    /// <summary>
    /// Date de création de la conversation.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO représentant un participant à une conversation.
/// Informations récupérées depuis MySQL et jointes aux données MongoDB.
/// </summary>
public class ParticipantDto
{
    /// <summary>
    /// Identifiant unique de l'utilisateur (GUID MySQL).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Prénom du participant.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Nom de famille du participant.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Nom complet (propriété calculée).
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// URL de l'avatar du participant.
    /// Affiché dans l'en-tête de la conversation.
    /// </summary>
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// DTO représentant un message individuel.
/// Utilisé dans les listes de messages et comme LastMessage.
/// </summary>
public class MessageDto
{
    /// <summary>
    /// ID de l'expéditeur (GUID MySQL en string).
    /// Permet de déterminer si le message est "envoyé" ou "reçu".
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Nom complet de l'expéditeur.
    /// Dénormalisé pour un affichage rapide sans requête supplémentaire.
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// Contenu textuel du message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Date et heure d'envoi du message.
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Date et heure de lecture du message (nullable).
    /// Null si le destinataire n'a pas encore lu le message.
    /// Permet d'afficher les accusés de lecture ("Vu à 14:32").
    /// </summary>
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// DTO pour créer une nouvelle conversation avec un message initial.
/// Utilisé par POST /api/conversations.
///
/// Note : On ne crée pas de conversation vide. Une conversation
/// est toujours initiée avec un premier message.
/// </summary>
public class CreateConversationDto
{
    /// <summary>
    /// ID de l'utilisateur avec qui démarrer la conversation.
    /// Doit être un utilisateur existant et différent de l'utilisateur connecté.
    /// </summary>
    [Required(ErrorMessage = "L'ID du participant est requis")]
    public Guid ParticipantId { get; set; }

    /// <summary>
    /// Premier message de la conversation.
    /// Obligatoire car on ne crée pas de conversation sans message.
    /// </summary>
    [Required(ErrorMessage = "Le message initial est requis")]
    public string InitialMessage { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour envoyer un message dans une conversation existante.
/// Utilisé par POST /api/conversations/{id}/messages.
/// </summary>
public class SendMessageDto
{
    /// <summary>
    /// Contenu textuel du message à envoyer.
    /// </summary>
    [Required(ErrorMessage = "Le contenu est requis")]
    public string Content { get; set; } = string.Empty;
}
