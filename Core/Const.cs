namespace Core;

public static class Const {
    public const string SessionCookieName = "ezywms_session";

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

    public static Guid DefaultUserId = new("00000000-0000-0000-0000-000000000000");
}