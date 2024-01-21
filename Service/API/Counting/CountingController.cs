using System;
using System.Collections.Generic;
using System.Web.Http;
using Service.API.Counting.Models;
using Service.API.Models;
using Service.Shared;

namespace Service.API.Counting;

[Authorize, RoutePrefix("api/Counting")]
public class CountingController : LWApiController {
    [HttpPost]
    [ActionName("Create")]
    public Models.Counting CreateCounting([FromBody] CreateParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.CountingSupervisor))
            throw new UnauthorizedAccessException("You don't have access for counting creation");

        var validateReturnValue = parameters.Validate(Data, EmployeeID);
        if (validateReturnValue != null)
            return validateReturnValue;

        int id = Data.Counting.CreateCounting(parameters, EmployeeID);
        return Data.Counting.GetCounting(id);
    }
    [HttpPost]
    [ActionName("AddItem")]
    public AddItemResponse AddItem([FromBody] AddItemParameter parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Counting))
            throw new UnauthorizedAccessException("You don't have access for adding item to counting");
        if (!parameters.Validate(Data, EmployeeID))
            return new AddItemResponse { ClosedCounting = true };
        return Data.Counting.AddItem(parameters, EmployeeID);
    }
    [HttpPost]
    [ActionName("Cancel")]
    public bool CancelCounting([FromBody] IDParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.CountingSupervisor))
            throw new UnauthorizedAccessException("You don't have access for counting cancellation");

        return Data.Counting.CancelCounting(parameters.ID, EmployeeID);
    }

    [HttpPost]
    [ActionName("Process")]
    public bool ProcessCounting([FromBody] IDParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.CountingSupervisor))
            throw new UnauthorizedAccessException("You don't have access for counting cancellation");
        return Data.Counting.ProcessCounting(parameters.ID, EmployeeID, Data.General.AlertUsers);
    }
    
    [HttpGet]
    [ActionName("Countings")]
    public IEnumerable<Models.Counting> GetCountings([FromUri] FilterParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Counting, Authorization.CountingSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get counting");
        parameters.WhsCode = Data.General.GetEmployeeData(EmployeeID).WhsCode;
        return Data.Counting.GetCountings(parameters);
    }

    [HttpGet]
    [Route("Counting/{id:int}")]
    public Models.Counting GetCounting(int id) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Counting, Authorization.CountingSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get counting");
        return Data.Counting.GetCounting(id);
    }
    [HttpPost]
    [ActionName("CountingContent")]
    public IEnumerable<CountingContent> CountingContent([FromBody] CountingContentParameters parameters) {
        if (!Global.ValidateAuthorization(EmployeeID, Authorization.Counting, Authorization.CountingSupervisor))
            throw new UnauthorizedAccessException("You don't have access to get counting content");
        return Data.Counting.GetCountingContent(parameters.ID, parameters.BinEntry);
    }
}