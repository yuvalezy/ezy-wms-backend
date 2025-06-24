using Core.DTOs.Settings;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class AuthorizationGroupService : IAuthorizationGroupService {
    private readonly SystemDbContext _db;

    public AuthorizationGroupService(SystemDbContext db) {
        _db = db;
    }

    public async Task<AuthorizationGroupResponse> CreateAsync(CreateAuthorizationGroupRequest request) {
        // Check if name already exists
        var existingGroup = await _db.AuthorizationGroups
            .AnyAsync(ag => ag.Name.ToLower() == request.Name.ToLower());
        
        if (existingGroup) {
            throw new InvalidOperationException($"Authorization group with name '{request.Name}' already exists.");
        }

        var authGroup = new AuthorizationGroup {
            Name = request.Name,
            Description = request.Description,
            Authorizations = request.Authorizations,
            CreatedAt = DateTime.UtcNow
        };

        _db.AuthorizationGroups.Add(authGroup);
        await _db.SaveChangesAsync();

        return MapToResponse(authGroup);
    }

    public async Task<AuthorizationGroupResponse> UpdateAsync(UpdateAuthorizationGroupRequest request) {
        var authGroup = await _db.AuthorizationGroups.FindAsync(request.Id);
        if (authGroup == null) {
            throw new KeyNotFoundException($"Authorization group with ID {request.Id} not found.");
        }

        // Check if new name conflicts with another group
        var nameConflict = await _db.AuthorizationGroups
            .AnyAsync(ag => ag.Id != request.Id && ag.Name.ToLower() == request.Name.ToLower());
        
        if (nameConflict) {
            throw new InvalidOperationException($"Authorization group with name '{request.Name}' already exists.");
        }

        authGroup.Name = request.Name;
        authGroup.Description = request.Description;
        authGroup.Authorizations = request.Authorizations;
        authGroup.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return await MapToResponseWithCanDelete(authGroup);
    }

    public async Task<bool> DeleteAsync(Guid id) {
        var authGroup = await _db.AuthorizationGroups.FindAsync(id);
        if (authGroup == null) {
            return false;
        }

        // Check if the authorization group is in use by any users
        var isInUse = await IsAuthorizationGroupInUse(id);
        if (isInUse) {
            throw new InvalidOperationException("Cannot delete authorization group that is assigned to users.");
        }

        _db.AuthorizationGroups.Remove(authGroup);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<AuthorizationGroupResponse?> GetByIdAsync(Guid id) {
        var authGroup = await _db.AuthorizationGroups.FindAsync(id);
        return authGroup == null ? null : await MapToResponseWithCanDelete(authGroup);
    }

    public async Task<IEnumerable<AuthorizationGroupResponse>> GetAllAsync() {
        var authGroups = await _db.AuthorizationGroups
            .OrderBy(ag => ag.Name)
            .ToListAsync();

        var responses = new List<AuthorizationGroupResponse>();
        foreach (var authGroup in authGroups) {
            responses.Add(await MapToResponseWithCanDelete(authGroup));
        }

        return responses;
    }

    private async Task<bool> IsAuthorizationGroupInUse(Guid authGroupId) {
        return await _db.Users.AnyAsync(u => u.AuthorizationGroupId == authGroupId);
    }

    private AuthorizationGroupResponse MapToResponse(AuthorizationGroup authGroup) {
        return new AuthorizationGroupResponse {
            Id = authGroup.Id,
            Name = authGroup.Name,
            Description = authGroup.Description,
            Authorizations = authGroup.Authorizations,
            CreatedAt = authGroup.CreatedAt,
            UpdatedAt = authGroup.UpdatedAt,
            CanDelete = true // Default to true for new records
        };
    }

    private async Task<AuthorizationGroupResponse> MapToResponseWithCanDelete(AuthorizationGroup authGroup) {
        var response = MapToResponse(authGroup);
        response.CanDelete = !await IsAuthorizationGroupInUse(authGroup.Id);
        return response;
    }
}