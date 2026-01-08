// APPLICATIONDBCONTEXT.CS - Contexte Entity Framework Core pour MySQL
// Cette classe est le point central d'accès à la base de données MySQL.
// Elle hérite de DbContext et configure :
// - Les DbSet (tables) accessibles
// - Les relations entre entités (Fluent API)
// - Les contraintes d'unicité et index
// - La mise à jour automatique des timestamps

using Microsoft.EntityFrameworkCore;
using ProSocialApi.Data.Entities;

namespace ProSocialApi.Data.Context;

/// <summary>
/// Contexte de base de données Entity Framework Core pour MySQL.
/// Gère toutes les opérations CRUD sur les entités relationnelles :
/// Users, Connections, Posts, Comments, Likes.
/// </summary>
public class ApplicationDbContext : DbContext
{
    // CONSTRUCTEUR
    /// <summary>
    /// Constructeur avec injection des options de configuration.
    /// Les options (chaîne de connexion, provider MySQL) sont configurées dans Program.cs
    /// et injectées automatiquement par le container DI.
    /// </summary>
    /// <param name="options">Options de configuration du DbContext</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DBSETS - Tables de la base de données
    // Chaque DbSet<T> représente une table en base de données.
    // Permet d'effectuer des opérations LINQ qui sont traduites en SQL.
    // Exemple : context.Users.Where(u => u.Email == "test@test.com")
    /// <summary>
    /// Table des utilisateurs - Contient tous les profils utilisateurs.
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Table des connexions - Relations entre utilisateurs (demandes envoyées/reçues).
    /// </summary>
    public DbSet<Connection> Connections { get; set; }

    /// <summary>
    /// Table des posts - Publications créées par les utilisateurs.
    /// </summary>
    public DbSet<Post> Posts { get; set; }

    /// <summary>
    /// Table des commentaires - Commentaires sur les posts.
    /// </summary>
    public DbSet<Comment> Comments { get; set; }

    /// <summary>
    /// Table des likes - "J'aime" donnés aux posts.
    /// </summary>
    public DbSet<Like> Likes { get; set; }

    // CONFIGURATION DU MODÈLE (Fluent API)
    // OnModelCreating est appelé lors de la création du modèle EF Core.
    // On y configure les relations, index, et contraintes qui ne peuvent
    // pas être exprimées via les Data Annotations dans les entités.
    /// <summary>
    /// Configure le modèle de données avec la Fluent API.
    /// Définit les relations, index, et contraintes pour toutes les entités.
    /// </summary>
    /// <param name="modelBuilder">Builder pour configurer le modèle</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Appelle la configuration de base (important pour certains providers)
        base.OnModelCreating(modelBuilder);
        // CONFIGURATION DE L'ENTITÉ USER
        modelBuilder.Entity<User>(entity =>
        {
            // Index unique sur l'email - Un email ne peut être utilisé qu'une fois
            // Cela garantit qu'on ne peut pas créer deux comptes avec le même email
            entity.HasIndex(e => e.Email).IsUnique();
        });
        // CONFIGURATION DE L'ENTITÉ CONNECTION
        // Les connexions sont une relation many-to-many auto-référencée sur User.
        // Un utilisateur peut avoir plusieurs connexions envoyées ET reçues.
        modelBuilder.Entity<Connection>(entity =>
        {
            // Index composite unique : une seule demande possible entre deux utilisateurs
            // Empêche d'envoyer plusieurs fois une demande à la même personne
            entity.HasIndex(e => new { e.RequesterId, e.AddresseeId }).IsUnique();

            // Relation : Connection.Requester -> User (celui qui envoie la demande)
            // Un User peut avoir plusieurs SentConnections (demandes envoyées)
            // OnDelete Cascade : si l'utilisateur est supprimé, ses demandes aussi
            entity.HasOne(c => c.Requester)
                .WithMany(u => u.SentConnections)
                .HasForeignKey(c => c.RequesterId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relation : Connection.Addressee -> User (celui qui reçoit la demande)
            // Un User peut avoir plusieurs ReceivedConnections (demandes reçues)
            entity.HasOne(c => c.Addressee)
                .WithMany(u => u.ReceivedConnections)
                .HasForeignKey(c => c.AddresseeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Stocke l'enum ConnectionStatus comme string en base de données
            // Plus lisible dans la BDD : "Pending", "Accepted", "Rejected"
            // Au lieu de : 0, 1, 2
            entity.Property(e => e.Status)
                .HasConversion<string>();
        });
        // CONFIGURATION DE L'ENTITÉ POST
        modelBuilder.Entity<Post>(entity =>
        {
            // Relation : Post.Author -> User (l'auteur du post)
            // Un User peut avoir plusieurs Posts
            // Cascade : si l'utilisateur est supprimé, ses posts aussi
            entity.HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        // CONFIGURATION DE L'ENTITÉ COMMENT
        modelBuilder.Entity<Comment>(entity =>
        {
            // Relation : Comment.Author -> User (l'auteur du commentaire)
            // Un User peut avoir plusieurs Comments
            entity.HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relation : Comment.Post -> Post (le post commenté)
            // Un Post peut avoir plusieurs Comments
            // Cascade : si le post est supprimé, ses commentaires aussi
            entity.HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        // CONFIGURATION DE L'ENTITÉ LIKE
        modelBuilder.Entity<Like>(entity =>
        {
            // Index composite unique : un utilisateur ne peut liker qu'une fois un post
            // Empêche de liker plusieurs fois le même post
            entity.HasIndex(e => new { e.UserId, e.PostId }).IsUnique();

            // Relation : Like.User -> User (celui qui like)
            // Un User peut avoir plusieurs Likes (sur différents posts)
            entity.HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relation : Like.Post -> Post (le post liké)
            // Un Post peut avoir plusieurs Likes (de différents utilisateurs)
            entity.HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // Mise à jour automatique des timestamps
    // Ces méthodes interceptent les appels SaveChanges pour mettre à jour
    // automatiquement la propriété UpdatedAt des entités modifiées.
    /// <summary>
    /// Version synchrone de SaveChanges.
    /// Met à jour les timestamps avant de sauvegarder.
    /// </summary>
    /// <returns>Nombre d'entités modifiées</returns>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Version asynchrone de SaveChanges (recommandée pour les applications web).
    /// Met à jour les timestamps avant de sauvegarder.
    /// </summary>
    /// <param name="cancellationToken">Token d'annulation</param>
    /// <returns>Nombre d'entités modifiées</returns>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Met à jour automatiquement les propriétés UpdatedAt des entités modifiées.
    /// Utilise le ChangeTracker d'EF Core pour détecter les entités en état "Modified".
    /// </summary>
    private void UpdateTimestamps()
    {
        // Récupère toutes les entités marquées comme modifiées
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        // Pour chaque entité modifiée, met à jour son timestamp
        // On vérifie le type car toutes les entités n'ont pas forcément UpdatedAt
        foreach (var entry in entries)
        {
            if (entry.Entity is User user)
                user.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is Post post)
                post.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is Comment comment)
                comment.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is Connection connection)
                connection.UpdatedAt = DateTime.UtcNow;
            // Note : Like n'a pas de UpdatedAt car un like ne se modifie pas
        }
    }
}
