// DATASEEDER.CS - Classe de peuplement initial de la base de données
// Cette classe génère des données de test réalistes pour le développement.
// Elle crée des utilisateurs, connexions, posts, commentaires et likes
// pour simuler un réseau social actif.
//
// Utilisation : Appelée automatiquement au démarrage de l'application
// si la base de données est vide (voir Program.cs).

using Microsoft.EntityFrameworkCore;
using ProSocialApi.Data.Context;
using ProSocialApi.Data.Entities;

namespace ProSocialApi.Data;

/// <summary>
/// Classe statique pour le peuplement initial (seeding) de la base de données.
///
/// Objectif : Fournir un jeu de données cohérent pour le développement et les tests.
///
/// Données créées :
/// - 6 utilisateurs avec des profils professionnels variés
/// - 11 connexions (réseau social entre les utilisateurs)
/// - 7 posts avec du contenu varié
/// - 6 commentaires sur les posts
/// - 20 likes distribués sur les posts
///
/// Sécurité : Les mots de passe sont hashés avec BCrypt avant stockage.
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Peuple la base de données avec des données de test.
    ///
    /// Comportement idempotent :
    /// - Si des utilisateurs existent déjà, le seed est ignoré
    /// - Cela permet de relancer l'application sans dupliquer les données
    ///
    /// Ordre d'insertion (respecte les clés étrangères) :
    /// 1. Users (pas de dépendances)
    /// 2. Connections (dépend de Users)
    /// 3. Posts (dépend de Users)
    /// 4. Comments (dépend de Posts et Users)
    /// 5. Likes (dépend de Posts et Users)
    /// </summary>
    /// <param name="context">Le contexte Entity Framework pour accéder à la base</param>
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // VÉRIFICATION : Ne pas reseed si des données existent
        // Cette vérification rend le seed idempotent (peut être appelé plusieurs fois)
        if (await context.Users.AnyAsync())
        {
            Console.WriteLine("Base de données déjà peuplée, seed ignoré.");
            return;
        }

        Console.WriteLine("Démarrage du seed...");
        // ÉTAPE 1 : CRÉATION DES UTILISATEURS
        // 5 profils professionnels différents pour simuler un réseau diversifié
        // Les GUIDs sont fixes pour faciliter les tests et la reproductibilité
        // Les mots de passe sont tous "Password123" (hashés avec BCrypt)

        var users = new List<User>
        {
            // Utilisateur 1 : Gérard Larcher
            new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "gerard.larcher@email.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FirstName = "Gérard",
                LastName = "Larcher",
                Headline = "Fervent défenseur des privilèges",
                Bio = "J'aime bien la cantine",
                AvatarUrl = "https://i.pravatar.cc/150?u=gerard"
            },
            // Utilisateur 2 : François Bayrou
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "francois.bayrou@email.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FirstName = "François",
                LastName = "Bayrou",
                Headline = "Chef de nous",
                Bio = "Expert en conseil de gestion stratifié dans l'expansion lucrative",
                AvatarUrl = "https://i.pravatar.cc/150?u=francois"
            },
            // Utilisateur 3 : Yael Braun-Pivet
            new User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Email = "yael.braunpivet@email.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FirstName = "Yael",
                LastName = "Braun-Pivet",
                Headline = "Assistante sociale",
                Bio = "Je crée des expériences mémorables.",
                AvatarUrl = "https://i.pravatar.cc/150?u=yael"
            },
            // Utilisateur 4 : Noël Flantier
            new User
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Email = "noel.flantier@email.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FirstName = "Noël",
                LastName = "Flantier",
                Headline = "Reporter",
                Bio = "Je fais des photos. Sur un reportage brésilien",
                AvatarUrl = "https://i.pravatar.cc/150?u=noel"
            },
            // Utilisateur 5 : Dolores Koulechov
            new User
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Email = "dolores.koulechov@email.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FirstName = "Dolores",
                LastName = "Koulechov",
                Headline = "Lieutenant colonel de l'armée israélienne",
                Bio = "Je suis la secrétaire de qui ?",
                AvatarUrl = "https://i.pravatar.cc/150?u=dolores"
            },
            // Utilisateur 6 : Jason Saluden
            new User
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Email = "saluden.jason@gmail.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FirstName = "Jason",
                LastName = "Saluden",
                Headline = "Développeur Web Full Stack",
                Bio = "Passionné par le code, le café et les architectures cloud. Toujours en quête de nouvelles technologies à explorer.",
                AvatarUrl = "https://i.pravatar.cc/150?u=jason"
            }
        };

        // Insertion en base avec sauvegarde
        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
        Console.WriteLine($"{users.Count} utilisateurs créés");
        // ÉTAPE 2 : CRÉATION DES CONNEXIONS (RÉSEAU SOCIAL)
        // Graphe de connexions :
        // - Gérard <-> François (Accepted)
        // - Gérard <-> Yael (Accepted)
        // - François <-> Noël (Accepted)
        // - Yael <-> Dolores (Accepted)
        // - Noël -> Gérard (Pending - demande en attente)
        // - Dolores <-> François (Accepted)
        //
        // Cela crée un petit réseau interconnecté pour tester le feed

        var connections = new List<Connection>
        {
            // Gérard est connecté avec François et Yael (connexions acceptées)
            new Connection
            {
                RequesterId = users[0].Id, // Gérard a envoyé la demande
                AddresseeId = users[1].Id, // François a accepté
                Status = ConnectionStatus.Accepted
            },
            new Connection
            {
                RequesterId = users[0].Id, // Gérard
                AddresseeId = users[2].Id, // Yael
                Status = ConnectionStatus.Accepted
            },
            // François est connecté avec Noël
            new Connection
            {
                RequesterId = users[1].Id, // François
                AddresseeId = users[3].Id, // Noël
                Status = ConnectionStatus.Accepted
            },
            // Yael est connectée avec Dolores
            new Connection
            {
                RequesterId = users[2].Id, // Yael
                AddresseeId = users[4].Id, // Dolores
                Status = ConnectionStatus.Accepted
            },
            // Demande en attente : Noël veut se connecter avec Gérard
            // Utile pour tester l'endpoint GET /api/connections/pending
            new Connection
            {
                RequesterId = users[3].Id, // Noël a envoyé
                AddresseeId = users[0].Id, // Gérard doit accepter/refuser
                Status = ConnectionStatus.Pending
            },
            // Dolores est connectée avec François
            new Connection
            {
                RequesterId = users[4].Id, // Dolores
                AddresseeId = users[1].Id, // François
                Status = ConnectionStatus.Accepted
            },
            // Jason est connecté avec tout le monde (admin/dev)
            new Connection
            {
                RequesterId = users[5].Id, // Jason
                AddresseeId = users[0].Id, // Gérard
                Status = ConnectionStatus.Accepted
            },
            new Connection
            {
                RequesterId = users[5].Id, // Jason
                AddresseeId = users[1].Id, // François
                Status = ConnectionStatus.Accepted
            },
            new Connection
            {
                RequesterId = users[5].Id, // Jason
                AddresseeId = users[2].Id, // Yael
                Status = ConnectionStatus.Accepted
            },
            new Connection
            {
                RequesterId = users[5].Id, // Jason
                AddresseeId = users[3].Id, // Noël
                Status = ConnectionStatus.Accepted
            },
            new Connection
            {
                RequesterId = users[5].Id, // Jason
                AddresseeId = users[4].Id, // Dolores
                Status = ConnectionStatus.Accepted
            }
        };

        await context.Connections.AddRangeAsync(connections);
        await context.SaveChangesAsync();
        Console.WriteLine($"{connections.Count} connexions créées");
        // ÉTAPE 3 : CRÉATION DES POSTS
        // Posts avec du contenu varié
        // Les dates sont décalées dans le passé pour simuler de l'activité
        // Les GUIDs fixes permettent de référencer les posts facilement

        var posts = new List<Post>
        {
            // Post 1 : Gérard parle de la cantine
            new Post
            {
                Id = Guid.Parse("aaaa1111-1111-1111-1111-111111111111"),
                AuthorId = users[0].Id, // Gérard
                Content = "Excellent repas à la cantine aujourd'hui ! Le chef s'est surpassé. Qui d'autre apprécie les bons petits plats entre collègues ?",
                CreatedAt = DateTime.UtcNow.AddDays(-5)  // Il y a 5 jours
            },
            // Post 2 : Gérard partage une réflexion
            new Post
            {
                Id = Guid.Parse("aaaa2222-2222-2222-2222-222222222222"),
                AuthorId = users[0].Id, // Gérard
                Content = "Astuce du jour : Toujours défendre ses privilèges avec élégance. C'est un art qui se cultive !",
                CreatedAt = DateTime.UtcNow.AddDays(-2)  // Il y a 2 jours
            },
            // Post 3 : François parle de gestion
            new Post
            {
                Id = Guid.Parse("bbbb1111-1111-1111-1111-111111111111"),
                AuthorId = users[1].Id, // François
                Content = "Notre équipe vient de terminer une session de conseil stratifié ! L'expansion lucrative est en marche. La clé ? Une bonne communication et des réunions efficaces.",
                CreatedAt = DateTime.UtcNow.AddDays(-4)  // Il y a 4 jours
            },
            // Post 4 : Yael présente son travail social
            new Post
            {
                Id = Guid.Parse("cccc1111-1111-1111-1111-111111111111"),
                AuthorId = users[2].Id, // Yael
                Content = "Nouveau projet en cours : accompagnement de familles en difficulté. Le challenge ? Créer des expériences vraiment mémorables pour chacun.",
                CreatedAt = DateTime.UtcNow.AddDays(-3)  // Il y a 3 jours
            },
            // Post 5 : Noël parle de son reportage
            new Post
            {
                Id = Guid.Parse("dddd1111-1111-1111-1111-111111111111"),
                AuthorId = users[3].Id, // Noël
                Content = "Reportage brésilien terminé ! Des photos incroyables à partager bientôt. Si vous avez des questions sur le photojournalisme, n'hésitez pas !",
                CreatedAt = DateTime.UtcNow.AddDays(-1)  // Hier
            },
            // Post 6 : Dolores parle de son expérience
            new Post
            {
                Id = Guid.Parse("eeee1111-1111-1111-1111-111111111111"),
                AuthorId = users[4].Id, // Dolores
                Content = "Qui suis-je vraiment ? Lieutenant colonel ou secrétaire ? Les deux mon capitaine ! La polyvalence est ma force.",
                CreatedAt = DateTime.UtcNow.AddHours(-12)  // Il y a 12 heures
            },
            // Post 7 : Jason présente Pro Social
            new Post
            {
                Id = Guid.Parse("ffff1111-1111-1111-1111-111111111111"),
                AuthorId = users[5].Id, // Jason
                Content = "Bienvenue sur Pro Social ! Cette plateforme a été développée avec ASP.NET Core 9, Entity Framework, MongoDB et beaucoup de passion. N'hésitez pas à me faire vos retours !",
                CreatedAt = DateTime.UtcNow.AddHours(-6)  // Il y a 6 heures
            }
        };

        await context.Posts.AddRangeAsync(posts);
        await context.SaveChangesAsync();
        Console.WriteLine($"{posts.Count} posts créés");
        // ÉTAPE 4 : CRÉATION DES COMMENTAIRES
        // Interactions entre utilisateurs sur les posts
        // Les dates sont décalées pour être après la création des posts

        var comments = new List<Comment>
        {
            // Commentaires sur le post de Gérard (cantine) - posts[0]
            new Comment
            {
                PostId = posts[0].Id,
                AuthorId = users[1].Id, // François commente
                Content = "Félicitations Gérard ! La cantine c'est sacré, je suis bien d'accord.",
                CreatedAt = DateTime.UtcNow.AddDays(-5).AddHours(2)  // 2h après le post
            },
            new Comment
            {
                PostId = posts[0].Id,
                AuthorId = users[2].Id, // Yael commente
                Content = "Super ! Tu pourras nous recommander le menu du jour ?",
                CreatedAt = DateTime.UtcNow.AddDays(-5).AddHours(4)  // 4h après le post
            },
            // Commentaire sur le post de François (gestion) - posts[2]
            new Comment
            {
                PostId = posts[2].Id,
                AuthorId = users[0].Id, // Gérard commente
                Content = "Bravo à toute l'équipe ! L'expansion lucrative, j'adore le concept !",
                CreatedAt = DateTime.UtcNow.AddDays(-4).AddHours(3)
            },
            // Commentaires sur le post de Noël (reportage) - posts[4]
            // Simulation d'une conversation (question -> réponse)
            new Comment
            {
                PostId = posts[4].Id,
                AuthorId = users[1].Id, // François pose une question
                Content = "Le Brésil ! Vous avez utilisé quel matériel photo ?",
                CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(2)
            },
            new Comment
            {
                PostId = posts[4].Id,
                AuthorId = users[3].Id, // Noël répond (auteur du post)
                Content = "@François Un bon vieux Nikon, ça fonctionne très bien pour ce type de reportage.",
                CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(3)
            },
            // Commentaire sur le post de Dolores (polyvalence) - posts[5]
            new Comment
            {
                PostId = posts[5].Id,
                AuthorId = users[0].Id, // Gérard commente
                Content = "La polyvalence c'est excellent ! Vous gérez aussi la cantine ?",
                CreatedAt = DateTime.UtcNow.AddHours(-10)
            }
        };

        await context.Comments.AddRangeAsync(comments);
        await context.SaveChangesAsync();
        Console.WriteLine($"{comments.Count} commentaires créés");
        // ÉTAPE 5 : CRÉATION DES LIKES
        // Distribution réaliste des likes sur les posts
        // Certains posts sont plus populaires que d'autres

        var likes = new List<Like>
        {
            // Likes sur le post de Gérard (cantine) - 3 likes
            new Like { PostId = posts[0].Id, UserId = users[1].Id }, // François like
            new Like { PostId = posts[0].Id, UserId = users[2].Id }, // Yael like
            new Like { PostId = posts[0].Id, UserId = users[3].Id }, // Noël like

            // Likes sur le post de Gérard (privilèges) - 2 likes
            new Like { PostId = posts[1].Id, UserId = users[1].Id }, // François like
            new Like { PostId = posts[1].Id, UserId = users[4].Id }, // Dolores like

            // Likes sur le post de François - 2 likes
            new Like { PostId = posts[2].Id, UserId = users[0].Id }, // Gérard like
            new Like { PostId = posts[2].Id, UserId = users[3].Id }, // Noël like

            // Likes sur le post de Yael - 2 likes
            new Like { PostId = posts[3].Id, UserId = users[0].Id }, // Gérard like
            new Like { PostId = posts[3].Id, UserId = users[4].Id }, // Dolores like

            // Likes sur le post de Noël - 3 likes (post populaire)
            new Like { PostId = posts[4].Id, UserId = users[0].Id }, // Gérard like
            new Like { PostId = posts[4].Id, UserId = users[1].Id }, // François like
            new Like { PostId = posts[4].Id, UserId = users[4].Id }, // Dolores like

            // Likes sur le post de Dolores - 4 likes
            new Like { PostId = posts[5].Id, UserId = users[0].Id }, // Gérard like
            new Like { PostId = posts[5].Id, UserId = users[1].Id }, // François like
            new Like { PostId = posts[5].Id, UserId = users[2].Id }, // Yael like
            new Like { PostId = posts[5].Id, UserId = users[3].Id }, // Noël like

            // Likes sur le post de Jason - 5 likes (post le plus populaire)
            new Like { PostId = posts[6].Id, UserId = users[0].Id }, // Gérard like
            new Like { PostId = posts[6].Id, UserId = users[1].Id }, // François like
            new Like { PostId = posts[6].Id, UserId = users[2].Id }, // Yael like
            new Like { PostId = posts[6].Id, UserId = users[3].Id }, // Noël like
            new Like { PostId = posts[6].Id, UserId = users[4].Id }  // Dolores like
        };

        await context.Likes.AddRangeAsync(likes);
        await context.SaveChangesAsync();
        Console.WriteLine($"{likes.Count} likes créés");
        // RÉSUMÉ FINAL
        // Affiche les comptes disponibles pour faciliter les tests

        Console.WriteLine("Seed terminé avec succès !");
        Console.WriteLine("\nComptes de test disponibles (mot de passe: Password123) :");
        Console.WriteLine("   - gerard.larcher@email.com");
        Console.WriteLine("   - francois.bayrou@email.com");
        Console.WriteLine("   - yael.braunpivet@email.com");
        Console.WriteLine("   - noel.flantier@email.com");
        Console.WriteLine("   - dolores.koulechov@email.com");
        Console.WriteLine("   - saluden.jason@gmail.com");
    }
}
