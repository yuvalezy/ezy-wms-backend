using System.Linq.Expressions;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DbContexts;

public static class SqlConfigurations {
    public static void ConfigureForSqlServer(ModelBuilder modelBuilder) {
        ConfigureBaseEntity(modelBuilder);
    }

    private static void ConfigureBaseEntity(ModelBuilder modelBuilder) {
        // Configure BaseEntity properties for all entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()) {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)) 
                continue;
            var entityBuilder = modelBuilder.Entity(entityType.ClrType);
                
            entityBuilder
                .Property(nameof(BaseEntity.CreatedAt))
                .HasDefaultValueSql("GETUTCDATE()");
                
            // Create query filter using reflection
            var parameter  = Expression.Parameter(entityType.ClrType, "e");
            var property   = Expression.Property(parameter, nameof(BaseEntity.Deleted));
            var comparison = Expression.Equal(property, Expression.Constant(false));
            var lambda     = Expression.Lambda(comparison, parameter);
                    
            entityBuilder.HasQueryFilter(lambda);
        }
    }

}