using Adapters.Windows.Enums;

namespace Adapters.Sbo.Utils;

public static class QueryHelper {
    public static string ObjectTable(int objectType) => ObjectTable((ObjectTypes)objectType);


    public static string ObjectTable(ObjectTypes objectType) =>
        objectType switch {
            ObjectTypes.oJournalEntries           => "JRN",
            ObjectTypes.oInvoices                 => "INV",
            ObjectTypes.oOrders                   => "RDR",
            ObjectTypes.oDeliveryNotes            => "DLN",
            ObjectTypes.oPurchaseInvoices         => "PCH",
            ObjectTypes.oPurchaseOrders           => "POR",
            ObjectTypes.oPurchaseDeliveryNotes    => "PDN",
            ObjectTypes.oQuotations               => "QUT",
            ObjectTypes.oCreditNotes              => "RIN",
            ObjectTypes.oReturns                  => "RDN",
            ObjectTypes.oPurchaseCreditNotes      => "RPC",
            ObjectTypes.oPurchaseReturns          => "RPD",
            ObjectTypes.oDownPayments             => "DPI",
            ObjectTypes.oPurchaseDownPayments     => "DPO",
            ObjectTypes.oReturnRequest            => "RRR",
            ObjectTypes.oGoodsReturnRequest       => "PRR",
            ObjectTypes.oPurchaseQuotations       => "PQT",
            ObjectTypes.oPurchaseRequest          => "PRQ",
            ObjectTypes.oProductionOrders         => "WOR",
            ObjectTypes.oInventoryGenExit         => "IGE",
            ObjectTypes.oInventoryGenEntry        => "IGN",
            ObjectTypes.oStockTransfer            => "WTR",
            ObjectTypes.oInventoryTransferRequest => "WTQ",
            ObjectTypes.oLandedCosts              => "IPF",
            ObjectTypes.oInventoryOpeningBalance  => "IQI",
            ObjectTypes.oInventoryRevaluation     => "MRV",
            ObjectTypes.oInventoryCounting        => "INC",
            ObjectTypes.oInventoryPostings        => "IQR",
            _                                     => ""
        };
    public static  string UnionQuery(this   string value)                       => ProcessQuery(value, "union");
    private static string ProcessQuery(this string value, string replaceString) => !string.IsNullOrWhiteSpace(value) ? $" {replaceString} " : "";
}