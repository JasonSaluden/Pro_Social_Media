// PROGRAM.CS - Point d'entrée de l'application ASP.NET Core
// Ce fichier configure l'ensemble de l'application :
// - Les services (Dependency Injection)
// - L'authentification JWT
// - La base de données (MySQL via Entity Framework Core)
// - Le pipeline HTTP (middleware)
// - Swagger pour la documentation de l'API

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProSocialApi.Data;
using ProSocialApi.Data.Context;
using ProSocialApi.Services;
using ProSocialApi.Services.Interfaces;

// CONFIGURATION CRITIQUE : Désactivation du mapping automatique des claims 
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Initialise l'app
var builder = WebApplication.CreateBuilder(args);

// CONFIGURATION DES SERVICES 
var mysqlConnectionString = builder.Configuration.GetConnectionString("MySQL");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(mysqlConnectionString, ServerVersion.AutoDetect(mysqlConnectionString)));

// Configuration de MongoDB
builder.Services.AddSingleton<MongoDbContext>();

// Autres services
builder.Services.AddScoped<IAuthService, AuthService>();                // Authentification (login, register)
builder.Services.AddScoped<IUserService, UserService>();                // Gestion des profils utilisateurs
builder.Services.AddScoped<IConnectionService, ConnectionService>();    // Gestion des connexions entre utilisateurs
builder.Services.AddScoped<IPostService, PostService>();                // Gestion des publications
builder.Services.AddScoped<ICommentService, CommentService>();          // Gestion des commentaires
builder.Services.AddScoped<IMessageService, MessageService>();          // Messagerie (MongoDB)
builder.Services.AddScoped<IFeedService, FeedService>();                // Fil d'actualité personnalisé


// CONFIGURATION DE L'AUTHENTIFICATION JWT (JSON Web Token)

// Récupération de la clé secrète depuis la configuration
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    // Définit JWT Bearer comme schéma d'authentification par défaut (token généré)
    // Toutes les routes [Authorize] utilisent ce schéma
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Configuration des paramètres de validation du token
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Pour la prod (sécurité supplémentaire)
        ValidateIssuer = false,
        ValidateAudience = false,

        // Vérifie que le token n'est pas expiré
        ValidateLifetime = true,

        // Vérifie la signature du token 
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),

        // Tolérance pour les différences d'horloge entre serveurs
        ClockSkew = TimeSpan.Zero
    };

    // Événement pour logger dans le terminal
    options.Events = new JwtBearerEvents
    {
        // Appelé quand la validation du token échoue
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Erreur d'authentification : {context.Exception.Message}");
            return Task.CompletedTask;
        },
        // Appelé quand le token est validé avec succès
        OnTokenValidated = context =>
        {
            Console.WriteLine($"Token validé pour : {context.Principal?.Identity?.Name}");
            return Task.CompletedTask;
        }
    };
});

// Service permettant l'utilisation des attributs [Authorize] sur les controllers
builder.Services.AddAuthorization();

// Configuration des Controllers + Vues MVC
builder.Services.AddControllersWithViews();

// CONFIGURATION DE SWAGGER / OPENAPI
// Permet de tester les endpoints directement depuis le navigateur
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Informations générales sur l'API
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pro Social API",
        Version = "v1",
        Description = "API pour un faux linkedIn"
    });

    // Configuration de l'authentification JWT dans Swagger
    // Permet d'ajouter le token Bearer directement dans l'interface Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",           // Nom du header HTTP
        Type = SecuritySchemeType.Http,   // Type de sécurité HTTP
        Scheme = "Bearer",                // Schéma d'authentification
        BearerFormat = "JWT",             // Format du token
        In = ParameterLocation.Header,    // Le token est dans le header
        Description = "Entrez votre token JWT. Exemple: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    });

    // Applique la sécurité JWT à tous les endpoints
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>() // Pas de scopes spécifiques requis
        }
    });
});

// CONFIGURATION CORS (Cross-Origin Resource Sharing)
// Contrôle quelles origines (domaines) peuvent accéder à l'API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   // Accepte toutes les origines
              .AllowAnyMethod()   // Accepte toutes les méthodes HTTP (GET, POST, etc.)
              .AllowAnyHeader();  // Accepte tous les headers
    });
});

// CONSTRUCTION DE L'APPLICATION
var app = builder.Build();

// CONFIGURATION DU PIPELINE HTTP 
// Swagger UI (pour le dev)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Pro Social API v1");
        options.RoutePrefix = "swagger"; // Swagger accessible à /swagger
    });
}

// HTTPS Redirection - DÉSACTIVÉ sinon ça perd le header Authorization (probleme inconnu)
// app.UseHttpsRedirection();

// Activation de la politique CORS configurée plus haut
app.UseCors("AllowAll");

// Fichiers statiques (CSS, JS, images dans wwwroot/)
app.UseStaticFiles();

// Debug - Affiche les informations de chaque requête
// Affiche la méthode HTTP, le chemin, et le header Authorization (tronqué)
app.Use(async (context, next) =>
{
    // Récupère le header Authorization s'il existe
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

    // Log de la requête entrante
    Console.WriteLine($"Requete : {context.Request.Method} {context.Request.Path}");
    Console.WriteLine($"Auth Header: {(string.IsNullOrEmpty(authHeader) ? "ABSENT" : authHeader[..Math.Min(50, authHeader.Length)] + "...")}");

    // Passe au middleware suivant
    await next();

    // Log de la réponse
    Console.WriteLine($"Response: {context.Response.StatusCode}");
});

// Middlewares d'Authentification et d'Autorisation
// UseAuthentication : Valide le token JWT et crée le ClaimsPrincipal
// UseAuthorization : Vérifie les droits d'accès (attributs [Authorize])
// Ordre important
app.UseAuthentication();
app.UseAuthorization();

// Mapping des Controllers API
app.MapControllers();

// Route par défaut pour les vues MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Migration et création de la base de données avec des données de test
if (app.Environment.IsDevelopment())
{
    // Crée un scope
    using var scope = app.Services.CreateScope();

    // Récupère le DbContext
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Crée la base de données et les tables si nécessaire
    dbContext.Database.EnsureCreated();

    // Insère les données de test (utilisateurs, posts, etc.)
    await DataSeeder.SeedAsync(dbContext);
}

// Démarre l'application
app.Run();
