namespace ProSocialApi.Services.Interfaces;

/// <summary>
/// Service de sanitization pour nettoyer le contenu utilisateur et prevenir les attaques XSS
/// </summary>
public interface ISanitizationService
{
    /// <summary>
    /// Nettoie le contenu HTML en supprimant les balises et scripts dangereux
    /// </summary>
    /// <param name="input">Le contenu a nettoyer</param>
    /// <returns>Le contenu sanitize</returns>
    string Sanitize(string? input);

    /// <summary>
    /// Nettoie le contenu en supprimant TOUTES les balises HTML (texte brut uniquement)
    /// </summary>
    /// <param name="input">Le contenu a nettoyer</param>
    /// <returns>Le contenu en texte brut</returns>
    string StripAllHtml(string? input);
}
