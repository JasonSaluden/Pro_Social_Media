// POSTDTO.CS - DTOs pour les publications
// Ces DTOs définissent les structures de données pour les réponses liées
// aux posts. Inclut les informations de l'auteur et les statistiques
// (likes, commentaires) ainsi que l'état du like pour l'utilisateur connecté.

namespace ProSocialApi.DTOs.Posts;

/// <summary>
/// DTO représentant un post complet avec toutes ses métadonnées.
/// Utilisé par les endpoints :
/// - GET /api/posts/{id} (détail d'un post)
/// - GET /api/feed (fil d'actualité)
/// - GET /api/users/{id}/posts (posts d'un utilisateur)
///
/// Contient les statistiques et l'état du like pour permettre
/// un affichage complet côté client en une seule requête.
/// </summary>
public class PostDto
{
    // IDENTIFICATION
    /// <summary>
    /// Identifiant unique du post.
    /// </summary>
    public Guid Id { get; set; }

    // CONTENU

    /// <summary>
    /// Contenu textuel du post.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// URL de l'image attachée au post (optionnel).
    /// </summary>
    public string? ImageUrl { get; set; }

    // MÉTADONNÉES

    /// <summary>
    /// Date et heure de création du post.
    /// Utilisé pour l'affichage ("il y a 5 minutes") et le tri.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date et heure de la dernière modification.
    /// Si différent de CreatedAt, afficher "(modifié)" côté client.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // AUTEUR

    /// <summary>
    /// Informations de l'auteur du post.
    /// Inclus directement pour éviter une requête supplémentaire.
    /// </summary>
    public PostAuthorDto Author { get; set; } = null!;

    // STATISTIQUES ET INTERACTIONS

    /// <summary>
    /// Nombre total de likes sur ce post.
    /// Affiché sous le post (ex: "24 j'aime").
    /// </summary>
    public int LikesCount { get; set; }

    /// <summary>
    /// Nombre total de commentaires sur ce post.
    /// Affiché sous le post (ex: "5 commentaires").
    /// </summary>
    public int CommentsCount { get; set; }

    /// <summary>
    /// Indique si l'utilisateur connecté a déjà liké ce post.
    /// Permet d'afficher le bouton like en état "actif" si true.
    ///
    /// Important : Cette valeur dépend de l'utilisateur authentifié.
    /// Elle est calculée dans le service en fonction du userId du token.
    /// </summary>
    public bool IsLikedByCurrentUser { get; set; }
}

/// <summary>
/// DTO allégé représentant l'auteur d'un post.
/// Contient uniquement les informations nécessaires à l'affichage
/// de la carte auteur dans un post.
/// </summary>
public class PostAuthorDto
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
    /// Titre professionnel de l'auteur.
    /// Affiché sous le nom dans la carte auteur.
    /// </summary>
    public string? Headline { get; set; }

    /// <summary>
    /// URL de l'avatar de l'auteur.
    /// Affiché à côté du nom dans la carte auteur.
    /// </summary>
    public string? AvatarUrl { get; set; }
}
