using Core.Enums;

namespace Core.DTOs;

public abstract class AddItemRequestBase {
    public int       ID       { get; set; }
    public string    ItemCode { get; set; }
    public string    BarCode  { get; set; }
    public int?      BinEntry { get; set; }
    public UnitType? Unit     { get; set; }
}

// public static class AddItemReturnValueTypeDescription {
//     public static bool Value(this AddItemReturnValueType type, AddItemParameterBase parameter) {
//         string itemCode = parameter.ItemCode;
//         string barCode  = parameter.BarCode;
//         switch (type) {
//             case 0:
//                 return true;
//             case AddItemReturnValueType.NotAdded:
//                 return false;
//             default:
//                 throw new ArgumentException(type switch {
//                     AddItemReturnValueType.ItemCodeNotFound        => string.Format(ErrorMessages.ItemCodeWasNotFoundIndatabase, itemCode),
//                     AddItemReturnValueType.BinNotExists            => string.Format(ErrorMessages.BinWasNotFoundIndatabase, parameter.BinEntry.Value),
//                     AddItemReturnValueType.ItemCodeBarCodeMismatch => string.Format(ErrorMessages.BarCodentoMatchItemCode, barCode, itemCode),
//                     AddItemReturnValueType.TransactionIDNotExists  => string.Format(ErrorMessages.TransactionIDNotExists, parameter.ID),
//                     AddItemReturnValueType.NotPurchaseItem         => string.Format(ErrorMessages.ItemBarCodeNotPurchaseItem, itemCode, barCode),
//                     AddItemReturnValueType.NotStockItem            => string.Format(ErrorMessages.ItemBarCodeNotStockItem, itemCode, barCode),
//                     AddItemReturnValueType.ItemNotInWarehouse      => string.Format(ErrorMessages.BinNotInWarehouse, parameter.BinEntry.Value),
//                     AddItemReturnValueType.BinNotInWarehouse       => string.Format(ErrorMessages.ItemNotInWarehouse, itemCode, barCode),
//                     AddItemReturnValueType.BinMissing              => ErrorMessages.BinRequiredParameterForWarehouse,
//                     AddItemReturnValueType.ItemWasNotFoundInTransactionSpecificDocuments => string.Format(ErrorMessages.ItemBarCode1WasNotFoundInTransactionSpecificDocuments,
//                         itemCode,
//                         barCode),
//                     AddItemReturnValueType.QuantityMoreThenReleased => string.Format(ErrorMessages.ReleasedQuantityFromItemIsless, itemCode),
//                     AddItemReturnValueType.QuantityMoreAvailable    => string.Format(ErrorMessages.QuantityMoreThenAvailable, itemCode),
//                     _                                               => throw new ArgumentOutOfRangeException(nameof(type))
//                 });
//         }
//     }
// }