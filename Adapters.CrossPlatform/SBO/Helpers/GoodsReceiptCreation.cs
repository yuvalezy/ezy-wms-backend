using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.GoodsReceipt;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class GoodsReceiptCreation : IDisposable {
    public GoodsReceiptCreation(
        SboCompany sboCompany, 
        int number, 
        string whsCode, 
        int series, 
        Dictionary<string, List<GoodsReceiptCreationDataResponse>> data) {
        throw new NotImplementedException();
    }

    private List<(int Entry, int Number)> NewEntries { get; } = [];

    public ProcessGoodsReceiptResult Execute() {
        throw new NotImplementedException();
    }

    public void Dispose() {
        throw new NotImplementedException();
    }
}