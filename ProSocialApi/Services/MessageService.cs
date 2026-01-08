// MESSAGESERVICE.CS - Service de messagerie privée
// Implémente IMessageService : gère les conversations et messages privés.
// Utilise MongoDB pour le stockage (conversations avec messages imbriqués).
// Utilise MySQL (via EF Core) pour les infos utilisateurs.

using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using ProSocialApi.Data.Context;
using ProSocialApi.Data.MongoModels;
using ProSocialApi.DTOs.Messages;
using ProSocialApi.Services.Interfaces;

namespace ProSocialApi.Services;

/// <summary>
/// Service de messagerie privée entre utilisateurs.
/// Architecture hybride : MongoDB pour les conversations, MySQL pour les users.
/// </summary>
public class MessageService : IMessageService
{
    private readonly MongoDbContext _mongoContext;    // Pour les conversations (MongoDB)
    private readonly ApplicationDbContext _sqlContext; // Pour les infos utilisateurs (MySQL)

    public MessageService(MongoDbContext mongoContext, ApplicationDbContext sqlContext)
    {
        _mongoContext = mongoContext;
        _sqlContext = sqlContext;
    }

    /// <summary>
    /// Crée une nouvelle conversation ou retourne l'existante si elle existe déjà.
    /// Une conversation est créée avec un message initial obligatoire.
    /// </summary>
    public async Task<ConversationDto?> CreateConversationAsync(Guid userId, CreateConversationDto createDto)
    {
        // Vérifier que le participant cible existe dans MySQL
        var participant = await _sqlContext.Users.FindAsync(createDto.ParticipantId);
        if (participant == null)
            return null;

        // Convertir les GUIDs en strings pour MongoDB
        var userIdStr = userId.ToString();
        var participantIdStr = createDto.ParticipantId.ToString();

        // Vérifier si une conversation existe déjà entre ces deux utilisateurs
        var existingConversation = await _mongoContext.Conversations
            .Find(c => c.Participants.Contains(userIdStr) && c.Participants.Contains(participantIdStr))
            .FirstOrDefaultAsync();

        if (existingConversation != null)
        {
            // Retourner la conversation existante plutôt que d'en créer une nouvelle
            return await MapToConversationDto(existingConversation, userId);
        }

        // Créer la nouvelle conversation avec le message initial
        var conversation = new Conversation
        {
            Participants = new List<string> { userIdStr, participantIdStr },
            Messages = new List<Message>
            {
                new Message
                {
                    SenderId = userIdStr,
                    Content = createDto.InitialMessage,
                    SentAt = DateTime.UtcNow
                }
            },
            LastMessageAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Insérer dans MongoDB
        await _mongoContext.Conversations.InsertOneAsync(conversation);

        return await MapToConversationDto(conversation, userId);
    }

    /// <summary>
    /// Récupère toutes les conversations d'un utilisateur.
    /// Triées par date du dernier message (plus récentes en premier).
    /// </summary>
    public async Task<List<ConversationDto>> GetConversationsAsync(Guid userId)
    {
        var userIdStr = userId.ToString();

        // Requête MongoDB : trouver les conversations où l'utilisateur est participant
        var conversations = await _mongoContext.Conversations
            .Find(c => c.Participants.Contains(userIdStr))
            .SortByDescending(c => c.LastMessageAt) // Plus récentes en premier
            .ToListAsync();

        // Mapper chaque conversation en DTO (nécessite des requêtes MySQL pour les noms)
        var result = new List<ConversationDto>();
        foreach (var conv in conversations)
        {
            var dto = await MapToConversationDto(conv, userId);
            if (dto != null)
                result.Add(dto);
        }

        return result;
    }

    /// <summary>
    /// Récupère une conversation complète avec tous ses messages.
    /// Vérifie que l'utilisateur est bien participant de la conversation.
    /// </summary>
    public async Task<ConversationDetailDto?> GetConversationAsync(string conversationId, Guid userId)
    {
        var userIdStr = userId.ToString();

        // Requête MongoDB : conversation par ID où l'utilisateur est participant
        var conversation = await _mongoContext.Conversations
            .Find(c => c.Id == conversationId && c.Participants.Contains(userIdStr))
            .FirstOrDefaultAsync();

        if (conversation == null)
            return null;

        // Récupérer les infos des participants depuis MySQL
        var participantIds = conversation.Participants
            .Select(p => Guid.TryParse(p, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        var participants = await _sqlContext.Users
            .Where(u => participantIds.Contains(u.Id))
            .ToListAsync();

        // Construire le DTO détaillé avec tous les messages
        return new ConversationDetailDto
        {
            Id = conversation.Id,
            CreatedAt = conversation.CreatedAt,
            Participants = participants.Select(p => new ParticipantDto
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                AvatarUrl = p.AvatarUrl
            }).ToList(),
            Messages = conversation.Messages.Select(m =>
            {
                // Trouver le nom de l'expéditeur
                var sender = participants.FirstOrDefault(p => p.Id.ToString() == m.SenderId);
                return new MessageDto
                {
                    SenderId = m.SenderId,
                    SenderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : "Inconnu",
                    Content = m.Content,
                    SentAt = m.SentAt,
                    ReadAt = m.ReadAt
                };
            }).ToList()
        };
    }

    /// <summary>
    /// Envoie un message dans une conversation existante.
    /// Vérifie que l'utilisateur est participant de la conversation.
    /// Utilise l'opération MongoDB $push pour ajouter le message atomiquement.
    /// </summary>
    public async Task<MessageDto?> SendMessageAsync(string conversationId, Guid senderId, SendMessageDto sendDto)
    {
        var senderIdStr = senderId.ToString();

        // Vérifier que la conversation existe et que l'utilisateur en fait partie
        var conversation = await _mongoContext.Conversations
            .Find(c => c.Id == conversationId && c.Participants.Contains(senderIdStr))
            .FirstOrDefaultAsync();

        if (conversation == null)
            return null;

        // Créer le nouveau message
        var message = new Message
        {
            SenderId = senderIdStr,
            Content = sendDto.Content,
            SentAt = DateTime.UtcNow
        };

        // Mise à jour atomique : ajouter le message et mettre à jour les timestamps
        // $push ajoute un élément à la fin du tableau Messages
        var update = Builders<Conversation>.Update
            .Push(c => c.Messages, message)
            .Set(c => c.LastMessageAt, DateTime.UtcNow)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        await _mongoContext.Conversations.UpdateOneAsync(
            c => c.Id == conversationId,
            update);

        // Récupérer le nom de l'expéditeur depuis MySQL
        var sender = await _sqlContext.Users.FindAsync(senderId);

        return new MessageDto
        {
            SenderId = senderIdStr,
            SenderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : "Inconnu",
            Content = sendDto.Content,
            SentAt = message.SentAt
        };
    }

    /// <summary>
    /// Convertit une Conversation MongoDB en ConversationDto.
    /// Nécessite des requêtes MySQL pour récupérer les noms des participants.
    /// </summary>
    private async Task<ConversationDto?> MapToConversationDto(Conversation conversation, Guid currentUserId)
    {
        // Parser les IDs des participants
        var participantIds = conversation.Participants
            .Select(p => Guid.TryParse(p, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        // Récupérer les infos des participants depuis MySQL
        var participants = await _sqlContext.Users
            .Where(u => participantIds.Contains(u.Id))
            .ToListAsync();

        // Préparer le dernier message si présent
        var lastMessage = conversation.Messages.LastOrDefault();
        MessageDto? lastMessageDto = null;

        if (lastMessage != null)
        {
            var sender = participants.FirstOrDefault(p => p.Id.ToString() == lastMessage.SenderId);
            lastMessageDto = new MessageDto
            {
                SenderId = lastMessage.SenderId,
                SenderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : "Inconnu",
                Content = lastMessage.Content,
                SentAt = lastMessage.SentAt,
                ReadAt = lastMessage.ReadAt
            };
        }

        return new ConversationDto
        {
            Id = conversation.Id,
            CreatedAt = conversation.CreatedAt,
            LastMessageAt = conversation.LastMessageAt,
            LastMessage = lastMessageDto,
            Participants = participants.Select(p => new ParticipantDto
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                AvatarUrl = p.AvatarUrl
            }).ToList()
        };
    }
}
