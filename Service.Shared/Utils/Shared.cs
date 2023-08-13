using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using SAPbobsCOM;
using Service.Shared.Company;
using Service.Shared.Data;

namespace Service.Shared.Utils; 

/// <summary>
/// Shared methods and function for every day development use
/// </summary>
public static class Shared {
    #region Enumeration Functions

    /// <summary>
    /// Converts BoObjectTypes to a string code
    /// </summary>
    /// <param name="type">Object Type Parameter</param>
    /// <returns>For example: boObjectTypes.oOrders will return "17"</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// var    type       = BoObjectTypes.oInvoices;
    /// string objectType = Shared.ObjectType(type);
    /// ]]></code>
    /// </example>
    public static string ObjectType(BoObjectTypes type) => ((int)type).ToString();

    /// <summary>
    /// Easily convert an object value to BoYesNoEnum value
    /// </summary>
    /// <param name="value">String value</param>
    /// <returns>If value equals "Y", "1", 1 or true it will return BoYesNoEnum.tYes, otherwise it will return BoYesNoEnum.tNo</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string      cellValue = "Y";
    /// BoYesNoEnum value     = Shared.GetYesNo(cellValue);
    /// ]]></code>
    /// </example>
    public static BoYesNoEnum GetYesNo(object value) => value is true or "Y" or "1" or 1 ? BoYesNoEnum.tYES : BoYesNoEnum.tNO;

    #endregion


    #region Regular Objects

    private static Recordset rsExecuteQuery;

    /// <summary>
    /// Easy method to get a DI API Record Set object
    /// </summary>
    /// <returns>A record set object</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// var rs = Shared.GetRecordset();
    /// ]]></code>
    /// </example>
    public static Recordset GetRecordset() => (Recordset)ConnectionController.Company.GetBusinessObject(BoObjectTypes.BoRecordset);

    /// <summary>
    /// Get current execute record object
    /// </summary>
    /// <returns>A record set object</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// var rs = Shared.CurrentRecordset;
    /// rs.DoQuery("--do....");
    /// ]]></code>
    /// </example>
    public static Recordset CurrentRecordset {
        get {
            rsExecuteQuery ??= GetRecordset();
            return rsExecuteQuery;
        }
    }

    /// <summary>
    /// Get current execute record object
    /// </summary>
    /// <returns>A record set object</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// var rs = Shared.GetRecordset();
    /// rs.DoQuery("--do....");
    /// Shared.SetCurrentRecordset(rs);
    /// ]]></code>
    /// </example>
    public static void SetCurrentRecordset(Recordset rs) {
        ReleaseExecuteQuery();
        rsExecuteQuery = rs;
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return it
    /// </summary>
    /// <returns>A record set object</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string ItemCode = "TESTITEMCODE";
    /// string query    = $"select \"ItemCode\", \"ItemName\" from OITM where \"ItemCode\" = '{ItemCode}'";
    /// //execute directly
    /// var rs = Shared.ExecuteRecordset(query);
    /// //or execute using the extension method
    /// var rs = query.ExecuteRecordset();
    /// ]]></code>
    /// </example>
    public static Recordset ExecuteRecordset(this string query) {
        rsExecuteQuery ??= GetRecordset();
        try {
            rsExecuteQuery.DoQuery(query);
        }
        catch {
            ReleaseComObject(rsExecuteQuery);
            rsExecuteQuery = GetRecordset();
            rsExecuteQuery.DoQuery(query);
        }

        return rsExecuteQuery;
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object
    /// </summary>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string query    = $"update \"TESTTABLE\" set \"U_Test\" = 'Y' where \"Code\" = 'TESTCOCDE'";
    /// query.ExecuteQuery();
    /// ]]></code>
    /// </example>
    public static bool ExecuteQuery(this string query) {
        rsExecuteQuery ??= GetRecordset();
        try {
            rsExecuteQuery.DoQuery(query);
        }
        catch {
            ReleaseComObject(rsExecuteQuery);
            rsExecuteQuery = GetRecordset();
            rsExecuteQuery.DoQuery(query);
        }

        return true;
    }

    /// <summary>
    /// Method to dispose the current recordset object.
    /// </summary>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// var rs = Shared.CurrentRecordset;
    /// rs.DoQuery("--do....");
    /// Shared.ReleaseExecuteQuery();
    /// ]]></code>
    /// </example>
    public static void ReleaseExecuteQuery() {
        if (rsExecuteQuery == null)
            return;
        ReleaseComObject(rsExecuteQuery);
        rsExecuteQuery = null;
    }

#if DEBUG
    public static bool CheckExecuteQueryReleased() => rsExecuteQuery == null;
#endif

    #endregion

    #region Recordset Generics

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a Value
    /// </summary>
    /// <returns>A record set result value</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string ItemCode = "TESTITEMCODE";
    /// string query    = $"select \"ItemName\" from OITM where \"ItemCode\" = '{ItemCode.ToQuery()}'";
    /// string itemName = query.ExecuteQueryValue<string>();
    /// //do
    /// ]]></code>
    /// </example>
    public static T ExecuteQueryValue<T>(this string query) {
        ExecuteQuery(query);
        return !rsExecuteQuery.EoF ? ChangeType<T>(0) : default;
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a list of Values
    /// </summary>
    /// <returns>A record set result value list</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string query = $"select \"ItemCode\" from OITM where \"SellItem\" = 'Y'";
    /// var    list  = query.ExecuteQueryList<string>();
    /// list.ForEach(value => {
    ///     //do
    /// });
    /// ]]></code>
    /// </example>
    public static List<T> ExecuteQueryList<T>(this string query) {
        ExecuteQuery(query);
        var list = new List<T>();
        while (!rsExecuteQuery.EoF) {
            list.Add(ChangeType<T>(0));
            rsExecuteQuery.MoveNext();
        }

        return list;
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a Recordset Reader Data
    /// </summary>
    /// <returns>A record set object</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string query = $@"select ""ItemCode"", ""ItemName"", ""OnHand"", ""IsCommited"", ""OnOrder"", ""QryGroup1"", ""VATLiable"", ""InvntItem"", ""PrchseItem"", ""SellItem"", ""DfltWH"", ""CardCode"", 
    /// ""CreateDate"", ""UpdateDate"", Case When ""ManBtchNum"" = 'Y' Then 'B' When ""ManSerNum"" = 'Y' Then 'S' Else 'R' End ""ItemType""
    /// from OITM where ""OnHand"" > 0";
    /// var list = query.ExecuteQueryReader<Item>();
    /// list.ForEach(value => {
    ///     //do
    /// });
    /// ]]></code>
    /// </example>
    public static List<T> ExecuteQueryReader<T>(this string query) where T : class {
        ExecuteQuery(query);
        return !rsExecuteQuery.EoF ? new RecordsetReader<T>(rsExecuteQuery).Read().ToList() : new List<T>();
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a Recordset Reader Data
    /// </summary>
    /// <returns>A record set object</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string ItemCode = "TESTITEMCODE";
    /// 
    /// string query = $@"select ""ItemCode"", ""ItemName"", ""OnHand"", ""IsCommited"", ""OnOrder"", ""QryGroup1"", ""VATLiable"", ""InvntItem"", ""PrchseItem"", ""SellItem"", ""DfltWH"", ""CardCode"", 
    /// ""CreateDate"", ""UpdateDate"", Case When ""ManBtchNum"" = 'Y' Then 'B' When ""ManSerNum"" = 'Y' Then 'S' Else 'R' End ""ItemType""
    /// from OITM where ""ItemCode"" = '{ItemCode}'";
    ///         
    /// var item = query.ExecuteQueryReaderRow<Item>();
    /// ]]></code>
    /// </example>
    public static T ExecuteQueryReaderRow<T>(this string query) where T : class {
        ExecuteQuery(query);
        return !rsExecuteQuery.EoF ? new RecordsetReader<T>(rsExecuteQuery).Read().ToList()[0] : default;
    }

    private static T ChangeType<T>(int index) {
        try {
            object value          = rsExecuteQuery.Fields.Item(index).Value;
            var    conversionType = typeof(T);
            if (conversionType.FullName == "System.Boolean")
                value = value.ToString().ToBool();
            return !conversionType.IsEnum
                ? value != null ? (T)Convert.ChangeType(value, conversionType) : default
                : value.GetType().FullName switch {
                    "System.String"                                                     => (T)Enum.ToObject(conversionType, Convert.ToChar(value)),
                    "System.Char" or "System.Int16" or "System.Int32" or "System.Int64" => (T)Enum.ToObject(conversionType, value),
                    _                                                                   => (T)Convert.ChangeType(value, conversionType)
                };
        }
        catch (Exception ex) {
            Debug.WriteLine(ex.Message);
            return default;
        }
    }

    #region Tuples

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a tuple value
    /// </summary>
    /// <returns>Tuple with specified T values</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string ItemCode = "TESTITEMCODE";
    /// string query = $@"select ""ItemName"", ""OnHand"" from OITM where ""ItemCode"" = '{ItemCode}'";
    /// 
    /// (string itemName, double onHand) = query.ExecuteQueryValue<string, double>();
    /// ]]></code>
    /// </example>
    public static Tuple<T1, T2> ExecuteQueryValue<T1, T2>(this string query) {
        ExecuteQuery(query);
        return !rsExecuteQuery.EoF
            ? new Tuple<T1, T2>(
                ChangeType<T1>(0),
                ChangeType<T2>(1)
            )
            : new Tuple<T1, T2>(
                default,
                default
            );
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a tuple value
    /// </summary>
    /// <returns>Tuple with specified T values</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string ItemCode = "TESTITEMCODE";
    /// string query = $@"select ""ItemName"", ""OnHand"", ""SellItem"" from OITM where ""ItemCode"" = '{ItemCode}'";
    /// 
    /// (string itemName, double onHand, bool sellItem) = 
    ///     query.ExecuteQueryValue<string, double, bool>();
    /// ]]></code>
    /// </example>
    public static Tuple<T1, T2, T3> ExecuteQueryValue<T1, T2, T3>(this string query) {
        ExecuteQuery(query);
        return !rsExecuteQuery.EoF
            ? new Tuple<T1, T2, T3>(
                ChangeType<T1>(0),
                ChangeType<T2>(1),
                ChangeType<T3>(2)
            )
            : new Tuple<T1, T2, T3>(
                default,
                default,
                default
            );
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a tuple value
    /// </summary>
    /// <returns>Tuple with specified T values</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string ItemCode = "TESTITEMCODE";
    /// string query =
    ///     $@"select ""ItemName"", ""OnHand"", ""SellItem"", ""CreateDate""
    ///     from OITM where ""ItemCode"" = '{ItemCode}'";
    /// 
    /// (string itemName, double onHand, bool sellItem, DateTime createDate) = 
    ///     query.ExecuteQueryValue<string, double, bool, DateTime>();
    /// ]]></code>
    /// </example>
    public static Tuple<T1, T2, T3, T4> ExecuteQueryValue<T1, T2, T3, T4>(this string query) {
        ExecuteQuery(query);
        return !rsExecuteQuery.EoF
            ? new Tuple<T1, T2, T3, T4>(
                ChangeType<T1>(0),
                ChangeType<T2>(1),
                ChangeType<T3>(2),
                ChangeType<T4>(3)
            )
            : new Tuple<T1, T2, T3, T4>(
                default,
                default,
                default,
                default
            );
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a tuple value
    /// </summary>
    /// <returns>Tuple with specified T values</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string ItemCode = "TESTITEMCODE";
    /// string query =
    ///     $@"select ""ItemName"", ""OnHand"", ""SellItem"", ""CreateDate"", ""UpdateDate""
    ///     from OITM where ""ItemCode"" = '{ItemCode}'";
    /// 
    /// (string itemName, double onHand, bool sellItem, DateTime createDate, DateTime updateDate) = 
    ///     query.ExecuteQueryValue<string, double, bool, DateTime, DateTime>();
    /// ]]></code>
    /// </example>
    public static Tuple<T1, T2, T3, T4, T5> ExecuteQueryValue<T1, T2, T3, T4, T5>(this string query) {
        ExecuteQuery(query);
        return !rsExecuteQuery.EoF
            ? new Tuple<T1, T2, T3, T4, T5>(
                ChangeType<T1>(0),
                ChangeType<T2>(1),
                ChangeType<T3>(2),
                ChangeType<T4>(3),
                ChangeType<T5>(4)
            )
            : new Tuple<T1, T2, T3, T4, T5>(
                default,
                default,
                default,
                default,
                default
            );
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a tuple value
    /// </summary>
    /// <returns>Tuple with specified T values</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string ItemCode = "TESTITEMCODE";
    /// string query =
    ///     $@"select ""ItemName"", ""OnHand"", ""SellItem"", ""CreateDate"", ""UpdateDate"", 
    ///     Case When ""ManBtchNum"" = 'Y' Then 'B' When ""ManSerNum"" = 'Y' Then 'S' Else 'R' End ""ItemType""
    ///     from OITM where ""ItemCode"" = '{ItemCode}'";
    /// 
    /// (string itemName, double onHand, bool sellItem, DateTime createDate, DateTime updateDate, ItemType itemType) = 
    ///     query.ExecuteQueryValue<string, double, bool, DateTime, DateTime, ItemType>();
    /// ]]></code>
    /// </example>
    public static Tuple<T1, T2, T3, T4, T5, T6> ExecuteQueryValue<T1, T2, T3, T4, T5, T6>(this string query) {
        ExecuteQuery(query);
        return !rsExecuteQuery.EoF
            ? new Tuple<T1, T2, T3, T4, T5, T6>(
                ChangeType<T1>(0),
                ChangeType<T2>(1),
                ChangeType<T3>(2),
                ChangeType<T4>(3),
                ChangeType<T5>(4),
                ChangeType<T6>(5)
            )
            : new Tuple<T1, T2, T3, T4, T5, T6>(
                default,
                default,
                default,
                default,
                default,
                default
            );
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a tuple value
    /// </summary>
    /// <returns>Tuple with specified T values</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string ItemCode = "TESTITEMCODE";
    /// string query =
    ///     $@"select ""ItemName"", ""OnHand"", ""SellItem"", ""CreateDate"", ""UpdateDate"", 
    ///     Case When ""ManBtchNum"" = 'Y' Then 'B' When ""ManSerNum"" = 'Y' Then 'S' Else 'R' End ""ItemType"", ""QryGroup1""
    ///     from OITM where ""ItemCode"" = '{ItemCode}'";
    /// 
    /// (string itemName, double onHand, bool sellItem, DateTime createDate, DateTime updateDate, ItemType itemType, bool property1) = 
    ///     query.ExecuteQueryValue<string, double, bool, DateTime, DateTime, ItemType, bool>();
    /// ]]></code>
    /// </example>
    public static Tuple<T1, T2, T3, T4, T5, T6, T7> ExecuteQueryValue<T1, T2, T3, T4, T5, T6, T7>(this string query) {
        ExecuteQuery(query);
        return !rsExecuteQuery.EoF
            ? new Tuple<T1, T2, T3, T4, T5, T6, T7>(
                ChangeType<T1>(0),
                ChangeType<T2>(1),
                ChangeType<T3>(2),
                ChangeType<T4>(3),
                ChangeType<T5>(4),
                ChangeType<T6>(5),
                ChangeType<T7>(6)
            )
            : new Tuple<T1, T2, T3, T4, T5, T6, T7>(
                default,
                default,
                default,
                default,
                default,
                default,
                default
            );
    }

    #endregion

    #region Tuples List

    /// <summary>
    /// Get XmlNodeList from query
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    private static XmlNodeList GetXmlNodeListFromQuery(this string query) {
        //Copying from RecordsetReader  to not to break current privacy logic
        ExecuteQuery(query);
        if (rsExecuteQuery.EoF)
            return default;
        var doc = new XmlDocument();
        doc.LoadXml(rsExecuteQuery.GetFixedXML(RecordsetXMLModeEnum.rxmData));
        var namespaceManager = new XmlNamespaceManager(doc.NameTable);
        namespaceManager.AddNamespace("di", "http://www.sap.com/SBO/SDK/DI");
        return doc.SelectNodes("//di:Recordset/di:Rows/di:Row", namespaceManager);
    }

    /// <summary>
    /// Safe parsing
    /// </summary>
    /// <param name="readValue"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private static T GetValue<T>(string readValue) {
        var conversionType = typeof(T);

        object returnValue = default(T);
        try {
            if (!conversionType.IsEnum) {
                switch (conversionType.FullName) {
                    case "System.Char":
                        returnValue = char.Parse(readValue);
                        break;
                    case "System.String":
                        returnValue = readValue;
                        break;
                    case "System.Boolean":
                        returnValue = readValue.ToBool();
                        break;
                    case "System.Int16":
                        returnValue = short.Parse(readValue);
                        break;
                    case "System.Nullable`1[System.Int32]":
                    case "System.Int32":
                        if (int.TryParse(readValue, out int intValue))
                            returnValue = intValue;
                        break;
                    case "System.Int64":
                        if (long.TryParse(readValue, out long longValue))
                            returnValue = longValue;
                        break;
                    case "System.Double":
                        returnValue = Numeric.FromStringToDouble(readValue);
                        break;
                    case "System.Decimal":
                        returnValue = Numeric.FromStringToDecimal(readValue);
                        break;
                    case "System.DateTime":
                        returnValue = DateTime.ParseExact(readValue, "yyyyMMdd", CultureInfo.InvariantCulture);
                        break;
                }
            }
            else {
                returnValue = !readValue.IsNumeric() ? (T)Enum.ToObject(conversionType, Convert.ToChar(readValue)) : (T)Enum.ToObject(conversionType, int.Parse(readValue));
            }
        }
        catch {
            return default;
        }

        return (T)returnValue;
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a list of tuples
    /// </summary>
    /// <returns>A tuple object</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string query =
    ///     $@"select ""ItemCode"", ""ItemName""
    ///     from OITM";
    /// 
    /// List<(string ItemCode, string ItemName)> list =
    ///     query.ExecuteQueryList<string, string>();
    /// 
    /// list.ForEach(value => {
    ///     //do
    /// });
    /// ]]></code>
    /// </example>
    public static List<(T1, T2)> ExecuteQueryList<T1, T2>(this string query) {
        var rows = GetXmlNodeListFromQuery(query);
        if (rows != null)
            return (from XmlNode row in rows
                select (GetValue<T1>(row.ChildNodes[0].ChildNodes[0]?.ChildNodes[1].InnerText), GetValue<T2>(row.ChildNodes[0].ChildNodes[1]?.ChildNodes[1].InnerText))).ToList();
        return new List<(T1, T2)>();
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a list of tuples
    /// </summary>
    /// <returns>A tuple object</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string query =
    ///     $@"select ""ItemCode"", ""ItemName"", ""OnHand""
    ///     from OITM";
    /// 
    /// List<(string ItemCode, string ItemName, double OnHand)> list =
    ///     query.ExecuteQueryList<string, string, double>();
    /// 
    /// list.ForEach(value => {
    ///     //do
    /// });
    /// ]]></code>
    /// </example>
    public static List<(T1, T2, T3)> ExecuteQueryList<T1, T2, T3>(this string query) {
        var rows = GetXmlNodeListFromQuery(query);
        if (rows != null)
            return (from XmlNode row in rows
                select (GetValue<T1>(row.ChildNodes[0].ChildNodes[0]?.ChildNodes[1].InnerText), GetValue<T2>(row.ChildNodes[0].ChildNodes[1]?.ChildNodes[1].InnerText),
                    GetValue<T3>(row.ChildNodes[0].ChildNodes[2]?.ChildNodes[1].InnerText))).ToList();
        return new List<(T1, T2, T3)>();
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a list of tuples
    /// </summary>
    /// <returns>A tuple object</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string query =
    ///     $@"select ""ItemCode"", ""ItemName"", ""OnHand"", ""SellItem""
    ///     from OITM";
    /// 
    /// List<(string ItemCode, string ItemName, double OnHand, bool SellItem)> list =
    ///     query.ExecuteQueryList<string, string, double, bool>();
    /// 
    /// list.ForEach(value => {
    ///     //do
    /// });
    /// ]]></code>
    /// </example>
    public static List<(T1, T2, T3, T4)> ExecuteQueryList<T1, T2, T3, T4>(this string query) {
        var rows = GetXmlNodeListFromQuery(query);
        if (rows != null)
            return (from XmlNode row in rows
                select (GetValue<T1>(row.ChildNodes[0].ChildNodes[0]?.ChildNodes[1].InnerText), GetValue<T2>(row.ChildNodes[0].ChildNodes[1]?.ChildNodes[1].InnerText),
                    GetValue<T3>(row.ChildNodes[0].ChildNodes[2]?.ChildNodes[1].InnerText), GetValue<T4>(row.ChildNodes[0].ChildNodes[3]?.ChildNodes[1].InnerText))).ToList();
        return new List<(T1, T2, T3, T4)>();
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a list of tuples
    /// </summary>
    /// <returns>A tuple object</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string query =
    ///     $@"select ""ItemCode"", ""ItemName"", ""OnHand"", ""SellItem"", ""CreateDate""
    ///     from OITM";
    /// 
    /// List<(string ItemCode, string ItemName, double OnHand, bool SellItem, DateTime CreateDate)> list =
    ///     query.ExecuteQueryList<string, string, double, bool, DateTime>();
    /// 
    /// list.ForEach(value => {
    ///     //do
    /// });
    /// ]]></code>
    /// </example>
    public static List<(T1, T2, T3, T4, T5)> ExecuteQueryList<T1, T2, T3, T4, T5>(this string query) {
        var rows = GetXmlNodeListFromQuery(query);
        if (rows != null)
            return (from XmlNode row in rows
                select (GetValue<T1>(row.ChildNodes[0].ChildNodes[0]?.ChildNodes[1].InnerText), GetValue<T2>(row.ChildNodes[0].ChildNodes[1]?.ChildNodes[1].InnerText),
                    GetValue<T3>(row.ChildNodes[0].ChildNodes[2]?.ChildNodes[1].InnerText), GetValue<T4>(row.ChildNodes[0].ChildNodes[3]?.ChildNodes[1].InnerText),
                    GetValue<T5>(row.ChildNodes[0].ChildNodes[4]?.ChildNodes[1].InnerText))).ToList();
        return new List<(T1, T2, T3, T4, T5)>();
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a list of tuples
    /// </summary>
    /// <returns>A tuple object</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string query =
    ///     $@"select ""ItemCode"", ""ItemName"", ""OnHand"", ""SellItem"", ""CreateDate"", ""UpdateDate"",
    ///     from OITM";
    /// 
    /// List<(string ItemCode, string ItemName, double OnHand, bool SellItem, DateTime CreateDate, DateTime UpdateDate)> list =
    ///     query.ExecuteQueryList<string, string, double, bool, DateTime, DateTime>();
    /// 
    /// list.ForEach(value => {
    ///     //do
    /// });
    /// ]]></code>
    /// </example>
    public static List<(T1, T2, T3, T4, T5, T6)> ExecuteQueryList<T1, T2, T3, T4, T5, T6>(this string query) {
        var rows = GetXmlNodeListFromQuery(query);
        if (rows != null)
            return (from XmlNode row in rows
                select (GetValue<T1>(row.ChildNodes[0].ChildNodes[0]?.ChildNodes[1].InnerText), GetValue<T2>(row.ChildNodes[0].ChildNodes[1]?.ChildNodes[1].InnerText),
                    GetValue<T3>(row.ChildNodes[0].ChildNodes[2]?.ChildNodes[1].InnerText), GetValue<T4>(row.ChildNodes[0].ChildNodes[3]?.ChildNodes[1].InnerText),
                    GetValue<T5>(row.ChildNodes[0].ChildNodes[4]?.ChildNodes[1].InnerText), GetValue<T6>(row.ChildNodes[0].ChildNodes[5]?.ChildNodes[1].InnerText))).ToList();
        return new List<(T1, T2, T3, T4, T5, T6)>();
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a list of tuples
    /// </summary>
    /// <returns>A tuple object</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string query =
    ///     $@"select ""ItemCode"", ""ItemName"", ""OnHand"", ""SellItem"", ""CreateDate"", ""UpdateDate"", 
    ///     Case When ""ManBtchNum"" = 'Y' Then 'B' When ""ManSerNum"" = 'Y' Then 'S' Else 'R' End ""ItemType""
    ///     from OITM";
    /// 
    /// List<(string ItemCode, string ItemName, double OnHand, bool SellItem, DateTime CreateDate, DateTime UpdateDate, ItemType ItemType)> list =
    ///     query.ExecuteQueryList<string, string, double, bool, DateTime, DateTime, ItemType>();
    /// 
    /// list.ForEach(value => {
    ///     //do
    /// });
    /// ]]></code>
    /// </example>
    public static List<(T1, T2, T3, T4, T5, T6, T7)> ExecuteQueryList<T1, T2, T3, T4, T5, T6, T7>(this string query) {
        var rows = GetXmlNodeListFromQuery(query);
        if (rows != null)
            return (from XmlNode row in rows
                select (GetValue<T1>(row.ChildNodes[0].ChildNodes[0]?.ChildNodes[1].InnerText), GetValue<T2>(row.ChildNodes[0].ChildNodes[1]?.ChildNodes[1].InnerText),
                        GetValue<T3>(row.ChildNodes[0].ChildNodes[2]?.ChildNodes[1].InnerText), GetValue<T4>(row.ChildNodes[0].ChildNodes[3]?.ChildNodes[1].InnerText),
                        GetValue<T5>(row.ChildNodes[0].ChildNodes[4]?.ChildNodes[1].InnerText), GetValue<T6>(row.ChildNodes[0].ChildNodes[5]?.ChildNodes[1].InnerText),
                        GetValue<T7>(row.ChildNodes[0].ChildNodes[6]?.ChildNodes[1].InnerText)
                    )).ToList();
        return new List<(T1, T2, T3, T4, T5, T6, T7)>();
    }

    /// <summary>
    /// Easy method to execute a DI API Record Set object and return a list of tuples
    /// </summary>
    /// <returns>A tuple object</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// string query =
    ///     $@"select ""ItemCode"", ""ItemName"", ""OnHand"", ""SellItem"", ""CreateDate"", ""UpdateDate"", 
    ///     Case When ""ManBtchNum"" = 'Y' Then 'B' When ""ManSerNum"" = 'Y' Then 'S' Else 'R' End ""ItemType"", ""QryGroup1""
    ///     from OITM";
    /// 
    /// List<(string ItemCode, string ItemName, double OnHand, bool SellItem, DateTime CreateDate, DateTime UpdateDate, ItemType ItemType, bool Property1)> list =
    ///     query.ExecuteQueryList<string, string, double, bool, DateTime, DateTime, ItemType, bool>();
    /// 
    /// list.ForEach(value => {
    ///     //do
    /// });
    /// ]]></code>
    /// </example>
    public static List<(T1, T2, T3, T4, T5, T6, T7, T8)> ExecuteQueryList<T1, T2, T3, T4, T5, T6, T7, T8>(this string query) {
        var rows = GetXmlNodeListFromQuery(query);
        if (rows != null)
            return (from XmlNode row in rows
                select (GetValue<T1>(row.ChildNodes[0].ChildNodes[0]?.ChildNodes[1].InnerText), GetValue<T2>(row.ChildNodes[0].ChildNodes[1]?.ChildNodes[1].InnerText),
                        GetValue<T3>(row.ChildNodes[0].ChildNodes[2]?.ChildNodes[1].InnerText), GetValue<T4>(row.ChildNodes[0].ChildNodes[3]?.ChildNodes[1].InnerText),
                        GetValue<T5>(row.ChildNodes[0].ChildNodes[4]?.ChildNodes[1].InnerText), GetValue<T6>(row.ChildNodes[0].ChildNodes[5]?.ChildNodes[1].InnerText),
                        GetValue<T7>(row.ChildNodes[0].ChildNodes[6]?.ChildNodes[1].InnerText), GetValue<T8>(row.ChildNodes[0].ChildNodes[7]?.ChildNodes[1].InnerText)
                    )).ToList();
        return new List<(T1, T2, T3, T4, T5, T6, T7, T8)>();
    }

    #endregion

    #endregion

    #region Development

    private static bool isSuperUserLoaded;
    private static bool isSuperUser;

    /// <summary>
    /// Property if current connected SBO user is a super user
    /// </summary>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// if (!Shared.IsSuperUser) {
    ///     //do
    /// }
    /// ]]></code>
    /// </example>
    public static bool IsSuperUser {
        get {
            if (isSuperUserLoaded)
                return isSuperUser;
            try {
                string sqlStr = $"select SUPERUSER from OUSR where INTERNAL_K = {ConnectionController.Company.UserSignature}";
                isSuperUser       = sqlStr.ExecuteQueryValue<bool>();
                isSuperUserLoaded = true;
            }
            catch {
            }

            return isSuperUser;
        }
    }

    /// <summary>
    /// Release COM Object method used when need to release DI API com objects
    /// </summary>
    /// <param name="objects">Array of objects to be released</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    /// var rs      = Shared.GetRecordset();
    /// var invoice = (Documents)ConnectionController.Company.GetBusinessObject(BoObjectTypes.oInvoices);
    /// //do
    /// Shared.ReleaseComObject(rs, invoice);
    /// ]]></code>
    /// </example>
    public static void ReleaseComObject(params object[] objects) {
        foreach (object obj in objects) {
            if (obj == null)
                continue;
            try {
                Marshal.ReleaseComObject(obj);
            }
            catch {
                //ignore
            }
        }

        GC.Collect();
    }

    #endregion
}