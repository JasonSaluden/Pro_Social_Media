using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProSocialApi.Data.Context;
using ProSocialApi.Data.Entities;
using ProSocialApi.DTOs.Auth;
using ProSocialApi.Services;

namespace ProSocialApi.Tests.Services;

/// <summary>
/// Tests unitaires pour AuthService.
/// Utilise une base de données InMemory pour simuler les opérations de base de données.
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthServiceTests()
    {
        // Configurer la base de données InMemory avec un nom unique pour chaque test
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        // Configurer les paramètres JWT pour les tests
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Secret", "TestSecretKeyForUnitTestingPurposesOnly12345!" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "Jwt:ExpiresInDays", "7" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _authService = new AuthService(_context, _configuration);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldReturnSuccessWithToken()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Password123",
            ConfirmPassword = "Password123",
            FirstName = "Jean",
            LastName = "Dupont",
            Headline = "Développeur .NET"
        };

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Inscription réussie");
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be("test@example.com");
        result.User.FirstName.Should().Be("Jean");
        result.User.LastName.Should().Be("Dupont");
        result.User.Headline.Should().Be("Développeur .NET");

        // Vérifier que l'utilisateur a bien été créé en base
        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        userInDb.Should().NotBeNull();
        userInDb!.Password.Should().NotBe("Password123"); // Le mot de passe doit être hashé
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldReturnFailure()
    {
        // Arrange - Créer un utilisateur existant
        var existingUser = new User
        {
            Email = "existing@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            FirstName = "Existing",
            LastName = "User"
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var registerDto = new RegisterDto
        {
            Email = "existing@example.com",
            Password = "Password123",
            ConfirmPassword = "Password123",
            FirstName = "Jean",
            LastName = "Dupont"
        };

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Cet email est déjà utilisé");
        result.Token.Should().BeNull();
        result.User.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithDifferentCaseEmail_ShouldReturnFailure()
    {
        // Arrange - Créer un utilisateur existant avec email en minuscules
        var existingUser = new User
        {
            Email = "test@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            FirstName = "Existing",
            LastName = "User"
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        // Essayer de s'inscrire avec le même email mais en majuscules
        var registerDto = new RegisterDto
        {
            Email = "TEST@EXAMPLE.COM",
            Password = "Password123",
            ConfirmPassword = "Password123",
            FirstName = "Jean",
            LastName = "Dupont"
        };

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Cet email est déjà utilisé");
    }

    [Fact]
    public async Task RegisterAsync_ShouldStoreEmailInLowerCase()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "TEST@EXAMPLE.COM",
            Password = "Password123",
            ConfirmPassword = "Password123",
            FirstName = "Jean",
            LastName = "Dupont"
        };

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Success.Should().BeTrue();
        result.User!.Email.Should().Be("test@example.com");

        var userInDb = await _context.Users.FirstAsync();
        userInDb.Email.Should().Be("test@example.com");
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnSuccessWithToken()
    {
        // Arrange - Créer un utilisateur
        var password = "Password123";
        var user = new User
        {
            Email = "test@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            FirstName = "Jean",
            LastName = "Dupont",
            Headline = "Développeur .NET"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = password
        };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Connexion réussie");
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be("test@example.com");
        result.User.FirstName.Should().Be("Jean");
        result.User.LastName.Should().Be("Dupont");
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ShouldReturnFailure()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "Password123"
        };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Email ou mot de passe incorrect");
        result.Token.Should().BeNull();
        result.User.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldReturnFailure()
    {
        // Arrange - Créer un utilisateur
        var user = new User
        {
            Email = "test@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            FirstName = "Jean",
            LastName = "Dupont"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Email ou mot de passe incorrect");
        result.Token.Should().BeNull();
        result.User.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithDifferentCaseEmail_ShouldSucceed()
    {
        // Arrange - Créer un utilisateur avec email en minuscules
        var password = "Password123";
        var user = new User
        {
            Email = "test@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            FirstName = "Jean",
            LastName = "Dupont"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Se connecter avec le même email mais en majuscules
        var loginDto = new LoginDto
        {
            Email = "TEST@EXAMPLE.COM",
            Password = password
        };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Connexion réussie");
    }

    #endregion

    #region JWT Token Tests

    [Fact]
    public async Task RegisterAsync_ShouldGenerateValidJwtToken()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Password123",
            ConfirmPassword = "Password123",
            FirstName = "Jean",
            LastName = "Dupont"
        };

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Token.Should().NotBeNullOrEmpty();
        // Un token JWT valide commence par "eyJ" (encodage base64 de "{")
        result.Token.Should().StartWith("eyJ");
        // Un token JWT a 3 parties séparées par des points
        result.Token!.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public async Task LoginAsync_ShouldGenerateValidJwtToken()
    {
        // Arrange
        var password = "Password123";
        var user = new User
        {
            Email = "test@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            FirstName = "Jean",
            LastName = "Dupont"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = password
        };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Token.Should().NotBeNullOrEmpty();
        result.Token.Should().StartWith("eyJ");
        result.Token!.Split('.').Should().HaveCount(3);
    }

    #endregion
}
