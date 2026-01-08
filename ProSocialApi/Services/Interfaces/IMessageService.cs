// IMESSAGESERVICE.CS - Interface du service de messagerie
// Définit le contrat pour les opérations de messagerie privée :
// - Création de conversations
// - Envoi de messages
// - Récupération des conversations et messages
//
// Note : Ce service utilise MongoDB pour le stockage,
// contrairement aux autres services qui utilisent MySQL.

using ProSocialApi.DTOs.Messages;

namespace ProSocialApi.Services.Interfaces;

/// <summary>
/// Interface pour le service de messagerie privée.
/// Gère les conversations entre utilisateurs (messages privés).
///
/// Implémentation : MessageService
/// Enregistrement DI : AddScoped&lt;IMessageService, MessageService&gt;()
///
/// Architecture :
/// - Les conversations et messages sont stockés dans MongoDB
/// - Les infos utilisateurs (noms, avatars) sont récupérées depuis MySQL
/// - Les IDs utilisateurs sont stockés comme strings (GUIDs) dans MongoDB
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Crée une nouvelle conversation avec un autre utilisateur.
    /// Une conversation est toujours initiée avec un premier message.
    ///
    /// Validations :
    /// - Le participant doit exister
    /// - Une conversation entre les deux utilisateurs ne doit pas déjà exister
    /// </summary>
    /// <param name="userId">ID de l'utilisateur qui crée la conversation</param>
    /// <param name="createDto">ID du participant + message initial</param>
    /// <returns>
    /// ConversationDto de la conversation créée,
    /// ou null si le participant n'existe pas ou conversation existante
    /// </returns>
    Task<ConversationDto?> CreateConversationAsync(Guid userId, CreateConversationDto createDto);

    /// <summary>
    /// Récupère la liste des conversations d'un utilisateur.
    /// Triées par date du dernier message (plus récentes en premier).
    /// Inclut un aperçu du dernier message.
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <returns>Liste des conversations avec aperçu du dernier message</returns>
    Task<List<ConversationDto>> GetConversationsAsync(Guid userId);

    /// <summary>
    /// Récupère une conversation complète avec tous ses messages.
    /// L'utilisateur doit être participant de la conversation.
    /// </summary>
    /// <param name="conversationId">ID MongoDB de la conversation (ObjectId)</param>
    /// <param name="userId">ID de l'utilisateur (doit être participant)</param>
    /// <returns>
    /// ConversationDetailDto avec tous les messages,
    /// ou null si conversation inexistante ou utilisateur non participant
    /// </returns>
    Task<ConversationDetailDto?> GetConversationAsync(string conversationId, Guid userId);

    /// <summary>
    /// Envoie un message dans une conversation existante.
    /// L'utilisateur doit être participant de la conversation.
    /// </summary>
    /// <param name="conversationId">ID MongoDB de la conversation</param>
    /// <param name="senderId">ID de l'utilisateur qui envoie (doit être participant)</param>
    /// <param name="sendDto">Contenu du message</param>
    /// <returns>
    /// MessageDto du message envoyé,
    /// ou null si conversation inexistante ou utilisateur non participant
    /// </returns>
    Task<MessageDto?> SendMessageAsync(string conversationId, Guid senderId, SendMessageDto sendDto);
}
