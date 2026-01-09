using Ganss.Xss;
using ProSocialApi.Services.Interfaces;

namespace ProSocialApi.Services;

/// <summary>
/// Implementation du service de sanitization utilisant HtmlSanitizer
/// </summary>
public class SanitizationService : ISanitizationService
{
    private readonly HtmlSanitizer _sanitizer;
    private readonly HtmlSanitizer _stripAllSanitizer;

    public SanitizationService()
    {
        // Sanitizer par defaut - autorise certaines balises HTML securisees
        _sanitizer = new HtmlSanitizer();

        // Configuration stricte : aucune balise HTML autorisee (texte brut)
        _stripAllSanitizer = new HtmlSanitizer();
        _stripAllSanitizer.AllowedTags.Clear();
        _stripAllSanitizer.AllowedAttributes.Clear();
        _stripAllSanitizer.AllowedCssProperties.Clear();
        _stripAllSanitizer.AllowedSchemes.Clear();
    }

    /// <inheritdoc />
    public string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return _sanitizer.Sanitize(input);
    }

    /// <inheritdoc />
    public string StripAllHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Supprime toutes les balises HTML
        var stripped = _stripAllSanitizer.Sanitize(input);

        // Decode les entites HTML (&amp; -> &, etc.)
        return System.Net.WebUtility.HtmlDecode(stripped);
    }
}
