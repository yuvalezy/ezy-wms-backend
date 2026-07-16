using Core.DTOs.Items;
using Core.DTOs.Transfer;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <inheritdoc cref="ITransferReportService"/>
public class TransferReportService(
    SystemDbContext db,
    IExternalSystemAdapter adapter,
    ITransferQuantityRepairService quantityRepairService) : ITransferReportService {

    public async Task<TransferSummaryReportResponse> GetTransferSummaryReport(Guid id) {
        // Restate before the lines are loaded and summed. This read sees source and target together, which is
        // the only condition under which the repair can keep the two sides in balance. Without it the report
        // would disagree with the scanning screen, which restates on every read.
        await quantityRepairService.RepairAsync(id);

        var transfer = await db.Transfers
        .Include(t => t.CreatedByUser)
        .Include(t => t.Lines.Where(l => l.LineStatus != LineStatus.Closed))
        .FirstOrDefaultAsync(t => t.Id == id);

        if (transfer == null) {
            throw new KeyNotFoundException($"Transfer with ID {id} not found.");
        }

        var lines = transfer.Lines.ToList();

        string[] itemCodes = lines.Select(l => l.ItemCode).Distinct().ToArray();
        int[] binEntries = lines.Where(l => l.BinEntry.HasValue).Select(l => l.BinEntry!.Value).Distinct().ToArray();

        // One call per lookup, not one per item/bin: this reports a whole document, where the scanning screen's
        // per-item ItemCheckAsync would become an N+1 against SAP.
        var itemsTask = adapter.BulkItemCheckAsync(itemCodes);
        var binCodesTask = adapter.BulkGetBinCodesAsync(binEntries);
        await Task.WhenAll(itemsTask, binCodesTask);

        return new TransferSummaryReportResponse {
            Id = transfer.Id,
            Number = transfer.Number,
            Name = transfer.Name,
            Date = transfer.Date,
            Status = transfer.Status,
            Comments = transfer.Comments,
            WhsCode = transfer.WhsCode,
            TargetWhsCode = transfer.TargetWhsCode,
            CreatedByUserName = transfer.CreatedByUser?.FullName,
            SapDocNumber = transfer.SapDocNumber,
            Lines = BuildLines(lines, await itemsTask, await binCodesTask)
        };
    }

    /// <summary>
    /// Collapses a transfer's lines into one row per item, carrying both sides' bins. Pure: no database, no SAP
    /// — the caller supplies the item and bin-code lookups.
    /// </summary>
    /// <param name="lines">The transfer's non-closed lines, both sides.</param>
    /// <param name="items">Item master keyed by item code. A missing entry degrades to the item code as its
    /// name and neutral conversion factors, rather than failing the whole report.</param>
    /// <param name="binCodes">Bin code keyed by bin entry. A missing entry falls back to the entry number.</param>
    public static List<TransferSummaryReportLine> BuildLines(
        IReadOnlyCollection<TransferLine> lines,
        IReadOnlyDictionary<string, ItemCheckResponse> items,
        IReadOnlyDictionary<int, string> binCodes) {

        return lines
        .GroupBy(l => l.ItemCode)
        .Select(group => {
            var sourceLines = group.Where(l => l.Type == SourceTarget.Source).ToList();
            var targetLines = group.Where(l => l.Type == SourceTarget.Target).ToList();

            decimal sourceQuantity = sourceLines.Sum(l => l.Quantity);
            decimal targetQuantity = targetLines.Sum(l => l.Quantity);

            items.TryGetValue(group.Key, out var item);

            return new TransferSummaryReportLine {
                ItemCode = group.Key,
                ItemName = item?.ItemName ?? group.Key,
                SourceQuantity = sourceQuantity,
                TargetQuantity = targetQuantity,
                OpenQuantity = sourceQuantity - targetQuantity,
                SourceBins = BuildBins(sourceLines, binCodes),
                TargetBins = BuildBins(targetLines, binCodes),
                NumInBuy = item?.NumInBuy ?? 1,
                BuyUnitMsr = item?.BuyUnitMsr ?? "",
                PurPackUn = item?.PurPackUn ?? 1,
                PurPackMsr = item?.PurPackMsr ?? "",
                Factor1 = item?.Factor1 ?? 1,
                Factor2 = item?.Factor2 ?? 1,
                Factor3 = item?.Factor3 ?? 1,
                Factor4 = item?.Factor4 ?? 1,
                CustomFields = item?.CustomFields
            };
        })
        .OrderBy(l => l.ItemCode, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    private static List<TransferContentBin> BuildBins(
        IEnumerable<TransferLine> lines,
        IReadOnlyDictionary<int, string> binCodes) {

        return lines
        .Where(l => l.BinEntry.HasValue)
        .GroupBy(l => l.BinEntry!.Value)
        .Select(g => new TransferContentBin {
            Entry = g.Key,
            Code = binCodes.TryGetValue(g.Key, out var code) ? code : g.Key.ToString(),
            Quantity = g.Sum(l => l.Quantity)
        })
        .OrderBy(b => b.Code, NaturalStringComparer.Instance)
        .ToList();
    }
}

/// <summary>
/// Orders strings so embedded numbers compare by value rather than by digit. Bin codes are letter-based and
/// zero-padded inconsistently (01-A1-10-04), so an ordinal sort puts 01-A1-10-40 before 01-A1-10-4.
/// </summary>
public sealed class NaturalStringComparer : IComparer<string> {
    public static readonly NaturalStringComparer Instance = new();

    public int Compare(string? x, string? y) {
        if (ReferenceEquals(x, y)) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        int i = 0, j = 0;
        while (i < x.Length && j < y.Length) {
            if (char.IsDigit(x[i]) && char.IsDigit(y[j])) {
                int startX = i, startY = j;
                while (i < x.Length && char.IsDigit(x[i])) i++;
                while (j < y.Length && char.IsDigit(y[j])) j++;

                // Compare magnitude first, so 4 < 40 regardless of zero padding.
                var numX = x.AsSpan(startX, i - startX).TrimStart('0');
                var numY = y.AsSpan(startY, j - startY).TrimStart('0');

                if (numX.Length != numY.Length) return numX.Length - numY.Length;

                int digits = numX.SequenceCompareTo(numY);
                if (digits != 0) return digits;

                // Same value, different padding ("4" vs "04"). Order the shorter run first so the result is a
                // total order rather than leaving these arbitrary to the sort's stability.
                int padding = (i - startX) - (j - startY);
                if (padding != 0) return padding;
            }
            else {
                int chars = char.ToUpperInvariant(x[i]).CompareTo(char.ToUpperInvariant(y[j]));
                if (chars != 0) return chars;
                i++;
                j++;
            }
        }

        return (x.Length - i) - (y.Length - j);
    }
}
