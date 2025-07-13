using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Models;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListLineService(SystemDbContext db, IExternalSystemAdapter adapter, ILogger<PickListService> logger, ISettings settings) : IPickListLineService {
    public async Task<PickListAddItemResponse> AddItem(SessionInfo sessionInfo, PickListAddItemRequest request) {
        if (request.Unit != UnitType.Unit) {
            var items = await adapter.ItemCheckAsync(request.ItemCode, null);
            var item  = items.FirstOrDefault();
            if (item == null) {
                throw new ApiErrorException((int)AddItemReturnValueType.ItemCodeNotFound, new { request.ItemCode, BarCode = (string?)null });
            }

            request.Quantity *= item.NumInBuy * (request.Unit == UnitType.Pack ? item.PurPackUn : 1);
        }

        // Validate the add item request
        var validationResults = await adapter.ValidatePickingAddItem(request);

        if (validationResults.Length == 0) {
            return new PickListAddItemResponse {
                ErrorMessage = "Item entry not found in pick",
                Status       = ResponseStatus.Error,
            };
        }

        if (!validationResults[0].IsValid)
            return new PickListAddItemResponse {
                ErrorMessage = validationResults[0].ErrorMessage,
                Status       = ResponseStatus.Error
            };

        int result = db.PickLists
            .Where(p => p.ItemCode == request.ItemCode && p.BinEntry == request.BinEntry && (p.Status == ObjectStatus.Open || p.Status == ObjectStatus.Processing))
            .Select(p => p.Quantity)
            .Concat(
                db.TransferLines
                    .Where(t => t.ItemCode == request.ItemCode && t.BinEntry == request.BinEntry && (t.LineStatus == LineStatus.Open || t.LineStatus == LineStatus.Processing))
                    .Select(t => t.Quantity)
            )
            .Sum();

        int binOnHand = validationResults.First().BinOnHand - result;

        var dbPickedQuantity = await db.PickLists.Where(v => v.AbsEntry == request.ID && v.ItemCode == request.ItemCode && (v.Status == ObjectStatus.Open || v.Status == ObjectStatus.Processing))
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
            return new PickListAddItemResponse {
                Status       = ResponseStatus.Error,
                ErrorMessage = "Quantity exceeds open quantity",
            };
        }

        check.ValidationResult.OpenQuantity -= check.PickedQuantity;

        if (request.Quantity > binOnHand) {
            return new PickListAddItemResponse {
                Status       = ResponseStatus.Error,
                ErrorMessage = "Quantity exceeds bin available stock",
            };
        }

        var pickList = new PickList {
            AbsEntry        = request.ID,
            PickEntry       = check.ValidationResult.PickEntry ?? request.PickEntry ?? 0,
            ItemCode        = request.ItemCode,
            Quantity        = request.Quantity,
            BinEntry        = request.BinEntry,
            Unit            = request.Unit,
            Status          = ObjectStatus.Open,
            SyncStatus      = SyncStatus.Pending,
            CreatedAt       = DateTime.UtcNow,
            CreatedByUserId = sessionInfo.Guid
        };

        await db.PickLists.AddAsync(pickList);
        await db.SaveChangesAsync();

        return PickListAddItemResponse.OkResponse;
    }
}