using System.Data;
using Core.DTOs.Items;

namespace Adapters.Common.Utils;

public static class ItemResponseHelper {
    /// <summary>
    /// Fills an ItemResponse object with standard item data from a data reader using field aliases
    /// </summary>
    /// <param name="reader">Data reader containing the query results</param>
    /// <param name="response">ItemResponse object to populate</param>
    public static void PopulateItemResponse(IDataReader reader, ItemResponse response) {
        response.ItemCode = reader["ItemCode"] as string ?? string.Empty;
        response.ItemName = reader["ItemName"] as string ?? string.Empty;
        response.BuyUnitMsr = reader["BuyUnitMsr"] as string ?? string.Empty;
        response.NumInBuy = Convert.ToDecimal(reader["NumInBuy"]);
        response.PurPackMsr = reader["PurPackMsr"] as string ?? string.Empty;
        response.PurPackUn = Convert.ToDecimal(reader["PurPackUn"]);
        response.Factor1 = Convert.ToDecimal(reader["PurFactor1"]);
        response.Factor2 = Convert.ToDecimal(reader["PurFactor2"]);
        response.Factor3 = Convert.ToDecimal(reader["PurFactor3"]);
        response.Factor4 = Convert.ToDecimal(reader["PurFactor4"]);
    }
}