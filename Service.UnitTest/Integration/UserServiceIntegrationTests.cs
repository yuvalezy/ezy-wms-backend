using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Entities;
using Core.Models;
using FluentAssertions;
using Infrastructure.DbContexts;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Service.UnitTest.Integration;

[TestFixture]
public class UserServiceIntegrationTests {
    private SystemDbContext dbContext;
    private UserService userService;
    private Mock<ILogger<UserService>> loggerMock;
    private AuthorizationGroup testAuthGroup;

    [SetUp]
    public async Task SetUp() {
        // Create a new in-memory database for each test
        var options = new DbContextOptionsBuilder<SystemDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        dbContext = new SystemDbContext(options);
        loggerMock = new Mock<ILogger<UserService>>();
        userService = new UserService(dbContext, loggerMock.Object);

        // Seed test data
        await SeedTestDataAsync();
    }

    [TearDown]
    public async Task TearDown() {
        await dbContext.DisposeAsync();
    }

    private async Task SeedTestDataAsync() {
        // Create a test authorization group
        testAuthGroup = new AuthorizationGroup {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            Authorizations = new List<Core.Enums.Authorization> { Core.Enums.Authorization.Counting, Core.Enums.Authorization.Picking }
        };
        dbContext.AuthorizationGroups.Add(testAuthGroup);

        // Create some test users
        var users = new[] {
            new User {
                Id = Guid.NewGuid(),
                FullName = "Test User 1",
                Password = "hashed_password_1",
                Email = "user1@test.com",
                Position = "Developer",
                SuperUser = false,
                Active = true,
                AuthorizationGroupId = testAuthGroup.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User {
                Id = Guid.NewGuid(),
                FullName = "Test User 2",
                Password = "hashed_password_2",
                Email = "user2@test.com",
                Position = "Manager",
                SuperUser = true,
                Active = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User {
                Id = Guid.NewGuid(),
                FullName = "Deleted User",
                Password = "hashed_password_3",
                Email = "deleted@test.com",
                Position = "Tester",
                SuperUser = false,
                Active = false,
                Deleted = true,
                DeletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        dbContext.Users.AddRange(users);
        await dbContext.SaveChangesAsync();
    }

    [Test]
    public async Task GetUsersAsync_ShouldReturnAllUsers_IncludingAuthorizationGroups() {
        // Act
        var result = await userService.GetUsersAsync();
        var users = result.ToList();

        // Assert
        users.Should().HaveCount(3);
        
        var userWithGroup = users.FirstOrDefault(u => u.AuthorizationGroupId == testAuthGroup.Id);
        userWithGroup.Should().NotBeNull();
        userWithGroup.AuthorizationGroupName.Should().Be("Test Group");
        
        users.Should().Contain(u => u.FullName == "Test User 1");
        users.Should().Contain(u => u.FullName == "Test User 2");
        users.Should().Contain(u => u.FullName == "Deleted User");
    }

    [Test]
    public async Task GetUserAsync_WithValidId_ShouldReturnUser_WithPasswordMasked() {
        // Arrange
        var existingUser = await dbContext.Users.FirstAsync(u => u.FullName == "Test User 1");

        // Act
        var result = await userService.GetUserAsync(existingUser.Id);

        // Assert
        result.Should().NotBeNull();
        result.FullName.Should().Be("Test User 1");
        result.Email.Should().Be("user1@test.com");
        result.Password.Should().Be("*********"); // Password should be masked
        result.AuthorizationGroup.Should().NotBeNull();
        result.AuthorizationGroup.Name.Should().Be("Test Group");
    }

    [Test]
    public async Task GetUserAsync_WithInvalidId_ShouldReturnNull() {
        // Act
        var result = await userService.GetUserAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task CreateUserAsync_WithValidData_ShouldCreateUser() {
        // Arrange
        var request = new CreateUserRequest {
            FullName = "New User",
            Password = "password123",
            Email = "newuser@test.com",
            Position = "Developer",
            SuperUser = false,
            AuthorizationGroupId = testAuthGroup.Id
        };

        // Act
        var result = await userService.CreateUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.FullName.Should().Be("New User");
        result.Email.Should().Be("newuser@test.com");
        result.Position.Should().Be("Developer");
        result.SuperUser.Should().BeFalse();
        result.Active.Should().BeTrue();
        result.Password.Should().Be("*********"); // Password should be masked

        // Verify user was actually saved to database
        var savedUser = await dbContext.Users.FindAsync(result.Id);
        savedUser.Should().NotBeNull();
        savedUser.Password.Should().NotBe("password123"); // Password should be hashed
        savedUser.Password.Should().Be("*********");
    }

    [Test]
    public async Task CreateUserAsync_WithInvalidAuthorizationGroup_ShouldThrowException() {
        // Arrange
        var request = new CreateUserRequest {
            FullName = "New User",
            Password = "password123",
            Email = "newuser@test.com",
            Position = "Developer",
            SuperUser = false,
            AuthorizationGroupId = Guid.NewGuid() // Non-existent group
        };

        // Act & Assert
        var act = async () => await userService.CreateUserAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Authorization group not found.");
    }

    [Test]
    public async Task UpdateUserAsync_WithValidData_ShouldUpdateUser() {
        // Arrange
        var existingUser = await dbContext.Users.FirstAsync(u => u.FullName == "Test User 1");
        var currentUserId = Guid.NewGuid(); // Different user making the update
        
        var request = new UpdateUserRequest {
            FullName = "Updated User Name",
            Email = "updated@test.com",
            Position = "Senior Developer",
            SuperUser = true
        };

        // Act
        var result = await userService.UpdateUserAsync(existingUser.Id, request, currentUserId);

        // Assert
        result.Should().BeTrue();

        // Verify changes in database
        var updatedUser = await dbContext.Users.FindAsync(existingUser.Id);
        updatedUser.FullName.Should().Be("Updated User Name");
        updatedUser.Email.Should().Be("updated@test.com");
        updatedUser.Position.Should().Be("Senior Developer");
        updatedUser.SuperUser.Should().BeTrue();
        updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task UpdateUserAsync_RemovingOwnSuperUserStatus_ShouldThrowException() {
        // Arrange
        var superUser = await dbContext.Users.FirstAsync(u => u.SuperUser);
        var request = new UpdateUserRequest { SuperUser = false };

        // Act & Assert
        var act = async () => await userService.UpdateUserAsync(superUser.Id, request, superUser.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot remove your own super user status.");
    }

    [Test]
    public async Task UpdateUserAsync_WithDeletedUser_ShouldReturnFalse() {
        // Arrange
        var deletedUser = await dbContext.Users.FirstAsync(u => u.Deleted);
        var request = new UpdateUserRequest { FullName = "New Name" };

        // Act
        var result = await userService.UpdateUserAsync(deletedUser.Id, request, Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task DeleteUserAsync_WithValidUser_ShouldSoftDeleteUser() {
        // Arrange
        var userToDelete = await dbContext.Users.FirstAsync(u => u.FullName == "Test User 1");
        var currentUserId = Guid.NewGuid(); // Different user

        // Act
        var result = await userService.DeleteUserAsync(userToDelete.Id, currentUserId);

        // Assert
        result.Should().BeTrue();

        // Verify soft delete in database
        var deletedUser = await dbContext.Users.FindAsync(userToDelete.Id);
        deletedUser.Deleted.Should().BeTrue();
        deletedUser.DeletedAt.Should().NotBeNull();
        deletedUser.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        deletedUser.Active.Should().BeFalse();
    }

    [Test]
    public async Task DeleteUserAsync_DeletingOwnAccount_ShouldThrowException() {
        // Arrange
        var user = await dbContext.Users.FirstAsync(u => u.FullName == "Test User 1");

        // Act & Assert
        var act = async () => await userService.DeleteUserAsync(user.Id, user.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete your own user account.");
    }

    [Test]
    public async Task DeleteUserAsync_DeletingDefaultUser_ShouldThrowException() {
        // Act & Assert
        var act = async () => await userService.DeleteUserAsync(Const.DefaultUserId, Guid.NewGuid());
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete the default system user.");
    }

    [Test]
    public async Task DisableUserAsync_WithActiveUser_ShouldDisableUser() {
        // Arrange
        var userToDisable = await dbContext.Users.FirstAsync(u => u.FullName == "Test User 1" && u.Active);
        var currentUserId = Guid.NewGuid(); // Different user

        // Act
        var result = await userService.DisableUserAsync(userToDisable.Id, currentUserId);

        // Assert
        result.Should().BeTrue();

        // Verify in database
        var disabledUser = await dbContext.Users.FindAsync(userToDisable.Id);
        disabledUser.Active.Should().BeFalse();
        disabledUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task DisableUserAsync_DisablingOwnAccount_ShouldThrowException() {
        // Arrange
        var user = await dbContext.Users.FirstAsync(u => u.FullName == "Test User 1");

        // Act & Assert
        var act = async () => await userService.DisableUserAsync(user.Id, user.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot disable your own user account.");
    }

    [Test]
    public async Task DisableUserAsync_AlreadyDisabledUser_ShouldThrowException() {
        // Arrange
        var user = await dbContext.Users.FirstAsync(u => u.FullName == "Test User 1");
        await userService.DisableUserAsync(user.Id, Guid.NewGuid());

        // Act & Assert
        var act = async () => await userService.DisableUserAsync(user.Id, Guid.NewGuid());
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User is already disabled.");
    }

    [Test]
    public async Task EnableUserAsync_WithDisabledUser_ShouldEnableUser() {
        // Arrange
        var user = await dbContext.Users.FirstAsync(u => u.FullName == "Test User 1");
        await userService.DisableUserAsync(user.Id, Guid.NewGuid());

        // Act
        var result = await userService.EnableUserAsync(user.Id);

        // Assert
        result.Should().BeTrue();

        // Verify in database
        var enabledUser = await dbContext.Users.FindAsync(user.Id);
        enabledUser.Active.Should().BeTrue();
        enabledUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task EnableUserAsync_AlreadyEnabledUser_ShouldThrowException() {
        // Arrange
        var user = await dbContext.Users.FirstAsync(u => u.FullName == "Test User 1" && u.Active);

        // Act & Assert
        var act = async () => await userService.EnableUserAsync(user.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User is already enabled.");
    }

    [Test]
    public async Task EnableUserAsync_WithDeletedUser_ShouldReturnFalse() {
        // Arrange
        var deletedUser = await dbContext.Users.FirstAsync(u => u.Deleted);

        // Act
        var result = await userService.EnableUserAsync(deletedUser.Id);

        // Assert
        result.Should().BeFalse();
    }
}