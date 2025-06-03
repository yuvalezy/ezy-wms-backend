namespace Core.Enums;
public enum AddItemReturnValueType {
    Ok                                            = 0,
    ItemCodeNotFound                              = -1,
    ItemCodeBarCodeMismatch                       = -2,
    TransactionIDNotExists                        = -3,
    NotAdded                                      = -4,
    NotPurchaseItem                               = -5,
    ItemWasNotFoundInTransactionSpecificDocuments = -6,
    QuantityMoreThenReleased                      = -7,
    NotStockItem                                  = -8,
    ItemNotInWarehouse                            = -9,
    BinNotExists                                  = -10,
    BinNotInWarehouse                             = -11,
    BinMissing                                    = -12,
    QuantityMoreAvailable                         = -13,
}
