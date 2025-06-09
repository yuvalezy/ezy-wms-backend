using Core.Interfaces;
using Core.Models.Settings;

namespace Adapters.CrossPlatform.SBO.Services;

public class SboCompany(ISettings settings) {
    private SboSettings sboSettings = settings.SboSettings ?? throw new InvalidOperationException("SBO settings are not configured.");
    
    public bool ConnectCompany() {
        throw new NotImplementedException("SBO Company connection not implemented for cross-platform adapter");
    }
}