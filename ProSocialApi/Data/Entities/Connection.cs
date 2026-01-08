using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProSocialApi.Data.Entities;

// CONNECTION.CS

/// <summary>
/// Énumération des statuts possibles d'une demande de connexion.
/// Le flux typique : Pending -> Accepted ou Pending -> Rejected
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// Demande en attente - Le destinataire n'a pas encore répondu.
    /// C'est l'état initial de toute nouvelle demande de connexion.
    /// </summary>
    Pending,

    /// <summary>
    /// Connexion acceptée - Les deux utilisateurs sont maintenant connectés.
    /// Ils peuvent voir mutuellement leurs posts dans leur fil d'actualité.
    /// </summary>
    Accepted,

    /// <summary>
    /// Connexion refusée - Le destinataire a décliné la demande.
    /// La demande reste en base pour historique et éviter le spam de demandes.
    /// </summary>
    Rejected
}

/// <summary>
/// Entité Connection - Représente une relation entre deux utilisateurs.
/// Modèle de relation Many-to-Many avec attributs (statut, dates).
/// La relation est directionnelle : Requester (demandeur) -> Addressee (destinataire).
/// </summary>
[Table("connections")] // Nom de la table en base de données
public class Connection
{
    // PROPRIÉTÉS D'IDENTIFICATION
    /// <summary>
    /// Identifiant unique de la connexion (clé primaire).
    /// Chaque demande de connexion a son propre ID.
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    // CLÉS ÉTRANGÈRES - Définissent les deux parties de la connexion
    /// <summary>
    /// ID de l'utilisateur qui a ENVOYÉ la demande de connexion.
    /// Clé étrangère vers la table Users.
    /// </summary>
    [Required]
    [Column("requester_id")]
    public Guid RequesterId { get; set; }

    /// <summary>
    /// ID de l'utilisateur qui a REÇU la demande de connexion.
    /// Clé étrangère vers la table Users.
    /// C'est lui qui peut accepter ou refuser la demande.
    /// </summary>
    [Required]
    [Column("addressee_id")]
    public Guid AddresseeId { get; set; }

    // PROPRIÉTÉS DE LA CONNEXION
    /// <summary>
    /// Statut actuel de la demande de connexion.
    /// Valeur par défaut : Pending (en attente de réponse).
    /// EF Core stocke les enums comme des entiers (0, 1, 2) par défaut.
    /// </summary>
    [Required]
    [Column("status")]
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;

    // PROPRIÉTÉS DE TRACKING (Audit)
    /// <summary>
    /// Date et heure d'envoi de la demande de connexion.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date et heure de la dernière modification (acceptation/refus).
    /// Permet de savoir quand le statut a changé.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // PROPRIÉTÉS DE NAVIGATION 
    /// <summary>
    /// Utilisateur qui a envoyé la demande de connexion.
    /// Navigation vers l'entité User via RequesterId.
    /// </summary>
    [ForeignKey("RequesterId")]
    public virtual User Requester { get; set; } = null!;

    /// <summary>
    /// Utilisateur qui a reçu la demande de connexion.
    /// Navigation vers l'entité User via AddresseeId.
    /// </summary>
    [ForeignKey("AddresseeId")]
    public virtual User Addressee { get; set; } = null!;
}
