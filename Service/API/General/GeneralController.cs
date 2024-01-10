using System;
using System.Collections.Generic;
using System.Linq;
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
            Name = Global.CompanyName
        };
}

[Authorize]
public class GeneralController : LWApiController {
    private readonly Data data = new();

    [HttpGet]
    [ActionName("UserInfo")]
    public UserInfo GetUserInfo() {
        var employeeData = data.General.GetEmployeeData(EmployeeID);
        return new UserInfo {
            ID             = EmployeeID,
            Name           = employeeData.Name,
            Branch         = employeeData.WhsName,
            BinLocations      = employeeData.EnableBin,
            Authorizations = Global.UserAuthorizations[EmployeeID]
        };
    }

    [HttpGet]
    [ActionName("Vendors")]
    public IEnumerable<BusinessPartner> GetVendors() {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access for vendors list");
        return data.General.GetVendors();
    }

    [HttpGet]
    [ActionName("ItemByBarCode")]
    public IEnumerable<Item> ScanItemBarCode([FromUri] string scanCode) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceipt))
            throw new UnauthorizedAccessException("You don't have access for Scan Item BarCode");
        return data.General.ScanItemBarCode(scanCode);
    }

    [HttpPost]
    [ActionName("ItemCheck")]
    public IEnumerable<ItemCheckResponse> ItemCheck([FromBody] ItemBarCodeParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access for Item Check");
        return data.General.ItemCheck(parameters.ItemCode, parameters.Barcode);
    }

    [HttpPost]
    [ActionName("UpdateItemBarCode")]
    public UpdateItemBarCodeResponse AddItemBarCode([FromBody] UpdateBarCodeParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.GoodsReceiptSupervisor))
            throw new UnauthorizedAccessException("You don't have access for Adding Item BarCode");
        var validationResponse = parameters.Validate(data);
        if (validationResponse != null)
            return validationResponse;
        using var itemBarcode = new ItemBarCodeUpdate(EmployeeID, parameters.ItemCode, parameters.AddBarcodes, parameters.RemoveBarcodes);
        return itemBarcode.Execute();
    }
}