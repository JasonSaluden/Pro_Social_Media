using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProSocialApi.Data.Context;
using ProSocialApi.Data.Entities;
using ProSocialApi.DTOs.Posts;
using ProSocialApi.Services;

namespace ProSocialApi.Tests.Services;

/// <summary>
/// Tests unitaires pour PostService.
/// Utilise une base de données InMemory pour simuler les opérations de base de données.
/// </summary>
public class PostServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PostService _postService;

    public PostServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _postService = new PostService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Helper Methods

    private User CreateUser(string firstName = "Test", string lastName = "User")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid()}@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            FirstName = firstName,
            LastName = lastName,
            Headline = "Développeur"
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    private Post CreatePost(Guid authorId, string content = "Test content")
    {
        var post = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Content = content
        };
        _context.Posts.Add(post);
        _context.SaveChanges();
        return post;
    }

    private Like CreateLike(Guid postId, Guid userId)
    {
        var like = new Like
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            UserId = userId
        };
        _context.Likes.Add(like);
        _context.SaveChanges();
        return like;
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldReturnPostDto()
    {
        // Arrange
        var author = CreateUser("Jean", "Dupont");
        var createDto = new CreatePostDto
        {
            Content = "Mon premier post !",
            ImageUrl = "https://example.com/image.jpg"
        };

        // Act
        var result = await _postService.CreateAsync(author.Id, createDto);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Mon premier post !");
        result.ImageUrl.Should().Be("https://example.com/image.jpg");
        result.Author.Should().NotBeNull();
        result.Author.FirstName.Should().Be("Jean");
        result.Author.LastName.Should().Be("Dupont");
        result.LikesCount.Should().Be(0);
        result.CommentsCount.Should().Be(0);

        // Vérifier en base
        var postInDb = await _context.Posts.FirstOrDefaultAsync();
        postInDb.Should().NotBeNull();
        postInDb!.Content.Should().Be("Mon premier post !");
    }

    [Fact]
    public async Task CreateAsync_WithoutImage_ShouldReturnPostDto()
    {
        // Arrange
        var author = CreateUser();
        var createDto = new CreatePostDto
        {
            Content = "Post sans image"
        };

        // Act
        var result = await _postService.CreateAsync(author.Id, createDto);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Post sans image");
        result.ImageUrl.Should().BeNull();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingPost_ShouldReturnPostDto()
    {
        // Arrange
        var author = CreateUser("Jean", "Dupont");
        var post = CreatePost(author.Id, "Contenu du post");

        // Act
        var result = await _postService.GetByIdAsync(post.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(post.Id);
        result.Content.Should().Be("Contenu du post");
        result.Author.FirstName.Should().Be("Jean");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentPost_ShouldReturnNull()
    {
        // Act
        var result = await _postService.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithCurrentUserId_ShouldSetIsLikedByCurrentUser()
    {
        // Arrange
        var author = CreateUser("Jean", "Dupont");
        var liker = CreateUser("Marie", "Martin");
        var post = CreatePost(author.Id, "Contenu du post");
        CreateLike(post.Id, liker.Id);

        // Act
        var resultForLiker = await _postService.GetByIdAsync(post.Id, liker.Id);
        var resultForOther = await _postService.GetByIdAsync(post.Id, author.Id);

        // Assert
        resultForLiker!.IsLikedByCurrentUser.Should().BeTrue();
        resultForOther!.IsLikedByCurrentUser.Should().BeFalse();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidAuthor_ShouldReturnUpdatedPostDto()
    {
        // Arrange
        var author = CreateUser();
        var post = CreatePost(author.Id, "Contenu original");
        var updateDto = new UpdatePostDto
        {
            Content = "Contenu modifié"
        };

        // Act
        var result = await _postService.UpdateAsync(post.Id, author.Id, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Be("Contenu modifié");

        // Vérifier en base
        var postInDb = await _context.Posts.FindAsync(post.Id);
        postInDb!.Content.Should().Be("Contenu modifié");
    }

    [Fact]
    public async Task UpdateAsync_WithDifferentUser_ShouldReturnNull()
    {
        // Arrange
        var author = CreateUser("Jean", "Dupont");
        var otherUser = CreateUser("Marie", "Martin");
        var post = CreatePost(author.Id, "Contenu original");
        var updateDto = new UpdatePostDto
        {
            Content = "Contenu modifié"
        };

        // Act
        var result = await _postService.UpdateAsync(post.Id, otherUser.Id, updateDto);

        // Assert
        result.Should().BeNull();

        // Le post n'a pas été modifié
        var postInDb = await _context.Posts.FindAsync(post.Id);
        postInDb!.Content.Should().Be("Contenu original");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentPost_ShouldReturnNull()
    {
        // Arrange
        var user = CreateUser();
        var updateDto = new UpdatePostDto
        {
            Content = "Contenu modifié"
        };

        // Act
        var result = await _postService.UpdateAsync(Guid.NewGuid(), user.Id, updateDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithPartialUpdate_ShouldOnlyUpdateProvidedFields()
    {
        // Arrange
        var author = CreateUser();
        var post = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = author.Id,
            Content = "Contenu original",
            ImageUrl = "https://example.com/original.jpg"
        };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        var updateDto = new UpdatePostDto
        {
            Content = "Contenu modifié"
            // ImageUrl non fourni
        };

        // Act
        var result = await _postService.UpdateAsync(post.Id, author.Id, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Be("Contenu modifié");
        result.ImageUrl.Should().Be("https://example.com/original.jpg");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidAuthor_ShouldReturnTrue()
    {
        // Arrange
        var author = CreateUser();
        var post = CreatePost(author.Id);

        // Act
        var result = await _postService.DeleteAsync(post.Id, author.Id);

        // Assert
        result.Should().BeTrue();

        var postInDb = await _context.Posts.FindAsync(post.Id);
        postInDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithDifferentUser_ShouldReturnFalse()
    {
        // Arrange
        var author = CreateUser("Jean", "Dupont");
        var otherUser = CreateUser("Marie", "Martin");
        var post = CreatePost(author.Id);

        // Act
        var result = await _postService.DeleteAsync(post.Id, otherUser.Id);

        // Assert
        result.Should().BeFalse();

        var postInDb = await _context.Posts.FindAsync(post.Id);
        postInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentPost_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var result = await _postService.DeleteAsync(Guid.NewGuid(), user.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetUserPostsAsync Tests

    [Fact]
    public async Task GetUserPostsAsync_ShouldReturnUserPosts()
    {
        // Arrange
        var user1 = CreateUser("Jean", "Dupont");
        var user2 = CreateUser("Marie", "Martin");

        CreatePost(user1.Id, "Post 1 de Jean");
        CreatePost(user1.Id, "Post 2 de Jean");
        CreatePost(user2.Id, "Post de Marie");

        // Act
        var result = await _postService.GetUserPostsAsync(user1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.All(p => p.Author.FirstName == "Jean").Should().BeTrue();
    }

    [Fact]
    public async Task GetUserPostsAsync_ShouldReturnPostsInDescendingOrder()
    {
        // Arrange
        var user = CreateUser();
        var post1 = CreatePost(user.Id, "Premier post");

        // Attendre un peu pour avoir une différence de temps
        await Task.Delay(10);

        var post2 = CreatePost(user.Id, "Deuxième post");

        // Act
        var result = await _postService.GetUserPostsAsync(user.Id);

        // Assert
        result.Should().HaveCount(2);
        result[0].Content.Should().Be("Deuxième post"); // Le plus récent en premier
        result[1].Content.Should().Be("Premier post");
    }

    [Fact]
    public async Task GetUserPostsAsync_WithNoPost_ShouldReturnEmptyList()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var result = await _postService.GetUserPostsAsync(user.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserPostsAsync_WithCurrentUserId_ShouldSetIsLikedByCurrentUser()
    {
        // Arrange
        var author = CreateUser("Jean", "Dupont");
        var liker = CreateUser("Marie", "Martin");
        var post1 = CreatePost(author.Id, "Post 1");
        var post2 = CreatePost(author.Id, "Post 2");
        CreateLike(post1.Id, liker.Id);

        // Act
        var result = await _postService.GetUserPostsAsync(author.Id, liker.Id);

        // Assert
        var likedPost = result.First(p => p.Content == "Post 1");
        var notLikedPost = result.First(p => p.Content == "Post 2");

        likedPost.IsLikedByCurrentUser.Should().BeTrue();
        notLikedPost.IsLikedByCurrentUser.Should().BeFalse();
    }

    #endregion

    #region LikeAsync Tests

    [Fact]
    public async Task LikeAsync_WithValidPostAndUser_ShouldReturnSuccess()
    {
        // Arrange
        var author = CreateUser("Jean", "Dupont");
        var liker = CreateUser("Marie", "Martin");
        var post = CreatePost(author.Id);

        // Act
        var (success, message) = await _postService.LikeAsync(post.Id, liker.Id);

        // Assert
        success.Should().BeTrue();
        message.Should().Be("Post liké");

        var likeInDb = await _context.Likes.FirstOrDefaultAsync(l => l.PostId == post.Id && l.UserId == liker.Id);
        likeInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task LikeAsync_WithNonExistentPost_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var (success, message) = await _postService.LikeAsync(Guid.NewGuid(), user.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Post non trouvé");
    }

    [Fact]
    public async Task LikeAsync_WhenAlreadyLiked_ShouldReturnFailure()
    {
        // Arrange
        var author = CreateUser("Jean", "Dupont");
        var liker = CreateUser("Marie", "Martin");
        var post = CreatePost(author.Id);
        CreateLike(post.Id, liker.Id);

        // Act
        var (success, message) = await _postService.LikeAsync(post.Id, liker.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Vous avez déjà liké ce post");
    }

    [Fact]
    public async Task LikeAsync_ShouldIncreaseLikesCount()
    {
        // Arrange
        var author = CreateUser();
        var liker = CreateUser();
        var post = CreatePost(author.Id);

        // Act
        await _postService.LikeAsync(post.Id, liker.Id);
        var result = await _postService.GetByIdAsync(post.Id);

        // Assert
        result!.LikesCount.Should().Be(1);
    }

    #endregion

    #region UnlikeAsync Tests

    [Fact]
    public async Task UnlikeAsync_WhenLiked_ShouldReturnSuccess()
    {
        // Arrange
        var author = CreateUser();
        var liker = CreateUser();
        var post = CreatePost(author.Id);
        CreateLike(post.Id, liker.Id);

        // Act
        var (success, message) = await _postService.UnlikeAsync(post.Id, liker.Id);

        // Assert
        success.Should().BeTrue();
        message.Should().Be("Like retiré");

        var likeInDb = await _context.Likes.FirstOrDefaultAsync(l => l.PostId == post.Id && l.UserId == liker.Id);
        likeInDb.Should().BeNull();
    }

    [Fact]
    public async Task UnlikeAsync_WhenNotLiked_ShouldReturnFailure()
    {
        // Arrange
        var author = CreateUser();
        var user = CreateUser();
        var post = CreatePost(author.Id);

        // Act
        var (success, message) = await _postService.UnlikeAsync(post.Id, user.Id);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Vous n'avez pas liké ce post");
    }

    [Fact]
    public async Task UnlikeAsync_ShouldDecreaseLikesCount()
    {
        // Arrange
        var author = CreateUser();
        var liker = CreateUser();
        var post = CreatePost(author.Id);
        CreateLike(post.Id, liker.Id);

        // Vérifier le count avant
        var beforeUnlike = await _postService.GetByIdAsync(post.Id);
        beforeUnlike!.LikesCount.Should().Be(1);

        // Act
        await _postService.UnlikeAsync(post.Id, liker.Id);
        var afterUnlike = await _postService.GetByIdAsync(post.Id);

        // Assert
        afterUnlike!.LikesCount.Should().Be(0);
    }

    #endregion
}
