using Core.DTOs;
using Core.Interfaces;

namespace Infrastructure.Services;

public class TransferLineService : ITransferLineService {
    public async Task<TransferAddItemResponse> AddItem(string warehouse, TransferAddItemRequest request) {
        if (!await ValidateAddItem(warehouse, request))
            return new TransferAddItemResponse { ClosedTransfer = true };
        // public bool Validate(DataConnector conn, Data data, int empID) {
        //     var value = (AddItemReturnValueType)data.Transfer.ValidateAddItem(conn, this, empID);
        //     return value.Value(this);
        // }

        // using var conn = Global.Connector;
        // conn.BeginTransaction();
        // try {
        //     if (!requests.Validate(conn, Data, EmployeeID))
        //         return new TransferAddItemResponse { ClosedTransfer = true };
        //     var addItemResponse = Data.Transfer.AddItem(conn, requests, EmployeeID);
        //     conn.CommitTransaction();
        //     return addItemResponse;
        // }
        // catch (Exception e) {
        //     Console.WriteLine(e);
        //     throw;
        // }
    }

    private async Task<bool> ValidateAddItem(string warehouse, TransferAddItemRequest request) {
    }
}