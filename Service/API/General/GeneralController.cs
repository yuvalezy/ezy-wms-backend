using System;
using System.Collections.Generic;
using System.Web.Http;
using Service.API.General.Models;
using Service.API.Models;
using Service.Shared;

namespace Service.API.General;

public class PublicController : LWApiController {
    private readonly Data data = new();

    [HttpGet]
    [ActionName("CompanyInfo")]
    public CompanyInfo GetCompanyInfo() =>
        new() {
            Name = Global.CompanyName,
        };
}

[Authorize]
public class GeneralController : LWApiController {
    private readonly Data data = new();

    [HttpGet]
    [ActionName("HomeInfo")]
    public HomeInfo GetHomeInfo() {
        var employeeData = data.General.GetEmployeeData(EmployeeID);
        return data.General.GetHomeInfo(employeeData);
    }

    [HttpGet]
    [ActionName("UserInfo")]
    public UserInfo GetUserInfo() {
        var employeeData = data.General.GetEmployeeData(EmployeeID);
        return new UserInfo {
            ID             = EmployeeID,
            Name           = employeeData.Name,
            Branch         = employeeData.WhsName,
            BinLocations   = employeeData.EnableBin,
            Authorizations = Global.UserAuthorizations[EmployeeID],
            Settings = new ApplicationSettings {
                GRPOModificationSupervisor   = Global.GRPOModificationsRequiredSupervisor,
                GRPOCreateSupervisorRequired = Global.GRPOCreateSupervisorRequired,
                TransferTargetItems          = Global.TransferTargetItems
            }
        };
    }

    [HttpGet]
    [ActionName("Vendors")]
    public IEnumerable<BusinessPartner> GetVendors() {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt, Authorization.GoodsReceiptSupervisor, Authorization.GoodsReceiptConfirmation,
                Authorization.GoodsReceiptConfirmationSupervisor))
            throw new UnauthorizedAccessException("You don't have access for vendors list");
        return data.General.GetVendors();
    }

    [HttpGet]
    [ActionName("ScanBinLocation")]
    public BinLocation ScanBinLocation([FromUri] string bin) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt, Authorization.GoodsReceiptConfirmation, Authorization.Counting))
            throw new UnauthorizedAccessException("You don't have access for Scan Bin Location");
        return data.General.ScanBinLocation(bin);
    }

    [HttpGet]
    [ActionName("ItemByBarCode")]
    public IEnumerable<Item> ScanItemBarCode([FromUri] string scanCode, [FromUri] bool item = false) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt, Authorization.GoodsReceiptConfirmation, Authorization.TransferRequest))
            throw new UnauthorizedAccessException("You don't have access for Scan Item BarCode");
        return data.General.ScanItemBarCode(scanCode, item);
    }

    [HttpPost]
    [ActionName("ItemCheck")]
    public IEnumerable<ItemCheckResponse> ItemCheck([FromBody] ItemBarCodeParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor, Authorization.CountingSupervisor, Authorization.TransferSupervisor, Authorization.PickingSupervisor,
                Authorization.GoodsReceiptConfirmation, Authorization.GoodsReceiptConfirmationSupervisor))
            throw new UnauthorizedAccessException("You don't have access for Item Check");
        return data.General.ItemCheck(parameters.ItemCode, parameters.Barcode);
    }

    [HttpGet]
    [ActionName("BinCheck")]
    public IEnumerable<BinContent> BinCheck([FromUri] int binEntry) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor, Authorization.CountingSupervisor, Authorization.TransferSupervisor, Authorization.PickingSupervisor,
                Authorization.GoodsReceiptConfirmation, Authorization.GoodsReceiptConfirmationSupervisor))
            throw new UnauthorizedAccessException("You don't have access for Bin Check");
        return data.General.BinCheck(binEntry);
    }

    [HttpPost]
    [ActionName("ItemStock")]
    public IEnumerable<ItemStockResponse> ItemStock([FromBody] ItemBarCodeParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor, Authorization.CountingSupervisor, Authorization.TransferSupervisor, Authorization.GoodsReceiptConfirmation,
                Authorization.GoodsReceiptConfirmationSupervisor))
            throw new UnauthorizedAccessException("You don't have access for Item Stock");
        string whsCode = Data.General.GetEmployeeData(EmployeeID).WhsCode;
        return data.General.ItemStock(parameters.ItemCode, whsCode);
    }

    [HttpPost]
    [ActionName("UpdateItemBarCode")]
    public UpdateItemBarCodeResponse AddItemBarCode([FromBody] UpdateBarCodeParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor, Authorization.CountingSupervisor, Authorization.TransferSupervisor, Authorization.GoodsReceiptConfirmation,
                Authorization.GoodsReceiptConfirmationSupervisor))
            throw new UnauthorizedAccessException("You don't have access for Adding Item BarCode");
        var validationResponse = parameters.Validate(data);
        if (validationResponse != null)
            return validationResponse;
        using var itemBarcode = new ItemBarCodeUpdate(EmployeeID, parameters.ItemCode, parameters.AddBarcodes, parameters.RemoveBarcodes);
        return itemBarcode.Execute();
    }
}