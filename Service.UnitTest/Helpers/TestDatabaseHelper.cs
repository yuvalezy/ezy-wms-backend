using System;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Service.UnitTest.Helpers;

public static class TestDatabaseHelper {
    /// <summary>
    /// Creates a new in-memory database context for testing
    /// </summary>
    /// <param name="databaseName">Optional database name. If not provided, a new GUID will be used</param>
    /// <returns>A new SystemDbContext configured with an in-memory database</returns>
    public static SystemDbContext CreateInMemoryContext(string databaseName = null) {
        var options = new DbContextOptionsBuilder<SystemDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new SystemDbContext(options);
        
        // Ensure the database is created
        context.Database.EnsureCreated();
        
        return context;
    }
    
    /// <summary>
    /// Creates a new in-memory database context and applies pending migrations
    /// </summary>
    /// <param name="databaseName">Optional database name. If not provided, a new GUID will be used</param>
    /// <returns>A new SystemDbContext configured with an in-memory database</returns>
    public static SystemDbContext CreateInMemoryContextWithMigrations(string databaseName = null) {
        var context = CreateInMemoryContext(databaseName);
        
        // Note: In-memory database doesn't support migrations, but we ensure it's created
        context.Database.EnsureCreated();
        
        return context;
    }
}