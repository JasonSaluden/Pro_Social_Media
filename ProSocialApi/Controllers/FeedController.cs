using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProSocialApi.DTOs.Posts;
using ProSocialApi.Services.Interfaces;

namespace ProSocialApi.Controllers;

// FEEDCONTROLLER.CS - Contrôleur du fil d'actualité

/// <summary>
/// Contrôleur REST pour le fil d'actualité personnalisé.
///
/// Le feed est le cœur de l'expérience utilisateur d'un réseau social.
/// Il agrège les publications pertinentes pour chaque utilisateur.
///
/// Algorithme actuel :
/// 1. Récupérer les IDs des connexions acceptées de l'utilisateur
/// 2. Ajouter l'ID de l'utilisateur lui-même (pour voir ses propres posts)
/// 3. Récupérer les posts de tous ces utilisateurs
/// 4. Trier par date décroissante et paginer
///
/// Note : Pour un réseau social à grande échelle, cet algorithme devrait
/// être optimisé (cache, pré-calcul, algorithmes de recommandation ML).
///
/// Endpoint disponible :
/// - GET /api/feed : Récupérer son fil d'actualité paginé
/// </summary>
[ApiController]
[Route("api/[controller]")]              // Route de base : /api/feed
[Authorize]                              // Requiert authentification (feed personnalisé)
[Produces("application/json")]
public class FeedController : ControllerBase
{
    // Service de feed injecté via DI
    private readonly IFeedService _feedService;

    /// <summary>
    /// Constructeur avec injection de dépendances.
    /// </summary>
    /// <param name="feedService">Service gérant la logique du fil d'actualité</param>
    public FeedController(IFeedService feedService)
    {
        _feedService = feedService;
    }

    // FIL D'ACTUALITÉ - GET /api/feed

    /// <summary>
    /// Récupère le fil d'actualité personnalisé de l'utilisateur connecté.
    ///
    /// Le feed contient :
    /// - Les posts des connexions acceptées de l'utilisateur
    /// - Les propres posts de l'utilisateur
    ///
    /// Les posts sont triés du plus récent au plus ancien avec pagination.
    ///
    /// Paramètres de pagination :
    /// - page : Numéro de page (défaut: 1, minimum: 1)
    /// - pageSize : Nombre de posts par page (défaut: 20, min: 1, max: 50)
    ///
    /// Les valeurs hors limites sont automatiquement corrigées :
    /// - pageSize > 50 → 50
    /// - pageSize &lt; 1 → 20
    /// - page &lt; 1 → 1
    /// </summary>
    /// <param name="page">Numéro de page (1-based)</param>
    /// <param name="pageSize">Nombre de posts par page</param>
    /// <returns>
    /// 200 OK : Liste des posts du feed (peut être vide si pas de connexions/posts)
    /// 401 Unauthorized : Token manquant ou invalide
    /// </returns>
    /// <remarks>
    /// Exemples d'utilisation :
    /// - GET /api/feed : Première page, 20 posts
    /// - GET /api/feed?page=2 : Deuxième page, 20 posts
    /// - GET /api/feed?page=1&amp;pageSize=10 : Première page, 10 posts
    ///
    /// Chaque post inclut :
    /// - Informations de l'auteur (nom, avatar, titre)
    /// - Contenu et image du post
    /// - Compteurs (likes, commentaires)
    /// - IsLikedByCurrentUser (si l'utilisateur a liké ce post)
    ///
    /// Pour le scroll infini côté client :
    /// 1. Charger page=1
    /// 2. Quand l'utilisateur scrolle en bas, charger page=2, etc.
    /// 3. Si la réponse est vide ou contient moins que pageSize, fin du feed
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<PostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PostDto>>> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();
        // VALIDATION ET NORMALISATION DES PARAMÈTRES DE PAGINATION
        // Ces garde-fous évitent les abus (requêtes trop grandes)
        // et les erreurs (valeurs négatives)

        if (pageSize > 50) pageSize = 50;    // Maximum 50 posts par page (performance)
        if (pageSize < 1) pageSize = 20;     // Minimum 1, défaut 20
        if (page < 1) page = 1;              // Page minimum : 1

        // Récupérer le feed paginé via le service
        var posts = await _feedService.GetFeedAsync(userId.Value, page, pageSize);
        return Ok(posts);
    }

    // MÉTHODE UTILITAIRE : EXTRACTION DE L'ID UTILISATEUR
    /// <summary>
    /// Extrait l'ID de l'utilisateur connecté depuis les claims du token JWT.
    /// </summary>
    /// <returns>GUID de l'utilisateur si trouvé et valide, null sinon</returns>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }
}
