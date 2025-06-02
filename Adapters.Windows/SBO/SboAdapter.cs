using Adapters.Windows.SBO.Repositories;
using Core.Interfaces;
using Core.Models;

namespace Adapters.Windows.SBO;

public class SboAdapter(SboEmployeeRepository employeeRepository, SboGeneralRepository generalRepository) : IExternalSystemAdapter {
    public async Task<ExternalValue?>                GetUserInfoAsync(string id)                 => await employeeRepository.GetByIdAsync(id);
    public async Task<IEnumerable<ExternalValue>>    GetUsersAsync()                             => await employeeRepository.GetAllAsync();
    public async Task<string?>                       GetCompanyNameAsync()                       => await generalRepository.GetCompanyNameAsync();
    public async Task<IEnumerable<Warehouse>>        GetWarehousesAsync(string[]? filter = null) => await generalRepository.GetWarehousesAsync(filter);
    public async Task<Warehouse?>                    GetWarehouseAsync(string     id)            => (await generalRepository.GetWarehousesAsync([id])).FirstOrDefault();
    public async Task<(int itemCount, int binCount)> GetItemAndBinCount(string    warehouse)     => await generalRepository.GetItemAndBinCountAsync(warehouse);
    public async Task<IEnumerable<ExternalValue>>    GetVendorsAsync()                           => await generalRepository.GetVendorsAsync();
    public async Task<bool>                          ValidateVendorsAsync(string id)             => await generalRepository.ValidateVendorsAsync(id);
    public async Task<BinLocation?>                  ScanBinLocationAsync(string bin)            => await generalRepository.ScanBinLocationAsync(bin);
    public async Task<IEnumerable<Item>>             ScanItemBarCodeAsync(string scanCode, bool item = false) => await generalRepository.ScanItemBarCodeAsync(scanCode, item);
    public async Task<IEnumerable<ItemCheckResponse>> ItemCheckAsync(string itemCode, string barcode) => await generalRepository.ItemCheckAsync(itemCode, barcode);
    public async Task<IEnumerable<BinContent>>       BinCheckAsync(int binEntry)                 => await generalRepository.BinCheckAsync(binEntry);
    public async Task<IEnumerable<ItemStockResponse>> ItemStockAsync(string itemCode, string whsCode) => await generalRepository.ItemStockAsync(itemCode, whsCode);
}