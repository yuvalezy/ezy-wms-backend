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

        var reportIds = await ValidateReportIdsAsync(request.ReportIds);

        var authGroup = new AuthorizationGroup {
            Name = request.Name,
            Description = request.Description,
            Authorizations = request.Authorizations,
            CreatedAt = DateTime.UtcNow,
            // Inserted with the group so the grants land in the same SaveChanges — EF fills in the FK.
            Reports = reportIds.Select(id => new AuthorizationGroupReport { ReportDefinitionId = id }).ToList()
        };

        _db.AuthorizationGroups.Add(authGroup);
        await _db.SaveChangesAsync();

        var response = MapToResponse(authGroup);
        response.ReportIds = reportIds;
        return response;
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

        var reportIds = await ValidateReportIdsAsync(request.ReportIds);

        authGroup.Name = request.Name;
        authGroup.Description = request.Description;
        authGroup.Authorizations = request.Authorizations;
        authGroup.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await SyncReportGrantsAsync(authGroup.Id, reportIds);

        var response = await MapToResponseWithCanDelete(authGroup);
        response.ReportIds = reportIds;
        return response;
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

        // Report grants cascade with the group.
        _db.AuthorizationGroups.Remove(authGroup);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<AuthorizationGroupResponse?> GetByIdAsync(Guid id) {
        var authGroup = await _db.AuthorizationGroups.FindAsync(id);
        if (authGroup == null) {
            return null;
        }

        var response = await MapToResponseWithCanDelete(authGroup);
        response.ReportIds = await GetReportIdsAsync(id);
        return response;
    }

    public async Task<IEnumerable<AuthorizationGroupResponse>> GetAllAsync() {
        var authGroups = await _db.AuthorizationGroups
            .OrderBy(ag => ag.Name)
            .ToListAsync();

        var groupIds = authGroups.Select(ag => ag.Id).ToList();

        // Both of these are batched rather than resolved per group: this list is rendered on every visit to
        // the authorization-groups screen.
        var inUse = (await _db.Users
                .Where(u => u.AuthorizationGroupId != null && groupIds.Contains(u.AuthorizationGroupId.Value))
                .Select(u => u.AuthorizationGroupId!.Value)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        var grantsByGroup = (await _db.AuthorizationGroupReports
                .Where(g => groupIds.Contains(g.AuthorizationGroupId) && !g.ReportDefinition.Deleted)
                .Select(g => new { g.AuthorizationGroupId, g.ReportDefinitionId })
                .ToListAsync())
            .GroupBy(g => g.AuthorizationGroupId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ReportDefinitionId).ToList());

        return authGroups.Select(authGroup => {
            var response = MapToResponse(authGroup);
            response.CanDelete = !inUse.Contains(authGroup.Id);
            response.ReportIds = grantsByGroup.TryGetValue(authGroup.Id, out var ids) ? ids : [];
            return response;
        }).ToList();
    }

    /// <summary>
    /// Rejects unknown or deleted report ids up front, so a stale form posts a 400 rather than an FK
    /// violation or a grant to a report nobody can see. Returns the de-duplicated set.
    /// </summary>
    private async Task<List<Guid>> ValidateReportIdsAsync(ICollection<Guid> reportIds) {
        var requested = reportIds.Distinct().ToList();
        if (requested.Count == 0) {
            return requested;
        }

        var known = await _db.ReportDefinitions
            .Where(r => !r.Deleted && requested.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync();

        var unknown = requested.Except(known).ToList();
        if (unknown.Count > 0) {
            throw new InvalidOperationException($"Unknown report(s): {string.Join(", ", unknown)}.");
        }

        return requested;
    }

    /// <summary>
    /// Replaces the group's grants with <paramref name="reportIds"/>, adding and removing only what changed.
    /// </summary>
    /// <remarks>
    /// Unlike <c>Authorizations</c> — which <c>SessionInfo</c> snapshots at login, so edits do nothing until
    /// re-login — these rows are read live on every access check, so this takes effect immediately.
    /// </remarks>
    private async Task SyncReportGrantsAsync(Guid groupId, List<Guid> reportIds) {
        var current = await _db.AuthorizationGroupReports
            .Where(g => g.AuthorizationGroupId == groupId)
            .ToListAsync();

        var stale = current.Where(g => !reportIds.Contains(g.ReportDefinitionId)).ToList();
        if (stale.Count > 0) {
            _db.AuthorizationGroupReports.RemoveRange(stale);
        }

        var currentIds = current.Select(g => g.ReportDefinitionId).ToHashSet();
        foreach (var reportId in reportIds.Where(id => !currentIds.Contains(id))) {
            _db.AuthorizationGroupReports.Add(new AuthorizationGroupReport {
                AuthorizationGroupId = groupId,
                ReportDefinitionId = reportId
            });
        }

        await _db.SaveChangesAsync();
    }

    private async Task<List<Guid>> GetReportIdsAsync(Guid groupId) =>
        await _db.AuthorizationGroupReports
            .Where(g => g.AuthorizationGroupId == groupId && !g.ReportDefinition.Deleted)
            .Select(g => g.ReportDefinitionId)
            .ToListAsync();

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
