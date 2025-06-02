using Core.Interfaces;
using Core.Models;

namespace Adapters.CrossPlatform.SBO;

public class SapBusinessOneServiceLayerAdapter : IExternalSystemAdapter {
    public Task<ExternalUserResponse?> GetUserInfoAsync(string id) {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ExternalUserResponse>> GetUsersAsync() {
        throw new NotImplementedException();
    }
}