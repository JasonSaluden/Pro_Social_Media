// CREATEPOSTDTO.CS - DTOs pour la création et modification de posts
// Ces DTOs définissent les structures de données pour les requêtes de
// création et modification de posts. Ils contiennent les validations
// nécessaires pour garantir des données valides.

using System.ComponentModel.DataAnnotations;

namespace ProSocialApi.DTOs.Posts;

/// <summary>
/// DTO pour la création d'un nouveau post (POST /api/posts).
/// Contient le contenu textuel obligatoire et une image optionnelle.
/// </summary>
public class CreatePostDto
{
    /// <summary>
    /// Contenu textuel du post.
    /// - [Required] : Champ obligatoire (un post doit avoir du contenu)
    /// - [MinLength(1)] : Au moins 1 caractère (évite les posts vides)
    ///
    /// Note : Pas de MaxLength car on veut permettre les posts longs.
    /// La limite est gérée côté base de données (type TEXT).
    /// </summary>
    [Required(ErrorMessage = "Le contenu est requis")]
    [MinLength(1, ErrorMessage = "Le contenu ne peut pas être vide")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// URL de l'image à attacher au post (optionnel).
    /// - [Url] : Vérifie que c'est une URL valide (http:// ou https://)
    ///
    /// Note : L'upload d'image n'est pas géré par cette API.
    /// L'image doit être uploadée sur un service externe (Cloudinary, S3, etc.)
    /// et seule l'URL est transmise ici.
    /// </summary>
    [Url(ErrorMessage = "L'URL de l'image n'est pas valide")]
    public string? ImageUrl { get; set; }
}

/// <summary>
/// DTO pour la modification d'un post existant (PUT /api/posts/{id}).
///
/// Pattern : Partial Update (mise à jour partielle)
/// - Toutes les propriétés sont nullables
/// - Seules les propriétés non-null sont mises à jour
///
/// Note : Seul l'auteur du post peut le modifier (vérifié dans le controller).
/// </summary>
public class UpdatePostDto
{
    /// <summary>
    /// Nouveau contenu du post (optionnel).
    /// Si fourni, doit contenir au moins 1 caractère.
    /// Si null, le contenu actuel est conservé.
    /// </summary>
    [MinLength(1, ErrorMessage = "Le contenu ne peut pas être vide")]
    public string? Content { get; set; }

    /// <summary>
    /// Nouvelle URL d'image (optionnel).
    /// - Si null : L'image actuelle est conservée
    /// - Si string.Empty : L'image est supprimée
    /// - Si URL valide : L'image est mise à jour
    /// </summary>
    [Url(ErrorMessage = "L'URL de l'image n'est pas valide")]
    public string? ImageUrl { get; set; }
}
