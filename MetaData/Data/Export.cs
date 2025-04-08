using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MetaData.Models;
using SAPbobsCOM;
using Service.Shared.Utils;

namespace MetaData.Data;

public class Export {
    private Connection connection = new();
    private string     exportPath = Path.Combine("data", "exports");

    public void Run() {
        if (!connection.Initialize()) {
            Console.WriteLine("Failed to initialize connection. Export aborted.");
            return;
        }

        Console.WriteLine("Connection successful. Starting export...");

        // Create export directory if it doesn't exist
        if (!Directory.Exists(exportPath)) {
            Directory.CreateDirectory(exportPath);
        }

        // Export user-defined tables starting with "LW_YUVAL08"
        ExportUserDefinedTables();

        // Export fields starting with "LW_" from system tables
        ExportSystemTablesLWFields();

        Console.WriteLine("Export completed successfully.");
    }

    private void ExportUserDefinedTables() {
        var company = connection.GetCompany();

        var tables = new List<TableInfo>();

        // Get all user tables
        Console.WriteLine("Exporting user-defined tables starting with 'LW_YUVAL08'...");

        // Create a recordset to query user tables
        var rs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
        rs.DoQuery("SELECT TableName, Descr, ObjectType FROM OUTB WHERE TableName LIKE 'LW_YUVAL08%'");

        while (!rs.EoF) {
            string tableName  = rs.Fields.Item("TableName").Value.ToString();
            string tableDesc  = rs.Fields.Item("Descr").Value.ToString();
            string objectType = rs.Fields.Item("ObjectType").Value.ToString();

            Console.WriteLine($"Processing table: {tableName}");

            var tableInfo = new TableInfo {
                TableName        = tableName,
                TableDescription = tableDesc,
                TableType        = "User-Defined",
                ObjectType       = objectType,
            };

            // Get fields for this table
            GetTableFields(company, tableName, tableInfo);

            // Get indexes for this table
            // GetTableIndexes(company, tableNameWithoutPrefix, tableInfo);

            // Save individual table to file
            SaveTableToFile(tableInfo);

            tables.Add(tableInfo);
            rs.MoveNext();
        }

        // Save all user tables to a single file
        string allTablesJson = JsonSerializer.Serialize(tables, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(exportPath, "all_user_tables.json"), allTablesJson);

        Console.WriteLine($"Exported {tables.Count} user-defined tables.");
    }

    private void ExportSystemTablesLWFields() {
        var company = connection.GetCompany();

        Console.WriteLine("Exporting 'LW_' fields from system tables...");

        // Create a recordset to query system tables with LW_ fields
        var rs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);

        // Query to find all system tables that have fields starting with LW_
        string query = @" SELECT DISTINCT T0.TableID 
                 FROM CUFD T0 
                 WHERE T0.AliasID LIKE 'LW%' AND T0.TableID NOT LIKE '@LW_YUVAL08%' ";

        rs.DoQuery(query);

        var systemTablesWithLWFields = new Dictionary<string, List<TableFieldInfo>>();

        while (!rs.EoF) {
            string tableName = rs.Fields.Item("TableID").Value.ToString();

            Console.WriteLine($"Processing system table: {tableName}");

            // Get LW_ fields for this table
            var fields = GetSystemTableLWFields(company, tableName);

            if (fields.Count > 0) {
                systemTablesWithLWFields.Add(tableName, fields);

                // Save individual table's LW fields to file
                var tableInfo = new TableInfo {
                    TableName        = tableName,
                    TableDescription = "System Table",
                    TableType        = "System",
                    Fields           = fields
                };

                SaveTableToFile(tableInfo, "system_");
            }

            rs.MoveNext();
        }

        // Save all system tables with LW fields to a single file
        string allSystemTablesJson = JsonSerializer.Serialize(systemTablesWithLWFields, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(exportPath, "all_system_tables_lw_fields.json"), allSystemTablesJson);

        Console.WriteLine($"Exported LW_ fields from {systemTablesWithLWFields.Count} system tables.");
    }

    private List<TableFieldInfo> GetSystemTableLWFields(Company company, string tableId) {
        var fields = new List<TableFieldInfo>();

        // Query to get LW_ fields with their properties and default values from UFD1
        var rs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
        string query = $@"
                SELECT 
                    T0.AliasID, T0.Descr, T0.TypeID, T0.EditType,T0.EditSize, T0.Dflt, T0.NotNull,
                    T1.FldValue, T1.Descr AS ValueDescr
                FROM CUFD T0
                LEFT JOIN UFD1 T1 ON T0.TableID = T1.TableID AND T0.FieldID = T1.FieldID
                WHERE T0.TableID = '{tableId}' AND T0.AliasID LIKE 'LW_%' 
                ORDER BY T0.AliasID, T1.FldValue
                ";

        rs.DoQuery(query);

        string currentFieldName = "";
        var    defaultValues    = new Dictionary<string, Dictionary<string, string>>();

        while (!rs.EoF) {
            string fieldName = rs.Fields.Item("AliasID").Value.ToString();

            if (fieldName != currentFieldName) {
                // New field
                currentFieldName = fieldName;
                string editType = rs.Fields.Item("EditType").Value.ToString().Trim();
                var currentField = new TableFieldInfo {
                    Name         = fieldName,
                    Description  = rs.Fields.Item("Descr").Value.ToString(),
                    Type         = rs.Fields.Item("TypeID").Value.ToString(),
                    EditType     = !string.IsNullOrEmpty(editType) ? editType : null,
                    Size         = Convert.ToInt32(rs.Fields.Item("EditSize").Value),
                    DefaultValue = rs.Fields.Item("Dflt").Value.ToString(),
                    IsMandatory  = rs.Fields.Item("NotNull").Value.ToString() == "Y",
                };

                defaultValues[fieldName] = new Dictionary<string, string>();
                fields.Add(currentField);
            }

            // Check if there's a valid value in UFD1
            if (!rs.IsNull("FldValue")) {
                string value = rs.Fields.Item("FldValue").Value.ToString();
                string desc  = rs.IsNull("ValueDescr") ? "" : rs.Fields.Item("ValueDescr").Value.ToString();
                defaultValues[fieldName][value] = desc;
            }

            rs.MoveNext();
        }

        // Add default values to fields
        foreach (var field in fields) {
            if (defaultValues.ContainsKey(field.Name) && defaultValues[field.Name].Count > 0) {
                field.ValidValues = defaultValues[field.Name];
            }
        }

        return fields;
    }

    private void GetTableFields(Company company, string tableName, TableInfo tableInfo) {
        // Query to get fields with their properties and default values from UFD1
        var rs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
        string query = $@"
                SELECT 
                    T0.FieldID, T0.AliasID, T0.Descr, T0.TypeID, T0.EditType, T0.EditSize, T0.Dflt, T0.NotNull,
                    T1.FldValue, T1.Descr AS ValueDescr
                FROM CUFD T0
                LEFT JOIN UFD1 T1 ON T0.TableID = T1.TableID AND T0.FieldID = T1.FieldID
                WHERE T0.TableID = '@{tableName}'
                ORDER BY T0.AliasID, T1.FldValue";

        rs.DoQuery(query);

        string currentFieldName = "";
        var    defaultValues    = new Dictionary<string, Dictionary<string, string>>();

        while (!rs.EoF) {
            string fieldName = rs.Fields.Item("AliasID").Value.ToString();

            if (fieldName != currentFieldName) {
                // New field
                currentFieldName = fieldName;
                string editType = rs.Fields.Item("EditType").Value.ToString().Trim();
                var currentField = new TableFieldInfo {
                    Name         = fieldName,
                    Description  = rs.Fields.Item("Descr").Value.ToString(),
                    Type         = rs.Fields.Item("TypeID").Value.ToString(),
                    EditType     = !string.IsNullOrEmpty(editType) ? editType : null,
                    Size         = Convert.ToInt32(rs.Fields.Item("EditSize").Value),
                    DefaultValue = rs.Fields.Item("Dflt").Value.ToString(),
                    IsMandatory  = rs.Fields.Item("NotNull").Value.ToString() == "Y",
                };

                defaultValues[fieldName] = new Dictionary<string, string>();
                tableInfo.Fields.Add(currentField);
            }

            // Check if there's a valid value in UFD1
            if (!rs.IsNull("FldValue")) {
                string value = rs.Fields.Item("FldValue").Value.ToString();
                string desc  = rs.IsNull("ValueDescr") ? "" : rs.Fields.Item("ValueDescr").Value.ToString();
                defaultValues[fieldName][value] = desc;
            }

            rs.MoveNext();
        }

        // Add default values to fields
        foreach (var field in tableInfo.Fields) {
            if (defaultValues.ContainsKey(field.Name) && defaultValues[field.Name].Count > 0) {
                field.ValidValues = defaultValues[field.Name];
            }
        }
    }

    // private void GetTableIndexes(Company company, string tableNameWithoutPrefix, TableInfo tableInfo) {
    //     try {
    //         var rs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
    //
    //         // Query to get indexes for the user-defined table
    //         string query = $@"
    //             SELECT T0.IndexName, T0.IsUnique, T1.ColumnName 
    //             FROM OUDI T0 
    //             INNER JOIN OUDI1 T1 ON T0.IndexID = T1.IndexID AND T0.TableName = T1.TableName
    //             WHERE T0.TableName = '{tableNameWithoutPrefix}'
    //             ORDER BY T0.IndexName, T1.ColumnPosition";
    //
    //         rs.DoQuery(query);
    //
    //         string         currentIndexName = "";
    //         TableIndexInfo currentIndex     = null;
    //
    //         while (!rs.EoF) {
    //             string indexName = rs.Fields.Item("IndexName").Value.ToString();
    //
    //             if (indexName != currentIndexName) {
    //                 // New index found
    //                 currentIndexName = indexName;
    //                 currentIndex = new TableIndexInfo {
    //                     Name     = indexName,
    //                     IsUnique = rs.Fields.Item("IsUnique").Value.ToString() == "Y"
    //                 };
    //                 tableInfo.Indexes.Add(currentIndex);
    //             }
    //
    //             // Add field to current index
    //             currentIndex.Fields.Add(rs.Fields.Item("ColumnName").Value.ToString());
    //
    //             rs.MoveNext();
    //         }
    //     }
    //     catch (Exception ex) {
    //         Console.WriteLine($"Error getting indexes for table {tableNameWithoutPrefix}: {ex.Message}");
    //     }
    // }

    private void SaveTableToFile(TableInfo tableInfo, string prefix = "") {
        try {
            string fileName = $"{prefix}{tableInfo.TableName.Replace("@", "")}.json";
            string filePath = Path.Combine(exportPath, fileName);

            string json = JsonSerializer.Serialize(tableInfo, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);

            Console.WriteLine($"Saved table info to {filePath}");
        }
        catch (Exception ex) {
            Console.WriteLine($"Error saving table {tableInfo.TableName} to file: {ex.Message}");
        }
    }
}