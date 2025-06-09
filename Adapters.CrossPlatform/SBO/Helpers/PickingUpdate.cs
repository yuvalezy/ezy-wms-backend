using Adapters.CrossPlatform.SBO.Services;
using Core.Entities;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class PickingUpdate(
    int                absEntry,
    List<PickList>     data,
    SboDatabaseService dbService,
    SboCompany         sboCompany,
    string?            filtersPickReady,
    ILoggerFactory     loggerFactory) : IDisposable {
    private ILogger<PickingUpdate> logger = loggerFactory.CreateLogger<PickingUpdate>();
    public void Execute() {
        throw new NotImplementedException();
    }

    public void Dispose() {
    }
}