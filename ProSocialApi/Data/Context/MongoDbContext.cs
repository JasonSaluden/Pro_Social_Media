// MONGODBCONTEXT.CS - Contexte MongoDB pour les données non-relationnelles
// Cette classe gère la connexion à MongoDB et l'accès aux collections.

using MongoDB.Driver;
using ProSocialApi.Data.MongoModels;

namespace ProSocialApi.Data.Context;

/// <summary>
/// Contexte MongoDB pour les données non-relationnelles.
/// Gère les conversations (messagerie) et les notifications.
///
/// Pourquoi MongoDB pour ces données ?
/// - Messagerie : Les messages sont naturellement imbriqués dans une conversation
///   (document pattern), évitant les JOINs coûteux pour récupérer une conversation
/// - Notifications : Structure variable selon le type (like, comment, message...)
///   MongoDB gère mieux les schémas flexibles
/// </summary>
public class MongoDbContext
{
    // CHAMPS PRIVÉS
    /// <summary>
    /// Instance de la base de données MongoDB.
    /// Initialisée une seule fois dans le constructeur.
    /// </summary>
    private readonly IMongoDatabase _database;
    // CONSTRUCTEUR
    /// <summary>
    /// Initialise la connexion à MongoDB et crée les index nécessaires.
    ///
    /// La configuration est injectée depuis appsettings.json :
    /// - ConnectionStrings:MongoDB pour la chaîne de connexion
    /// - MongoDB:DatabaseName pour le nom de la base (défaut: pro_social_db)
    /// </summary>
    /// <param name="configuration">Configuration de l'application (injection DI)</param>
    public MongoDbContext(IConfiguration configuration)
    {
        // Récupère la chaîne de connexion depuis la configuration
        var connectionString = configuration.GetConnectionString("MongoDB");

        // Récupère le nom de la base de données (avec valeur par défaut)
        var databaseName = configuration["MongoDB:DatabaseName"] ?? "pro_social_db";

        // Crée le client MongoDB
        // Le MongoClient gère automatiquement un pool de connexions
        var client = new MongoClient(connectionString);

        // Obtient la référence à la base de données
        // Si elle n'existe pas, elle sera créée automatiquement au premier insert
        _database = client.GetDatabase(databaseName);

        // Crée les index pour optimiser les requêtes fréquentes
        CreateIndexes();
    }

    // COLLECTIONS 
    /// <summary>
    /// Collection des conversations (messagerie).
    /// Chaque document contient une conversation avec ses messages imbriqués.
    /// Structure optimisée pour récupérer une conversation entière en une requête.
    /// </summary>
    public IMongoCollection<Conversation> Conversations =>
        _database.GetCollection<Conversation>("conversations");

    /// <summary>
    /// Collection des notifications.
    /// Chaque document représente une notification pour un utilisateur.
    /// Stocke différents types : demandes de connexion, likes, commentaires, messages.
    /// </summary>
    public IMongoCollection<Notification> Notifications =>
        _database.GetCollection<Notification>("notifications");

    // CRÉATION DES INDEX
    /// <summary>
    /// Crée les index MongoDB pour optimiser les requêtes fréquentes.
    /// Appelé une seule fois au démarrage de l'application.
    ///
    /// Si les index existent déjà, MongoDB les ignore (opération idempotente).
    /// </summary>
    private void CreateIndexes()
    {
        // INDEX POUR LES CONVERSATIONS
        var conversationIndexes = Builders<Conversation>.IndexKeys;

        Conversations.Indexes.CreateMany(new[]
        {
            // Index sur Participants
            new CreateIndexModel<Conversation>(
                conversationIndexes.Ascending(c => c.Participants)),

            // Index sur LastMessageAt 
            new CreateIndexModel<Conversation>(
                conversationIndexes.Descending(c => c.LastMessageAt))
        });
        // INDEX POUR LES NOTIFICATIONS
        var notificationIndexes = Builders<Notification>.IndexKeys;

        Notifications.Indexes.CreateMany(new[]
        {
            // Index composite (UserId + CreatedAt DESC) : Optimise la requête
            // "récupérer les notifications d'un utilisateur, triées par date".
            new CreateIndexModel<Notification>(
                notificationIndexes.Combine(
                    notificationIndexes.Ascending(n => n.UserId),
                    notificationIndexes.Descending(n => n.CreatedAt)
                )),

            // Index composite (UserId + Read) : Optimise la requête
            // "récupérer les notifications non lues d'un utilisateur".
            new CreateIndexModel<Notification>(
                notificationIndexes.Combine(
                    notificationIndexes.Ascending(n => n.UserId),
                    notificationIndexes.Ascending(n => n.Read)
                ))
        });
    }
}
