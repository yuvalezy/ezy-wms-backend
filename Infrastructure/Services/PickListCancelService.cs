using Core.DTOs.Items;
using Core.DTOs.PickList;
using Core.DTOs.Transfer;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickListCancelService(
    IPickListProcessService pickListProcessService,
    ITransferDocumentService transferDocumentService,
    ITransferLineService transferLineService,
    IExternalSystemAdapter adapter,
    ISettings settings,
    IExternalSystemAlertService alertService,
    ILogger<PickListCancelService> logger) : IPickListCancelService {
    public async Task<ProcessPickListCancelResponse> CancelPickListAsync(int absEntry, SessionInfo sessionInfo) {
        // Process the picking in case something has not been synced into SAP B1
        var response = await pickListProcessService.ProcessPickList(absEntry, sessionInfo.Guid);
        if (response.Status != ResponseStatus.Ok && response.ErrorMessage != $"No open pick list items found for AbsEntry {absEntry}") {
            return response.ToDto();
        }

        // Get cancel bin entry from settings
        var enableBinLocations = sessionInfo.EnableBinLocations;
        int cancelBinEntry = enableBinLocations ? settings.GetCancelPickingBinEntry(sessionInfo.Warehouse) : 0;
        if (enableBinLocations && cancelBinEntry == 0) {
            throw new Exception("Cancel Picking Bin Entry is not set in the Settings.Filters.CancelPickingBinEntry");
        }

        // Get current picked data from SAP
        var selection = (await adapter.GetPickingSelection(absEntry)).ToArray();

        // Get alert recipients
        var alertRecipients = await alertService.GetAlertRecipientsAsync(AlertableObjectType.PickListCancellation);

        // Cancel Pick List in SAP
        response = await adapter.CancelPickList(absEntry, selection, sessionInfo.Warehouse, cancelBinEntry, enableBinLocations, alertRecipients);
        if (selection.Length == 0)
            return response.ToDto();

        // Create a new transfer for cancelled pick list items
        var transfer = await transferDocumentService.CreateTransfer(new CreateTransferRequest {
            Name = $"Cancelación Picking {absEntry}",
            Comments = "Reubicación de artículos de picking"
        }, sessionInfo);

        await HandleRegularItemCancellation(selection, transfer.Id, cancelBinEntry, sessionInfo);

        return response.ToDto(transfer.Id);
    }

    private async Task HandleRegularItemCancellation(PickingSelectionResponse[] selection, Guid transferId, int cancelBinEntry, SessionInfo sessionInfo) {
        // Add picked items into transfer.
        var items = selection
        .GroupBy(v => new { v.ItemCode, BarCode = v.CodeBars, v.NumInBuy, v.PackUn })
        .Select(v => new RegularItemCancellationRequest(v.Key.ItemCode, v.Key.BarCode, v.Key.NumInBuy, v.Key.PackUn, v.Sum(w => w.Quantity)));

        foreach (var item in items) {
            await ProcessRegularItem(item, transferId, cancelBinEntry, sessionInfo);
        }
    }

    private record RegularItemCancellationRequest(string ItemCode, string BarCode, decimal NumInBuy, decimal PackUn, decimal Quantity);

    private async Task ProcessRegularItem(RegularItemCancellationRequest item, Guid transferId, int cancelBinEntry, SessionInfo sessionInfo) {
        var addRequest = new TransferAddItemRequest {
            ID = transferId,
            ItemCode = item.ItemCode,
            BarCode = item.BarCode,
            Type = SourceTarget.Source,
        };

        decimal quantity = item.Quantity;
        decimal numInBuy = item.NumInBuy;
        decimal packUn = item.PackUn;

        // Calculate packs
        decimal packs = packUn == 1 ? 0 : Math.Floor(quantity / (numInBuy * packUn));

        // Calculate dozens
        decimal remainderAfterPacks = packUn == 1 ? quantity : (quantity - packs * numInBuy * packUn);
        decimal dozens = Math.Floor(remainderAfterPacks / numInBuy);

        // Calculate units
        decimal units = (remainderAfterPacks % numInBuy);

        addRequest.BinEntry = cancelBinEntry;

        if (packs > 0) {
            addRequest.Quantity = packs;
            addRequest.Unit = UnitType.Pack;
            await transferLineService.AddItem(sessionInfo, addRequest);
        }

        if (dozens > 0) {
            addRequest.Quantity = dozens;
            addRequest.Unit = UnitType.Dozen;
            await transferLineService.AddItem(sessionInfo, addRequest);
        }

        if (units > 0) {
            addRequest.Quantity = units;
            addRequest.Unit = UnitType.Unit;
            await transferLineService.AddItem(sessionInfo, addRequest);
        }
    }
}
