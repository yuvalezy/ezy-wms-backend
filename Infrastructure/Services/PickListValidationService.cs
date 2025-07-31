using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class PickListValidationService(SystemDbContext db, IExternalSystemAdapter adapter) : IPickListValidationService {
    public async Task<(bool IsValid, string? ErrorMessage, PickingValidationResult? ValidationResult)> ValidateItemForPicking(
        PickListAddItemRequest request) {
        
        var validationResults = await adapter.ValidatePickingAddItem(request);

        if (validationResults.Length == 0) {
            return (false, "Item entry not found in pick", null);
        }

        if (!validationResults[0].IsValid) {
            return (false, validationResults[0].ErrorMessage, null);
        }

        return (true, null, validationResults[0]);
    }

    public async Task<int> CalculateBinOnHandQuantity(string itemCode, int? binEntry, PickingValidationResult validationResult) {
        int result = db.PickLists
            .Where(p => p.ItemCode == itemCode && p.BinEntry == binEntry && 
                        (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
            .Select(p => p.Quantity)
            .Concat(
                db.TransferLines
                    .Where(t => t.ItemCode == itemCode && t.BinEntry == binEntry && 
                                (t.LineStatus == LineStatus.Open || t.LineStatus == LineStatus.Processing))
                    .Select(t => t.Quantity)
            )
            .Sum();

        return validationResult.BinOnHand - result;
    }

    public async Task<(bool IsValid, string? ErrorMessage, PickingValidationResult? SelectedValidation)> ValidateQuantityAgainstPickList(int absEntry, string itemCode, int quantity, IEnumerable<PickingValidationResult> validationResults) {
        var dbPickedQuantity = await db.PickLists
            .Where(v => v.AbsEntry == absEntry && v.ItemCode == itemCode && 
                        (v.Status == ObjectStatus.Open || v.Status == ObjectStatus.Processing))
            .GroupBy(v => v.PickEntry)
            .Select(v => new { PickEntry = v.Key, Quantity = v.Sum(vv => vv.Quantity) })
            .ToArrayAsync();

        var check = (from v in validationResults.Where(a => a.IsValid)
                join p in dbPickedQuantity on v.PickEntry equals p.PickEntry into gj
                from sub in gj.DefaultIfEmpty()
                where v.OpenQuantity - (sub?.Quantity ?? 0) >= 0
                select new { ValidationResult = v, PickedQuantity = sub?.Quantity ?? 0 })
            .FirstOrDefault();

        if (check == null) {
            return (false, "Quantity exceeds open quantity", null);
        }

        check.ValidationResult.OpenQuantity -= check.PickedQuantity;
        return (true, null, check.ValidationResult);
    }

    public async Task<Dictionary<string, int>> CalculateOpenQuantitiesForPickList(int absEntry, IEnumerable<PickingDetailItemResponse> pickingDetails) {
        var dbPicked = await db.PickLists
            .Where(p => p.AbsEntry == absEntry &&
                        (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
            .GroupBy(p => p.ItemCode)
            .Select(g => new { ItemCode = g.Key, PickedQty = g.Sum(p => p.Quantity) })
            .ToDictionaryAsync(x => x.ItemCode, x => x.PickedQty);

        var itemOpenQuantities = new Dictionary<string, int>();
        foreach (var item in pickingDetails) {
            var pickedQty = dbPicked.TryGetValue(item.ItemCode, out var qty) ? qty : 0;
            var openQty = item.OpenQuantity - pickedQty;

            if (itemOpenQuantities.ContainsKey(item.ItemCode)) {
                itemOpenQuantities[item.ItemCode] += openQty;
            }
            else {
                itemOpenQuantities[item.ItemCode] = openQty;
            }
        }

        return itemOpenQuantities;
    }
}