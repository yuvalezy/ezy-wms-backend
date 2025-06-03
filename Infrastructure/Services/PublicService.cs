using Core.DTOs;
using Core.Enums;
using Core.Interfaces;
using Core.Models;

namespace Infrastructure.Services;

public class PublicService(IExternalSystemAdapter externalSystemAdapter, ISettings settings, IUserService userService) : IPublicService {
    public async Task<IEnumerable<Warehouse>> GetWarehousesAsync(string[]? filter) {
        var warehouses = await externalSystemAdapter.GetWarehousesAsync(filter);
        return warehouses;
    }

    public async Task<HomeInfoResponse> GetHomeInfoAsync(string warehouse) {
        var homeInfo = new HomeInfoResponse();

        var response = await externalSystemAdapter.GetItemAndBinCount(warehouse);
        homeInfo.BinCheck  = response.binCount;
        homeInfo.ItemCheck = response.itemCount;

        //todo FINISH COUNT FOR Goods Receipt
//                    (select Count(1) from "@LW_YUVAL08_GRPO" where "U_WhsCode" = @WhsCode and "U_Status" in ('O', 'I') and "U_Type" <> 'R') "GoodsReceipt",
        //todo FINISH COUNT FOR Receipt Confimation
//                    (select Count(1) from "@LW_YUVAL08_GRPO" where "U_WhsCode" = @WhsCode and "U_Status" in ('O', 'I') and "U_Type" = 'R')  "ReceiptConfirmation",
        //todo FINISH COUNT FOR Pick list
//                    (select Count(distinct PICKS."AbsEntry")
//                     from OPKL PICKS
//                              inner join PKL1 T1 on T1."AbsEntry" = PICKS."AbsEntry"
//                              inner join OILM T2 on T2.TransType = T1.BaseObject and T2.DocEntry = T1.OrderEntry and T2.DocLineNum = T1.OrderLine
//                     where T2.LocCode = @WhsCode
//                       and PICKS."Status" in ('R', 'P', 'D'))                                                                               "Picking",
        //todo FINISH COUNT FOR Counting
//                    (select Count(1) from "@LW_YUVAL08_OINC" where "U_WhsCode" = @WhsCode and "U_Status" in ('O', 'I'))                     "Counting",
        //todo FINISH COUNT FOR Transfers
//                    (select Count(1) from "@LW_YUVAL08_TRANS" where "U_WhsCode" = @WhsCode and "U_Status" in ('O', 'I'))                    "Transfers"

        return homeInfo;
    }

    public async Task<UserInfoResponse> GetUserInfoAsync(SessionInfo info) {
        var user = await userService.GetUserAsync(Guid.Parse(info.UserId));
        return new UserInfoResponse {
            ID               = info.UserId,
            Name             = info.Name,
            CurrentWarehouse = info.Warehouse,
            BinLocations     = info.EnableBinLocations,
            Roles            = info.Roles,
            Warehouses       = await externalSystemAdapter.GetWarehousesAsync(info.SuperUser ? null : user!.Warehouses.ToArray()),
            SuperUser        = info.SuperUser,
            Settings         = settings.Options,
        };
    }

    public async Task<IEnumerable<ExternalValue>> GetVendorsAsync() => await externalSystemAdapter.GetVendorsAsync();

    public async Task<BinLocation?> ScanBinLocationAsync(string bin) => await externalSystemAdapter.ScanBinLocationAsync(bin);

    public async Task<IEnumerable<Item>> ScanItemBarCodeAsync(string scanCode, bool item = false) => await externalSystemAdapter.ScanItemBarCodeAsync(scanCode, item);

    public async Task<IEnumerable<ItemCheckResponse>> ItemCheckAsync(string? itemCode, string? barcode) => await externalSystemAdapter.ItemCheckAsync(itemCode, barcode);

    public async Task<IEnumerable<BinContent>> BinCheckAsync(int binEntry) => await externalSystemAdapter.BinCheckAsync(binEntry);

    public async Task<IEnumerable<ItemStockResponse>> ItemStockAsync(string itemCode, string whsCode) => await externalSystemAdapter.ItemStockAsync(itemCode, whsCode);

    public async Task<UpdateItemBarCodeResponse> UpdateItemBarCode(string userId, UpdateBarCodeRequest request) {
        if (request.AddBarcodes != null) {
            foreach (string barcode in request.AddBarcodes) {
                var check = (await externalSystemAdapter.ScanItemBarCodeAsync(barcode)).FirstOrDefault();
                if (check != null) {
                    return new UpdateItemBarCodeResponse() {
                        ExistItem = check.Code, Status = ResponseStatus.Error
                    };
                }
            }
        }

        var response = await externalSystemAdapter.UpdateItemBarCode(request);
        //todo create logs for the user id
        // item.UserFields.Fields.Item("U_LW_UPDATE_USER").Value      = employeeID;
        // item.UserFields.Fields.Item("U_LW_UPDATE_TIMESTAMP").Value = DateTime.Now;

        return response;
    }
}