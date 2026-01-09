// USERSERVICE.CS - Service de gestion des utilisateurs
// Implémente IUserService : gère les profils utilisateurs (CRUD).
// Utilise Entity Framework Core pour les opérations en base de données.

using Microsoft.EntityFrameworkCore;
using ProSocialApi.Data.Context;
using ProSocialApi.Data.Entities;
using ProSocialApi.DTOs.Users;
using ProSocialApi.Services.Interfaces;

namespace ProSocialApi.Services;

/// <summary>
/// Service de gestion des profils utilisateurs.
/// Fournit les opérations CRUD et la recherche d'utilisateurs.
/// </summary>
public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly ISanitizationService _sanitizer;

    public UserService(ApplicationDbContext context, ISanitizationService sanitizer)
    {
        _context = context;
        _sanitizer = sanitizer;
    }

    /// <summary>
    /// Récupère un utilisateur par son ID avec ses statistiques.
    /// Inclut le nombre de connexions acceptées et le nombre de posts.
    /// </summary>
    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        // Charger l'utilisateur avec ses relations nécessaires aux statistiques
        // Les filtres Where() dans Include() permettent de ne charger que les connexions acceptées
        var user = await _context.Users
            .Include(u => u.Posts)
            .Include(u => u.SentConnections.Where(c => c.Status == ConnectionStatus.Accepted))
            .Include(u => u.ReceivedConnections.Where(c => c.Status == ConnectionStatus.Accepted))
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return null;

        return MapToDto(user);
    }

    /// <summary>
    /// Met à jour le profil d'un utilisateur (pattern Partial Update).
    /// Seuls les champs non-null sont modifiés.
    /// </summary>
    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto updateDto)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return null;

        // Mise à jour conditionnelle : seuls les champs fournis (non null) sont modifiés
        if (updateDto.FirstName != null)
            user.FirstName = updateDto.FirstName;

        if (updateDto.LastName != null)
            user.LastName = updateDto.LastName;

        if (updateDto.Headline != null)
            user.Headline = _sanitizer.StripAllHtml(updateDto.Headline); // Sanitize XSS

        if (updateDto.Bio != null)
            user.Bio = _sanitizer.StripAllHtml(updateDto.Bio); // Sanitize XSS

        if (updateDto.AvatarUrl != null)
            user.AvatarUrl = updateDto.AvatarUrl;

        // Mise à jour manuelle du timestamp (aussi fait par le DbContext)
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Recharger avec les relations pour retourner le DTO complet avec statistiques
        return await GetByIdAsync(id);
    }

    /// <summary>
    /// Recherche des utilisateurs par nom, prénom ou email.
    /// Retourne max 20 résultats pour éviter les surcharges.
    /// </summary>
    public async Task<List<UserDto>> SearchAsync(string query)
    {
        // Requête vide = pas de résultats
        if (string.IsNullOrWhiteSpace(query))
            return new List<UserDto>();

        var lowerQuery = query.ToLower();

        // Recherche insensible à la casse sur plusieurs champs
        var users = await _context.Users
            .Include(u => u.Posts)
            .Include(u => u.SentConnections.Where(c => c.Status == ConnectionStatus.Accepted))
            .Include(u => u.ReceivedConnections.Where(c => c.Status == ConnectionStatus.Accepted))
            .Where(u =>
                u.FirstName.ToLower().Contains(lowerQuery) ||
                u.LastName.ToLower().Contains(lowerQuery) ||
                u.Email.ToLower().Contains(lowerQuery) ||
                (u.Headline != null && u.Headline.ToLower().Contains(lowerQuery)))
            .Take(20) // Limite pour éviter les résultats trop volumineux
            .ToListAsync();

        return users.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Supprime définitivement un compte utilisateur.
    /// Les données liées sont supprimées en cascade (configuré dans le DbContext).
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Convertit une entité User en UserDto avec statistiques calculées.
    /// </summary>
    private static UserDto MapToDto(User user)
    {
        // Calcul du nombre total de connexions acceptées (envoyées + reçues)
        var acceptedConnections =
            user.SentConnections.Count(c => c.Status == ConnectionStatus.Accepted) +
            user.ReceivedConnections.Count(c => c.Status == ConnectionStatus.Accepted);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Headline = user.Headline,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            ConnectionsCount = acceptedConnections,
            PostsCount = user.Posts.Count
        };
    }
}
