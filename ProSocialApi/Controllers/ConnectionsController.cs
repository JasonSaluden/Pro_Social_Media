using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProSocialApi.DTOs.Connections;
using ProSocialApi.Services.Interfaces;

namespace ProSocialApi.Controllers;

// CONNECTIONSCONTROLLER.CS - Contrôleur de gestion des connexions

/// <summary>
/// Contrôleur REST pour la gestion des connexions entre utilisateurs.
/// Endpoints disponibles :
/// - POST /api/connections/request/{userId} : Envoyer une demande
/// - PUT /api/connections/{id}/accept : Accepter une demande
/// - PUT /api/connections/{id}/reject : Refuser une demande
/// - GET /api/connections : Lister ses connexions acceptées
/// - GET /api/connections/pending : Lister les demandes en attente reçues
/// - DELETE /api/connections/{id} : Supprimer une connexion
/// </summary>
[ApiController]
[Route("api/[controller]")]              // Route de base : /api/connections
[Authorize]                              // TOUS les endpoints nécessitent authentification
[Produces("application/json")]
public class ConnectionsController : ControllerBase
{
    // Service de connexions injectés
    private readonly IConnectionService _connectionService;

    /// <summary>
    /// Constructeur avec injection de dépendances.
    /// </summary>
    /// <param name="connectionService">Service gérant la logique des connexions</param>
    public ConnectionsController(IConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    // ENVOI DE DEMANDE - POST /api/connections/request/{userId}

    /// <summary>
    /// Envoie une demande de connexion à un autre utilisateur.
    ///
    /// Validations effectuées :
    /// - L'utilisateur cible doit exister
    /// - On ne peut pas s'envoyer une demande à soi-même
    /// - Aucune connexion (pending, accepted, rejected) ne doit déjà exister
    ///
    /// La demande créée a le statut "Pending" et sera visible dans
    /// les demandes en attente de l'utilisateur cible.
    /// </summary>
    /// <param name="userId">ID de l'utilisateur à qui envoyer la demande</param>
    /// <returns>
    /// 200 OK : Demande envoyée avec succès
    /// 400 Bad Request : Erreur de validation (soi-même, existe déjà, etc.)
    /// 401 Unauthorized : Token manquant ou invalide
    /// </returns>
    [HttpPost("request/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendRequest(Guid userId)
    {
        // Récupérer l'ID de l'utilisateur courant depuis le token
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        // Appeler le service pour envoyer la demande
        var (success, message) = await _connectionService.SendRequestAsync(currentUserId.Value, userId);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    // ACCEPTATION DE DEMANDE - PUT /api/connections/{id}/accept
    /// <summary>
    /// Accepte une demande de connexion en attente.
    ///
    /// Conditions requises :
    /// - La demande doit exister et être au statut "Pending"
    /// - L'utilisateur courant doit être le destinataire (Addressee) de la demande
    ///
    /// Une fois acceptée, la connexion passe au statut "Accepted" et
    /// apparaît dans la liste des connexions des deux utilisateurs.
    /// </summary>
    /// <param name="id">ID de la demande de connexion à accepter</param>
    /// <returns>
    /// 200 OK : Demande acceptée
    /// 400 Bad Request : Demande inexistante, déjà traitée, ou non autorisé
    /// 401 Unauthorized : Token manquant ou invalide
    /// </returns>
    [HttpPut("{id:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptRequest(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var (success, message) = await _connectionService.AcceptRequestAsync(id, currentUserId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    // REFUS DE DEMANDE - PUT /api/connections/{id}/reject

    /// <summary>
    /// Refuse une demande de connexion en attente.
    ///
    /// Conditions requises :
    /// - La demande doit exister et être au statut "Pending"
    /// - L'utilisateur courant doit être le destinataire (Addressee) de la demande
    ///
    /// La demande passe au statut "Rejected" et reste en base (historique).
    /// Note : Une demande refusée empêche de renvoyer une nouvelle demande.
    /// </summary>
    /// <param name="id">ID de la demande de connexion à refuser</param>
    /// <returns>
    /// 200 OK : Demande refusée
    /// 400 Bad Request : Demande inexistante, déjà traitée, ou non autorisé
    /// 401 Unauthorized : Token manquant ou invalide
    /// </returns>
    [HttpPut("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectRequest(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var (success, message) = await _connectionService.RejectRequestAsync(id, currentUserId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    // LISTE DES CONNEXIONS - GET /api/connections

    /// <summary>
    /// Récupère la liste des connexions acceptées de l'utilisateur courant.
    ///
    /// Équivalent à "Mon réseau" sur LinkedIn.
    /// Retourne les informations de base de chaque connexion (l'autre utilisateur).
    ///
    /// Note : Une connexion peut avoir été initiée par l'un ou l'autre,
    /// le service retourne toujours l'AUTRE utilisateur de la connexion.
    /// </summary>
    /// <returns>
    /// 200 OK : Liste des connexions (peut être vide)
    /// 401 Unauthorized : Token manquant ou invalide
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ConnectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConnectionDto>>> GetConnections()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var connections = await _connectionService.GetConnectionsAsync(currentUserId.Value);
        return Ok(connections);
    }

    // SUGGESTIONS - GET /api/connections/suggestions

    /// <summary>
    /// Récupère des suggestions d'utilisateurs à qui envoyer une demande de connexion.
    /// Exclut l'utilisateur lui-même et ceux avec qui une connexion existe déjà.
    /// </summary>
    /// <param name="limit">Nombre maximum de suggestions (défaut: 10)</param>
    /// <returns>
    /// 200 OK : Liste des utilisateurs suggérés
    /// 401 Unauthorized : Token manquant ou invalide
    /// </returns>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(List<ConnectionUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConnectionUserDto>>> GetSuggestions([FromQuery] int limit = 10)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var suggestions = await _connectionService.GetSuggestionsAsync(currentUserId.Value, limit);
        return Ok(suggestions);
    }

    // DEMANDES EN ATTENTE - GET /api/connections/pending

    /// <summary>
    /// Récupère les demandes de connexion en attente REÇUES par l'utilisateur.
    ///
    /// Ces sont les demandes que d'autres utilisateurs ont envoyées et
    /// qui attendent une réponse (accepter/refuser).
    /// Triées par date de création (plus récentes en premier).
    ///
    /// Note : N'inclut PAS les demandes envoyées par l'utilisateur courant.
    /// Pour voir ses propres demandes envoyées, une autre fonctionnalité serait nécessaire.
    /// </summary>
    /// <returns>
    /// 200 OK : Liste des demandes en attente (peut être vide)
    /// 401 Unauthorized : Token manquant ou invalide
    /// </returns>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<ConnectionRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConnectionRequestDto>>> GetPendingRequests()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var requests = await _connectionService.GetPendingRequestsAsync(currentUserId.Value);
        return Ok(requests);
    }

    // SUPPRESSION DE CONNEXION - DELETE /api/connections/{id}

    /// <summary>
    /// Supprime une connexion existante.
    ///
    /// Équivalent à "Se déconnecter" sur LinkedIn.
    /// Les deux parties de la connexion peuvent la supprimer.
    /// La suppression est définitive (pas de soft delete).
    ///
    /// Note : Après suppression, l'un peut renvoyer une demande à l'autre.
    /// </summary>
    /// <param name="id">ID de la connexion à supprimer</param>
    /// <returns>
    /// 200 OK : Connexion supprimée
    /// 400 Bad Request : Connexion inexistante ou non autorisé
    /// 401 Unauthorized : Token manquant ou invalide
    /// </returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveConnection(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var (success, message) = await _connectionService.RemoveConnectionAsync(id, currentUserId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    // EXTRACTION DE L'ID UTILISATEUR

    /// <summary>
    /// Extrait l'ID de l'utilisateur connecté depuis les claims du token JWT.
    /// Méthode utilitaire réutilisée dans tous les endpoints.
    /// </summary>
    /// <returns>GUID de l'utilisateur si trouvé et valide, null sinon</returns>
    private Guid? GetCurrentUserId()
    {
        // Rechercher le claim contenant l'ID utilisateur (standard ou JWT)
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        // Valider et parser le GUID
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }
}
