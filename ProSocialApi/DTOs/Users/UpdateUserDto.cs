// UPDATEUSERDTO.CS - DTO pour la mise à jour du profil utilisateur
// DTO utilisé pour recevoir les modifications du profil depuis le client.
// Toutes les propriétés sont nullables car l'utilisateur peut choisir de
// ne modifier que certains champs (mise à jour partielle).

using System.ComponentModel.DataAnnotations;

namespace ProSocialApi.DTOs.Users;

/// <summary>
/// DTO pour la mise à jour du profil (PUT /api/users/me).
///
/// Pattern : Partial Update (mise à jour partielle)
/// - Toutes les propriétés sont nullables
/// - Seules les propriétés non-null sont mises à jour
/// - Les propriétés null sont ignorées (conservent leur valeur actuelle)
///
/// Ce pattern évite d'écraser accidentellement des données avec des valeurs vides.
/// </summary>
public class UpdateUserDto
{
    /// <summary>
    /// Nouveau prénom (optionnel).
    /// Si null, le prénom actuel est conservé.
    /// </summary>
    [MaxLength(100)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Nouveau nom de famille (optionnel).
    /// Si null, le nom actuel est conservé.
    /// </summary>
    [MaxLength(100)]
    public string? LastName { get; set; }

    /// <summary>
    /// Nouveau titre professionnel (optionnel).
    /// Peut être mis à string.Empty pour supprimer le titre.
    /// </summary>
    [MaxLength(255)]
    public string? Headline { get; set; }

    /// <summary>
    /// Nouvelle biographie (optionnel).
    /// Pas de limite de longueur (stocké en TEXT).
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Nouvelle URL d'avatar (optionnel).
    /// - [MaxLength(500)] : Limite de sécurité pour les URLs
    /// - [Url] : Vérifie que c'est une URL valide (http:// ou https://)
    ///
    /// Note : La validation [Url] a causé des erreurs lors des tests
    /// avec des valeurs comme "string". Utiliser une vraie URL ou null.
    /// </summary>
    [MaxLength(500)]
    [Url(ErrorMessage = "L'URL de l'avatar n'est pas valide")]
    public string? AvatarUrl { get; set; }
}
