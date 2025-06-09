using System.Text.Json;
using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.Items;
using Core.Enums;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class ItemBarCodeUpdate(SboCompany sboCompany, string itemCode, string[]? addBarcodes, string[]? removeBarcodes) : IDisposable {
    
    public async Task<UpdateItemBarCodeResponse> Execute() {
        var response = new UpdateItemBarCodeResponse();
        
        try {
            var item = await sboCompany.GetAsync<ItemMasterData>($"Items('{itemCode}')");
            
            if (item == null) {
                response.Status = ResponseStatus.Error;
                response.ErrorMessage = $"Item Code {itemCode} not found!";
                return response;
            }
            
            var currentBarcodes = item.ItemBarCodeCollection?.ToList() ?? new List<ItemBarCode>();
            
            RemoveBarcodes(currentBarcodes);
            AddNewBarcodes(currentBarcodes);
            
            var updateData = new {
                ItemBarCodeCollection = currentBarcodes
            };
            
            var patchResponse = await sboCompany.PatchAsync($"Items('{itemCode}')", updateData);
            
            if (patchResponse.success) {
                response.Status = ResponseStatus.Ok;
            } else {
                response.Status = ResponseStatus.Error;
                response.ErrorMessage = patchResponse.errorMessage ?? "Failed to update item barcodes";
            }
        }
        catch (Exception ex) {
            response.Status = ResponseStatus.Error;
            response.ErrorMessage = ex.Message;
        }
        
        return response;
    }
    
    private void AddNewBarcodes(List<ItemBarCode> currentBarcodes) {
        if (addBarcodes == null) return;
        
        foreach (string barcode in addBarcodes) {
            if (currentBarcodes.All(b => b.Barcode != barcode)) {
                currentBarcodes.Add(new ItemBarCode {
                    Barcode = barcode,
                    UoMEntry = -1
                });
            }
        }
    }
    
    private void RemoveBarcodes(List<ItemBarCode> currentBarcodes) {
        if (removeBarcodes == null) return;
        
        for (int i = currentBarcodes.Count - 1; i >= 0; i--) {
            if (removeBarcodes.Contains(currentBarcodes[i].Barcode)) {
                currentBarcodes.RemoveAt(i);
            }
        }
    }

    public void Dispose() {
    }
    
    private class ItemMasterData {
        public string ItemCode { get; set; } = string.Empty;
        public string BarCode { get; set; } = string.Empty;
        public ItemBarCode[]? ItemBarCodeCollection { get; set; }
    }
    
    private class ItemBarCode {
        public string Barcode { get; set; } = string.Empty;
        public int UoMEntry { get; set; }
    }
}