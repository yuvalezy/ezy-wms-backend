using Core.Models;

namespace Core.Interfaces;

public interface IPublicService {
    Task<IEnumerable<Warehouse>>     GetWarehousesAsync(string[]? filter);
    Task<HomeInfo>                   GetHomeInfoAsync(string      warehouse);
    Task<UserInfoResponse>           GetUserInfoAsync(SessionInfo info);
    Task<IEnumerable<ExternalValue>> GetVendorsAsync();
    Task<BinLocation?>               ScanBinLocationAsync(string bin);
}