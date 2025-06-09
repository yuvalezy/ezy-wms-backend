using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.InventoryCounting;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class CountingCreation : IDisposable {
    public CountingCreation(
        SboCompany sboCompany,
        int countingNumber,
        string whsCode,
        int series,
        Dictionary<string, InventoryCountingCreationDataResponse> data,
        ILoggerFactory loggerFactory) {
        throw new NotImplementedException();
    }

    public (int Entry, int Number) NewEntry { get; private set; }

    public ProcessInventoryCountingResponse Execute() {
        throw new NotImplementedException();
    }

    public void Dispose() {
        throw new NotImplementedException();
    }
}