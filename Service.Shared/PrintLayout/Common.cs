using System.Collections.Generic;
using System.Linq;

namespace Service.Shared.PrintLayout; 

public class Common {
    public static string                               LayoutsTable                  { get; set; }
    public static string                               LayoutManagerUDT              { get; set; }
    public static string                               FormDefaultPrinterUDT         { get; set; }
    public static string                               LayoutsSpecificFiltersTable   { get; set; }
    public static string                               GetLayoutsStoredProcedureName { get; set; }
    public static List<LayoutDefinition>               ObjectTypes                   { get; set; } = new();
    public static IOrderedEnumerable<LayoutDefinition> SortedObjectTypes()           => ObjectTypes.OrderBy(x => x.Name);
}