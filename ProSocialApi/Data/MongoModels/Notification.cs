// NOTIFICATION.CS - Modèles MongoDB pour le système de notifications
// Ce fichier contient les modèles pour les notifications utilisateur :
// - NotificationType : Énumération des types de notifications
// - NotificationData : Données contextuelles variables selon le type
// - Notification : Document principal de notification
//
// Pourquoi MongoDB pour les notifications ?
// - Structure variable selon le type (like vs message vs connexion)
// - Hautes performances en lecture (notifications fréquemment consultées)
// - Facile de marquer plusieurs notifications comme lues (updateMany)
// - Pas besoin de transactions complexes

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProSocialApi.Data.MongoModels;

/// <summary>
/// Types de notifications possibles dans l'application.
/// Chaque type a des données contextuelles différentes.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Notification de demande de connexion reçue.
    /// Données : fromUserId, fromUserName, connectionId
    /// </summary>
    ConnectionRequest,

    /// <summary>
    /// Notification de connexion acceptée.
    /// Données : fromUserId, fromUserName, connectionId
    /// </summary>
    ConnectionAccepted,

    /// <summary>
    /// Notification de like sur un de vos posts.
    /// Données : fromUserId, fromUserName, postId
    /// </summary>
    NewLike,

    /// <summary>
    /// Notification de commentaire sur un de vos posts.
    /// Données : fromUserId, fromUserName, postId, commentId
    /// </summary>
    NewComment,

    /// <summary>
    /// Notification de nouveau message privé reçu.
    /// Données : fromUserId, fromUserName, conversationId
    /// </summary>
    NewMessage
}

/// <summary>
/// Données contextuelles d'une notification.
/// Structure flexible : seuls les champs pertinents sont remplis selon le type.
///
/// Exemple pour un like :
/// - fromUserId: "guid-de-lutilisateur"
/// - fromUserName: "Jean Dupont"
/// - postId: "guid-du-post"
/// - Les autres champs sont null
///
/// Cette approche est typique de MongoDB : un seul modèle flexible
/// plutôt que plusieurs tables liées comme en SQL.
/// </summary>
public class NotificationData
{
    /// <summary>
    /// ID de l'utilisateur qui a déclenché la notification.
    /// Exemple : l'utilisateur qui a liké votre post.
    /// </summary>
    [BsonElement("fromUserId")]
    public string? FromUserId { get; set; }

    /// <summary>
    /// Nom de l'utilisateur qui a déclenché la notification.
    /// Dénormalisé (stocké en double) pour éviter une requête supplémentaire
    /// lors de l'affichage des notifications.
    ///
    /// Note : Si l'utilisateur change de nom, les vieilles notifications
    /// garderont l'ancien nom. C'est un compromis accepté pour les performances.
    /// </summary>
    [BsonElement("fromUserName")]
    public string? FromUserName { get; set; }

    /// <summary>
    /// ID du post concerné (pour les notifications de type Like ou Comment).
    /// Permet de rediriger l'utilisateur vers le post en cliquant sur la notification.
    /// </summary>
    [BsonElement("postId")]
    public string? PostId { get; set; }

    /// <summary>
    /// ID du commentaire concerné (pour les notifications de type Comment).
    /// Permet de mettre en surbrillance le commentaire spécifique.
    /// </summary>
    [BsonElement("commentId")]
    public string? CommentId { get; set; }

    /// <summary>
    /// ID de la conversation concernée (pour les notifications de type Message).
    /// Permet d'ouvrir directement la conversation.
    /// </summary>
    [BsonElement("conversationId")]
    public string? ConversationId { get; set; }

    /// <summary>
    /// ID de la connexion concernée (pour les notifications de type Connection).
    /// Permet d'accepter/refuser directement depuis la notification.
    /// </summary>
    [BsonElement("connectionId")]
    public string? ConnectionId { get; set; }
}

/// <summary>
/// Document principal représentant une notification utilisateur.
/// Stocké dans la collection "notifications" de MongoDB.
///
/// Flux typique :
/// 1. Action déclenchante (like, comment, message...)
/// 2. Création d'une notification pour le destinataire
/// 3. L'utilisateur consulte ses notifications (Read = false par défaut)
/// 4. L'utilisateur clique -> marque comme lu (Read = true)
/// </summary>
public class Notification
{
    // IDENTIFICATION
    /// <summary>
    /// Identifiant unique de la notification (ObjectId MongoDB).
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    // DESTINATAIRE
    /// <summary>
    /// ID de l'utilisateur qui REÇOIT la notification.
    /// Correspond au GUID de l'utilisateur dans MySQL.
    /// Indexé pour des requêtes rapides par utilisateur.
    /// </summary>
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    // TYPE ET DONNÉES
    /// <summary>
    /// Type de notification (ConnectionRequest, NewLike, etc.).
    /// Stocké comme string en base pour la lisibilité.
    /// Détermine comment interpréter les données et afficher la notification.
    /// </summary>
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)] // Stocke "NewLike" au lieu de 2
    public NotificationType Type { get; set; }

    /// <summary>
    /// Données contextuelles de la notification.
    /// Structure variable selon le type de notification.
    /// </summary>
    [BsonElement("data")]
    public NotificationData Data { get; set; } = new();

    // STATUT
    /// <summary>
    /// Indique si la notification a été lue par l'utilisateur.
    /// False par défaut (nouvelle notification non lue).
    /// Passe à true quand l'utilisateur consulte/clique sur la notification.
    /// </summary>
    [BsonElement("read")]
    public bool Read { get; set; } = false;

    // CONTENU AFFICHÉ
    /// <summary>
    /// Message lisible à afficher dans l'interface.
    /// Pré-formaté lors de la création pour un affichage rapide.
    ///
    /// Exemples :
    /// - "Jean Dupont a aimé votre publication"
    /// - "Marie Martin vous a envoyé une demande de connexion"
    /// - "Nouveau message de Pierre Durand"
    /// </summary>
    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    // MÉTADONNÉES
    /// <summary>
    /// Date et heure de création de la notification.
    /// Utilisé pour l'affichage ("il y a 5 minutes") et le tri.
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
