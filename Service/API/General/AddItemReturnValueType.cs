using System;
using Newtonsoft.Json;

namespace Service.API.General;

public enum AddItemReturnValueType {
    Ok                                            = 0,
    ItemCodeNotFound                              = -1,
    ItemCodeBarCodeMismatch                       = -2,
    TransactionIDNotExists                        = -3,
    NotAdded                                      = -4,
    NotPurchaseItem                               = -5,
    ItemWasNotFoundInTransactionSpecificDocuments = -6,
    QuantityMoreThenReleased                      = -7,
}

public abstract class AddItemParameterBase {
    public int ID { get; set; }
    public string ItemCode { get; set; }
    public string BarCode  { get; set; }
}

public static class AddItemReturnValueTypeDescription {
    public static bool Value(this AddItemReturnValueType type, AddItemParameterBase parameter) =>
        type switch {
            AddItemReturnValueType.ItemCodeNotFound =>
                throw new ArgumentException(string.Format(ErrorMessages.ItemCodeWasNotFoundIndatabase, parameter.ItemCode)),
            AddItemReturnValueType.ItemCodeBarCodeMismatch =>
                throw new ArgumentException(string.Format(ErrorMessages.BarCodentoMatchItemCode, parameter.BarCode, parameter.ItemCode)),
            AddItemReturnValueType.TransactionIDNotExists =>
                throw new ArgumentException(string.Format(ErrorMessages.TransactionIDNotExists, parameter.ID)),
            AddItemReturnValueType.NotPurchaseItem =>
                throw new ArgumentException(string.Format(ErrorMessages.ItemBarCodeNotPurchaseItem, parameter.ItemCode, parameter.BarCode)),
            AddItemReturnValueType.ItemWasNotFoundInTransactionSpecificDocuments =>
                throw new ArgumentException(string.Format(ErrorMessages.ItemBarCode1WasNotFoundInTransactionSpecificDocuments, parameter.ItemCode, parameter.BarCode)),
            AddItemReturnValueType.QuantityMoreThenReleased =>
                throw new ArgumentException(string.Format(ErrorMessages.ReleasedQuantityFromItemIsless, parameter.ItemCode)),
            AddItemReturnValueType.NotAdded => false,
            _                               => true
        };
}