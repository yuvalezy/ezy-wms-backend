using System;
using System.Threading.Tasks;
using Infrastructure.DbContexts;
using NUnit.Framework;
using Service.UnitTest.Helpers;

namespace Service.UnitTest.Integration;

/// <summary>
/// Base class for integration tests that require database access
/// </summary>
public abstract class IntegrationTestBase {
    protected SystemDbContext DbContext { get; private set; }

    [SetUp]
    public virtual async Task BaseSetUp() {
        // Create a new in-memory database for each test
        DbContext = TestDatabaseHelper.CreateInMemoryContext();
        
        // Allow derived classes to seed data
        await SeedDataAsync();
    }

    [TearDown]
    public virtual async Task BaseTearDown() {
        // Clean up the database context
        if (DbContext != null) {
            await DbContext.DisposeAsync();
        }
    }

    /// <summary>
    /// Override this method in derived classes to seed test data
    /// </summary>
    protected virtual Task SeedDataAsync() {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Helper method to clear all data from the database
    /// </summary>
    protected async Task ClearDatabaseAsync() {
        // Remove all entities
        DbContext.Users.RemoveRange(DbContext.Users);
        DbContext.AuthorizationGroups.RemoveRange(DbContext.AuthorizationGroups);
        
        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Helper method to verify that an entity exists in the database
    /// </summary>
    protected async Task<T> GetEntityAsync<T>(Guid id) where T : class {
        return await DbContext.FindAsync<T>(id);
    }
}