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
/// - 5 utilisateurs avec des profils professionnels variés
/// - 6 connexions (réseau social entre les utilisateurs)
/// - 6 posts avec du contenu tech/professionnel
/// - 6 commentaires sur les posts
/// - 15 likes distribués sur les posts
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
            // Utilisateur 1 : Alice - Développeuse Full Stack
            new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "alice.dupont@email.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FirstName = "Alice",
                LastName = "Dupont",
                Headline = "Développeuse Full Stack | React & .NET",
                Bio = "Passionnée par le développement web et les nouvelles technologies. 10 ans d'expérience dans le secteur IT.",
                AvatarUrl = "https://i.pravatar.cc/150?u=alice"  // Service d'avatars placeholder
            },
            // Utilisateur 2 : Bob - Chef de Projet
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "bob.martin@email.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FirstName = "Bob",
                LastName = "Martin",
                Headline = "Chef de Projet Digital",
                Bio = "Expert en gestion de projet agile. Scrum Master certifié.",
                AvatarUrl = "https://i.pravatar.cc/150?u=bob"
            },
            // Utilisateur 3 : Claire - Designer UX/UI
            new User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Email = "claire.bernard@email.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FirstName = "Claire",
                LastName = "Bernard",
                Headline = "UX/UI Designer | Figma Expert",
                Bio = "Je crée des expériences utilisateur mémorables. Design thinking enthusiast.",
                AvatarUrl = "https://i.pravatar.cc/150?u=claire"
            },
            // Utilisateur 4 : David - DevOps Engineer
            new User
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Email = "david.petit@email.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FirstName = "David",
                LastName = "Petit",
                Headline = "DevOps Engineer | AWS & Kubernetes",
                Bio = "Automatisation, CI/CD, et infrastructure as code. Cloud native advocate.",
                AvatarUrl = "https://i.pravatar.cc/150?u=david"
            },
            // Utilisateur 5 : Emma - Data Scientist
            new User
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Email = "emma.leroy@email.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FirstName = "Emma",
                LastName = "Leroy",
                Headline = "Data Scientist | Python & Machine Learning",
                Bio = "Transforming data into insights. PhD en Intelligence Artificielle.",
                AvatarUrl = "https://i.pravatar.cc/150?u=emma"
            }
        };

        // Insertion en base avec sauvegarde
        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
        Console.WriteLine($"{users.Count} utilisateurs créés");
        // ÉTAPE 2 : CRÉATION DES CONNEXIONS (RÉSEAU SOCIAL)
        // Graphe de connexions :
        // - Alice <-> Bob (Accepted)
        // - Alice <-> Claire (Accepted)
        // - Bob <-> David (Accepted)
        // - Claire <-> Emma (Accepted)
        // - David -> Alice (Pending - demande en attente)
        // - Emma <-> Bob (Accepted)
        //
        // Cela crée un petit réseau interconnecté pour tester le feed

        var connections = new List<Connection>
        {
            // Alice est connectée avec Bob et Claire (connexions acceptées)
            new Connection
            {
                RequesterId = users[0].Id, // Alice a envoyé la demande
                AddresseeId = users[1].Id, // Bob a accepté
                Status = ConnectionStatus.Accepted
            },
            new Connection
            {
                RequesterId = users[0].Id, // Alice
                AddresseeId = users[2].Id, // Claire
                Status = ConnectionStatus.Accepted
            },
            // Bob est connecté avec David
            new Connection
            {
                RequesterId = users[1].Id, // Bob
                AddresseeId = users[3].Id, // David
                Status = ConnectionStatus.Accepted
            },
            // Claire est connectée avec Emma
            new Connection
            {
                RequesterId = users[2].Id, // Claire
                AddresseeId = users[4].Id, // Emma
                Status = ConnectionStatus.Accepted
            },
            // Demande en attente : David veut se connecter avec Alice
            // Utile pour tester l'endpoint GET /api/connections/pending
            new Connection
            {
                RequesterId = users[3].Id, // David a envoyé
                AddresseeId = users[0].Id, // Alice doit accepter/refuser
                Status = ConnectionStatus.Pending
            },
            // Emma est connectée avec Bob
            new Connection
            {
                RequesterId = users[4].Id, // Emma
                AddresseeId = users[1].Id, // Bob
                Status = ConnectionStatus.Accepted
            }
        };

        await context.Connections.AddRangeAsync(connections);
        await context.SaveChangesAsync();
        Console.WriteLine($"{connections.Count} connexions créées");
        // ÉTAPE 3 : CRÉATION DES POSTS
        // Posts avec du contenu tech/professionnel réaliste
        // Les dates sont décalées dans le passé pour simuler de l'activité
        // Les GUIDs fixes permettent de référencer les posts facilement

        var posts = new List<Post>
        {
            // Post 1 : Alice parle de sa formation .NET 8
            new Post
            {
                Id = Guid.Parse("aaaa1111-1111-1111-1111-111111111111"),
                AuthorId = users[0].Id, // Alice
                Content = "Ravie d'annoncer que je viens de terminer une formation avancée sur .NET 8 ! Les nouvelles fonctionnalités de performance sont impressionnantes. Qui d'autre a testé les nouveaux features ?",
                CreatedAt = DateTime.UtcNow.AddDays(-5)  // Il y a 5 jours
            },
            // Post 2 : Alice partage une astuce technique
            new Post
            {
                Id = Guid.Parse("aaaa2222-2222-2222-2222-222222222222"),
                AuthorId = users[0].Id, // Alice
                Content = "Astuce du jour : Utilisez les records en C# pour vos DTOs. Immutabilité + moins de code = moins de bugs !",
                CreatedAt = DateTime.UtcNow.AddDays(-2)  // Il y a 2 jours
            },
            // Post 3 : Bob parle de son équipe agile
            new Post
            {
                Id = Guid.Parse("bbbb1111-1111-1111-1111-111111111111"),
                AuthorId = users[1].Id, // Bob
                Content = "Notre équipe vient de terminer un sprint record ! 47 story points livrés. La clé ? Une bonne communication et des daily meetings efficaces de 15 min max.",
                CreatedAt = DateTime.UtcNow.AddDays(-4)  // Il y a 4 jours
            },
            // Post 4 : Claire présente son nouveau projet UX
            new Post
            {
                Id = Guid.Parse("cccc1111-1111-1111-1111-111111111111"),
                AuthorId = users[2].Id, // Claire
                Content = "Nouveau projet en cours : refonte complète de l'UX d'une app bancaire. Le challenge ? Simplifier des workflows complexes tout en respectant les contraintes réglementaires.",
                CreatedAt = DateTime.UtcNow.AddDays(-3)  // Il y a 3 jours
            },
            // Post 5 : David parle de migration Kubernetes
            new Post
            {
                Id = Guid.Parse("dddd1111-1111-1111-1111-111111111111"),
                AuthorId = users[3].Id, // David
                Content = "Migration réussie vers Kubernetes ! 200 microservices, zéro downtime. Si vous avez des questions sur K8s, n'hésitez pas à me contacter.",
                CreatedAt = DateTime.UtcNow.AddDays(-1)  // Hier
            },
            // Post 6 : Emma parle de son modèle ML
            new Post
            {
                Id = Guid.Parse("eeee1111-1111-1111-1111-111111111111"),
                AuthorId = users[4].Id, // Emma
                Content = "Notre nouveau modèle de ML atteint 94% de précision sur la détection de fraude. Le secret ? Feature engineering + XGBoost + beaucoup de café ☕",
                CreatedAt = DateTime.UtcNow.AddHours(-12)  // Il y a 12 heures
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
            // Commentaires sur le post d'Alice (.NET 8) - posts[0]
            new Comment
            {
                PostId = posts[0].Id,
                AuthorId = users[1].Id, // Bob commente
                Content = "Félicitations Alice ! J'ai hâte de voir tes projets avec .NET 8.",
                CreatedAt = DateTime.UtcNow.AddDays(-5).AddHours(2)  // 2h après le post
            },
            new Comment
            {
                PostId = posts[0].Id,
                AuthorId = users[2].Id, // Claire commente
                Content = "Super ! Tu pourras nous faire un retour d'expérience ?",
                CreatedAt = DateTime.UtcNow.AddDays(-5).AddHours(4)  // 4h après le post
            },
            // Commentaire sur le post de Bob (sprint) - posts[2]
            new Comment
            {
                PostId = posts[2].Id,
                AuthorId = users[0].Id, // Alice commente
                Content = "Bravo à toute l'équipe ! 47 points c'est impressionnant 💪",
                CreatedAt = DateTime.UtcNow.AddDays(-4).AddHours(3)
            },
            // Commentaires sur le post de David (K8s) - posts[4]
            // Simulation d'une conversation (question -> réponse)
            new Comment
            {
                PostId = posts[4].Id,
                AuthorId = users[1].Id, // Bob pose une question
                Content = "200 microservices ! Vous utilisez quel service mesh ?",
                CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(2)
            },
            new Comment
            {
                PostId = posts[4].Id,
                AuthorId = users[3].Id, // David répond (auteur du post)
                Content = "@Bob On utilise Istio, ça fonctionne très bien pour notre use case.",
                CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(3)
            },
            // Commentaire sur le post d'Emma (ML) - posts[5]
            new Comment
            {
                PostId = posts[5].Id,
                AuthorId = users[0].Id, // Alice commente
                Content = "94% c'est excellent ! Vous avez essayé les transformers aussi ?",
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
            // Likes sur le post d'Alice (.NET 8) - 3 likes
            new Like { PostId = posts[0].Id, UserId = users[1].Id }, // Bob like
            new Like { PostId = posts[0].Id, UserId = users[2].Id }, // Claire like
            new Like { PostId = posts[0].Id, UserId = users[3].Id }, // David like

            // Likes sur le post d'Alice (astuce) - 2 likes
            new Like { PostId = posts[1].Id, UserId = users[1].Id }, // Bob like
            new Like { PostId = posts[1].Id, UserId = users[4].Id }, // Emma like

            // Likes sur le post de Bob - 2 likes
            new Like { PostId = posts[2].Id, UserId = users[0].Id }, // Alice like
            new Like { PostId = posts[2].Id, UserId = users[3].Id }, // David like

            // Likes sur le post de Claire - 2 likes
            new Like { PostId = posts[3].Id, UserId = users[0].Id }, // Alice like
            new Like { PostId = posts[3].Id, UserId = users[4].Id }, // Emma like

            // Likes sur le post de David - 3 likes (post populaire)
            new Like { PostId = posts[4].Id, UserId = users[0].Id }, // Alice like
            new Like { PostId = posts[4].Id, UserId = users[1].Id }, // Bob like
            new Like { PostId = posts[4].Id, UserId = users[4].Id }, // Emma like

            // Likes sur le post d'Emma - 4 likes (post le plus populaire)
            new Like { PostId = posts[5].Id, UserId = users[0].Id }, // Alice like
            new Like { PostId = posts[5].Id, UserId = users[1].Id }, // Bob like
            new Like { PostId = posts[5].Id, UserId = users[2].Id }, // Claire like
            new Like { PostId = posts[5].Id, UserId = users[3].Id }  // David like
        };

        await context.Likes.AddRangeAsync(likes);
        await context.SaveChangesAsync();
        Console.WriteLine($"{likes.Count} likes créés");
        // RÉSUMÉ FINAL
        // Affiche les comptes disponibles pour faciliter les tests

        Console.WriteLine("Seed terminé avec succès !");
        Console.WriteLine("\nComptes de test disponibles (mot de passe: Password123) :");
        Console.WriteLine("   - alice.dupont@email.com");
        Console.WriteLine("   - bob.martin@email.com");
        Console.WriteLine("   - claire.bernard@email.com");
        Console.WriteLine("   - david.petit@email.com");
        Console.WriteLine("   - emma.leroy@email.com");
    }
}
