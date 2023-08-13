using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using SAPbobsCOM;
using Service.Shared.Utils;

namespace Service.Shared.Data; 

/// <summary>
/// This object is used to extract data from a RecordSet in a more efficient method. <br />
/// When extracting a big amount of lines from a recordset, the calls to the COM Object are too slow. <br />
/// This allows a more efficient method of reading big data from the database.
/// </summary>
/// <typeparam name="T">T is any class where it has properties with the [RecordsetReaderColumn] attribute.</typeparam>
/// <example>
///   <code lang="C#"><![CDATA[string query = @"select ""ItemCode"", ""ItemName"", ""OnHand"", ""IsCommited"", ""OnOrder"", ""QryGroup1"", ""VATLiable"", ""InvntItem"", ""PrchseItem"", ""SellItem"", ""DfltWH"", ""CardCode"", 
///         ""CreateDate"", ""UpdateDate"", Case When ""ManBtchNum"" = 'Y' Then 'B' When ""ManSerNum"" = 'Y' Then 'S' Else 'R' End ""ItemType""
///         from OITM where ""OnHand"" > 0";
///         var rs    = Shared.GetRecordset();
///         rs.DoQuery(query);
///         var reader = new RecordsetReader<Item>(rs);
///         //read all lines
///         var items  = reader.Read();
///         Console.WriteLine($"Total Available Stock: {Numeric.FormatNumber(items.Sum(v => v.Available), NumberFormatType.Quantity)}");
///         Console.WriteLine($"Total Regular Items: {items.Count(v => v.ItemType == ItemType.Regular)}");
///         Console.WriteLine($"Total Batch Items: {items.Count(v => v.ItemType == ItemType.Batch)}");
///         Console.WriteLine($"Total Serial Items: {items.Count(v => v.ItemType == ItemType.Serial)}");
///         //read 1 line
///         var item = reader.ReadLine(0);
///         Console.WriteLine($"Available Stock: {Numeric.FormatNumber(item.Available, NumberFormatType.Quantity)}");
///         Shared.ReleaseComObject(rs);
/// 
///     public class Item {
///         [RecordsetReaderColumn]               public string   ItemCode         { get; set; }
///         [RecordsetReaderColumn]               public string   ItemName         { get; set; }
///         [RecordsetReaderColumn("OnHand")]     public double   Stock            { get; set; }
///         [RecordsetReaderColumn("IsCommited")] public double   Committed        { get; set; }
///         [RecordsetReaderColumn]               public double   OnOrder          { get; set; }
///         public                                       double   Available        => Stock - Committed;
///         [RecordsetReaderColumn("QryGroup1")]  public bool     Enable           { get; set; }
///         [RecordsetReaderColumn("VATLiable")]  public bool     Tax              { get; set; }
///         [RecordsetReaderColumn("InvntItem")]  public bool     InventoryItem    { get; set; }
///         [RecordsetReaderColumn("PrchseItem")] public bool     PurchaseItem     { get; set; }
///         [RecordsetReaderColumn]               public bool     SellItem         { get; set; }
///         [RecordsetReaderColumn("DfltWH")]     public string   DefaultWarehouse { get; set; }
///         [RecordsetReaderColumn("CardCode")]   public string   DefaultSupplier  { get; set; }
///         [RecordsetReaderColumn]               public DateTime CreateDate       { get; set; }
///         [RecordsetReaderColumn]               public DateTime UpdateDate       { get; set; }
///         [RecordsetReaderColumn]               public ItemType ItemType         { get; set; }
///     }
///     public enum ItemType {
///         Batch   = 'B',
///         Serial  = 'S',
///         Regular = 'R'
///     }
/// 
/// ]]></code>
/// </example>
/// <example>
///   <code lang="C#"><![CDATA[string query = @"select ""ItemCode"", ""ItemName"", ""OnHand"", ""IsCommited"", ""OnOrder"", ""QryGroup1"", ""VATLiable"", ""InvntItem"", ""PrchseItem"", ""SellItem"", ""DfltWH"", ""CardCode"", 
///         ""CreateDate"", ""UpdateDate"", Case When ""ManBtchNum"" = 'Y' Then 'B' When ""ManSerNum"" = 'Y' Then 'S' Else 'R' End ""ItemType""
///         from OITM where ""OnHand"" > 0";
///         var items = query.ExecuteReader<Item>();
///         Console.WriteLine($"Total Available Stock: {Numeric.FormatNumber(items.Sum(v => v.Available), NumberFormatType.Quantity)}");
///         Console.WriteLine($"Total Regular Items: {items.Count(v => v.ItemType == ItemType.Regular)}");
///         Console.WriteLine($"Total Batch Items: {items.Count(v => v.ItemType == ItemType.Batch)}");
///         Console.WriteLine($"Total Serial Items: {items.Count(v => v.ItemType == ItemType.Serial)}");
///         //read 1 line
///         var item = query.ExecuteQueryReaderRow<Item>();
///         Console.WriteLine($"Available Stock: {Numeric.FormatNumber(item.Available, NumberFormatType.Quantity)}");
/// 
///     public class Item {
///         [RecordsetReaderColumn]               public string   ItemCode         { get; set; }
///         [RecordsetReaderColumn]               public string   ItemName         { get; set; }
///         [RecordsetReaderColumn("OnHand")]     public double   Stock            { get; set; }
///         [RecordsetReaderColumn("IsCommited")] public double   Committed        { get; set; }
///         [RecordsetReaderColumn]               public double   OnOrder          { get; set; }
///         public                                       double   Available        => Stock - Committed;
///         [RecordsetReaderColumn("QryGroup1")]  public bool     Enable           { get; set; }
///         [RecordsetReaderColumn("VATLiable")]  public bool     Tax              { get; set; }
///         [RecordsetReaderColumn("InvntItem")]  public bool     InventoryItem    { get; set; }
///         [RecordsetReaderColumn("PrchseItem")] public bool     PurchaseItem     { get; set; }
///         [RecordsetReaderColumn]               public bool     SellItem         { get; set; }
///         [RecordsetReaderColumn("DfltWH")]     public string   DefaultWarehouse { get; set; }
///         [RecordsetReaderColumn("CardCode")]   public string   DefaultSupplier  { get; set; }
///         [RecordsetReaderColumn]               public DateTime CreateDate       { get; set; }
///         [RecordsetReaderColumn]               public DateTime UpdateDate       { get; set; }
///         [RecordsetReaderColumn]               public ItemType ItemType         { get; set; }
///     }
///     public enum ItemType {
///         Batch   = 'B',
///         Serial  = 'S',
///         Regular = 'R'
///     }
/// 
/// ]]></code>
/// </example>
public class RecordsetReader<T> : BaseReader<T> where T : class {
    private readonly Recordset rs;

    private readonly Dictionary<string, RecordsetReaderColumn> readerColumns;

    private IEnumerable<XElement> rows;

    /// <summary>
    /// Initialize the recordset reader object
    /// </summary>
    /// <param name="recordset">Recordset where the data content will be extracted from</param>
    public RecordsetReader(Recordset recordset) {
        rs            = recordset;
        readerColumns = new();
        foreach (var prop in typeof(T).GetProperties()) {
            object[] attributes = prop.GetCustomAttributes(true);
            foreach (object attribute in attributes) {
                if (attribute is not RecordsetReaderColumn readerColumn)
                    continue;
                if (string.IsNullOrWhiteSpace(readerColumn.ID))
                    readerColumn.ID = prop.Name;
                readerColumn.Property = prop;
                readerColumns.Add(readerColumn.ID, readerColumn);
            }
        }
    }

    private void LoadXML() {
        string xml     = rs.GetAsXML();
        var    xmlData = XElement.Parse(xml);
        rows = xmlData.Descendants("row");
    }

    /// <summary>
    /// Returns an IEnumerable of rows from the data
    /// </summary>
    public IEnumerable<T> Read() {
        LoadXML();
        foreach (var row in rows)
            yield return RowToObject(row);
    }

    /// <summary>
    /// Returns a specific row data
    /// </summary>
    public T ReadLine(int index) {
        LoadXML();
        return RowToObject(rows.ElementAt(index));
    }

    private T RowToObject(XElement row) {
        var value = GetObject();
        foreach (var cell in row.Elements()) {
            string cellName = cell.Name.LocalName;
            if (!readerColumns.ContainsKey(cellName))
                continue;
            SetValue(value, cell.Value, readerColumns[cellName]);
        }

        return value;
    }

    private static void SetValue(T value, string readValue, ReaderColumn readerColumn) {
        if (string.IsNullOrWhiteSpace(readValue))
            return;
        var    prop         = readerColumn.Property;
        string propertyType = readerColumn.AlterType ?? prop.PropertyType.ToString();
        try {
            switch (propertyType) {
                case "System.Char":
                    prop.SetValue(value, char.Parse(readValue));
                    break;
                case "System.String":
                    prop.SetValue(value, readValue);
                    break;
                case "System.Boolean":
                    prop.SetValue(value, readValue is "Y" or "1" or "true");
                    break;
                case "System.Int16":
                    prop.SetValue(value, short.Parse(readValue));
                    break;
                case "System.Nullable`1[System.Int32]":
                case "System.Int32":
                    prop.SetValue(value, int.Parse(readValue));
                    break;
                case "System.Int64":
                    prop.SetValue(value, long.Parse(readValue));
                    break;
                case "System.Double":
                    prop.SetValue(value, Numeric.FromStringToDouble(readValue));
                    break;
                case "System.Decimal":
                    prop.SetValue(value, Numeric.FromStringToDecimal(readValue));
                    break;
                case "System.DateTime":
                    prop.SetValue(value, DateTime.ParseExact(readValue, "yyyyMMdd", CultureInfo.InvariantCulture));
                    break;
                default:
                    if (prop.PropertyType.IsEnum)
                        prop.SetValue(value, !readValue.IsNumeric() ? Convert.ToChar(readValue) : int.Parse(readValue));
                    break;
            }
        }
        catch (Exception ex) {
            throw new Exception("Error reading rs value for column: " + readerColumn.ID + ", type: " + propertyType + ": " + ex.Message);
        }
    }
}