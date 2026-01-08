using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProSocialApi.Data.Entities;

// USER.CS

/// <summary>
/// Entité User - Représente un utilisateur inscrit sur le réseau social.
/// Contient les informations de profil, d'authentification et les relations
/// avec les posts, commentaires, likes et connexions.
/// </summary>
[Table("users")] // Nom de la table en base de données
public class User
{
    // PROPRIÉTÉS D'IDENTIFICATION
    /// <summary>
    /// Identifiant unique de l'utilisateur (clé primaire).
    /// Utilise un GUID pour garantir l'unicité sans dépendre d'un auto-increment.
    /// Avantages du GUID :
    /// - Génération côté client possible
    /// - Pas de conflit lors de fusions de bases de données
    /// - Plus difficile à deviner (sécurité)
    /// </summary>
    [Key]                    // Définit cette propriété comme clé primaire
    [Column("id")]           // Nom de la colonne en base de données
    public Guid Id { get; set; } = Guid.NewGuid(); // Génère automatiquement un nouvel ID

    // PROPRIÉTÉS D'AUTHENTIFICATION
    /// <summary>
    /// Adresse email de l'utilisateur.
    /// Sert d'identifiant de connexion (login).
    /// Doit être unique dans la base de données (configuré dans DbContext).
    /// </summary>
    [Required]               // Champ obligatoire - génère une contrainte NOT NULL en BDD
    [EmailAddress]           // Validation du format email côté serveur
    [Column("email")]        // Nom de la colonne
    [MaxLength(255)]         // Longueur maximale - optimise le stockage et permet l'indexation
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mot de passe hashé de l'utilisateur.
    /// IMPORTANT : Ne JAMAIS stocker de mot de passe en clair !
    /// Le hash est généré avec BCrypt qui inclut automatiquement un salt.
    /// Format BCrypt : $2a$10$N9qo8uLOickgx2ZMRZoMy...
    /// </summary>
    [Required]
    [Column("password")]
    [MaxLength(255)]         // Les hashs BCrypt font ~60 caractères, 255 laisse de la marge
    public string Password { get; set; } = string.Empty;

    // PROPRIÉTÉS DE PROFIL
    /// <summary>
    /// Prénom de l'utilisateur.
    /// Affiché dans le profil et les interactions sociales.
    /// </summary>
    [Required]
    [Column("first_name")]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Nom de famille de l'utilisateur.
    /// </summary>
    [Required]
    [Column("last_name")]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Titre professionnel / Headline (optionnel).
    /// Exemple : "Développeur Full Stack | React & .NET"
    /// Affiché sous le nom dans les listes et profils.
    /// </summary>
    [Column("headline")]
    [MaxLength(255)]
    public string? Headline { get; set; } // Nullable car optionnel

    /// <summary>
    /// Biographie / À propos (optionnel).
    /// Texte libre décrivant l'utilisateur, son parcours, ses compétences.
    /// Pas de MaxLength car stocké en TEXT pour permettre des descriptions longues.
    /// </summary>
    [Column("bio")]
    public string? Bio { get; set; }

    /// <summary>
    /// URL de l'avatar / photo de profil (optionnel).
    /// Peut pointer vers un service externe (Gravatar, Cloudinary, etc.)
    /// ou vers le stockage local de l'application.
    /// </summary>
    [Column("avatar_url")]
    [MaxLength(500)]         // Les URLs peuvent être longues
    public string? AvatarUrl { get; set; }

    // PROPRIÉTÉS DE TRACKING (Audit)
    /// <summary>
    /// Date et heure de création du compte.
    /// Initialisé automatiquement à l'heure UTC actuelle.
    /// UTC évite les problèmes de fuseaux horaires.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date et heure de la dernière modification du profil.
    /// Mis à jour automatiquement par le DbContext lors de SaveChanges().
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // PROPRIÉTÉS DE NAVIGATION (Relations)
    // Les propriétés de navigation permettent à EF Core de charger les entités
    // liées via Include() (eager loading) ou automatiquement (lazy loading).
    // "virtual" permet le lazy loading si activé.
    /// <summary>
    /// Collection des posts créés par cet utilisateur.
    /// Relation One-to-Many : Un utilisateur peut avoir plusieurs posts.
    /// </summary>
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    /// <summary>
    /// Collection des commentaires écrits par cet utilisateur.
    /// Relation One-to-Many : Un utilisateur peut avoir plusieurs commentaires.
    /// </summary>
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    /// <summary>
    /// Collection des likes donnés par cet utilisateur.
    /// Relation One-to-Many : Un utilisateur peut liker plusieurs posts.
    /// </summary>
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();

    /// <summary>
    /// Connexions envoyées par cet utilisateur (demandes faites).
    /// Dans une connexion, cet utilisateur est le "Requester" (demandeur).
    /// </summary>
    public virtual ICollection<Connection> SentConnections { get; set; } = new List<Connection>();

    /// <summary>
    /// Connexions reçues par cet utilisateur (demandes reçues).
    /// Dans une connexion, cet utilisateur est l'"Addressee" (destinataire).
    /// </summary>
    public virtual ICollection<Connection> ReceivedConnections { get; set; } = new List<Connection>();

    // PROPRIÉTÉS CALCULÉES (Non mappées en BDD)
    /// <summary>
    /// Nom complet de l'utilisateur (prénom + nom).
    /// Propriété calculée, non stockée en base de données.
    /// [NotMapped] indique à EF Core d'ignorer cette propriété.
    /// </summary>
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}
