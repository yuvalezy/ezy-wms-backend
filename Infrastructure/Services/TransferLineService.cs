using Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.DbContexts;

namespace Infrastructure.Services;

public class TransferLineService(SystemDbContext db, IExternalSystemAdapter adapter) : ITransferLineService {
    public async Task<TransferAddItemResponse> AddItem(Guid userId, string warehouse, TransferAddItemRequest request) {
        if (!await ValidateAddItem(warehouse, request))
            return new TransferAddItemResponse { ClosedTransfer = true };

        var transaction = await db.Database.BeginTransactionAsync();
        try {
            var transfer = await db.Transfers.FindAsync(request.ID);
            if (transfer == null) {
                throw new KeyNotFoundException($"Transfer with ID {request.ID} not found.");
            }


            int quantity = request.Quantity;
            if (request.Unit != UnitType.Unit) {
                var items = await adapter.ItemCheckAsync(request.ItemCode, request.BarCode);
                var item  = items.FirstOrDefault();
                if (item == null) {
                    throw new ApiErrorException((int)AddItemReturnValueType.ItemCodeNotFound, new { request.ItemCode, request.BarCode });
                }
                quantity *= item.NumInBuy * (request.Unit == UnitType.Pack ? item.PurPackUn : 1);
            }
            var line = new TransferLine {
                ItemCode     = request.ItemCode,
                BarCode      = request.BarCode,
                BinEntry     = request.BinEntry,
                Date         = DateTime.UtcNow,
                Quantity     = quantity,
                Type         = request.Type,
                UnitType     = request.Unit,
                TransferId   = request.ID,
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = userId,
                LineStatus      = LineStatus.Open
            };
            
            transfer.Lines.Add(line);
            if (transfer.Status == ObjectStatus.Open)
                transfer.Status = ObjectStatus.InProgress;

            db.Update(transfer);
            await transaction.CommitAsync();
            
            return new TransferAddItemResponse {LineID = line.Id};
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<bool> ValidateAddItem(string warehouse, TransferAddItemRequest request) {
        /*
         * select Case
    When T1.ItemCode is null Then -1
    When T0.BarCode <> T1.CodeBars and T3.BcdCode is null Then -2
    When T2.Code is null Then -3
    When T2.U_Status not in ('O', 'I') Then -4
    When T1.InvntItem = 'N' Then -8
    When T4.WhsCode is null Then -9
    When @BinEntry is not null and T5.AbsEntry is null Then -10
    When @BinEntry is not null and T5.WhsCode <> @WhsCode Then -11
    When @BinEntry is null and T6.BinActivat = 'Y' Then -12
    Else 0 End ValidationMessage
from (select @ID ID, @BarCode BarCode, @ItemCode ItemCode) T0
         left outer join OITM T1 on T1.ItemCode = T0.ItemCode
left outer join "@LW_YUVAL08_OINC" T2 on T2.Code = T0.ID
         left outer join OBCD T3 on T3.ItemCode = T0.ItemCode and T3.BcdCode = @BarCode
left outer join OITW T4 on T4.ItemCode = T1.ItemCode and T4.WhsCode = @WhsCode
left outer join OBIN T5 on T5.AbsEntry = @BinEntry
left outer join OWHS T6 on T6.WhsCode = @WhsCode

         */

        // public bool Validate(DataConnector conn, Data data, int empID) {
        //     var value = (AddItemReturnValueType)data.Transfer.ValidateAddItem(conn, this, empID);
        //     return value.IsValid(this);
        // }

        //         conn.GetValue<int>(GetQuery("ValidateAddItemParameters"), [
//             new Parameter("@ID", SqlDbType.Int, parameters.ID),
//             new Parameter("@ItemCode", SqlDbType.NVarChar, 50, parameters.ItemCode),
//             new Parameter("@BarCode", SqlDbType.NVarChar, 254, parameters.BarCode),
//             new Parameter("@empID", SqlDbType.Int, employeeID),
//             new Parameter("@BinEntry", SqlDbType.Int, parameters.BinEntry is > 0 ? parameters.BinEntry.Value : DBNull.Value),
//             new Parameter("@Quantity", SqlDbType.Int, parameters.Quantity),
//             new Parameter("@Type", SqlDbType.Char, 1, ((char)parameters.Type).ToString()),
//             new Parameter("@Unit", SqlDbType.SmallInt, 1, parameters.Unit)
//         ]);
    }
}