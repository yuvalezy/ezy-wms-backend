using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.Items;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class ItemBarCodeUpdate : IDisposable {
    public ItemBarCodeUpdate(
        SboDatabaseService dbService, 
        SboCompany sboCompany, 
        string itemCode, 
        string[]? addBarcodes, 
        string[]? removeBarcodes) {
        throw new NotImplementedException();
    }

    public UpdateItemBarCodeResponse Execute() {
        throw new NotImplementedException();
    }

    public void Dispose() {
        throw new NotImplementedException();
    }
}