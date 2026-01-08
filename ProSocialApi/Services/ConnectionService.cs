// CONNECTIONSERVICE.CS - Service de gestion des connexions
// Implémente IConnectionService : gère les relations entre utilisateurs.
// Flux : Demande (Pending) -> Acceptation/Refus -> Connexion établie

using Microsoft.EntityFrameworkCore;
using ProSocialApi.Data.Context;
using ProSocialApi.Data.Entities;
using ProSocialApi.DTOs.Connections;
using ProSocialApi.Services.Interfaces;

namespace ProSocialApi.Services;

/// <summary>
/// Service de gestion des connexions entre utilisateurs.
/// Gère le cycle de vie complet des demandes de connexion.
/// </summary>
public class ConnectionService : IConnectionService
{
    private readonly ApplicationDbContext _context;

    public ConnectionService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Envoie une demande de connexion à un autre utilisateur.
    /// Vérifie les contraintes : pas soi-même, utilisateur existe, pas de doublon.
    /// </summary>
    public async Task<(bool Success, string Message)> SendRequestAsync(Guid requesterId, Guid addresseeId)
    {
        // Validation : pas de demande à soi-même
        if (requesterId == addresseeId)
            return (false, "Vous ne pouvez pas vous envoyer une demande à vous-même");

        // Validation : l'utilisateur cible doit exister
        var addressee = await _context.Users.FindAsync(addresseeId);
        if (addressee == null)
            return (false, "Utilisateur non trouvé");

        // Vérifier si une connexion existe déjà (dans les deux sens A->B ou B->A)
        var existingConnection = await _context.Connections
            .FirstOrDefaultAsync(c =>
                (c.RequesterId == requesterId && c.AddresseeId == addresseeId) ||
                (c.RequesterId == addresseeId && c.AddresseeId == requesterId));

        // Retourner un message approprié selon le statut existant
        if (existingConnection != null)
        {
            return existingConnection.Status switch
            {
                ConnectionStatus.Pending => (false, "Une demande est déjà en attente"),
                ConnectionStatus.Accepted => (false, "Vous êtes déjà connectés"),
                ConnectionStatus.Rejected => (false, "Cette demande a été refusée"),
                _ => (false, "Une connexion existe déjà")
            };
        }

        // Créer la nouvelle demande avec statut Pending
        var connection = new Connection
        {
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = ConnectionStatus.Pending
        };

        _context.Connections.Add(connection);
        await _context.SaveChangesAsync();

        return (true, "Demande de connexion envoyée");
    }

    /// <summary>
    /// Accepte une demande de connexion en attente.
    /// Seul le destinataire (Addressee) peut accepter.
    /// </summary>
    public async Task<(bool Success, string Message)> AcceptRequestAsync(Guid connectionId, Guid userId)
    {
        var connection = await _context.Connections.FindAsync(connectionId);

        if (connection == null)
            return (false, "Demande non trouvée");

        // Vérification d'autorisation : seul le destinataire peut accepter
        if (connection.AddresseeId != userId)
            return (false, "Vous n'êtes pas autorisé à accepter cette demande");

        // Vérification d'état : doit être en attente
        if (connection.Status != ConnectionStatus.Pending)
            return (false, "Cette demande n'est plus en attente");

        // Accepter la connexion
        connection.Status = ConnectionStatus.Accepted;
        connection.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "Demande acceptée");
    }

    /// <summary>
    /// Refuse une demande de connexion en attente.
    /// La demande est conservée avec statut Rejected (historique).
    /// </summary>
    public async Task<(bool Success, string Message)> RejectRequestAsync(Guid connectionId, Guid userId)
    {
        var connection = await _context.Connections.FindAsync(connectionId);

        if (connection == null)
            return (false, "Demande non trouvée");

        // Vérification d'autorisation
        if (connection.AddresseeId != userId)
            return (false, "Vous n'êtes pas autorisé à refuser cette demande");

        if (connection.Status != ConnectionStatus.Pending)
            return (false, "Cette demande n'est plus en attente");

        connection.Status = ConnectionStatus.Rejected;
        connection.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "Demande refusée");
    }

    /// <summary>
    /// Récupère toutes les connexions acceptées d'un utilisateur.
    /// Retourne les infos de l'autre personne de chaque connexion.
    /// </summary>
    public async Task<List<ConnectionDto>> GetConnectionsAsync(Guid userId)
    {
        // Charger les connexions acceptées avec les infos des deux parties
        var connections = await _context.Connections
            .Include(c => c.Requester)
            .Include(c => c.Addressee)
            .Where(c =>
                (c.RequesterId == userId || c.AddresseeId == userId) &&
                c.Status == ConnectionStatus.Accepted)
            .ToListAsync();

        // Mapper en retournant toujours l'AUTRE utilisateur de la connexion
        return connections.Select(c => new ConnectionDto
        {
            Id = c.Id,
            Status = c.Status,
            CreatedAt = c.CreatedAt,
            // Si l'utilisateur est le Requester, retourner l'Addressee, et vice versa
            User = MapToConnectionUser(c.RequesterId == userId ? c.Addressee : c.Requester)
        }).ToList();
    }

    /// <summary>
    /// Récupère les demandes de connexion en attente REÇUES par l'utilisateur.
    /// Permet à l'utilisateur de voir qui veut se connecter avec lui.
    /// </summary>
    public async Task<List<ConnectionRequestDto>> GetPendingRequestsAsync(Guid userId)
    {
        var requests = await _context.Connections
            .Include(c => c.Requester)
            .Where(c => c.AddresseeId == userId && c.Status == ConnectionStatus.Pending)
            .OrderByDescending(c => c.CreatedAt) // Plus récentes en premier
            .ToListAsync();

        return requests.Select(c => new ConnectionRequestDto
        {
            Id = c.Id,
            CreatedAt = c.CreatedAt,
            Requester = MapToConnectionUser(c.Requester)
        }).ToList();
    }

    /// <summary>
    /// Supprime une connexion existante.
    /// Les deux parties de la connexion peuvent la supprimer.
    /// </summary>
    public async Task<(bool Success, string Message)> RemoveConnectionAsync(Guid connectionId, Guid userId)
    {
        var connection = await _context.Connections.FindAsync(connectionId);

        if (connection == null)
            return (false, "Connexion non trouvée");

        // Vérification : l'utilisateur doit faire partie de la connexion
        if (connection.RequesterId != userId && connection.AddresseeId != userId)
            return (false, "Vous n'êtes pas autorisé à supprimer cette connexion");

        _context.Connections.Remove(connection);
        await _context.SaveChangesAsync();

        return (true, "Connexion supprimée");
    }

    /// <summary>
    /// Récupère des suggestions d'utilisateurs à qui envoyer une demande de connexion.
    /// Exclut l'utilisateur lui-même et tous ceux avec qui une connexion existe déjà.
    /// </summary>
    public async Task<List<ConnectionUserDto>> GetSuggestionsAsync(Guid userId, int limit = 10)
    {
        // Récupérer les IDs des utilisateurs avec qui une connexion existe déjà
        var connectedUserIds = await _context.Connections
            .Where(c => c.RequesterId == userId || c.AddresseeId == userId)
            .Select(c => c.RequesterId == userId ? c.AddresseeId : c.RequesterId)
            .ToListAsync();

        // Ajouter l'utilisateur lui-même à la liste d'exclusion
        connectedUserIds.Add(userId);

        // Récupérer les utilisateurs qui ne sont pas dans la liste d'exclusion
        var suggestions = await _context.Users
            .Where(u => !connectedUserIds.Contains(u.Id))
            .OrderBy(u => Guid.NewGuid()) // Ordre aléatoire pour varier les suggestions
            .Take(limit)
            .ToListAsync();

        return suggestions.Select(MapToConnectionUser).ToList();
    }

    /// <summary>
    /// Convertit une entité User en ConnectionUserDto.
    /// </summary>
    private static ConnectionUserDto MapToConnectionUser(User user)
    {
        return new ConnectionUserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Headline = user.Headline,
            AvatarUrl = user.AvatarUrl
        };
    }
}
