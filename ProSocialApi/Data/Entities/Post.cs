using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProSocialApi.Data.Entities;

// POST.CS

/// <summary>
/// Entité Post - Représente une publication créée par un utilisateur.
/// Contient le contenu textuel, une image optionnelle, et les métadonnées.
/// Relations : Un auteur (User), plusieurs commentaires (Comment), plusieurs likes (Like).
/// </summary>
[Table("posts")] // Nom de la table en base de données
public class Post
{
    // PROPRIÉTÉS D'IDENTIFICATION
    /// <summary>
    /// Identifiant unique du post (clé primaire).
    /// Utilise un GUID pour l'unicité globale.
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    // PROPRIÉTÉS DE CONTENU
    /// <summary>
    /// Contenu textuel du post.
    /// Champ obligatoire - un post doit avoir au minimum du texte.
    /// Stocké en TEXT (pas de MaxLength) pour permettre des posts longs.
    /// </summary>
    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// URL de l'image attachée au post (optionnel).
    /// Peut pointer vers un service de stockage externe (Cloudinary, S3, etc.).
    /// Si null, le post ne contient que du texte.
    /// </summary>
    [Column("image_url")]
    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    // CLÉ ÉTRANGÈRE - Auteur du post
    /// <summary>
    /// ID de l'utilisateur qui a créé ce post.
    /// Clé étrangère vers la table Users.
    /// </summary>
    [Required]
    [Column("author_id")]
    public Guid AuthorId { get; set; }

    // PROPRIÉTÉS DE TRACKING (Audit)
    /// <summary>
    /// Date et heure de création du post.
    /// Utilisé pour trier les posts du plus récent au plus ancien.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date et heure de la dernière modification du post.
    /// Permet de savoir si un post a été édité après publication.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // PROPRIÉTÉS DE NAVIGATION (Relations)
    /// <summary>
    /// Auteur du post - L'utilisateur qui a créé cette publication.
    /// Navigation vers l'entité User via AuthorId.
    /// </summary>
    [ForeignKey("AuthorId")]
    public virtual User Author { get; set; } = null!;

    /// <summary>
    /// Collection des commentaires sur ce post.
    /// Relation One-to-Many : Un post peut avoir plusieurs commentaires.
    /// Initialisé avec une liste vide pour éviter les NullReferenceException.
    /// </summary>
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    /// <summary>
    /// Collection des likes sur ce post.
    /// Relation One-to-Many : Un post peut avoir plusieurs likes.
    /// </summary>
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();

    // PROPRIÉTÉS CALCULÉES (Non mappées en BDD)
    // Ces propriétés sont calculées à partir des collections de navigation.
    // [NotMapped] indique à EF Core de ne pas créer de colonne pour ces propriétés.
    // Utile pour accéder rapidement aux compteurs sans requête supplémentaire
    // APRÈS avoir chargé les collections via Include().
    /// <summary>
    /// Nombre total de likes sur ce post.
    /// Calculé à partir de la collection Likes (doit être chargée via Include).
    /// </summary>
    [NotMapped]
    public int LikesCount => Likes.Count;

    /// <summary>
    /// Nombre total de commentaires sur ce post.
    /// Calculé à partir de la collection Comments (doit être chargée via Include).
    /// </summary>
    [NotMapped]
    public int CommentsCount => Comments.Count;
}
