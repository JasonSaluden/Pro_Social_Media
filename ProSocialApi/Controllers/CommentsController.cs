using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProSocialApi.DTOs.Comments;
using ProSocialApi.Services.Interfaces;

namespace ProSocialApi.Controllers;

// COMMENTSCONTROLLER.CS - Contrôleur de gestion des commentaires

/// <summary>
/// Contrôleur REST pour la gestion des commentaires sur les publications
/// Architecture REST imbriquée
///
/// Endpoints disponibles :
/// - POST /api/posts/{postId}/comments : Ajouter un commentaire (authentifié)
/// - GET /api/posts/{postId}/comments : Lister les commentaires d'un post (public)
/// - DELETE /api/comments/{id} : Supprimer un commentaire (authentifié)
/// </summary>
[ApiController]
[Produces("application/json")]
public class CommentsController : ControllerBase
{
    // Service de commentaires injecté via DI
    private readonly ICommentService _commentService;

    /// <summary>
    /// Constructeur avec injection de dépendances.
    /// </summary>
    /// <param name="commentService">Service gérant la logique des commentaires</param>
    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }
    
    // CRÉATION DE COMMENTAIRE - POST /api/posts/{postId}/comments

    /// <summary>
    /// Ajoute un commentaire à un post existant.
    ///
    /// Le commentaire est automatiquement associé :
    /// - Au post spécifié dans l'URL (postId)
    /// - À l'utilisateur connecté (authorId via token JWT)
    ///
    /// Le commentaire est horodaté avec CreatedAt.
    /// </summary>
    /// <param name="postId">ID du post sur lequel commenter</param>
    /// <param name="createDto">Contenu du commentaire</param>
    /// <returns>
    /// 201 Created : Commentaire créé avec ses métadonnées
    /// 401 Unauthorized : Token manquant ou invalide
    /// 404 Not Found : Post inexistant
    /// </returns>
    [HttpPost("api/posts/{postId:guid}/comments")]
    [Authorize]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> Create(Guid postId, [FromBody] CreateCommentDto createDto)
    {
        // Extraire l'ID utilisateur depuis le token JWT
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Créer le commentaire via le service
        var comment = await _commentService.CreateAsync(postId, userId.Value, createDto);

        // Si null, le post n'existe pas
        if (comment == null)
            return NotFound(new { message = "Post non trouvé" });

        // Retourner 201 Created avec l'URL pour récupérer les commentaires du post
        return CreatedAtAction(nameof(GetByPostId), new { postId }, comment);
    }

    // LISTE DES COMMENTAIRES - GET /api/posts/{postId}/comments

    /// <summary>
    /// Récupère tous les commentaires d'un post.
    /// Chaque commentaire inclut les informations de son auteur
    /// (nom, prénom, avatar) pour l'affichage.
    /// </summary>
    /// <param name="postId">ID du post dont on veut les commentaires</param>
    /// <returns>
    /// 200 OK : Liste des commentaires (peut être vide)
    /// </returns>
    [HttpGet("api/posts/{postId:guid}/comments")]
    [ProducesResponseType(typeof(List<CommentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CommentDto>>> GetByPostId(Guid postId)
    {
        var comments = await _commentService.GetByPostIdAsync(postId);
        return Ok(comments);
    }

    // SUPPRESSION DE COMMENTAIRE - DELETE /api/comments/{id}

    /// <summary>
    /// Supprime un commentaire.
    ///
    /// Autorisations de suppression :
    /// - L'auteur du commentaire peut supprimer son propre commentaire
    /// - L'auteur du post parent peut supprimer n'importe quel commentaire
    /// sur son post
    /// </summary>
    /// <param name="id">ID du commentaire à supprimer</param>
    /// <returns>
    /// 204 No Content : Commentaire supprimé avec succès
    /// 401 Unauthorized : Token manquant ou invalide
    /// 404 Not Found : Commentaire inexistant ou non autorisé à supprimer
    /// </returns>
    [HttpDelete("api/comments/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Le service vérifie les droits 
        var result = await _commentService.DeleteAsync(id, userId.Value);

        if (!result)
            return NotFound(new { message = "Commentaire non trouvé ou vous n'êtes pas autorisé" });

        // 204 No Content : suppression réussie
        return NoContent();
    }

    // Extraction de l'ID utilisateur depuis le token JWT
    private Guid? GetCurrentUserId()
    {
        // Rechercher le claim standard ou JWT contenant l'ID utilisateur
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        // Valider que c'est un GUID valide
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }
}
