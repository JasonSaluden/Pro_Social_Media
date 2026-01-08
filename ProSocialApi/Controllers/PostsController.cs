using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProSocialApi.DTOs.Posts;
using ProSocialApi.Services.Interfaces;

namespace ProSocialApi.Controllers;

// POSTSCONTROLLER.CS - Contrôleur de gestion des publications
// Gère les endpoints relatifs aux posts : CRUD complet + système de likes.

/// <summary>
/// Contrôleur REST pour la gestion des publications (posts).
///
/// Fonctionnalités principales :
/// - CRUD complet sur les posts
/// - Système de like/unlike
/// - Consultation des posts par utilisateur
///
/// Endpoints disponibles :
/// - POST /api/posts : Créer un post (authentifié)
/// - GET /api/posts/{id} : Consulter un post (public)
/// - PUT /api/posts/{id} : Modifier un post (authentifié, auteur uniquement)
/// - DELETE /api/posts/{id} : Supprimer un post (authentifié, auteur uniquement)
/// - GET /api/posts/user/{userId} : Posts d'un utilisateur (public)
/// - POST /api/posts/{id}/like : Liker un post (authentifié)
/// - DELETE /api/posts/{id}/like : Retirer son like (authentifié)
/// </summary>
[ApiController]
[Route("api/[controller]")]              // Route de base : /api/posts
[Produces("application/json")]
public class PostsController : ControllerBase
{
    // Service de posts injecté via DI
    private readonly IPostService _postService;

    /// <summary>
    /// Constructeur avec injection de dépendances.
    /// </summary>
    /// <param name="postService">Service gérant la logique des publications</param>
    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    // CRÉATION DE POST - POST /api/posts
    /// <summary>
    /// Crée un nouveau post pour l'utilisateur connecté.
    ///
    /// Le post peut contenir :
    /// - Content : Texte du post (obligatoire)
    /// - ImageUrl : URL d'une image associée (optionnel)
    ///
    /// Le post est automatiquement lié à l'utilisateur connecté (authorId)
    /// et horodaté avec CreatedAt.
    /// </summary>
    /// <param name="createDto">Contenu et image optionnelle du post</param>
    /// <returns>
    /// 201 Created : Post créé avec son ID et toutes les métadonnées
    /// 401 Unauthorized : Token manquant ou invalide
    /// </returns>
    /// <remarks>
    /// Exemple de requête :
    /// POST /api/posts
    /// {
    ///     "content": "Hello world! Mon premier post.",
    ///     "imageUrl": "https://example.com/image.jpg" (optionnel)
    /// }
    /// </remarks>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PostDto>> Create([FromBody] CreatePostDto createDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Créer le post via le service
        var post = await _postService.CreateAsync(userId.Value, createDto);

        // Retourner 201 Created avec l'URL du nouveau post dans le header Location
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
    }

    // CONSULTATION D'UN POST - GET /api/posts/{id}
    /// <summary>
    /// Récupère un post par son ID avec toutes ses métadonnées.
    ///
    /// Endpoint public permettant à n'importe qui de consulter un post.
    /// Si l'utilisateur est connecté, le champ IsLikedByCurrentUser sera renseigné.
    ///
    /// Le DTO retourné inclut :
    /// - Informations de l'auteur
    /// - Compteur de likes et commentaires
    /// - État du like pour l'utilisateur courant (si authentifié)
    /// </summary>
    /// <param name="id">ID (GUID) du post à consulter</param>
    /// <returns>
    /// 200 OK : Post avec toutes ses métadonnées
    /// 404 Not Found : Post inexistant
    /// </returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> GetById(Guid id)
    {
        // Récupérer l'ID utilisateur si connecté (pour IsLikedByCurrentUser)
        var userId = GetCurrentUserId();
        var post = await _postService.GetByIdAsync(id, userId);

        if (post == null)
            return NotFound(new { message = "Post non trouvé" });

        return Ok(post);
    }

    // MODIFICATION DE POST - PUT /api/posts/{id}
    /// <summary>
    /// Modifie un post existant (pattern Partial Update).
    ///
    /// Seul l'auteur du post peut le modifier.
    /// Seuls les champs fournis (non null) sont mis à jour.
    /// Le champ UpdatedAt est automatiquement mis à jour.
    ///
    /// Champs modifiables :
    /// - Content : Nouveau texte du post
    /// - ImageUrl : Nouvelle URL d'image (ou null pour retirer l'image)
    /// </summary>
    /// <param name="id">ID du post à modifier</param>
    /// <param name="updateDto">Champs à mettre à jour</param>
    /// <returns>
    /// 200 OK : Post mis à jour
    /// 401 Unauthorized : Token manquant ou invalide
    /// 404 Not Found : Post inexistant ou l'utilisateur n'est pas l'auteur
    /// </returns>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> Update(Guid id, [FromBody] UpdatePostDto updateDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Le service vérifie que l'utilisateur est l'auteur
        var post = await _postService.UpdateAsync(id, userId.Value, updateDto);

        if (post == null)
            return NotFound(new { message = "Post non trouvé ou vous n'êtes pas l'auteur" });

        return Ok(post);
    }

    // SUPPRESSION DE POST - DELETE /api/posts/{id}
    /// <summary>
    /// Supprime un post et toutes ses données associées.
    ///
    /// Seul l'auteur du post peut le supprimer.
    /// La suppression cascade vers :
    /// - Les commentaires du post
    /// - Les likes du post
    ///
    /// Action irréversible.
    /// </summary>
    /// <param name="id">ID du post à supprimer</param>
    /// <returns>
    /// 204 No Content : Post supprimé avec succès
    /// 401 Unauthorized : Token manquant ou invalide
    /// 404 Not Found : Post inexistant ou l'utilisateur n'est pas l'auteur
    /// </returns>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _postService.DeleteAsync(id, userId.Value);

        if (!result)
            return NotFound(new { message = "Post non trouvé ou vous n'êtes pas l'auteur" });

        // 204 No Content : suppression réussie
        return NoContent();
    }

    // POSTS D'UN UTILISATEUR - GET /api/posts/user/{userId}
    /// <summary>
    /// Récupère tous les posts d'un utilisateur spécifique.
    ///
    /// Endpoint public pour afficher le profil/posts d'un utilisateur.
    /// Les posts sont triés du plus récent au plus ancien.
    ///
    /// Si l'utilisateur courant est connecté, le champ IsLikedByCurrentUser
    /// sera renseigné pour chaque post.
    /// </summary>
    /// <param name="userId">ID de l'utilisateur dont on veut voir les posts</param>
    /// <returns>
    /// 200 OK : Liste des posts (peut être vide si l'utilisateur n'a pas de posts)
    /// </returns>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(List<PostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PostDto>>> GetUserPosts(Guid userId)
    {
        // Récupérer l'ID de l'utilisateur courant si connecté
        var currentUserId = GetCurrentUserId();
        var posts = await _postService.GetUserPostsAsync(userId, currentUserId);
        return Ok(posts);
    }

    // LIKE D'UN POST - POST /api/posts/{id}/like
    /// <summary>
    /// Ajoute un like de l'utilisateur courant sur un post.
    ///
    /// Contraintes :
    /// - Le post doit exister
    /// - L'utilisateur ne doit pas avoir déjà liké ce post
    ///
    /// Après un like réussi, le compteur LikesCount du post augmente
    /// et IsLikedByCurrentUser devient true pour cet utilisateur.
    /// </summary>
    /// <param name="id">ID du post à liker</param>
    /// <returns>
    /// 200 OK : Like ajouté
    /// 400 Bad Request : Post inexistant ou déjà liké
    /// 401 Unauthorized : Token manquant ou invalide
    /// </returns>
    [HttpPost("{id:guid}/like")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Like(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var (success, message) = await _postService.LikeAsync(id, userId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    // UNLIKE D'UN POST - DELETE /api/posts/{id}/like
    /// <summary>
    /// Retire le like de l'utilisateur courant d'un post.
    ///
    /// L'utilisateur doit avoir préalablement liké le post.
    /// Après un unlike réussi, le compteur LikesCount diminue
    /// et IsLikedByCurrentUser devient false.
    /// </summary>
    /// <param name="id">ID du post dont retirer le like</param>
    /// <returns>
    /// 200 OK : Like retiré
    /// 400 Bad Request : L'utilisateur n'avait pas liké ce post
    /// 401 Unauthorized : Token manquant ou invalide
    /// </returns>
    [HttpDelete("{id:guid}/like")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Unlike(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var (success, message) = await _postService.UnlikeAsync(id, userId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    // MÉTHODE UTILITAIRE : EXTRACTION DE L'ID UTILISATEUR
    /// <summary>
    /// Extrait l'ID de l'utilisateur connecté depuis les claims du token JWT.
    /// Retourne null si pas connecté ou token invalide.
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
