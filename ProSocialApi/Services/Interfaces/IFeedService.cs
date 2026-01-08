// IFEEDSERVICE.CS - Interface du service de fil d'actualité
// Définit le contrat pour la génération du fil d'actualité personnalisé.
// Le feed affiche les posts des connexions de l'utilisateur + ses propres posts.

using ProSocialApi.DTOs.Posts;

namespace ProSocialApi.Services.Interfaces;

/// <summary>
/// Interface pour le service de fil d'actualité (feed).
/// Génère un flux personnalisé de posts pour chaque utilisateur.
///
/// Implémentation : FeedService
/// Enregistrement DI : AddScoped&lt;IFeedService, FeedService&gt;()
///
/// Logique du feed :
/// - Affiche les posts des connexions ACCEPTÉES de l'utilisateur
/// - Affiche également les propres posts de l'utilisateur
/// - Triés du plus récent au plus ancien
/// - Paginé pour les performances
/// </summary>
public interface IFeedService
{
    /// <summary>
    /// Génère le fil d'actualité personnalisé d'un utilisateur.
    ///
    /// Contenu du feed :
    /// 1. Posts des utilisateurs connectés (connexions acceptées)
    /// 2. Posts de l'utilisateur lui-même
    ///
    /// Les posts sont triés par date de création (plus récents en premier)
    /// et incluent les statistiques (likes, comments) ainsi que
    /// IsLikedByCurrentUser pour chaque post.
    /// </summary>
    /// <param name="userId">ID de l'utilisateur pour qui générer le feed</param>
    /// <param name="page">Numéro de page (commence à 1)</param>
    /// <param name="pageSize">Nombre de posts par page (max 50, défaut 20)</param>
    /// <returns>Liste paginée des posts du feed</returns>
    Task<List<PostDto>> GetFeedAsync(Guid userId, int page = 1, int pageSize = 20);
}
