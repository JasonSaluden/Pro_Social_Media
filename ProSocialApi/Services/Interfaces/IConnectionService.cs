// ICONNECTIONSERVICE.CS - Interface du service de gestion des connexions
// Définit le contrat pour les opérations sur les connexions entre utilisateurs :
// - Envoi de demandes de connexion
// - Acceptation/Refus de demandes
// - Liste des connexions et demandes en attente
// - Suppression de connexions

using ProSocialApi.DTOs.Connections;

namespace ProSocialApi.Services.Interfaces;

/// <summary>
/// Interface pour le service de gestion des connexions.
/// Gère les relations professionnelles entre utilisateurs (style LinkedIn).
///
/// Implémentation : ConnectionService
/// Enregistrement DI : AddScoped&lt;IConnectionService, ConnectionService&gt;()
///
/// Flux typique d'une connexion :
/// 1. A envoie une demande à B (SendRequest) -> Status = Pending
/// 2. B accepte (AcceptRequest) -> Status = Accepted
///    OU B refuse (RejectRequest) -> Status = Rejected
/// 3. Les deux peuvent voir la connexion dans leur liste
/// 4. L'un ou l'autre peut supprimer la connexion (RemoveConnection)
/// </summary>
public interface IConnectionService
{
    /// <summary>
    /// Envoie une demande de connexion à un autre utilisateur.
    ///
    /// Validations effectuées :
    /// - L'utilisateur cible existe
    /// - Pas de demande à soi-même
    /// - Pas de connexion existante entre les deux utilisateurs
    /// </summary>
    /// <param name="requesterId">ID de l'utilisateur qui envoie la demande</param>
    /// <param name="addresseeId">ID de l'utilisateur qui reçoit la demande</param>
    /// <returns>
    /// Tuple (Success, Message) :
    /// - (true, "Demande envoyée") si succès
    /// - (false, "Raison de l'échec") sinon
    /// </returns>
    Task<(bool Success, string Message)> SendRequestAsync(Guid requesterId, Guid addresseeId);

    /// <summary>
    /// Accepte une demande de connexion en attente.
    /// L'utilisateur doit être le destinataire (Addressee) de la demande.
    /// </summary>
    /// <param name="connectionId">ID de la connexion à accepter</param>
    /// <param name="userId">ID de l'utilisateur qui accepte (doit être l'Addressee)</param>
    /// <returns>
    /// Tuple (Success, Message) :
    /// - (true, "Connexion acceptée") si succès
    /// - (false, "Raison de l'échec") sinon (demande inexistante, pas le bon user, etc.)
    /// </returns>
    Task<(bool Success, string Message)> AcceptRequestAsync(Guid connectionId, Guid userId);

    /// <summary>
    /// Refuse une demande de connexion en attente.
    /// L'utilisateur doit être le destinataire (Addressee) de la demande.
    /// La demande reste en base avec Status = Rejected pour historique.
    /// </summary>
    /// <param name="connectionId">ID de la connexion à refuser</param>
    /// <param name="userId">ID de l'utilisateur qui refuse (doit être l'Addressee)</param>
    /// <returns>
    /// Tuple (Success, Message) :
    /// - (true, "Demande refusée") si succès
    /// - (false, "Raison de l'échec") sinon
    /// </returns>
    Task<(bool Success, string Message)> RejectRequestAsync(Guid connectionId, Guid userId);

    /// <summary>
    /// Récupère la liste des connexions ACCEPTÉES d'un utilisateur.
    /// Retourne les infos de l'autre personne de chaque connexion.
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <returns>Liste des connexions avec les infos de l'autre utilisateur</returns>
    Task<List<ConnectionDto>> GetConnectionsAsync(Guid userId);

    /// <summary>
    /// Récupère les demandes de connexion en attente REÇUES par un utilisateur.
    /// Ne retourne pas les demandes envoyées, seulement celles reçues.
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <returns>Liste des demandes en attente avec les infos du demandeur</returns>
    Task<List<ConnectionRequestDto>> GetPendingRequestsAsync(Guid userId);

    /// <summary>
    /// Supprime une connexion existante.
    /// L'utilisateur doit faire partie de la connexion (Requester ou Addressee).
    /// La connexion est supprimée définitivement de la base de données.
    /// </summary>
    /// <param name="connectionId">ID de la connexion à supprimer</param>
    /// <param name="userId">ID de l'utilisateur qui supprime (doit être partie de la connexion)</param>
    /// <returns>
    /// Tuple (Success, Message) :
    /// - (true, "Connexion supprimée") si succès
    /// - (false, "Raison de l'échec") sinon
    /// </returns>
    Task<(bool Success, string Message)> RemoveConnectionAsync(Guid connectionId, Guid userId);

    /// <summary>
    /// Récupère des suggestions d'utilisateurs à qui envoyer une demande de connexion.
    /// Exclut : l'utilisateur lui-même, les connexions existantes, les demandes en attente.
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <param name="limit">Nombre maximum de suggestions (défaut: 10)</param>
    /// <returns>Liste d'utilisateurs suggérés</returns>
    Task<List<ConnectionUserDto>> GetSuggestionsAsync(Guid userId, int limit = 10);
}
