using System.Text.Json;
using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.Items;
using Core.Enums;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class ItemBarCodeUpdate(SboCompany sboCompany, string itemCode, string[]? addBarcodes, string[]? removeBarcodes) : IDisposable {
    
    public async Task<UpdateItemBarCodeResponse> Execute() {
        var response = new UpdateItemBarCodeResponse();
        
        try {
            // Remove barcodes first
            if (removeBarcodes is { Length: > 0 }) {
                var removeResult = await RemoveBarcodesAsync();
                if (!removeResult.success) {
                    response.Status = ResponseStatus.Error;
                    response.ErrorMessage = removeResult.errorMessage ?? "Failed to remove barcodes";
                    return response;
                }
            }
            
            // Add new barcodes
            if (addBarcodes is { Length: > 0 }) {
                var addResult = await AddBarcodesAsync();
                if (!addResult.success) {
                    response.Status = ResponseStatus.Error;
                    response.ErrorMessage = addResult.errorMessage ?? "Failed to add barcodes";
                    return response;
                }
            }
            
            response.Status = ResponseStatus.Ok;
        }
        catch (Exception ex) {
            response.Status = ResponseStatus.Error;
            response.ErrorMessage = ex.Message;
        }
        
        return response;
    }
    
    private async Task<(bool success, string? errorMessage)> RemoveBarcodesAsync() {
        if (removeBarcodes == null || removeBarcodes.Length == 0) {
            return (true, null);
        }
        
        foreach (string barcode in removeBarcodes) {
            // Get the AbsEntry for this barcode
            var barcodeEntries = await sboCompany.GetAsync<BarCodeQueryResult>($"BarCodes?$select=AbsEntry&$filter=ItemNo eq '{itemCode}' and Barcode eq '{barcode}' and UoMEntry eq -1");

            if (barcodeEntries?.Value == null) {
                throw new KeyNotFoundException($"Entry for barcode {barcode} not found.");
            }
            foreach (var entry in barcodeEntries.Value) {
                bool deleteSuccess = await sboCompany.DeleteAsync($"BarCodes({entry.AbsEntry})");
                if (!deleteSuccess) {
                    return (false, $"Failed to delete barcode {barcode}");
                }
            }
        }
        
        return (true, null);
    }
    
    private async Task<(bool success, string? errorMessage)> AddBarcodesAsync() {
        if (addBarcodes == null || addBarcodes.Length == 0) {
            return (true, null);
        }
        
        var newBarcodes = new List<ItemBarCode>();
        
        foreach (string barcode in addBarcodes) {
            newBarcodes.Add(new ItemBarCode {
                Barcode = barcode,
                UoMEntry = -1
            });
        }
        
        var updateData = new {
            ItemBarCodeCollection = newBarcodes
        };
        
        return await sboCompany.PatchAsync($"Items('{itemCode}')", updateData);
    }

    public void Dispose() {
    }
    
    private class ItemBarCode {
        public string Barcode { get; set; } = string.Empty;
        public int UoMEntry { get; set; }
    }
    
    private class BarCodeQueryResult {
        public BarCodeEntry[]? Value { get; set; }
    }
    
    private class BarCodeEntry {
        public int AbsEntry { get; set; }
    }
}