using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProSocialApi.Data.Entities;

// COMMENT.CS - Entité représentant un commentaire sur un post

/// <summary>
/// Entité Comment - Représente un commentaire sur un post.
/// Relations : Un auteur (User), un post parent (Post).
/// Un utilisateur peut commenter plusieurs posts, et un post peut avoir plusieurs commentaires.
/// </summary>
[Table("comments")] // Nom de la table en base de données
public class Comment
{
    // PROPRIÉTÉS D'IDENTIFICATION
    /// <summary>
    /// Identifiant unique du commentaire (clé primaire).
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    // PROPRIÉTÉS DE CONTENU
    /// <summary>
    /// Contenu textuel du commentaire.
    /// Champ obligatoire - un commentaire doit avoir du contenu.
    /// Stocké en TEXT pour permettre des commentaires de longueur variable.
    /// </summary>
    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    // CLÉS ÉTRANGÈRES - Relations avec User et Post
    /// <summary>
    /// ID de l'utilisateur qui a écrit ce commentaire.
    /// Clé étrangère vers la table Users.
    /// </summary>
    [Required]
    [Column("author_id")]
    public Guid AuthorId { get; set; }

    /// <summary>
    /// ID du post sur lequel ce commentaire a été posté.
    /// Clé étrangère vers la table Posts.
    /// Si le post est supprimé, tous ses commentaires seront supprimés (CASCADE).
    /// </summary>
    [Required]
    [Column("post_id")]
    public Guid PostId { get; set; }

    // PROPRIÉTÉS DE TRACKING
    /// <summary>
    /// Date et heure de création du commentaire.
    /// Utilisé pour afficher les commentaires dans l'ordre chronologique.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date et heure de la dernière modification du commentaire.
    /// Permet de savoir si un commentaire a été édité.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // PROPRIÉTÉS DE NAVIGATION (Relations)
    /// <summary>
    /// Auteur du commentaire - L'utilisateur qui l'a écrit.
    /// Navigation vers l'entité User via AuthorId.
    /// Permet d'accéder aux infos de l'auteur (nom, avatar) pour l'affichage.
    /// </summary>
    [ForeignKey("AuthorId")]
    public virtual User Author { get; set; } = null!;

    /// <summary>
    /// Post parent - La publication sur laquelle ce commentaire a été posté.
    /// Navigation vers l'entité Post via PostId.
    /// </summary>
    [ForeignKey("PostId")]
    public virtual Post Post { get; set; } = null!;
}
