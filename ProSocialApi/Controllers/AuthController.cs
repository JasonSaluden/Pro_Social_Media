using Microsoft.AspNetCore.Mvc;
using ProSocialApi.DTOs.Auth;
using ProSocialApi.Services.Interfaces;

namespace ProSocialApi.Controllers;

// AUTHCONTROLLER.CS - Contrôleur d'authentification
// Gère les endpoints d'authentification : inscription et connexion.

/// <summary>
/// Contrôleur REST pour l'authentification des utilisateurs.
///
/// Endpoints disponibles :
/// - POST /api/auth/register : Inscription d'un nouvel utilisateur
/// - POST /api/auth/login : Connexion d'un utilisateur existant
/// - POST /api/auth/logout : Déconnexion (suppression du cookie)
/// </summary>
[ApiController]
[Route("api/[controller]")]              // Route de base : /api/auth
[Produces("application/json")]           // Force à renvoyer du JSON (mais ca le fait par défaut)
public class AuthController : ControllerBase
{
    // Service d'authentification
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    // Nom du cookie JWT
    private const string JwtCookieName = "jwt_token";

    /// <summary>
    /// Constructeur avec injection de dépendances.
    /// </summary>
    /// <param name="authService">Service gérant la logique d'authentification</param>
    /// <param name="configuration">Configuration pour récupérer les paramètres JWT</param>
    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    /// <summary>
    /// Configure et envoie le cookie HttpOnly contenant le JWT.
    /// </summary>
    private void SetJwtCookie(string token)
    {
        var expiresInDays = int.Parse(_configuration["Jwt:ExpiresInDays"] ?? "7");

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,           // Non accessible par JavaScript (protection XSS)
            Secure = true,             // Envoyé uniquement en HTTPS (desactivé en dev)
            SameSite = SameSiteMode.Strict, // Protection CSRF
            Expires = DateTimeOffset.UtcNow.AddDays(expiresInDays),
            Path = "/"                 // Accessible sur tout le site
        };

        // En développement, on peut désactiver Secure pour HTTP local
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            cookieOptions.Secure = false;
        }

        Response.Cookies.Append(JwtCookieName, token, cookieOptions);
    }

    /// <summary>
    /// Supprime le cookie JWT.
    /// </summary>
    private void RemoveJwtCookie()
    {
        Response.Cookies.Delete(JwtCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
    }

    // INSCRIPTION - POST /api/auth/register

    /// <summary>
    /// Inscription d'un nouvel utilisateur.
    /// </summary>
    /// <param name="registerDto">Données d'inscription (Email, Password, FirstName, LastName)</param>
    /// <returns>
    /// 200 OK : Inscription réussie avec token JWT et infos utilisateur
    /// 400 Bad Request : Email déjà utilisé ou données invalides
    /// </returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
    {
        // Appel au service d'authentification pour l'inscription
        var result = await _authService.RegisterAsync(registerDto);

        // Si échec (ex: email déjà pris), retourner 400
        if (!result.Success)
        {
            return BadRequest(result);
        }

        // Stocker le token dans un cookie HttpOnly
        if (!string.IsNullOrEmpty(result.Token))
        {
            SetJwtCookie(result.Token);
        }

        // Succès : retourner 200 avec les infos utilisateur (token aussi pour rétrocompatibilité)
        return Ok(result);
    }

    // CONNEXION - POST /api/auth/login

    /// <summary>
    /// Connexion d'un utilisateur existant.
    /// Authorization: Bearer {token}
    /// </summary>
    /// <param name="loginDto">Données de connexion (Email, Password)</param>
    /// <returns>
    /// 200 OK : Connexion réussie avec token JWT et infos utilisateur
    /// 401 Unauthorized : Email ou mot de passe incorrect
    /// </returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        // Appel au service d'authentification pour la connexion
        var result = await _authService.LoginAsync(loginDto);

        // Si échec (mauvais email/mot de passe), retourner 401 Unauthorized
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        // Stocker le token dans un cookie HttpOnly
        if (!string.IsNullOrEmpty(result.Token))
        {
            SetJwtCookie(result.Token);
        }

        // Succès : retourner 200 avec les infos utilisateur (token aussi pour rétrocompatibilité)
        return Ok(result);
    }

    // DECONNEXION - POST /api/auth/logout

    /// <summary>
    /// Déconnexion de l'utilisateur.
    /// Supprime le cookie JWT HttpOnly.
    /// </summary>
    /// <returns>200 OK : Déconnexion réussie</returns>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        RemoveJwtCookie();
        return Ok(new { success = true, message = "Déconnexion réussie" });
    }
}
