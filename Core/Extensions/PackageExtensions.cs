using Core.DTOs.Package;
using Core.Entities;
using System.Text.Json;
using Core.Interfaces;

namespace Core.Extensions;

public static class PackageExtensions {
    public static async Task<PackageDto> ToDto(this Package package, IExternalSystemAdapter adapter) {
        return new PackageDto {
            Id               = package.Id,
            Barcode          = package.Barcode,
            Status           = package.Status,
            WhsCode          = package.WhsCode,
            BinEntry         = package.BinEntry,
            BinCode          = await GetBinCodeAsync(package.BinEntry, adapter),
            CreatedBy        = package.CreatedByUser.ToDto(),
            CreatedAt        = package.CreatedAt,
            ClosedAt         = package.ClosedAt,
            ClosedBy         = package.ClosedBy,
            Notes            = package.Notes,
            CustomAttributes = ParseCustomAttributes(package.CustomAttributes),
            Contents         = await Task.WhenAll(package.Contents.Select(async c => await c.ToDto(adapter))),
            LocationHistory  = await Task.WhenAll(package.LocationHistory.Select(async c => await c.ToDto(adapter)))
        };
    }

    public static async Task<PackageContentDto> ToDto(this PackageContent content, IExternalSystemAdapter adapter) {
        var itemData = await adapter.GetItemInfo(content.ItemCode);
        return new PackageContentDto {
            Id        = content.Id,
            PackageId = content.PackageId,
            ItemCode  = content.ItemCode,
            ItemData  = itemData,
            Quantity  = content.Quantity,
            WhsCode   = content.WhsCode,
            BinCode   = await GetBinCodeAsync(content.BinEntry, adapter),
            CreatedAt = content.CreatedAt,
            CreatedBy = content.CreatedByUser.ToDto(),
        };
    }


    public static PackageTransactionDto ToDto(this PackageTransaction transaction) {
        return new PackageTransactionDto {
            Id                    = transaction.Id,
            PackageId             = transaction.PackageId,
            TransactionType       = transaction.TransactionType,
            ItemCode              = transaction.ItemCode,
            Quantity              = transaction.Quantity,
            UnitType              = transaction.UnitType,
            SourceOperationType   = transaction.SourceOperationType,
            SourceOperationId     = transaction.SourceOperationId,
            SourceOperationLineId = transaction.SourceOperationLineId,
            User                  = transaction.CreatedByUser.ToDto(),
            TransactionDate       = transaction.TransactionDate,
            Notes                 = transaction.Notes
        };
    }

    public static async Task<PackageLocationHistoryDto> ToDto(this PackageLocationHistory history, IExternalSystemAdapter adapter) {
        return new PackageLocationHistoryDto {
            Id                  = history.Id,
            PackageId           = history.PackageId,
            MovementType        = history.MovementType,
            FromWhsCode         = history.FromWhsCode,
            FromBinEntry        = history.FromBinEntry,
            FromBinCode         = await GetBinCodeAsync(history.FromBinEntry, adapter),
            ToWhsCode           = history.ToWhsCode,
            ToBinEntry          = history.ToBinEntry,
            ToBinCode           = await GetBinCodeAsync(history.ToBinEntry, adapter),
            SourceOperationType = history.SourceOperationType,
            SourceOperationId   = history.SourceOperationId,
            User                = history.CreatedByUser.ToDto(),
            MovementDate        = history.MovementDate,
            Notes               = history.Notes
        };
    }

    public static async Task<PackageInconsistencyDto> ToDto(this PackageInconsistency inconsistency, IExternalSystemAdapter adapter) {
        return new PackageInconsistencyDto {
            Id                = inconsistency.Id,
            PackageId         = inconsistency.PackageId,
            PackageBarcode    = inconsistency.PackageBarcode,
            ItemCode          = inconsistency.ItemCode,
            BatchNo           = inconsistency.BatchNo,
            SerialNo          = inconsistency.SerialNo,
            WhsCode           = inconsistency.WhsCode,
            BinCode           = await GetBinCodeAsync(inconsistency.BinEntry, adapter),
            SapQuantity       = inconsistency.SapQuantity,
            WmsQuantity       = inconsistency.WmsQuantity,
            PackageQuantity   = inconsistency.PackageQuantity,
            InconsistencyType = inconsistency.InconsistencyType,
            Severity          = inconsistency.Severity,
            DetectedAt        = inconsistency.DetectedAt,
            IsResolved        = inconsistency.IsResolved,
            ResolvedAt        = inconsistency.ResolvedAt,
            ResolvedBy        = inconsistency.ResolvedBy,
            ResolutionAction  = inconsistency.ResolutionAction,
            ErrorMessage      = inconsistency.ErrorMessage,
            Notes             = inconsistency.Notes
        };
    }

    private static Dictionary<string, object> ParseCustomAttributes(string? customAttributesJson) {
        if (string.IsNullOrEmpty(customAttributesJson))
            return new Dictionary<string, object>();

        try {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(customAttributesJson)
                   ?? new Dictionary<string, object>();
        }
        catch {
            return new Dictionary<string, object>();
        }
    }

    private static async Task<string?> GetBinCodeAsync(int? binEntry, IExternalSystemAdapter adapter) => binEntry.HasValue ? await adapter.GetBinCodeAsync(binEntry.Value) : null;
}