// IPOSTSERVICE.CS - Interface du service de gestion des posts
// Définit le contrat pour les opérations sur les publications :
// - Création, lecture, modification, suppression de posts
// - Récupération des posts d'un utilisateur
// - Gestion des likes (like/unlike)

using ProSocialApi.DTOs.Posts;

namespace ProSocialApi.Services.Interfaces;

/// <summary>
/// Interface pour le service de gestion des posts.
/// Gère le cycle de vie complet des publications et les interactions (likes).
///
/// Implémentation : PostService
/// Enregistrement DI : AddScoped&lt;IPostService, PostService&gt;()
/// </summary>
public interface IPostService
{
    /// <summary>
    /// Crée un nouveau post.
    /// L'auteur est défini par l'ID de l'utilisateur authentifié.
    /// </summary>
    /// <param name="authorId">ID de l'auteur (utilisateur connecté)</param>
    /// <param name="createDto">Contenu du post (texte + optionnellement image)</param>
    /// <returns>PostDto du post créé avec toutes les métadonnées</returns>
    Task<PostDto> CreateAsync(Guid authorId, CreatePostDto createDto);

    /// <summary>
    /// Récupère un post par son ID.
    /// Inclut les infos de l'auteur, les stats (likes, comments) et
    /// si l'utilisateur courant a liké le post.
    /// </summary>
    /// <param name="postId">ID du post recherché</param>
    /// <param name="currentUserId">
    /// ID de l'utilisateur courant (optionnel).
    /// Si fourni, permet de calculer IsLikedByCurrentUser.
    /// </param>
    /// <returns>PostDto ou null si le post n'existe pas</returns>
    Task<PostDto?> GetByIdAsync(Guid postId, Guid? currentUserId = null);

    /// <summary>
    /// Met à jour un post existant.
    /// Seul l'auteur du post peut le modifier.
    /// Utilise le pattern Partial Update.
    /// </summary>
    /// <param name="postId">ID du post à modifier</param>
    /// <param name="authorId">ID de l'utilisateur qui modifie (doit être l'auteur)</param>
    /// <param name="updateDto">Champs à mettre à jour</param>
    /// <returns>PostDto mis à jour, ou null si post inexistant ou pas l'auteur</returns>
    Task<PostDto?> UpdateAsync(Guid postId, Guid authorId, UpdatePostDto updateDto);

    /// <summary>
    /// Supprime un post.
    /// Seul l'auteur du post peut le supprimer.
    /// Supprime également tous les commentaires et likes associés (cascade).
    /// </summary>
    /// <param name="postId">ID du post à supprimer</param>
    /// <param name="authorId">ID de l'utilisateur qui supprime (doit être l'auteur)</param>
    /// <returns>True si supprimé, false si post inexistant ou pas l'auteur</returns>
    Task<bool> DeleteAsync(Guid postId, Guid authorId);

    /// <summary>
    /// Récupère tous les posts d'un utilisateur spécifique.
    /// Triés du plus récent au plus ancien.
    /// </summary>
    /// <param name="userId">ID de l'utilisateur dont on veut les posts</param>
    /// <param name="currentUserId">
    /// ID de l'utilisateur courant (optionnel).
    /// Si fourni, permet de calculer IsLikedByCurrentUser pour chaque post.
    /// </param>
    /// <returns>Liste des posts de l'utilisateur</returns>
    Task<List<PostDto>> GetUserPostsAsync(Guid userId, Guid? currentUserId = null);

    /// <summary>
    /// Ajoute un like d'un utilisateur sur un post.
    /// Un utilisateur ne peut liker qu'une seule fois un même post.
    /// </summary>
    /// <param name="postId">ID du post à liker</param>
    /// <param name="userId">ID de l'utilisateur qui like</param>
    /// <returns>
    /// Tuple (Success, Message) :
    /// - (true, "Post liké") si succès
    /// - (false, "Déjà liké") si l'utilisateur a déjà liké ce post
    /// - (false, "Post introuvable") si le post n'existe pas
    /// </returns>
    Task<(bool Success, string Message)> LikeAsync(Guid postId, Guid userId);

    /// <summary>
    /// Retire le like d'un utilisateur sur un post.
    /// </summary>
    /// <param name="postId">ID du post à unliker</param>
    /// <param name="userId">ID de l'utilisateur qui unlike</param>
    /// <returns>
    /// Tuple (Success, Message) :
    /// - (true, "Like retiré") si succès
    /// - (false, "Pas liké") si l'utilisateur n'avait pas liké ce post
    /// </returns>
    Task<(bool Success, string Message)> UnlikeAsync(Guid postId, Guid userId);
}
