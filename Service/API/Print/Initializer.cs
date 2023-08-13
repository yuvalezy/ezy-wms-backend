using Service.Shared.PrintLayout;

namespace Service.API.Print; 

public static class Initializer {
    public static void Initialize() {
        Common.LayoutsTable                  = "LWLayouts";
        Common.LayoutManagerUDT              = "LWPLM";
        Common.FormDefaultPrinterUDT         = "LWPLFDP";
        Common.LayoutsSpecificFiltersTable   = "LWPLMS";
        Common.GetLayoutsStoredProcedureName = "LWGetLayouts";
    }
}