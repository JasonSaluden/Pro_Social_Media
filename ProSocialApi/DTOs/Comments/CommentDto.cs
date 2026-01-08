// COMMENTDTO.CS - DTOs pour les commentaires
// Ces DTOs définissent les structures de données pour les réponses et
// requêtes liées aux commentaires sur les posts.

using System.ComponentModel.DataAnnotations;

namespace ProSocialApi.DTOs.Comments;

/// <summary>
/// DTO représentant un commentaire complet.
/// Utilisé par l'endpoint GET /api/posts/{postId}/comments.
///
/// Inclut les informations de l'auteur pour un affichage direct
/// sans requête supplémentaire.
/// </summary>
public class CommentDto
{
    /// <summary>
    /// Identifiant unique du commentaire.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Contenu textuel du commentaire.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Date et heure de création du commentaire.
    /// Utilisé pour l'affichage chronologique et le "il y a X temps".
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Informations de l'auteur du commentaire.
    /// Permet d'afficher le nom et l'avatar à côté du commentaire.
    /// </summary>
    public CommentAuthorDto Author { get; set; } = null!;
}

/// <summary>
/// DTO allégé représentant l'auteur d'un commentaire.
/// Version simplifiée (pas de Headline) car l'espace est limité
/// dans l'affichage des commentaires.
/// </summary>
public class CommentAuthorDto
{
    /// <summary>
    /// Identifiant unique de l'auteur.
    /// Permet de lier vers son profil.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Prénom de l'auteur.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Nom de famille de l'auteur.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Nom complet (propriété calculée).
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// URL de l'avatar de l'auteur.
    /// Affiché à côté du commentaire.
    /// </summary>
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// DTO pour la création d'un nouveau commentaire (POST /api/posts/{postId}/comments).
/// Simple car un commentaire n'a qu'un contenu textuel.
/// </summary>
public class CreateCommentDto
{
    /// <summary>
    /// Contenu textuel du commentaire.
    /// - [Required] : Champ obligatoire
    /// - [MinLength(1)] : Au moins 1 caractère
    ///
    /// Note : Pas de limite de longueur pour permettre des commentaires détaillés.
    /// </summary>
    [Required(ErrorMessage = "Le contenu est requis")]
    [MinLength(1, ErrorMessage = "Le commentaire ne peut pas être vide")]
    public string Content { get; set; } = string.Empty;
}
