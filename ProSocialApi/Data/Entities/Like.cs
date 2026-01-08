using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProSocialApi.Data.Entities;

// LIKE.CS

/// <summary>
/// Entité Like - Représente un "j'aime" donné par un utilisateur à un post.
/// Table de jonction entre Users et Posts avec contrainte d'unicité.
/// Un utilisateur peut liker plusieurs posts, un post peut avoir plusieurs likes,
/// mais un utilisateur ne peut liker qu'une seule fois un même post.
/// </summary>
[Table("likes")] // Nom de la table en base de données
public class Like
{
    // PROPRIÉTÉS D'IDENTIFICATION
    /// <summary>
    /// Identifiant unique du like (clé primaire).
    /// Note : Une alternative serait d'utiliser une clé composite (UserId, PostId).
    /// On utilise un GUID séparé pour la cohérence avec les autres entités.
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    // CLÉS ÉTRANGÈRES - Définissent le "qui" et le "quoi" du like
    /// <summary>
    /// ID de l'utilisateur qui a liké le post.
    /// Clé étrangère vers la table Users.
    /// </summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// ID du post qui a reçu le like.
    /// Clé étrangère vers la table Posts.
    /// Si le post est supprimé, tous ses likes seront supprimés (CASCADE).
    /// </summary>
    [Required]
    [Column("post_id")]
    public Guid PostId { get; set; }

    // PROPRIÉTÉS DE TRACKING
    /// <summary>
    /// Date et heure à laquelle le like a été donné.
    /// Utile pour les statistiques et l'ordre chronologique.
    /// Note : Pas de UpdatedAt car un like n'est jamais modifié, seulement créé ou supprimé.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // PROPRIÉTÉS DE NAVIGATION (Relations)
    /// <summary>
    /// Utilisateur qui a donné le like.
    /// Navigation vers l'entité User via UserId.
    /// </summary>
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Post qui a reçu le like.
    /// Navigation vers l'entité Post via PostId.
    /// </summary>
    [ForeignKey("PostId")]
    public virtual Post Post { get; set; } = null!;
}
