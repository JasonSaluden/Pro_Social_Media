using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProSocialApi.DTOs.Messages;
using ProSocialApi.Services.Interfaces;

namespace ProSocialApi.Controllers;

// CONVERSATIONSCONTROLLER.CS - Contrôleur de messagerie privée
/// <summary>
/// Contrôleur REST pour la messagerie privée entre utilisateurs.
///
/// Architecture des données (MongoDB) :
/// - Une Conversation contient 2 participants et une liste de Messages
/// - Les Messages sont imbriqués dans la Conversation (embedded documents)
/// - Chaque Message a un SenderId, Content et timestamps
///
/// Flux typique :
/// 1. User A crée une conversation avec User B (POST /)
/// 2. La conversation contient un message initial
/// 3. Les deux peuvent envoyer des messages (POST /{id}/messages)
/// 4. Les deux peuvent consulter la conversation complète (GET /{id})
///
/// Endpoints disponibles :
/// - POST /api/conversations : Créer une conversation avec message initial
/// - GET /api/conversations : Lister ses conversations
/// - GET /api/conversations/{id} : Consulter une conversation complète
/// - POST /api/conversations/{id}/messages : Envoyer un message
/// </summary>
[ApiController]
[Route("api/[controller]")]              // Route de base : /api/conversations
[Authorize]                              // Tous les endpoints nécessitent authentification
[Produces("application/json")]
public class ConversationsController : ControllerBase
{
    // Service de messagerie injecté via DI
    private readonly IMessageService _messageService;

    /// <summary>
    /// Constructeur avec injection de dépendances.
    /// </summary>
    /// <param name="messageService">Service gérant la logique de messagerie</param>
    public ConversationsController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    // CRÉATION DE CONVERSATION - POST /api/conversations

    /// <summary>
    /// Crée une nouvelle conversation avec un autre utilisateur.
    ///
    /// Comportement :
    /// - Si une conversation existe déjà entre les deux utilisateurs,
    ///   retourne la conversation existante (pas de doublon)
    /// - Sinon, crée une nouvelle conversation avec le message initial
    ///
    /// Le message initial est obligatoire pour éviter les conversations vides.
    /// </summary>
    /// <param name="createDto">ID du participant et message initial</param>
    /// <returns>
    /// 201 Created : Nouvelle conversation créée
    /// 200 OK : Conversation existante retournée (via CreatedAtAction)
    /// 400 Bad Request : Utilisateur cible inexistant
    /// 401 Unauthorized : Token manquant ou invalide
    /// </returns>
    /// <remarks>
    /// Exemple de requête :
    /// POST /api/conversations
    /// {
    ///     "participantId": "guid-du-destinataire",
    ///     "initialMessage": "Bonjour ! Comment ça va ?"
    /// }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ConversationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConversationDto>> Create([FromBody] CreateConversationDto createDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Créer ou récupérer la conversation existante
        var conversation = await _messageService.CreateConversationAsync(userId.Value, createDto);

        // Si null, l'utilisateur cible n'existe pas
        if (conversation == null)
            return BadRequest(new { message = "Impossible de créer la conversation. Utilisateur non trouvé." });

        // Retourner 201 Created avec l'URL de la nouvelle conversation
        return CreatedAtAction(nameof(GetById), new { id = conversation.Id }, conversation);
    }

    // LISTE DES CONVERSATIONS - GET /api/conversations

    /// <summary>
    /// Récupère toutes les conversations de l'utilisateur connecté.
    ///
    /// Les conversations sont triées par date du dernier message (plus récentes en premier).
    /// Chaque conversation inclut :
    /// - Les participants (avec leurs noms et avatars)
    /// - Le dernier message (aperçu pour la liste)
    /// - Les timestamps
    ///
    /// Équivalent à la boîte de réception LinkedIn/Messenger.
    /// </summary>
    /// <returns>
    /// 200 OK : Liste des conversations (peut être vide)
    /// 401 Unauthorized : Token manquant ou invalide
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ConversationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConversationDto>>> GetAll()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Récupérer toutes les conversations où l'utilisateur est participant
        var conversations = await _messageService.GetConversationsAsync(userId.Value);
        return Ok(conversations);
    }

    // DÉTAIL D'UNE CONVERSATION - GET /api/conversations/{id}

    /// <summary>
    /// Récupère une conversation complète avec tous ses messages.
    ///
    /// Vérifie que l'utilisateur est bien participant de la conversation
    /// (on ne peut pas lire les messages des autres).
    ///
    /// Les messages sont retournés dans l'ordre chronologique.
    /// Chaque message inclut les informations de l'expéditeur.
    /// </summary>
    /// <param name="id">ID MongoDB de la conversation (string ObjectId)</param>
    /// <returns>
    /// 200 OK : Conversation avec tous ses messages
    /// 401 Unauthorized : Token manquant ou invalide
    /// 404 Not Found : Conversation inexistante ou l'utilisateur n'est pas participant
    /// </returns>
    /// <remarks>
    /// Note : L'ID de conversation est un ObjectId MongoDB (string de 24 caractères hex),
    /// pas un GUID comme les autres entités.
    /// </remarks>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ConversationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationDetailDto>> GetById(string id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Le service vérifie que l'utilisateur est participant
        var conversation = await _messageService.GetConversationAsync(id, userId.Value);

        if (conversation == null)
            return NotFound(new { message = "Conversation non trouvée" });

        return Ok(conversation);
    }

    // ENVOI DE MESSAGE - POST /api/conversations/{id}/messages

    /// <summary>
    /// Envoie un nouveau message dans une conversation existante.
    ///
    /// Vérifie que l'utilisateur est participant de la conversation.
    /// Le message est ajouté à la liste des messages avec :
    /// - SenderId : ID de l'utilisateur courant
    /// - Content : Contenu du message
    /// - SentAt : Timestamp d'envoi
    ///
    /// Met à jour automatiquement LastMessageAt de la conversation.
    /// </summary>
    /// <param name="id">ID de la conversation</param>
    /// <param name="sendDto">Contenu du message à envoyer</param>
    /// <returns>
    /// 201 Created : Message envoyé avec succès
    /// 401 Unauthorized : Token manquant ou invalide
    /// 404 Not Found : Conversation inexistante ou l'utilisateur n'est pas participant
    /// </returns>
    /// <remarks>
    /// Exemple de requête :
    /// POST /api/conversations/{id}/messages
    /// {
    ///     "content": "Merci pour ta réponse !"
    /// }
    /// </remarks>
    [HttpPost("{id}/messages")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDto>> SendMessage(string id, [FromBody] SendMessageDto sendDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Envoyer le message via le service
        var message = await _messageService.SendMessageAsync(id, userId.Value, sendDto);

        // Si null, conversation inexistante ou utilisateur non participant
        if (message == null)
            return NotFound(new { message = "Conversation non trouvée" });

        // Retourner 201 Created (sans URL spécifique pour un message individuel)
        return Created("", message);
    }

    // MÉTHODE UTILITAIRE : EXTRACTION DE L'ID UTILISATEUR

    /// <summary>
    /// Extrait l'ID de l'utilisateur connecté depuis les claims du token JWT.
    /// </summary>
    /// <returns>GUID de l'utilisateur si trouvé et valide, null sinon</returns>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }
}
