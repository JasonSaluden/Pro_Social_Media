using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProSocialApi.Data.Context;
using ProSocialApi.Data.Entities;
using ProSocialApi.Services;

namespace ProSocialApi.Tests.Services;

/// <summary>
/// Tests unitaires pour ConnectionService.
/// Utilise une base de données InMemory pour simuler les opérations de base de données.
/// </summary>
public class ConnectionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ConnectionService _connectionService;

    public ConnectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _connectionService = new ConnectionService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Helper Methods

    private User CreateUser(string firstName = "Test", string lastName = "User", string? email = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email ?? $"{Guid.NewGuid()}@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            FirstName = firstName,
            LastName = lastName,
            Headline = "Développeur"
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    private Connection CreateConnection(Guid requesterId, Guid addresseeId, ConnectionStatus status = ConnectionStatus.Pending)
    {
        var connection = new Connection
        {
            Id = Guid.NewGuid(),
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = status
        };
        _context.Connections.Add(connection);
        _context.SaveChanges();
        return connection;
    }

    #endregion

    #region SendRequestAsync Tests

    [Fact]
    public async Task SendRequestAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var requester = CreateUser("Jean", "Dupont");
        var addressee = CreateUser("Marie", "Martin");

        // Act
        var (success, message) = await _connectionService.SendRequestAsync(requester.Id, addressee.Id);

        // Assert
        success.Should().BeTrue();
        message.Should().Be("Demande de connexion envoyée");

        var connection = await _context.Connections.FirstOrDefaultAsync();
        connection.Should().NotBeNull();
        connection!.RequesterId.Should().Be(requester.Id);
        connection.AddresseeId.Should().Be(addressee.Id);
        connection.Status.Should().Be(ConnectionStatus.Pending);
    }

    [Fact]
    public async Task SendRequestAsync_ToSelf_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var (success, message) = await _connectionService.SendRequestAsync(user.Id, user.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Vous ne pouvez pas vous envoyer une demande à vous-même");
    }

    [Fact]
    public async Task SendRequestAsync_ToNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var requester = CreateUser();
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var (success, message) = await _connectionService.SendRequestAsync(requester.Id, nonExistentUserId);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Utilisateur non trouvé");
    }

    [Fact]
    public async Task SendRequestAsync_WithExistingPendingRequest_ShouldReturnFailure()
    {
        // Arrange
        var requester = CreateUser("Jean", "Dupont");
        var addressee = CreateUser("Marie", "Martin");
        CreateConnection(requester.Id, addressee.Id, ConnectionStatus.Pending);

        // Act
        var (success, message) = await _connectionService.SendRequestAsync(requester.Id, addressee.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Une demande est déjà en attente");
    }

    [Fact]
    public async Task SendRequestAsync_WhenAlreadyConnected_ShouldReturnFailure()
    {
        // Arrange
        var requester = CreateUser("Jean", "Dupont");
        var addressee = CreateUser("Marie", "Martin");
        CreateConnection(requester.Id, addressee.Id, ConnectionStatus.Accepted);

        // Act
        var (success, message) = await _connectionService.SendRequestAsync(requester.Id, addressee.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Vous êtes déjà connectés");
    }

    [Fact]
    public async Task SendRequestAsync_WhenPreviouslyRejected_ShouldReturnFailure()
    {
        // Arrange
        var requester = CreateUser("Jean", "Dupont");
        var addressee = CreateUser("Marie", "Martin");
        CreateConnection(requester.Id, addressee.Id, ConnectionStatus.Rejected);

        // Act
        var (success, message) = await _connectionService.SendRequestAsync(requester.Id, addressee.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Cette demande a été refusée");
    }

    [Fact]
    public async Task SendRequestAsync_WhenReverseConnectionExists_ShouldReturnFailure()
    {
        // Arrange
        var userA = CreateUser("Jean", "Dupont");
        var userB = CreateUser("Marie", "Martin");
        // B a déjà envoyé une demande à A
        CreateConnection(userB.Id, userA.Id, ConnectionStatus.Pending);

        // Act - A essaie d'envoyer une demande à B
        var (success, message) = await _connectionService.SendRequestAsync(userA.Id, userB.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Une demande est déjà en attente");
    }

    #endregion

    #region AcceptRequestAsync Tests

    [Fact]
    public async Task AcceptRequestAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var requester = CreateUser("Jean", "Dupont");
        var addressee = CreateUser("Marie", "Martin");
        var connection = CreateConnection(requester.Id, addressee.Id, ConnectionStatus.Pending);

        // Act
        var (success, message) = await _connectionService.AcceptRequestAsync(connection.Id, addressee.Id);

        // Assert
        success.Should().BeTrue();
        message.Should().Be("Demande acceptée");

        var updatedConnection = await _context.Connections.FindAsync(connection.Id);
        updatedConnection!.Status.Should().Be(ConnectionStatus.Accepted);
    }

    [Fact]
    public async Task AcceptRequestAsync_WithNonExistentConnection_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var (success, message) = await _connectionService.AcceptRequestAsync(Guid.NewGuid(), user.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Demande non trouvée");
    }

    [Fact]
    public async Task AcceptRequestAsync_ByRequester_ShouldReturnFailure()
    {
        // Arrange
        var requester = CreateUser("Jean", "Dupont");
        var addressee = CreateUser("Marie", "Martin");
        var connection = CreateConnection(requester.Id, addressee.Id, ConnectionStatus.Pending);

        // Act - Le demandeur essaie d'accepter sa propre demande
        var (success, message) = await _connectionService.AcceptRequestAsync(connection.Id, requester.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Vous n'êtes pas autorisé à accepter cette demande");
    }

    [Fact]
    public async Task AcceptRequestAsync_AlreadyAccepted_ShouldReturnFailure()
    {
        // Arrange
        var requester = CreateUser("Jean", "Dupont");
        var addressee = CreateUser("Marie", "Martin");
        var connection = CreateConnection(requester.Id, addressee.Id, ConnectionStatus.Accepted);

        // Act
        var (success, message) = await _connectionService.AcceptRequestAsync(connection.Id, addressee.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Cette demande n'est plus en attente");
    }

    #endregion

    #region RejectRequestAsync Tests

    [Fact]
    public async Task RejectRequestAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var requester = CreateUser("Jean", "Dupont");
        var addressee = CreateUser("Marie", "Martin");
        var connection = CreateConnection(requester.Id, addressee.Id, ConnectionStatus.Pending);

        // Act
        var (success, message) = await _connectionService.RejectRequestAsync(connection.Id, addressee.Id);

        // Assert
        success.Should().BeTrue();
        message.Should().Be("Demande refusée");

        var updatedConnection = await _context.Connections.FindAsync(connection.Id);
        updatedConnection!.Status.Should().Be(ConnectionStatus.Rejected);
    }

    [Fact]
    public async Task RejectRequestAsync_WithNonExistentConnection_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var (success, message) = await _connectionService.RejectRequestAsync(Guid.NewGuid(), user.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Demande non trouvée");
    }

    [Fact]
    public async Task RejectRequestAsync_ByRequester_ShouldReturnFailure()
    {
        // Arrange
        var requester = CreateUser("Jean", "Dupont");
        var addressee = CreateUser("Marie", "Martin");
        var connection = CreateConnection(requester.Id, addressee.Id, ConnectionStatus.Pending);

        // Act
        var (success, message) = await _connectionService.RejectRequestAsync(connection.Id, requester.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Vous n'êtes pas autorisé à refuser cette demande");
    }

    [Fact]
    public async Task RejectRequestAsync_AlreadyRejected_ShouldReturnFailure()
    {
        // Arrange
        var requester = CreateUser("Jean", "Dupont");
        var addressee = CreateUser("Marie", "Martin");
        var connection = CreateConnection(requester.Id, addressee.Id, ConnectionStatus.Rejected);

        // Act
        var (success, message) = await _connectionService.RejectRequestAsync(connection.Id, addressee.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Cette demande n'est plus en attente");
    }

    #endregion

    #region GetConnectionsAsync Tests

    [Fact]
    public async Task GetConnectionsAsync_ShouldReturnOnlyAcceptedConnections()
    {
        // Arrange
        var user = CreateUser("Jean", "Dupont");
        var friend1 = CreateUser("Marie", "Martin");
        var friend2 = CreateUser("Pierre", "Bernard");
        var pending = CreateUser("Paul", "Durand");

        CreateConnection(user.Id, friend1.Id, ConnectionStatus.Accepted);
        CreateConnection(friend2.Id, user.Id, ConnectionStatus.Accepted);
        CreateConnection(user.Id, pending.Id, ConnectionStatus.Pending);

        // Act
        var connections = await _connectionService.GetConnectionsAsync(user.Id);

        // Assert
        connections.Should().HaveCount(2);
        connections.Select(c => c.User.FirstName).Should().Contain("Marie");
        connections.Select(c => c.User.FirstName).Should().Contain("Pierre");
        connections.Select(c => c.User.FirstName).Should().NotContain("Paul");
    }

    [Fact]
    public async Task GetConnectionsAsync_WithNoConnections_ShouldReturnEmptyList()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var connections = await _connectionService.GetConnectionsAsync(user.Id);

        // Assert
        connections.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConnectionsAsync_ShouldReturnOtherUserInfo()
    {
        // Arrange
        var user = CreateUser("Jean", "Dupont");
        var friend = CreateUser("Marie", "Martin", "marie@example.com");
        CreateConnection(user.Id, friend.Id, ConnectionStatus.Accepted);

        // Act
        var connections = await _connectionService.GetConnectionsAsync(user.Id);

        // Assert
        connections.Should().HaveCount(1);
        var connection = connections[0];
        connection.User.FirstName.Should().Be("Marie");
        connection.User.LastName.Should().Be("Martin");
        connection.Status.Should().Be(ConnectionStatus.Accepted);
    }

    #endregion

    #region GetPendingRequestsAsync Tests

    [Fact]
    public async Task GetPendingRequestsAsync_ShouldReturnOnlyPendingRequestsReceived()
    {
        // Arrange
        var user = CreateUser("Jean", "Dupont");
        var requester1 = CreateUser("Marie", "Martin");
        var requester2 = CreateUser("Pierre", "Bernard");
        var target = CreateUser("Paul", "Durand");

        // Demandes reçues
        CreateConnection(requester1.Id, user.Id, ConnectionStatus.Pending);
        CreateConnection(requester2.Id, user.Id, ConnectionStatus.Pending);

        // Demande envoyée (ne doit pas apparaître)
        CreateConnection(user.Id, target.Id, ConnectionStatus.Pending);

        // Act
        var requests = await _connectionService.GetPendingRequestsAsync(user.Id);

        // Assert
        requests.Should().HaveCount(2);
        requests.Select(r => r.Requester.FirstName).Should().Contain("Marie");
        requests.Select(r => r.Requester.FirstName).Should().Contain("Pierre");
        requests.Select(r => r.Requester.FirstName).Should().NotContain("Paul");
    }

    [Fact]
    public async Task GetPendingRequestsAsync_ShouldNotIncludeAcceptedOrRejected()
    {
        // Arrange
        var user = CreateUser("Jean", "Dupont");
        var requester1 = CreateUser("Marie", "Martin");
        var requester2 = CreateUser("Pierre", "Bernard");
        var requester3 = CreateUser("Paul", "Durand");

        CreateConnection(requester1.Id, user.Id, ConnectionStatus.Pending);
        CreateConnection(requester2.Id, user.Id, ConnectionStatus.Accepted);
        CreateConnection(requester3.Id, user.Id, ConnectionStatus.Rejected);

        // Act
        var requests = await _connectionService.GetPendingRequestsAsync(user.Id);

        // Assert
        requests.Should().HaveCount(1);
        requests[0].Requester.FirstName.Should().Be("Marie");
    }

    [Fact]
    public async Task GetPendingRequestsAsync_WithNoRequests_ShouldReturnEmptyList()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var requests = await _connectionService.GetPendingRequestsAsync(user.Id);

        // Assert
        requests.Should().BeEmpty();
    }

    #endregion

    #region RemoveConnectionAsync Tests

    [Fact]
    public async Task RemoveConnectionAsync_ByRequester_ShouldReturnSuccess()
    {
        // Arrange
        var requester = CreateUser("Jean", "Dupont");
        var addressee = CreateUser("Marie", "Martin");
        var connection = CreateConnection(requester.Id, addressee.Id, ConnectionStatus.Accepted);

        // Act
        var (success, message) = await _connectionService.RemoveConnectionAsync(connection.Id, requester.Id);

        // Assert
        success.Should().BeTrue();
        message.Should().Be("Connexion supprimée");

        var deletedConnection = await _context.Connections.FindAsync(connection.Id);
        deletedConnection.Should().BeNull();
    }

    [Fact]
    public async Task RemoveConnectionAsync_ByAddressee_ShouldReturnSuccess()
    {
        // Arrange
        var requester = CreateUser("Jean", "Dupont");
        var addressee = CreateUser("Marie", "Martin");
        var connection = CreateConnection(requester.Id, addressee.Id, ConnectionStatus.Accepted);

        // Act
        var (success, message) = await _connectionService.RemoveConnectionAsync(connection.Id, addressee.Id);

        // Assert
        success.Should().BeTrue();
        message.Should().Be("Connexion supprimée");
    }

    [Fact]
    public async Task RemoveConnectionAsync_ByUnrelatedUser_ShouldReturnFailure()
    {
        // Arrange
        var requester = CreateUser("Jean", "Dupont");
        var addressee = CreateUser("Marie", "Martin");
        var unrelatedUser = CreateUser("Paul", "Durand");
        var connection = CreateConnection(requester.Id, addressee.Id, ConnectionStatus.Accepted);

        // Act
        var (success, message) = await _connectionService.RemoveConnectionAsync(connection.Id, unrelatedUser.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Vous n'êtes pas autorisé à supprimer cette connexion");
    }

    [Fact]
    public async Task RemoveConnectionAsync_WithNonExistentConnection_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var (success, message) = await _connectionService.RemoveConnectionAsync(Guid.NewGuid(), user.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Connexion non trouvée");
    }

    #endregion

    #region GetSuggestionsAsync Tests

    [Fact]
    public async Task GetSuggestionsAsync_ShouldExcludeCurrentUser()
    {
        // Arrange
        var user = CreateUser("Jean", "Dupont");
        var other1 = CreateUser("Marie", "Martin");
        var other2 = CreateUser("Pierre", "Bernard");

        // Act
        var suggestions = await _connectionService.GetSuggestionsAsync(user.Id);

        // Assert
        suggestions.Should().NotContain(s => s.Id == user.Id);
        suggestions.Select(s => s.FirstName).Should().Contain("Marie");
        suggestions.Select(s => s.FirstName).Should().Contain("Pierre");
    }

    [Fact]
    public async Task GetSuggestionsAsync_ShouldExcludeConnectedUsers()
    {
        // Arrange
        var user = CreateUser("Jean", "Dupont");
        var connected = CreateUser("Marie", "Martin");
        var notConnected = CreateUser("Pierre", "Bernard");

        CreateConnection(user.Id, connected.Id, ConnectionStatus.Accepted);

        // Act
        var suggestions = await _connectionService.GetSuggestionsAsync(user.Id);

        // Assert
        suggestions.Should().NotContain(s => s.Id == connected.Id);
        suggestions.Select(s => s.FirstName).Should().Contain("Pierre");
    }

    [Fact]
    public async Task GetSuggestionsAsync_ShouldExcludePendingRequests()
    {
        // Arrange
        var user = CreateUser("Jean", "Dupont");
        var pending = CreateUser("Marie", "Martin");
        var available = CreateUser("Pierre", "Bernard");

        CreateConnection(user.Id, pending.Id, ConnectionStatus.Pending);

        // Act
        var suggestions = await _connectionService.GetSuggestionsAsync(user.Id);

        // Assert
        suggestions.Should().NotContain(s => s.Id == pending.Id);
        suggestions.Select(s => s.FirstName).Should().Contain("Pierre");
    }

    [Fact]
    public async Task GetSuggestionsAsync_ShouldRespectLimit()
    {
        // Arrange
        var user = CreateUser("Jean", "Dupont");
        for (int i = 0; i < 20; i++)
        {
            CreateUser($"User{i}", "Test");
        }

        // Act
        var suggestions = await _connectionService.GetSuggestionsAsync(user.Id, limit: 5);

        // Assert
        suggestions.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetSuggestionsAsync_WithNoOtherUsers_ShouldReturnEmptyList()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var suggestions = await _connectionService.GetSuggestionsAsync(user.Id);

        // Assert
        suggestions.Should().BeEmpty();
    }

    #endregion
}
