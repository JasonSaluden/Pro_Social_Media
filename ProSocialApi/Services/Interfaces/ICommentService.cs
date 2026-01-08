// ICOMMENTSERVICE.CS - Interface du service de gestion des commentaires
// Définit le contrat pour les opérations sur les commentaires :
// - Création de commentaires sur les posts
// - Récupération des commentaires d'un post
// - Suppression de commentaires

using ProSocialApi.DTOs.Comments;

namespace ProSocialApi.Services.Interfaces;

/// <summary>
/// Interface pour le service de gestion des commentaires.
/// Gère les commentaires sur les publications.
///
/// Implémentation : CommentService
/// Enregistrement DI : AddScoped&lt;ICommentService, CommentService&gt;()
/// </summary>
public interface ICommentService
{
    /// <summary>
    /// Crée un nouveau commentaire sur un post.
    /// </summary>
    /// <param name="postId">ID du post à commenter</param>
    /// <param name="authorId">ID de l'utilisateur qui commente</param>
    /// <param name="createDto">Contenu du commentaire</param>
    /// <returns>
    /// CommentDto du commentaire créé, ou null si le post n'existe pas
    /// </returns>
    Task<CommentDto?> CreateAsync(Guid postId, Guid authorId, CreateCommentDto createDto);

    /// <summary>
    /// Récupère tous les commentaires d'un post.
    /// Triés du plus ancien au plus récent (ordre chronologique).
    /// Inclut les informations de l'auteur de chaque commentaire.
    /// </summary>
    /// <param name="postId">ID du post</param>
    /// <returns>Liste des commentaires (peut être vide si aucun commentaire)</returns>
    Task<List<CommentDto>> GetByPostIdAsync(Guid postId);

    /// <summary>
    /// Supprime un commentaire.
    /// Seul l'auteur du commentaire peut le supprimer.
    ///
    /// Note : L'auteur du post parent pourrait aussi avoir le droit
    /// de supprimer les commentaires (à implémenter si nécessaire).
    /// </summary>
    /// <param name="commentId">ID du commentaire à supprimer</param>
    /// <param name="userId">ID de l'utilisateur qui supprime (doit être l'auteur)</param>
    /// <returns>True si supprimé, false si commentaire inexistant ou pas l'auteur</returns>
    Task<bool> DeleteAsync(Guid commentId, Guid userId);
}
