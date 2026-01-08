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
/// </summary>
[ApiController]                          
[Route("api/[controller]")]              // Route de base : /api/auth
[Produces("application/json")]           // Force à renvoyer du JSON (mais ca le fait par défaut)
public class AuthController : ControllerBase
{
    // Service d'authentification
    private readonly IAuthService _authService;

    /// <summary>
    /// Constructeur avec injection de dépendances.
    /// </summary>
    /// <param name="authService">Service gérant la logique d'authentification</param>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
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

        // Succès : retourner 200 avec le token et les infos utilisateur
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

        // Succès : retourner 200 avec le token et les infos utilisateur
        return Ok(result);
    }
}
