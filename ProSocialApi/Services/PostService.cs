// POSTSERVICE.CS - Service de gestion des publications
// Implémente IPostService : gère le cycle de vie des posts et les likes.
// Opérations : CRUD posts, like/unlike, récupération des posts d'un utilisateur.

using Microsoft.EntityFrameworkCore;
using ProSocialApi.Data.Context;
using ProSocialApi.Data.Entities;
using ProSocialApi.DTOs.Posts;
using ProSocialApi.Services.Interfaces;

namespace ProSocialApi.Services;

/// <summary>
/// Service de gestion des publications.
/// Gère la création, modification, suppression des posts et les interactions (likes).
/// </summary>
public class PostService : IPostService
{
    private readonly ApplicationDbContext _context;

    public PostService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Crée un nouveau post pour l'utilisateur spécifié.
    /// </summary>
    public async Task<PostDto> CreateAsync(Guid authorId, CreatePostDto createDto)
    {
        var post = new Post
        {
            AuthorId = authorId,
            Content = createDto.Content,
            ImageUrl = createDto.ImageUrl
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Recharger le post complet avec relations pour le DTO
        return (await GetByIdAsync(post.Id, authorId))!;
    }

    /// <summary>
    /// Récupère un post par son ID avec toutes ses métadonnées.
    /// </summary>
    public async Task<PostDto?> GetByIdAsync(Guid postId, Guid? currentUserId = null)
    {
        // Charger le post avec ses relations (auteur, likes, commentaires)
        var post = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
            return null;

        return MapToDto(post, currentUserId);
    }

    /// <summary>
    /// Met à jour un post existant (pattern Partial Update).
    /// Vérifie que l'utilisateur est bien l'auteur du post.
    /// </summary>
    public async Task<PostDto?> UpdateAsync(Guid postId, Guid authorId, UpdatePostDto updateDto)
    {
        var post = await _context.Posts.FindAsync(postId);

        // Vérification : post existe ET utilisateur est l'auteur
        if (post == null || post.AuthorId != authorId)
            return null;

        // Mise à jour conditionnelle des champs fournis
        if (updateDto.Content != null)
            post.Content = updateDto.Content;

        if (updateDto.ImageUrl != null)
            post.ImageUrl = updateDto.ImageUrl;

        post.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(postId, authorId);
    }

    /// <summary>
    /// Supprime un post. Seul l'auteur peut supprimer son post.
    /// Les commentaires et likes sont supprimés en cascade.
    /// </summary>
    public async Task<bool> DeleteAsync(Guid postId, Guid authorId)
    {
        var post = await _context.Posts.FindAsync(postId);

        if (post == null || post.AuthorId != authorId)
            return false;

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Récupère tous les posts d'un utilisateur, triés du plus récent au plus ancien.
    /// </summary>
    public async Task<List<PostDto>> GetUserPostsAsync(Guid userId, Guid? currentUserId = null)
    {
        var posts = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Where(p => p.AuthorId == userId)
            .OrderByDescending(p => p.CreatedAt) // Plus récents en premier
            .ToListAsync();

        return posts.Select(p => MapToDto(p, currentUserId)).ToList();
    }

    /// <summary>
    /// Ajoute un like d'un utilisateur sur un post.
    /// Vérifie que le post existe et que l'utilisateur n'a pas déjà liké.
    /// </summary>
    public async Task<(bool Success, string Message)> LikeAsync(Guid postId, Guid userId)
    {
        // Vérifier que le post existe
        var post = await _context.Posts.FindAsync(postId);
        if (post == null)
            return (false, "Post non trouvé");

        // Vérifier si l'utilisateur a déjà liké ce post
        var existingLike = await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (existingLike != null)
            return (false, "Vous avez déjà liké ce post");

        // Créer le like
        var like = new Like
        {
            PostId = postId,
            UserId = userId
        };

        _context.Likes.Add(like);
        await _context.SaveChangesAsync();

        return (true, "Post liké");
    }

    /// <summary>
    /// Retire le like d'un utilisateur sur un post.
    /// </summary>
    public async Task<(bool Success, string Message)> UnlikeAsync(Guid postId, Guid userId)
    {
        // Rechercher le like existant
        var like = await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (like == null)
            return (false, "Vous n'avez pas liké ce post");

        _context.Likes.Remove(like);
        await _context.SaveChangesAsync();

        return (true, "Like retiré");
    }

    /// <summary>
    /// Convertit une entité Post en PostDto.
    /// Calcule les statistiques et vérifie si l'utilisateur courant a liké.
    /// </summary>
    private static PostDto MapToDto(Post post, Guid? currentUserId)
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
            // Vérifie si l'utilisateur courant fait partie des likes
            IsLikedByCurrentUser = currentUserId.HasValue && post.Likes.Any(l => l.UserId == currentUserId.Value)
        };
    }
}
