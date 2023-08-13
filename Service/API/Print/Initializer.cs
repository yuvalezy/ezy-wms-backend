using Service.Shared.PrintLayout;

namespace Service.API.Print; 

public static class Initializer {
    public static void Initialize() {
        Common.LayoutsTable                  = "B1SLMLayouts";
        Common.LayoutManagerUDT              = "B1SPLM";
        Common.FormDefaultPrinterUDT         = "B1SPLFDP";
        Common.LayoutsSpecificFiltersTable   = "B1SPLMS";
        Common.GetLayoutsStoredProcedureName = "B1SLMGetLayouts";
    }
}