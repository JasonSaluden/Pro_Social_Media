// FEEDSERVICE.CS - Service de fil d'actualité personnalisé
// Implémente IFeedService : génère le feed personnalisé de chaque utilisateur.
// Le feed affiche les posts des connexions + les propres posts de l'utilisateur.

using Microsoft.EntityFrameworkCore;
using ProSocialApi.Data.Context;
using ProSocialApi.Data.Entities;
using ProSocialApi.DTOs.Posts;
using ProSocialApi.Services.Interfaces;

namespace ProSocialApi.Services;

/// <summary>
/// Service de génération du fil d'actualité personnalisé.
/// Affiche les posts des connexions acceptées et les propres posts de l'utilisateur.
/// </summary>
public class FeedService : IFeedService
{
    private readonly ApplicationDbContext _context;

    public FeedService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Génère le fil d'actualité personnalisé pour un utilisateur.
    ///
    /// Algorithme :
    /// 1. Récupérer les IDs des connexions acceptées de l'utilisateur
    /// 2. Ajouter l'ID de l'utilisateur lui-même (pour voir ses propres posts)
    /// 3. Récupérer les posts de tous ces utilisateurs
    /// 4. Trier par date décroissante et paginer
    ///
    /// Note : Pour un réseau social à grande échelle, cet algorithme devrait
    /// être optimisé (cache, pré-calcul, algorithmes de recommandation).
    /// </summary>
    public async Task<List<PostDto>> GetFeedAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        // Étape 1 : Récupérer les IDs des connexions acceptées
        // Une connexion peut être dans les deux sens (Requester ou Addressee)
        var connectionIds = await _context.Connections
            .Where(c =>
                (c.RequesterId == userId || c.AddresseeId == userId) &&
                c.Status == ConnectionStatus.Accepted)
            .Select(c => c.RequesterId == userId ? c.AddresseeId : c.RequesterId)
            .ToListAsync();

        // Étape 2 : Ajouter l'utilisateur lui-même pour voir ses propres posts
        connectionIds.Add(userId);

        // Étape 3 : Récupérer les posts de ces utilisateurs avec pagination
        var posts = await _context.Posts
            .Include(p => p.Author)      // Infos de l'auteur
            .Include(p => p.Likes)       // Pour le compteur et IsLikedByCurrentUser
            .Include(p => p.Comments)    // Pour le compteur
            .Where(p => connectionIds.Contains(p.AuthorId))
            .OrderByDescending(p => p.CreatedAt) // Plus récents en premier
            .Skip((page - 1) * pageSize)         // Pagination : sauter les posts précédents
            .Take(pageSize)                       // Prendre seulement pageSize posts
            .ToListAsync();

        // Étape 4 : Mapper en DTOs avec l'info IsLikedByCurrentUser
        return posts.Select(p => MapToDto(p, userId)).ToList();
    }

    /// <summary>
    /// Convertit une entité Post en PostDto.
    /// Inclut les statistiques et l'état du like pour l'utilisateur courant.
    /// </summary>
    private static PostDto MapToDto(Post post, Guid currentUserId)
    {
        return new PostDto
        {
            Id = post.Id,
            Content = post.Content,
            ImageUrl = post.ImageUrl,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            Author = new PostAuthorDto
            {
                Id = post.Author.Id,
                FirstName = post.Author.FirstName,
                LastName = post.Author.LastName,
                Headline = post.Author.Headline,
                AvatarUrl = post.Author.AvatarUrl
            },
            LikesCount = post.Likes.Count,
            CommentsCount = post.Comments.Count,
            // Vérifie si l'utilisateur courant a liké ce post
            IsLikedByCurrentUser = post.Likes.Any(l => l.UserId == currentUserId)
        };
    }
}
