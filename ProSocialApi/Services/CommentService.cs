// COMMENTSERVICE.CS - Service de gestion des commentaires
// Implémente ICommentService : gère les commentaires sur les posts.
// Opérations : création, liste, suppression de commentaires.

using Microsoft.EntityFrameworkCore;
using ProSocialApi.Data.Context;
using ProSocialApi.Data.Entities;
using ProSocialApi.DTOs.Comments;
using ProSocialApi.Services.Interfaces;

namespace ProSocialApi.Services;

/// <summary>
/// Service de gestion des commentaires sur les publications.
/// </summary>
public class CommentService : ICommentService
{
    private readonly ApplicationDbContext _context;

    public CommentService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Crée un nouveau commentaire sur un post.
    /// Vérifie que le post existe avant de créer le commentaire.
    /// </summary>
    public async Task<CommentDto?> CreateAsync(Guid postId, Guid authorId, CreateCommentDto createDto)
    {
        // Vérifier que le post cible existe
        var post = await _context.Posts.FindAsync(postId);
        if (post == null)
            return null;

        // Créer le commentaire
        var comment = new Comment
        {
            PostId = postId,
            AuthorId = authorId,
            Content = createDto.Content
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        // Recharger le commentaire avec les infos de l'auteur pour le DTO
        var createdComment = await _context.Comments
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == comment.Id);

        return MapToDto(createdComment!);
    }

    /// <summary>
    /// Récupère tous les commentaires d'un post.
    /// Triés du plus ancien au plus récent (ordre chronologique de conversation).
    /// </summary>
    public async Task<List<CommentDto>> GetByPostIdAsync(Guid postId)
    {
        var comments = await _context.Comments
            .Include(c => c.Author)
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt) // Plus anciens en premier (ordre de lecture)
            .ToListAsync();

        return comments.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Supprime un commentaire.
    /// Autorisé pour : l'auteur du commentaire OU l'auteur du post parent.
    /// </summary>
    public async Task<bool> DeleteAsync(Guid commentId, Guid userId)
    {
        // Charger le commentaire avec le post parent pour vérifier les droits
        var comment = await _context.Comments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null)
            return false;

        // Vérification des droits :
        // - L'auteur du commentaire peut supprimer son commentaire
        // - L'auteur du post peut supprimer les commentaires sur son post (modération)
        if (comment.AuthorId != userId && comment.Post.AuthorId != userId)
            return false;

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Convertit une entité Comment en CommentDto.
    /// </summary>
    private static CommentDto MapToDto(Comment comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            Author = new CommentAuthorDto
            {
                Id = comment.Author.Id,
                FirstName = comment.Author.FirstName,
                LastName = comment.Author.LastName,
                AvatarUrl = comment.Author.AvatarUrl
            }
        };
    }
}
