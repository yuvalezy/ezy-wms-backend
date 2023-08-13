using System;
using System.Data;
using System.Text.RegularExpressions;
using Sap.Data.Hana;
using Service.Shared.Company;

namespace Service.Shared.Data; 

/// <summary>
/// Utility for creating queries for SAP Business One DI API / UI API Applications
/// </summary>
/// <remarks></remarks>
public static class QueryHelper {
    /// <summary>
    /// Replaces NULL with the specified replacement value.
    /// </summary>
    /// <example>
    ///   <code lang="C#"><![CDATA[string sql = $"select Coalesce(\"U_Test\", -1) from \"OITM\" where \"ItemCode\" = 'TestItem'";]]></code>
    ///   <para>In this example for sql server we will get the following string
    /// result</para>
    ///   <code><![CDATA[select IsNull("U_Test", -1) from OITM where "ItemCode" = 'TestItem']]></code>
    ///   <para>and for hana the following string result</para>
    ///   <code><![CDATA[select IFNULL("U_Test", -1) from OITM where "ItemCode" = 'TestItem']]></code>
    /// </example>
    public static string IsNull { get; internal set; } = "IsNull";

    /// <summary>
    /// Returns a string that is the result of concatenating two or more string values.
    /// </summary>
    /// <example>
    /// <code lang="C#"><![CDATA[string sql = $"select \"U_TestString\" {QueryHelper.Concat} '- Hello World' from \"OITM\" where \"ItemCode\" = 'TestItem'";]]></code>
    /// <para>In this example for sql server we will get the following string
    /// result</para>
    /// <code><![CDATA[select "U_TestString" + ' - Hello World' from "OITM" where "ItemCode" = 'TestItem']]></code>
    /// <para>and for hana the following string result</para>
    /// <code><![CDATA[select "U_TestString" || ' - Hello World' from "OITM" where "ItemCode" = 'TestItem']]></code>
    /// </example>
    public static string Concat { get; internal set; } = "+";

    /// <summary>
    /// Returns the current database system timestamp as a datetime value without the database time zone offset. 
    /// </summary>
    /// <example>
    ///   <code lang="C#"><![CDATA[string sql = $"select \"CreateDate\", \"UpdateDate\", {QueryHelper.Now} \"Now\" from \"OITM\" where \"ItemCode\" = 'TestItem'";]]></code>
    ///   <para>In this example for sql server we will get the following string
    /// result</para>
    ///   <code><![CDATA[select "CreateDate", "UpdateDate", getdate() "Now" from "OITM" where "ItemCode" = 'TestItem']]></code>
    ///   <para>and for hana the following string result</para>
    ///   <code><![CDATA[select "CreateDate", "UpdateDate", CURRENT_TIMESTAMP "Now" from "OITM" where "ItemCode" = 'TestItem']]></code>
    /// </example>
    public static string Now { get; internal set; } = "getdate()";

    /// <summary>
    /// Dbo is the default schema in SQL Server. This is used to call custom functions in sql.
    /// In SAP HANA this is not required
    /// </summary>
    /// <example>
    ///   <code lang="C#"><![CDATA[string sql = $"select {QueryHelper.Dbo}\"MyFunction\"(\"CreateDate\", {QueryHelper.Now}) \"MyFunctionResult\" from \"OITM\" where \"ItemCode\" = 'TestItem'";]]></code>
    ///   <para>In this example for sql server we will get the following string
    /// result</para>
    ///   <code><![CDATA[select dbo."MyFunction"("CreateDate", getdate()) "MyFunctionResult" from "OITM" where "ItemCode" = 'TestItem']]></code>
    ///   <para>and for hana the following string result</para>
    ///   <code><![CDATA[select "MyFunction"("CreateDate", CURRENT_TIMESTAMP) "MyFunctionResult" from "OITM" where "ItemCode" = 'TestItem']]></code>
    /// </example>
    public static string Dbo { get; internal set; } = "dbo.";

    public static string OtherDB { get; internal set; } = "..";

    /// <summary>
    /// This is the variable declaration for a query
    /// </summary>
    /// <example>
    ///   <code lang="C#"><![CDATA[string sql = $"select * from OITM where \"ItemCode\" = {QueryHelper.Var}Test";]]></code>
    ///   <para>In this example for sql server we will get the following string
    /// result</para>
    ///   <code><![CDATA[select * from OITM where "ItemCode" = @Test]]></code>
    ///   <para>and for hana the following string result</para>
    ///   <code><![CDATA[select * from OITM where "ItemCode" = :Test]]></code>
    /// </example>
    public static string Var { get; internal set; } = "@";

    /// <summary>
    /// This is a from dummy statement used only in SAP HANA when doing queries without a database source.
    /// </summary>
    /// <example>
    ///   <code lang="C#"><![CDATA[string sql = $"select {QueryHelper.Now} \"ServerDateTime\" {QueryHelper.FromDummy}";]]></code>
    ///   <para>In this example for sql server we will get the following string
    /// result</para>
    ///   <code><![CDATA[select getdate() "ServerDateTime"]]></code>
    ///   <para>and for hana the following string result</para>
    ///   <code><![CDATA[select CURRENT_TIMESTAMP "ServerDateTime" from DUMMY]]></code>
    /// </example>
    public static string FromDummy => ConnectionController.DatabaseType == DatabaseType.HANA ? " from dummy" : "";

    /// <summary>
    /// This is used to update the query variables depending on the current database type.
    /// This is trigger automatically when the <see cref="DatabaseType"/> value is changed.
    /// </summary>
    /// <remarks></remarks>
    public static void SetValues() {
        switch (ConnectionController.DatabaseType) {
            case DatabaseType.HANA:
                IsNull  = "IFNULL";
                Concat  = "||";
                Now     = "NOW()";
                Dbo     = "";
                OtherDB = ".";
                Var     = ":";
                break;
            default:
                IsNull  = "IsNull";
                Concat  = "+";
                Now     = "getdate()";
                Dbo     = "dbo.";
                OtherDB = "..";
                Var     = "@";
                break;
        }
    }

    internal static string ApplySpecificDatabase(string query) {
        if (string.IsNullOrWhiteSpace(query))
            return query;
        query = Regex.Replace(query, "{IsNull}", IsNull, RegexOptions.IgnoreCase);
        query = Regex.Replace(query, "{Concat}", Concat, RegexOptions.IgnoreCase);
        query = Regex.Replace(query, "{Now}", Now, RegexOptions.IgnoreCase);
        query = Regex.Replace(query, "{Dbo}", Dbo, RegexOptions.IgnoreCase);
        query = Regex.Replace(query, "{OtherDB}", OtherDB, RegexOptions.IgnoreCase);
        query = Regex.Replace(query, "{Var}", Var, RegexOptions.IgnoreCase);
        query = Regex.Replace(query, "{FromDummy}", FromDummy, RegexOptions.IgnoreCase);
        return query;
    }

    /// <summary>
    /// Get an object table by object type ID
    /// </summary>
    /// <param name="objectType">Integer object type id</param>
    /// <returns>If objectType equals 17 it will return RDR</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// int    docEntry   = 1;
    /// int    objectCode = 17; //Sales Order Object Code
    /// string query      = $"select \"CardCode\", \"CardName\", \"DocTotal\" from O{Query.ObjectTable(objectCode)} where \"DocEntry\" = {docEntry}";
    /// (string cardCode, string cardName, double docTotal) = query.ExecuteQueryValue<string, string, double>();
    /// 
    /// ]]></code>
    /// </example>
    public static string ObjectTable(int objectType) => ObjectTable((ObjectTypes)objectType);

    /// <summary>
    /// Get an object table by BoObjectTypes enumeration
    /// </summary>
    /// <param name="objectType">BoObjectTypes enumeration</param>
    /// <returns>If objectType equals BoObjectTypes.oOrders it will return RDR</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// int    docEntry   = 1; //Key of the Sales Order
    /// ObjectTypes objectType = ObjectTypes.oOrders;
    /// string query      = $"select \"ItemCode\", \"Dscription\", \"LineTotal\" from {Query.ObjectTable(objectType)}1 where \"DocEntry\" = {docEntry}";
    /// List<(string itemCode, string itemName, double lineTotal)> list = query.ExecuteQueryList<string, string, double>();
    /// ]]></code>
    /// </example>
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

    /// <summary>
    /// Helper to read numeric columns from sql and decimal columns from hana from a datareader.
    /// </summary>
    /// <param name="reader">An object that implements the IDataReader interface</param>
    /// <param name="id">Column ID to read</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static double ReadDouble(this IDataReader reader, string id) {
        try {
            object value = reader[id];
            return ConnectionController.DatabaseType switch {
                DatabaseType.SQL  => Convert.ToDouble(value),
                DatabaseType.HANA => Convert.ToDouble(((HanaDecimal)value).ToDecimal()),
                _                 => throw new Exception("ConnectionController.DatabaseType is not defined")
            };
        }
        catch (Exception e) {
            throw new Exception($"Error Reading Double Column \"{id}\": {e.Message}");
        }
    }
    /// <summary>
    /// Helper to read numeric columns from sql and decimal columns from hana from a datareader.
    /// </summary>
    /// <param name="reader">An object that implements the IDataReader interface</param>
    /// <param name="id">Column ID to read</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static decimal ReadDecimal(this IDataReader reader, string id) {
        try {
            object value = reader[id];
            return ConnectionController.DatabaseType switch {
                DatabaseType.SQL  => Convert.ToDecimal(value),
                DatabaseType.HANA => ((HanaDecimal)value).ToDecimal(),
                _                 => throw new Exception("ConnectionController.DatabaseType is not defined")
            };
        }
        catch (Exception e) {
            throw new Exception($"Error Reading Decimal Column \"{id}\": {e.Message}");
        }
    }
}