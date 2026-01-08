# Architecture - Réseau Social Professionnel (C# / ASP.NET Core)

## Stack Technique

| Composant | Technologie | Justification |
|-----------|-------------|---------------|
| Framework | ASP.NET Core 9 | Performance, typage fort natif |
| BDD Relationnelle | MySQL (XAMPP) | Données relationnelles, intégrité référentielle |
| ORM SQL | Entity Framework Core + Pomelo | Migrations, LINQ, requêtes type-safe |
| BDD Documents | MongoDB | Données flexibles, messagerie, notifications |
| Driver MongoDB | MongoDB.Driver | Driver officiel .NET |
| Authentification | JWT Bearer | Stateless, sécurisé, standard industrie |
| Hash mot de passe | BCrypt.Net | Algorithme sécurisé |
| Documentation | Swagger / Swashbuckle | Auto-générée depuis les attributs |

---

## Structure du Projet

```
ProSocialApi/
├── Controllers/                    # Contrôleurs API
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── ConnectionsController.cs
│   ├── PostsController.cs
│   ├── CommentsController.cs
│   ├── LikesController.cs
│   ├── MessagesController.cs
│   ├── NotificationsController.cs
│   └── FeedController.cs
│
├── Data/
│   ├── Entities/                   # Entités Entity Framework (MySQL)
│   │   ├── User.cs
│   │   ├── Connection.cs
│   │   ├── Post.cs
│   │   ├── Comment.cs
│   │   └── Like.cs
│   │
│   ├── MongoModels/                # Modèles MongoDB
│   │   ├── Conversation.cs
│   │   └── Notification.cs
│   │
│   └── Context/
│       ├── ApplicationDbContext.cs  # DbContext EF Core
│       └── MongoDbContext.cs        # Context MongoDB
│
├── DTOs/                           # Data Transfer Objects
│   ├── Auth/
│   │   ├── LoginDto.cs
│   │   ├── RegisterDto.cs
│   │   └── AuthResponseDto.cs
│   ├── Users/
│   │   ├── UserDto.cs
│   │   └── UpdateUserDto.cs
│   ├── Posts/
│   │   ├── CreatePostDto.cs
│   │   └── PostDto.cs
│   ├── Comments/
│   │   └── CreateCommentDto.cs
│   ├── Connections/
│   │   └── ConnectionDto.cs
│   └── Messages/
│       ├── CreateConversationDto.cs
│       └── SendMessageDto.cs
│
├── Services/                       # Logique métier
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   ├── IUserService.cs
│   │   └── ...
│   ├── AuthService.cs
│   ├── UserService.cs
│   └── ...
│
├── appsettings.json               # Configuration
├── appsettings.Development.json
└── Program.cs                     # Point d'entrée + DI
```

---

## Modèle de Données

### MySQL (Entity Framework Core) - Données Relationnelles

```
┌─────────────────┐       ┌─────────────────┐
│      User       │       │   Connection    │
├─────────────────┤       ├─────────────────┤
│ Id (Guid)       │◄──────┤ RequesterId     │
│ Email           │◄──────┤ AddresseeId     │
│ Password        │       │ Status          │
│ FirstName       │       │ CreatedAt       │
│ LastName        │       └─────────────────┘
│ Headline        │
│ Bio             │       ┌─────────────────┐
│ AvatarUrl       │       │      Post       │
│ CreatedAt       │       ├─────────────────┤
│ UpdatedAt       │◄──────┤ AuthorId        │
└─────────────────┘       │ Content         │
        │                 │ ImageUrl        │
        │                 │ CreatedAt       │
        │                 └────────┬────────┘
        │                          │
        │                 ┌────────▼────────┐
        │                 │    Comment      │
        │                 ├─────────────────┤
        └────────────────►│ AuthorId        │
                          │ PostId          │
                          │ Content         │
                          └─────────────────┘

        ┌─────────────────┐
        │      Like       │
        ├─────────────────┤
        │ UserId          │ (Unique: UserId + PostId)
        │ PostId          │
        │ CreatedAt       │
        └─────────────────┘
```

### MongoDB - Documents Flexibles

```csharp
// Collection: conversations
{
    "_id": ObjectId,
    "participants": ["userId1", "userId2"],
    "messages": [
        {
            "senderId": "userId1",
            "content": "Salut !",
            "sentAt": ISODate,
            "readAt": null
        }
    ],
    "lastMessageAt": ISODate,
    "createdAt": ISODate
}

// Collection: notifications
{
    "_id": ObjectId,
    "userId": "userId",
    "type": "ConnectionRequest",
    "data": {
        "fromUserId": "...",
        "fromUserName": "Jean Dupont"
    },
    "read": false,
    "message": "Jean Dupont vous a envoyé une demande de connexion",
    "createdAt": ISODate
}
```

---

## Endpoints API

### Auth
| Méthode | Endpoint | Description |
|---------|----------|-------------|
| POST | `/api/auth/register` | Inscription |
| POST | `/api/auth/login` | Connexion |

### Users
| Méthode | Endpoint | Description |
|---------|----------|-------------|
| GET | `/api/users/{id}` | Consulter un profil |
| PUT | `/api/users/{id}` | Mettre à jour son profil |
| GET | `/api/users/search?q=` | Rechercher des utilisateurs |

### Connections
| Méthode | Endpoint | Description |
|---------|----------|-------------|
| POST | `/api/connections/request/{userId}` | Envoyer une demande |
| PUT | `/api/connections/{id}/accept` | Accepter |
| PUT | `/api/connections/{id}/reject` | Refuser |
| GET | `/api/connections` | Lister ses connexions |
| GET | `/api/connections/pending` | Demandes en attente |

### Posts
| Méthode | Endpoint | Description |
|---------|----------|-------------|
| POST | `/api/posts` | Créer un post |
| GET | `/api/posts/{id}` | Voir un post |
| PUT | `/api/posts/{id}` | Modifier |
| DELETE | `/api/posts/{id}` | Supprimer |

### Comments
| Méthode | Endpoint | Description |
|---------|----------|-------------|
| POST | `/api/posts/{postId}/comments` | Commenter |
| GET | `/api/posts/{postId}/comments` | Voir les commentaires |
| DELETE | `/api/comments/{id}` | Supprimer |

### Likes
| Méthode | Endpoint | Description |
|---------|----------|-------------|
| POST | `/api/posts/{postId}/like` | Liker |
| DELETE | `/api/posts/{postId}/like` | Retirer son like |

### Messages
| Méthode | Endpoint | Description |
|---------|----------|-------------|
| POST | `/api/conversations` | Créer une conversation |
| GET | `/api/conversations` | Lister ses conversations |
| GET | `/api/conversations/{id}` | Voir une conversation |
| POST | `/api/conversations/{id}/messages` | Envoyer un message |

### Feed
| Méthode | Endpoint | Description |
|---------|----------|-------------|
| GET | `/api/feed` | Fil d'actualité |

### Notifications
| Méthode | Endpoint | Description |
|---------|----------|-------------|
| GET | `/api/notifications` | Voir ses notifications |
| PUT | `/api/notifications/{id}/read` | Marquer comme lue |

---

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "MySQL": "Server=localhost;Port=3306;Database=pro_social_db;User=root;Password=;",
    "MongoDB": "mongodb://localhost:27017"
  },
  "MongoDB": {
    "DatabaseName": "pro_social_db"
  },
  "Jwt": {
    "Secret": "VotreCleSecrete...",
    "Issuer": "ProSocialApi",
    "Audience": "ProSocialApiUsers",
    "ExpiresInDays": 7
  }
}
```

---

## Commandes Utiles

```bash
# Démarrer le serveur
dotnet run

# Démarrer en mode watch (rechargement auto)
dotnet watch run

# Créer une migration
dotnet ef migrations add NomMigration

# Appliquer les migrations
dotnet ef database update

# Build release
dotnet build -c Release
```

---

## Prérequis

1. **.NET 9 SDK** installé
2. **XAMPP** démarré (MySQL sur port 3306)
3. **MongoDB** démarré (port 27017)
4. Créer la base de données MySQL : `pro_social_db`

---

## Démarrage rapide

```bash
# 1. Aller dans le dossier du projet
cd ProSocialApi

# 2. Démarrer l'API
dotnet run

# 3. Ouvrir Swagger
# http://localhost:5000 (ou le port affiché)
```
