using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProSocialApi.DTOs.Users;
using ProSocialApi.Services.Interfaces;

namespace ProSocialApi.Controllers;

// USERSCONTROLLER.CS - Contrôleur de gestion des utilisateurs
// Gère les endpoints relatifs aux profils utilisateurs : consultation,
// modification, recherche et suppression de compte.
/// <summary>
/// Contrôleur REST pour la gestion des profils utilisateurs.
///
/// Endpoints disponibles :
/// - GET /api/users/me : Récupérer son propre profil (authentifié)
/// - GET /api/users/{id} : Consulter un profil par ID (public)
/// - PUT /api/users/me : Modifier son profil (authentifié)
/// - GET /api/users/search?q=... : Rechercher des utilisateurs (public)
/// - DELETE /api/users/me : Supprimer son compte (authentifié)
///
/// Note sur la conception :
/// - Les endpoints "me" utilisent le token JWT pour identifier l'utilisateur
/// - Les endpoints avec {id} permettent de consulter n'importe quel profil (visibilité publique)
/// </summary>
[ApiController]
[Route("api/[controller]")]              // Route de base : /api/users
[Produces("application/json")]
public class UsersController : ControllerBase
{
    // Service utilisateur injecté via DI
    private readonly IUserService _userService;

    /// <summary>
    /// Constructeur avec injection de dépendances.
    /// </summary>
    /// <param name="userService">Service gérant la logique des utilisateurs</param>
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // PROFIL COURANT - GET /api/users/me
    /// <summary>
    /// Récupère le profil complet de l'utilisateur connecté.
    ///
    /// Utilise le claim "sub" ou "NameIdentifier" du token JWT pour identifier l'utilisateur.
    /// Cet endpoint est la façon standard de récupérer "qui suis-je" après connexion.
    /// </summary>
    /// <returns>
    /// 200 OK : Profil de l'utilisateur connecté
    /// 401 Unauthorized : Token manquant ou invalide
    /// 404 Not Found : Utilisateur supprimé ou inexistant
    /// </returns>
    [HttpGet("me")]
    [Authorize]                          // Requiert un token JWT valide
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        // Extraire l'ID utilisateur depuis le token JWT
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Récupérer le profil depuis le service
        var user = await _userService.GetByIdAsync(userId.Value);

        if (user == null)
            return NotFound(new { message = "Utilisateur non trouvé" });

        return Ok(user);
    }

    // PROFIL PAR ID - GET /api/users/{id}
    /// <summary>
    /// Récupère le profil public d'un utilisateur par son ID.
    ///
    /// Endpoint public permettant à n'importe qui de consulter un profil.
    /// Utile pour afficher le profil d'un autre utilisateur dans l'application.
    ///
    /// Note : La contrainte {id:guid} assure que seuls les GUIDs valides sont acceptés.
    /// </summary>
    /// <param name="id">ID (GUID) de l'utilisateur à consulter</param>
    /// <returns>
    /// 200 OK : Profil de l'utilisateur demandé
    /// 404 Not Found : Utilisateur inexistant
    /// </returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);

        if (user == null)
            return NotFound(new { message = "Utilisateur non trouvé" });

        return Ok(user);
    }

    // UPLOAD AVATAR - POST /api/users/me/avatar
    /// <summary>
    /// Upload une image de profil pour l'utilisateur connecté.
    /// L'image est stockée dans wwwroot/uploads/avatars/.
    /// </summary>
    /// <param name="file">Fichier image (jpg, png, gif, webp)</param>
    /// <returns>
    /// 200 OK : URL de l'avatar uploadé
    /// 400 Bad Request : Fichier invalide ou trop volumineux
    /// 401 Unauthorized : Token manquant ou invalide
    /// </returns>
    [HttpPost("me/avatar")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Validation du fichier
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Aucun fichier fourni" });

        // Limite de taille : 5 Mo
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "Le fichier ne doit pas dépasser 5 Mo" });

        // Extensions autorisées
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { message = "Format non supporté. Utilisez jpg, png, gif ou webp" });

        // Créer le dossier uploads/avatars s'il n'existe pas
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
        Directory.CreateDirectory(uploadsFolder);

        // Générer un nom de fichier unique
        var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        // Sauvegarder le fichier
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // URL relative pour accéder à l'image
        var avatarUrl = $"/uploads/avatars/{fileName}";

        // Mettre à jour le profil avec la nouvelle URL
        await _userService.UpdateAsync(userId.Value, new UpdateUserDto { AvatarUrl = avatarUrl });

        return Ok(new { avatarUrl });
    }

    // MISE À JOUR DU PROFIL - PUT /api/users/me
    /// <summary>
    /// Met à jour le profil de l'utilisateur connecté.
    ///
    /// Utilise le pattern "Partial Update" : seuls les champs fournis
    /// (non null) dans le DTO sont mis à jour. Les champs absents
    /// conservent leur valeur actuelle.
    ///
    /// Champs modifiables :
    /// - FirstName, LastName : Nom et prénom
    /// - Headline : Titre professionnel (ex: "Software Engineer")
    /// - Bio : Description du profil
    /// - Location : Localisation géographique
    /// - AvatarUrl : URL de la photo de profil
    /// </summary>
    /// <param name="updateDto">Données à mettre à jour (champs optionnels)</param>
    /// <returns>
    /// 200 OK : Profil mis à jour avec toutes les nouvelles valeurs
    /// 401 Unauthorized : Token manquant ou invalide
    /// 404 Not Found : Utilisateur inexistant
    /// </returns>
    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> UpdateCurrentUser([FromBody] UpdateUserDto updateDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = await _userService.UpdateAsync(userId.Value, updateDto);

        if (user == null)
            return NotFound(new { message = "Utilisateur non trouvé" });

        return Ok(user);
    }

    // RECHERCHE D'UTILISATEURS - GET /api/users/search?q=...
    /// <summary>
    /// Recherche des utilisateurs par terme de recherche.
    ///
    /// La recherche s'effectue sur plusieurs champs :
    /// - FirstName (prénom)
    /// - LastName (nom)
    /// - Headline (titre professionnel)
    ///
    /// Utilise une recherche insensible à la casse avec Contains().
    ///
    /// Note : Endpoint public pour permettre la découverte d'utilisateurs
    /// sans être connecté (comme LinkedIn permet de voir des profils).
    /// </summary>
    /// <param name="q">Terme de recherche (query string)</param>
    /// <returns>
    /// 200 OK : Liste des utilisateurs correspondants (peut être vide)
    /// </returns>
    /// <remarks>
    /// Exemple : GET /api/users/search?q=john
    /// Retourne tous les utilisateurs dont le nom, prénom ou titre contient "john"
    /// </remarks>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserDto>>> Search([FromQuery] string q)
    {
        var users = await _userService.SearchAsync(q);
        return Ok(users);
    }

    // SUPPRESSION DU COMPTE - DELETE /api/users/me
    /// <summary>
    /// Supprime définitivement le compte de l'utilisateur connecté.
    ///
    /// ATTENTION : Action irréversible !
    /// La suppression cascade vers :
    /// - Posts de l'utilisateur
    /// - Commentaires de l'utilisateur
    /// - Likes de l'utilisateur
    /// - Connexions (demandes envoyées et reçues)
    ///
    /// Note : Les conversations MongoDB ne sont pas automatiquement supprimées
    /// (à implémenter si nécessaire pour conformité RGPD).
    /// </summary>
    /// <returns>
    /// 204 No Content : Compte supprimé avec succès
    /// 401 Unauthorized : Token manquant ou invalide
    /// 404 Not Found : Utilisateur inexistant
    /// </returns>
    [HttpDelete("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCurrentUser()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _userService.DeleteAsync(userId.Value);

        if (!result)
            return NotFound(new { message = "Utilisateur non trouvé" });

        // 204 No Content : suppression réussie, pas de contenu à retourner
        return NoContent();
    }

    // MÉTHODE UTILITAIRE : EXTRACTION DE L'ID UTILISATEUR
    /// <summary>
    /// Extrait l'ID de l'utilisateur connecté depuis les claims du token JWT.
    ///
    /// Recherche dans l'ordre :
    /// 1. Claim standard "NameIdentifier" (ClaimTypes.NameIdentifier)
    /// 2. Claim JWT "sub" (subject - standard JWT)
    ///
    /// Cette méthode est utilisée par tous les endpoints [Authorize]
    /// pour identifier l'utilisateur courant.
    /// </summary>
    /// <returns>
    /// GUID de l'utilisateur si trouvé et valide, null sinon
    /// </returns>
    private Guid? GetCurrentUserId()
    {
        // Rechercher le claim contenant l'ID utilisateur
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        // Vérifier que le claim existe et est un GUID valide
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }
}
