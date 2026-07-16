using Core.DTOs.Items;
using Core.Interfaces;
using Infrastructure.DbContexts;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Shared plumbing for the per-module quantity repair services: loading the current item factors without
/// letting a SAP hiccup fail the read that triggered the repair.
/// </summary>
/// <remarks>
/// Must be resolved <b>scoped</b>, so the repair shares its caller's <see cref="SystemDbContext"/> — that is
/// what makes mutating the loaded lines and saving here visible to the caller's own query.
/// </remarks>
public abstract class QuantityRepairServiceBase(SystemDbContext db, IExternalSystemAdapter adapter, ILogger logger) {
    protected SystemDbContext Db => db;

    protected ILogger Logger => logger;

    /// <summary>
    /// One bulk lookup rather than a SAP round-trip per line. Returns null when there is nothing to look up,
    /// or when SAP could not be reached — in which case the caller must leave stored quantities alone.
    /// </summary>
    protected async Task<Dictionary<string, ItemCheckResponse>?> TryLoadItemsAsync(IEnumerable<string> itemCodes) {
        string[] codes = itemCodes.Distinct().ToArray();
        if (codes.Length == 0)
            return null;

        try {
            return await adapter.BulkItemCheckAsync(codes);
        }
        catch (Exception ex) {
            // Opening a document must not fail because SAP is briefly unavailable; the stored base is the
            // same one every read used before this repair existed.
            logger.LogWarning(ex, "Could not load item factors to restate quantities; leaving stored quantities unchanged");
            return null;
        }
    }
}
