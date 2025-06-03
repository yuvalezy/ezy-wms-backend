using Core.Enums;

namespace Core.DTOs;

public abstract class AddItemRequestBase {
    public Guid     ID       { get; set; }
    public string   ItemCode { get; set; }
    public string   BarCode  { get; set; }
    public int?     BinEntry { get; set; }
    public UnitType Unit     { get; set; }
}

// Custom exception for API errors
public class ApiErrorException : Exception {
    public int    ErrorId   { get; set; }
    public object ErrorData { get; set; }

    public ApiErrorException(int errorId, object errorData) {
        ErrorId   = errorId;
        ErrorData = errorData;
    }
}

public static class AddItemReturnValueTypeDescription {
    public static bool IsValid(this AddItemReturnValueType type, AddItemRequestBase parameter) {
        string itemCode = parameter.ItemCode;
        string barCode  = parameter.BarCode;

        switch (type) {
            case 0:
                return true;
            case AddItemReturnValueType.NotAdded:
                return false;
            default:
                errorData;
                switch (type) {
                    case AddItemReturnValueType.ItemCodeNotFound:
                        errorData = new { ErrorID = (int)type, ItemCode = itemCode };
                        break;
                    case AddItemReturnValueType.BinNotExists:
                        errorData = new { ErrorID = (int)type, BinEntry = parameter.BinEntry.Value };
                        break;
                    case AddItemReturnValueType.ItemCodeBarCodeMismatch:
                        errorData = new { ErrorID = (int)type, BarCode = barCode, ItemCode = itemCode };
                        break;
                    case AddItemReturnValueType.TransactionIDNotExists:
                        errorData = new { ErrorID = (int)type, ID = parameter.ID };
                        break;
                    case AddItemReturnValueType.NotPurchaseItem:
                    case AddItemReturnValueType.NotStockItem:
                        errorData = new { ErrorID = (int)type, ItemCode = itemCode, BarCode = barCode };
                        break;
                    case AddItemReturnValueType.ItemNotInWarehouse:
                        errorData = new { ErrorID = (int)type, BinEntry = parameter.BinEntry.Value };
                        break;
                    case AddItemReturnValueType.BinNotInWarehouse:
                        errorData = new { ErrorID = (int)type, ItemCode = itemCode, BarCode = barCode };
                        break;
                    case AddItemReturnValueType.BinMissing:
                        errorData = new { ErrorID = (int)type };
                        break;
                    case AddItemReturnValueType.ItemWasNotFoundInTransactionSpecificDocuments:
                        errorData = new { ErrorID = (int)type, ItemCode = itemCode, BarCode = barCode };
                        break;
                    case AddItemReturnValueType.QuantityMoreThenReleased:
                    case AddItemReturnValueType.QuantityMoreAvailable:
                        errorData = new { ErrorID = (int)type, ItemCode = itemCode };
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type));
                }

                throw new ApiErrorException((int)type, errorData);
        }
    }
}