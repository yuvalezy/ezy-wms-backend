using System;
using System.Linq;
using SAPbobsCOM;

namespace Service.Shared.Utils; 

public static class Extensions {
    #region Regular Types

    /// <summary>
    /// Convert string to a query compatible string value.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     string ItemCode = "TestItem";
    ///     string TestValue = "Hello'World!";
    ///     string query;
    /// 
    ///     //Instead of doing a replace
    ///     query    = $"update \"@TEST\" set \"U_ItemName\" = '{TestValue.Replace("'", "''")}' where \"Code\" = '{ItemCode.Replace("'", "''")}' ";
    ///     //Use the ToQuery Extension
    /// 
    ///     query    = $"update \"@TEST\" set \"U_ItemName\" = '{TestValue.ToQuery()}' where \"Code\" = '{ItemCode.ToQuery()}' ";
    /// 
    ///     query.ExecuteQuery();
    /// ]]></code>
    /// </example>
    public static string ToQuery(this string value) => value.Replace("'", "''");

    /// <summary>
    /// Check if string is not null or empty to a query compatible string value or NULL 
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <returns>N'\{value\}' if not empty and not NULL</returns>
    /// <returns>NULL if empty or NULL</returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     string ItemCode  = "TestItem";
    ///     string TestValue = "Hello'World!";
    ///     string query;
    /// 
    ///     //When saving a field and you want to support a null value, you have to prepare a save vale parameter first
    ///     string SaveValue = !string.IsNullOrWhiteSpace(TestValue) ? $"N'{TestValue.ToQuery()}'" : "NULL";
    ///     query = $"update \"@TEST\" set \"U_ItemName\" = {SaveValue} where \"Code\" = '{ItemCode.Replace("'", "''")}' ";
    /// 
    ///     //Use the ToSaveValue Extension
    ///     query = $"update \"@TEST\" set \"U_ItemName\" = {TestValue.ToSaveQuery()} where \"Code\" = '{ItemCode.ToQuery()}' ";
    /// 
    ///     query.ExecuteQuery();
    /// ]]></code>
    /// </example>
    public static string ToSaveQuery(this string value) => !string.IsNullOrWhiteSpace(value) ? $"N'{value.ToQuery()}'" : "NULL";

    /// <summary>
    /// Convert formatted numeric string value to regular numeric string value
    /// </summary>
    /// <param name="value">Formatted Number String value to convert</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     string ItemCode       = "TestItem";
    ///     string formattedValue = "1.000,35";
    ///     //It will convert the formatted value to 1000.35 value
    ///     string query = $"update \"@TEST\" set \"U_Total\" = {formattedValue.ToNumericParse()} where \"Code\" = '{ItemCode.ToQuery()}' ";
    /// ]]></code>
    /// </example>
    public static string ToNumericParse(this string value) => Numeric.FromFormat(value).ToParseValue();

    /// <summary>
    /// Convert date time to parse SBO value
    /// </summary>
    /// <param name="value">DateTime value</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     //This will convert the date to format yyyyMMdd that SBO uses to set date values for data sources
    ///     Form.DataSources.UserDataSources.Item("srcDate").Value = DateTime.Now.ToDateParse();
    /// ]]></code>
    /// </example>
    public static string ToDateParse(this DateTime value) => value.ToString("yyyyMMdd");

    /// <summary>
    /// Converts formatted numeric string value to double value
    /// </summary>
    /// <param name="value">String value to convert</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     string formattedValue = "1.000,35";
    ///     //This will consider the localization of the machine where SAP is running,
    ///     //the local language of the SAP database,
    ///     //and the decimal and thousand separator settings in the Database Settings
    ///     double value          = formattedValue.ToDouble();
    ///     //do something
    /// ]]></code>
    /// </example>
    public static double ToDouble(this string value) => Convert.ToDouble(Numeric.FromFormat(value));

    /// <summary>
    /// Converts formatted numeric string value to decimal value
    /// </summary>
    /// <param name="value">String value to convert</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     string formattedValue = "1.000,35";
    ///     //This will consider the localization of the machine where SAP is running,
    ///     //the local language of the SAP database,
    ///     //and the decimal and thousand separator settings in the Database Settings
    ///     decimal value          = formattedValue.ToDecimal();
    ///     //do something
    /// ]]></code>
    /// </example>
    public static decimal ToDecimal(this string value) => Convert.ToDecimal(Numeric.FromFormat(value));

    /// <summary>
    /// Converts bool value to Y/N value to save in database
    /// </summary>
    /// <param name="value">Boolean value to convert</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     string ItemCode = "TestItem";
    ///     bool   value    = true;
    ///     string query    = $"update \"@TEST\" set \"U_Active\" = {value.ToYesNo()} where \"Code\" = '{ItemCode.ToQuery()}' ";
    /// ]]></code>
    /// </example>
    public static string ToYesNo(this bool value) => value ? "Y" : "N";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     string ItemCode = "TestItem";
    ///     string query    = $"select \"U_Active\" from \"@TEST\" where \"Code\" = '{ItemCode.ToQuery()}' ";
    ///     var    rs       = Shared.GetRecordset();
    ///     rs.DoQuery(query);
    ///     bool value = ((string)rs.Fields.Item(0).Value).ToBool();
    ///     //do
    /// ]]></code>
    /// </example>
    public static bool ToBool(this string value) => value.ToLower() is "y" or "1" or "true";

    /// <summary>
    /// Validated if a value is in a array
    /// </summary>
    /// <param name="value">Value to check</param>
    /// <param name="checkValues">Values array</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     string ItemCode = "TestItem";
    ///     string query    = $"select \"TreeType\" from OITM where \"ItemCode\" = '{ItemCode.ToQuery()}' ";
    ///     string treeType = query.ExecuteQueryValue<string>();
    ///     if (treeType.In("P", "S")) {
    ///         //do
    ///     }
    /// ]]></code>
    /// </example>
    public static bool In<T>(this T value, params T[] checkValues) => checkValues.Contains(value);

    #endregion



    #region Record set

    /// <summary>
    /// Check if record set item value is null
    /// </summary>
    /// <param name="rs">Record Set Object</param>
    /// <param name="id">Field ID</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     var rs = Shared.GetRecordset();
    ///     rs.DoQuery("select \"ItemName\" from OITM where \"ItemCode\" = 'TEST'");
    /// 
    ///     //Regular Method
    ///     if (string.IsNullOrWhiteSpace(rs.Fields.Item(0).Value.ToString())) {
    ///         //do
    ///     }
    /// 
    ///     //Improved Framework method
    ///     if (rs.IsNull(0)) {
    ///         //do
    ///     }
    /// ]]></code>
    /// </example>
    public static bool IsNull(this Recordset rs, object id) => string.IsNullOrWhiteSpace(rs.Fields.Item(id).Value.ToString());

    /// <summary>
    /// Get character value
    /// </summary>
    /// <param name="rs">Record Set Object</param>
    /// <param name="id">Field ID</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     var rs = Shared.GetRecordset();
    ///     rs.DoQuery("select \"TreeType\" from OITM where \"ItemCode\" = 'TEST'");
    /// 
    ///     char value;
    ///     
    ///     //Regular Method
    ///     value = Convert.ToChar(rs.Fields.Item(0).Value);
    /// 
    ///     //Improved Framework method
    ///     value = rs.GetChar(0);
    /// ]]></code>
    /// </example>
    public static char GetChar(this Recordset rs, object id) => Convert.ToChar(rs.Fields.Item(id).Value);

    /// <summary>
    /// Get String value
    /// </summary>
    /// <param name="rs">Record Set Object</param>
    /// <param name="id">Field ID</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     var rs = Shared.GetRecordset();
    ///     rs.DoQuery("select \"ItemName\" from OITM where \"ItemCode\" = 'TEST'");
    /// 
    ///     string value;
    ///     
    ///     //Regular Method
    ///     value = rs.Fields.Item(0).Value.ToString();
    /// 
    ///     //Improved Framework method
    ///     value = rs.GetString(0);
    /// ]]></code>
    /// </example>
    public static string GetString(this Recordset rs, object id) => rs.Fields.Item(id).Value.ToString().Trim();

    /// <summary>
    /// Get Boolean value
    /// </summary>
    /// <param name="rs">Record Set Object</param>
    /// <param name="id">Field ID</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     var rs = Shared.GetRecordset();
    ///     rs.DoQuery("select \"SellItem\" from OITM where \"ItemCode\" = 'TEST'");
    /// 
    ///     bool value;
    ///     
    ///     //Regular Method
    ///     value = rs.Fields.Item(0).Value.ToString() == "Y";
    /// 
    ///     //Improved Framework method
    ///     value = rs.GetBool(0);
    /// ]]></code>
    /// </example>
    public static bool GetBool(this Recordset rs, object id) => rs.Fields.Item(id).Value.ToString() is "Y" or "1" or "true";

    /// <summary>
    /// Get Int16 value
    /// </summary>
    /// <param name="rs">Record Set Object</param>
    /// <param name="id">Field ID</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     var rs = Shared.GetRecordset();
    ///     rs.DoQuery("select \"DocEntry\" from OITM where \"ItemCode\" = 'TEST'");
    /// 
    ///     short value;
    ///     
    ///     //Regular Method
    ///     value = (short)rs.Fields.Item(0).Value;
    /// 
    ///     //Improved Framework method
    ///     value = rs.GetInt16(0);
    /// ]]></code>
    /// </example>
    public static short GetInt16(this Recordset rs, object id) => Convert.ToInt16(rs.Fields.Item(id).Value);

    /// <summary>
    /// Get Integer value
    /// </summary>
    /// <param name="rs">Record Set Object</param>
    /// <param name="id">Field ID</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     var rs = Shared.GetRecordset();
    ///     rs.DoQuery("select \"DocEntry\" from OITM where \"ItemCode\" = 'TEST'");
    /// 
    ///     int value;
    ///     
    ///     //Regular Method
    ///     value = (int)rs.Fields.Item(0).Value;
    /// 
    ///     //Improved Framework method
    ///     value = rs.GetInt(0);
    /// ]]></code>
    /// </example>
    public static int GetInt(this Recordset rs, object id) => Convert.ToInt32(rs.Fields.Item(id).Value);

    /// <summary>
    /// Get Double value
    /// </summary>
    /// <param name="rs">Record Set Object</param>
    /// <param name="id">Field ID</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     var rs = Shared.GetRecordset();
    ///     rs.DoQuery("select \"OnHand\" from OITM where \"ItemCode\" = 'TEST'");
    /// 
    ///     double value;
    ///     
    ///     //Regular Method
    ///     value = (double)rs.Fields.Item(0).Value;
    /// 
    ///     //Improved Framework method
    ///     value = rs.GetDouble(0);
    /// ]]></code>
    /// </example>
    public static double GetDouble(this Recordset rs, object id) => Convert.ToDouble(rs.Fields.Item(id).Value);

    /// <summary>
    /// Get Decimal value
    /// </summary>
    /// <param name="rs">Record Set Object</param>
    /// <param name="id">Field ID</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     var rs = Shared.GetRecordset();
    ///     rs.DoQuery("select \"OnHand\" from OITM where \"ItemCode\" = 'TEST'");
    /// 
    ///     decimal value;
    ///     
    ///     //Regular Method
    ///     value = (decimal)rs.Fields.Item(0).Value;
    /// 
    ///     //Improved Framework method
    ///     value = rs.GetDecimal(0);
    /// ]]></code>
    /// </example>
    public static decimal GetDecimal(this Recordset rs, object id) => Convert.ToDecimal(rs.Fields.Item(id).Value);

    /// <summary>
    /// Get Date Time value
    /// </summary>
    /// <param name="rs">Record Set Object</param>
    /// <param name="id">Field ID</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     var rs = Shared.GetRecordset();
    ///     rs.DoQuery("select \"CreateDate\" from OITM where \"ItemCode\" = 'TEST'");
    /// 
    ///     DateTime value;
    ///     
    ///     //Regular Method
    ///     value = (DateTime)rs.Fields.Item(0).Value;
    /// 
    ///     //Improved Framework method
    ///     value = rs.GetDateTime(0);
    /// ]]></code>
    /// </example>
    public static DateTime GetDateTime(this Recordset rs, object id) => (DateTime)rs.Fields.Item(id).Value;

    /// <summary>
    /// Get Date Time Parse String Value
    /// </summary>
    /// <param name="rs">Record Set Object</param>
    /// <param name="id">Field ID</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[
    ///     var rs = Shared.GetRecordset();
    ///     rs.DoQuery("select \"CreateDate\" from OITM where \"ItemCode\" = 'TEST'");
    /// 
    ///     var userSource = Form.DataSources.UserDataSources.Item("srcDate");
    ///     
    ///     //Regular Method
    ///     userSource.Value = ((DateTime)rs.Fields.Item(0).Value).ToString("yyyyMMdd");
    ///     
    ///     //Improved Framework method
    ///     userSource.Value = rs.DateParse(0);
    /// ]]></code>
    /// </example>
    public static string DateParse(this Recordset rs, object id) {
        var dateValue = (DateTime)rs.Fields.Item(id).Value;
        return dateValue.Year > 1900 ? dateValue.ToString("yyyyMMdd") : "";
    }

    #endregion

}