// CONVERSATION.CS - Modèles MongoDB pour la messagerie
// Ce fichier contient les modèles pour le système de messagerie :
// - Message : Un message individuel dans une conversation
// - Conversation : Une conversation entre deux utilisateurs avec ses messages
//
// Pourquoi MongoDB pour la messagerie ?
// - Les messages sont naturellement imbriqués dans une conversation (document pattern)
// - Récupérer une conversation = 1 requête (pas de JOIN)
// - Facile d'ajouter des messages à une conversation existante ($push)
// - Structure flexible pour ajouter des fonctionnalités (réactions, pièces jointes...)

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProSocialApi.Data.MongoModels;

/// <summary>
/// Représente un message individuel dans une conversation.
/// Stocké comme sous-document dans la collection Conversation.
/// Pas d'ID propre car identifié par sa position dans la conversation.
/// </summary>
public class Message
{
    // PROPRIÉTÉS DU MESSAGE
    /// <summary>
    /// ID de l'utilisateur qui a envoyé le message.
    /// Stocké comme string car les IDs MySQL sont des GUIDs.
    /// Permet de faire le lien avec la table Users de MySQL.
    /// </summary>
    [BsonElement("senderId")] // Nom du champ dans le document BSON
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Contenu textuel du message.
    /// </summary>
    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Date et heure d'envoi du message.
    /// Stockée en UTC pour éviter les problèmes de fuseaux horaires.
    /// </summary>
    [BsonElement("sentAt")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date et heure de lecture du message (nullable).
    /// Null si le destinataire n'a pas encore lu le message.
    /// Permet d'implémenter les "accusés de lecture".
    /// </summary>
    [BsonElement("readAt")]
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// Représente une conversation entre deux utilisateurs.
/// Contient la liste des messages échangés (document pattern).
///
/// Avantages du document pattern pour la messagerie :
/// - Récupération d'une conversation complète en 1 requête
/// - Ajout de message = $push sur le tableau (atomique et performant)
/// - Pas de JOIN nécessaire
///
/// Limitations (à considérer pour de très longues conversations) :
/// - Document MongoDB limité à 16 MB
/// - Solution : Archiver les vieux messages ou paginer
/// </summary>
public class Conversation
{
    // IDENTIFICATION
    /// <summary>
    /// Identifiant unique de la conversation (ObjectId MongoDB).
    /// Format : 24 caractères hexadécimaux (ex: "507f1f77bcf86cd799439011")
    /// Généré automatiquement par MongoDB lors de l'insertion.
    /// </summary>
    [BsonId]                                    // Marque comme clé primaire MongoDB
    [BsonRepresentation(BsonType.ObjectId)]     // Sérialise comme ObjectId (pas string)
    public string Id { get; set; } = string.Empty;

    // PARTICIPANTS
    /// <summary>
    /// Liste des IDs des participants à la conversation.
    /// Contient exactement 2 IDs (conversations 1-to-1 uniquement).
    /// Les IDs correspondent aux GUIDs des utilisateurs MySQL.
    ///
    /// Note : Pour des conversations de groupe, ce serait une liste de N participants.
    /// </summary>
    [BsonElement("participants")]
    public List<string> Participants { get; set; } = new();

    // MESSAGES
    /// <summary>
    /// Liste des messages de la conversation (du plus ancien au plus récent).
    /// Stockés directement dans le document (embedded documents).
    /// </summary>
    [BsonElement("messages")]
    public List<Message> Messages { get; set; } = new();

    // MÉTADONNÉES
    /// <summary>
    /// Date et heure du dernier message (nullable si aucun message).
    /// Utilisé pour trier les conversations par activité récente.
    /// Mis à jour à chaque nouveau message.
    /// </summary>
    [BsonElement("lastMessageAt")]
    public DateTime? LastMessageAt { get; set; }

    /// <summary>
    /// Date et heure de création de la conversation.
    /// Correspond au moment où le premier message a été envoyé.
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date et heure de la dernière modification.
    /// Mis à jour à chaque nouveau message ou modification.
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
