using System.Diagnostics;
using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListDetailService(
    SystemDbContext db,
    IExternalSystemAdapter adapter,
    ILogger<PickListDetailService> logger,
    ISettings settings,
    IPickListPackageEligibilityService eligibilityService,
    IPickListPackageService packageService) : IPickListDetailService {
    private readonly bool enablePackages = settings.Options.EnablePackages;

    public async Task GetPickListItemDetails(int absEntry, PickListDetailRequest request, PickListResponse response, PickList[] dbPick) {
        foreach (var detail in response.Detail!) {
            decimal dbPickQty = dbPick.Where(p => p.AbsEntry == absEntry && p.PickEntry == detail.Entry).Sum(p => p.Quantity);
            detail.TotalOpenItems -= dbPickQty;
        }

        if (request is { Type: not null, Entry: not null }) {
            var responseDetail = response.Detail!.First(v => v.Type == request.Type.Value && v.Entry == request.Entry.Value);
            await GetPickListItemDetailsExecute(absEntry, request, dbPick, responseDetail.Type, responseDetail.Entry, responseDetail);
            return;
        }

        if (request.PickCheck) {
            foreach (var responseDetail in response.Detail!) {
                await GetPickListItemDetailsExecute(absEntry, request, dbPick, responseDetail.Type, responseDetail.Entry, responseDetail);
            }
        }
    }

    private async Task GetPickListItemDetailsExecute(int absEntry, PickListDetailRequest request, PickList[] dbPick, int type, int entry, PickListDetailResponse responseDetail) {
        var itemParams = new Dictionary<string, object> {
            { "@AbsEntry", absEntry },
            { "@Type", type },
            { "@Entry", entry }
        };

        var items = await adapter.GetPickingDetailItems(itemParams);
        var itemDict = new Dictionary<string, PickListDetailItemResponse>();

        foreach (var item in items) {
            PickListDetailItemResponse itemResponse;
            if (!itemDict.TryGetValue(item.ItemCode, out var value)) {
                itemResponse = new PickListDetailItemResponse {
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    Quantity = item.Quantity,
                    Picked = item.Picked,
                    OpenQuantity = item.OpenQuantity,
                    NumInBuy = item.NumInBuy,
                    BuyUnitMsr = item.BuyUnitMsr,
                    PurPackUn = item.PurPackUn,
                    PurPackMsr = item.PurPackMsr,
                    Factor1 = item.Factor1,
                    Factor2 = item.Factor2,
                    Factor3 = item.Factor3,
                    Factor4 = item.Factor4,
                    CustomFields = item.CustomFields
                };

                itemDict[item.ItemCode] = itemResponse;
                responseDetail.Items!.Add(itemResponse);
            }
            else {
                itemResponse = value;
                itemResponse.Quantity += item.Quantity;
                itemResponse.Picked += item.Picked;
                itemResponse.OpenQuantity += item.OpenQuantity;
            }

            decimal dbPickQty = dbPick.Where(p => p.AbsEntry == absEntry && p.ItemCode == item.ItemCode).Sum(p => p.Quantity);
            itemResponse.Picked += dbPickQty;
            itemResponse.OpenQuantity -= dbPickQty;
        }

        // Get available bins if requested
        await GetPickListItemDetailsAvailableBins(absEntry, itemDict, responseDetail, type, entry, request.BinEntry, request.AvailableBins);
    }

    private async Task GetPickListItemDetailsAvailableBins(
        int absEntry,
        Dictionary<string, PickListDetailItemResponse> itemDict,
        PickListDetailResponse responseDetail,
        int type,
        int entry,
        int? binEntry,
        bool? availableBins) {
        if (availableBins != true) {
            return;
        }

        var binParams = new Dictionary<string, object> {
            { "@AbsEntry", absEntry },
            { "@Type", type },
            { "@Entry", entry }
        };

        if (binEntry.HasValue) {
            binParams.Add("@BinEntry", binEntry.Value);
        }

        var bins = (await adapter.GetPickingDetailItemsBins(binParams)).ToArray();

        // Process any closed pick lists that have package commitments
        await ProcessClosedPickListsWithPackages();

        var result = db.PickLists
        .Where(p => (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing) && p.SyncStatus != SyncStatus.ExternalCancel)
        .Select(p => new { p.ItemCode, p.BinEntry, p.Quantity })
        .Concat(
            db.TransferLines
            .Where(t => t.LineStatus == LineStatus.Open || t.LineStatus == LineStatus.Processing)
            .Select(t => new { t.ItemCode, t.BinEntry, t.Quantity })
        )
        .GroupBy(x => new { x.ItemCode, x.BinEntry })
        .Select(g => new {
            g.Key.ItemCode,
            g.Key.BinEntry,
            Quantity = g.Sum(x => x.Quantity)
        });

        string[] items = itemDict.Keys.ToArray();
        int[] binEntries = bins.Select(v => v.Entry).Distinct().ToArray();
        BinLocationPackageQuantityResponse[] packages = [];

        Dictionary<Guid, List<PackageContent>>? packageContentsLookup = null;

        if (enablePackages) {
            var packagesStatuses = new[] { PackageStatus.Active, PackageStatus.Locked };
            packages = await db
            .PackageContents
            .Include(p => p.Package)
            .Where(p => items.Contains(p.ItemCode) && p.BinEntry != null && binEntries.Contains(p.BinEntry.Value) && packagesStatuses.Contains(p.Package.Status))
            .Select(p => new BinLocationPackageQuantityResponse(p.PackageId, p.Package.Barcode, p.BinEntry!.Value, p.ItemCode,
                !binEntry.HasValue ? p.Quantity : p.Quantity - p.CommittedQuantity))
            .ToArrayAsync();

            // Get unique package IDs from the filtered packages
            var packageIds = packages.Select(p => p.Id).Distinct().ToArray();

            // Load ALL contents for these packages (not just the ones in our bins)
            var allPackageContents = await db.PackageContents
            .Where(pc => packageIds.Contains(pc.PackageId))
            .ToListAsync();

            // Group by PackageId for efficient lookup
            packageContentsLookup = allPackageContents
            .GroupBy(pc => pc.PackageId)
            .ToDictionary(g => g.Key, g => g.ToList());
        }

        foreach (var bin in bins) {
            if (!itemDict.TryGetValue(bin.ItemCode, out var item))
                continue;

            item.BinQuantities ??= [];
            var binResponse = new BinLocationQuantityResponse {
                Entry = bin.Entry,
                Code = bin.Code,
                Quantity = bin.Quantity - result.Where(v => v.ItemCode == item.ItemCode && v.BinEntry == bin.Entry).Sum(v => v.Quantity)
            };

            if (binResponse.Quantity <= 0)
                continue;

            if (enablePackages) {
                binResponse.Packages = packages.Where(p => p.BinEntry == bin.Entry && p.ItemCode == bin.ItemCode && p.Quantity > 0).OrderBy(p => p.Barcode).ToArray();
            }

            item.BinQuantities.Add(binResponse);
        }


        if (!binEntry.HasValue) {
            return;
        }

        // Calculate available quantities and filter if bin entry specified
        responseDetail.Items!.RemoveAll(v => v.BinQuantities == null || v.OpenQuantity == 0);
        foreach (var item in responseDetail.Items.Where(i => i.BinQuantities != null)) {
            item.Available = item.BinQuantities!.Sum(b => b.Quantity);
            item.Packages = item.BinQuantities!.Where(b => b.Packages != null).SelectMany(b => b.Packages!).Where(b => b.Quantity > 0).ToArray();
        }

        // Check for full packages when packages are enabled and bin entry is specified
        if (enablePackages && packageContentsLookup != null) {
            // Create lookup of ItemCode -> OpenQuantity
            var itemOpenQuantities = responseDetail.Items
            .ToDictionary(i => i.ItemCode, i => i.OpenQuantity);

            // Track processed packages to avoid duplicate checks
            var processedPackages = new HashSet<Guid>();

            foreach (var item in responseDetail.Items.Where(i => i.Packages != null)) {
                foreach (var package in item.Packages!) {
                    if (!processedPackages.Add(package.Id))
                        continue;

                    if (!packageContentsLookup.TryGetValue(package.Id, out var contents))
                        continue;

                    // Use eligibility service to check if package can be fully picked
                    bool canBeFullyPicked = eligibilityService.CanPackageBeFullyPicked(contents, itemOpenQuantities);

                    if (!canBeFullyPicked)
                        continue;

                    // Mark package as full in ALL items that reference it
                    foreach (var otherItem in responseDetail.Items.Where(i => i.Packages != null)) {
                        var pkg = otherItem.Packages!.FirstOrDefault(p => p.Id == package.Id);
                        if (pkg != null) {
                            pkg.FullPackage = true;
                        }
                    }
                }
            }
        }
    }

    public async Task ProcessClosedPickListsWithPackages() {
        // Find pick lists that are Closed and Synced locally but might have packages to process
        var syncedPickLists = await db.PickLists
        .Where(p => p.Status == ObjectStatus.Closed && p.SyncStatus == SyncStatus.Synced)
        .Select(p => p.AbsEntry)
        .Distinct()
        .ToArrayAsync();

        if (syncedPickLists.Length == 0) {
            return;
        }

        // Check if these pick lists have any unprocessed packages
        var pickListsWithUnprocessedPackages = await db.PickListPackages
        .Where(plp => syncedPickLists.Contains(plp.AbsEntry) && plp.ProcessedAt == null)
        .Select(plp => plp.AbsEntry)
        .Distinct()
        .ToArrayAsync();

        if (pickListsWithUnprocessedPackages.Length == 0) {
            return;
        }

        // Process each pick list that has unprocessed packages
        foreach (var absEntry in pickListsWithUnprocessedPackages) {
            try {
                // Get closure information from external system
                var closureInfo = await adapter.GetPickListClosureInfo(absEntry);

                if (closureInfo.IsClosed) {
                    logger.LogInformation("Processing package movements for closed pick list {AbsEntry}", absEntry);

                    // Process the closure (clear commitments, handle movements)
                    await packageService.ProcessPickListClosureAsync(absEntry, closureInfo, DatabaseExtensions.SystemUserId);
                }
                else {
                    logger.LogWarning("Pick list {AbsEntry} has unprocessed packages but is not closed in external system", absEntry);
                }
            }
            catch (Exception ex) {
                logger.LogError(ex, "Error processing pick list closure for AbsEntry {AbsEntry}", absEntry);
                // Continue processing other pick lists
            }
        }
    }
}