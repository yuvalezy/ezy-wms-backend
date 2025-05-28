namespace Service.Shared;

public static class Const {
    public const int    ReloadSettings                     = 129;
    public const int    ReloadRestAPISettings              = 130;
    public const int    ExecuteBackgroundHelloWorld        = 131;
    public const string RegistryPath                       = @"Software\yuval08\light-wms-service";
    public const string ServiceName                        = "LW-YUVAL08-SERV";
    public const string GoodsReceipt                       = "Entrada";
    public const string GoodsReceiptSupervisor             = "Supervisor Entrada";
    public const string GoodsReceiptConfirmationSupervisor = "Supervisor Conf Entr";
    public const string GoodsReceiptConfirmation           = "Conf Ent";
    public const string Picking                            = "Picking";
    public const string PickingSupervisor                  = "Supervisor Picking";
    public const string Counting                           = "Inventario";
    public const string CountingSupervisor                 = "Supervisor Inventari";
    public const string Transfer                           = "Transferencia";
    public const string TransferSupervisor                 = "Supervisor Transfere";
    public const string TransferRequest                    = "Solicitud Transferen";
}