using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.Transfer;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class TransferCreation : IDisposable {
    public TransferCreation(
        SboCompany sboCompany, 
        int transferNumber, 
        string whsCode, 
        string? comments, 
        int series, 
        Dictionary<string, TransferCreationDataResponse> data, 
        ILoggerFactory loggerFactory) {
        throw new NotImplementedException();
    }

    public int Entry { get; private set; }
    public int Number { get; private set; }

    public ProcessTransferResponse Execute() {
        throw new NotImplementedException();
    }

    public void Dispose() {
        throw new NotImplementedException();
    }
}