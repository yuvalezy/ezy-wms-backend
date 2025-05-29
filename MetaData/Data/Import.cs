using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using MetaData.Models;
using SAPbobsCOM;
using Service.Shared;
using Service.Shared.Utils;

namespace MetaData.Data;

public class Import {
    private Connection    connection = new();
    private string        importPath = Path.Combine("data", "exports"); // Path where export files are located
    private string        logPath    = Path.Combine("data", "imports");
    private StringBuilder logBuilder = new();
    private string        logFileName;

    public void Run() {
        if (!connection.Initialize()) {
            Console.WriteLine("Failed to initialize connection. Import aborted.");
            return;
        }

        // Create log file name with database name and timestamp
        var    company   = connection.GetCompany();
        string dbName    = company.CompanyDB;
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        logFileName = $"import_log_{dbName}_{timestamp}.txt";

        // Create import log directory if it doesn't exist
        if (!Directory.Exists(logPath)) {
            Directory.CreateDirectory(logPath);
        }

        LogMessage($"Import started at {DateTime.Now}");
        LogMessage($"Connected to database: {dbName}");

        try {
            // Import user-defined tables
            ImportUserDefinedTables();

            // Import system table fields
            ImportSystemTableFields();

            // Create Human Resources Roles
            CreateHumanResourcesRoles();

            LogMessage("Import completed successfully.");
        }
        catch (Exception ex) {
            LogMessage($"ERROR: Import failed with exception: {ex.Message}");
            LogMessage($"Stack trace: {ex.StackTrace}");
        }
        finally {
            // Save log file
            File.WriteAllText(Path.Combine(logPath, logFileName), logBuilder.ToString());
            Console.WriteLine($"Import log saved to {Path.Combine(logPath, logFileName)}");
        }
    }

    private void ImportUserDefinedTables() {
        LogMessage("Starting import of user-defined tables...");

        // Get list of user table JSON files
        var userTableFiles = Directory.GetFiles(importPath, "LW_YUVAL08*.json");
        LogMessage($"Found {userTableFiles.Length} user-defined table files to import.");

        foreach (var filePath in userTableFiles) {
            try {
                string json      = File.ReadAllText(filePath);
                var    tableInfo = JsonSerializer.Deserialize<TableInfo>(json);

                if (tableInfo == null) {
                    LogMessage($"WARNING: Could not deserialize file {filePath}. Skipping.");
                    continue;
                }

                LogMessage($"Processing table: {tableInfo.TableName}");

                // Remove @ prefix if present
                string tableName = tableInfo.TableName.StartsWith("@")
                    ? tableInfo.TableName.Substring(1)
                    : tableInfo.TableName;

                // Check if table exists
                bool tableExists = TableExists(tableName);
                LogMessage($"Table {tableName} exists: {tableExists}");

                if (!tableExists) {
                    // Create table
                    CreateUserTable(tableName, tableInfo.TableDescription, tableInfo.ObjectType);
                }

                // Process fields
                foreach (var fieldInfo in tableInfo.Fields) {
                    ProcessField(tableName, fieldInfo);
                }

                // // Process indexes
                // foreach (var indexInfo in tableInfo.Indexes) {
                //     ProcessUserTableIndex(tableName, indexInfo);
                // }
            }
            catch (Exception ex) {
                LogMessage($"ERROR processing file {filePath}: {ex.Message}");
            }
        }

        LogMessage("User-defined tables import completed.");
    }

    private void ImportSystemTableFields() {
        LogMessage("Starting import of system table fields...");

        // Get list of system table JSON files
        var systemTableFiles = Directory.GetFiles(importPath, "system_*.json");
        LogMessage($"Found {systemTableFiles.Length} system table files to import.");

        foreach (var filePath in systemTableFiles) {
            try {
                string json      = File.ReadAllText(filePath);
                var    tableInfo = JsonSerializer.Deserialize<TableInfo>(json);

                if (tableInfo == null) {
                    LogMessage($"WARNING: Could not deserialize file {filePath}. Skipping.");
                    continue;
                }

                LogMessage($"Processing system table: {tableInfo.TableName}");

                // Process fields
                foreach (var fieldInfo in tableInfo.Fields) {
                    ProcessField(tableInfo.TableName, fieldInfo);
                }
            }
            catch (Exception ex) {
                LogMessage($"ERROR processing file {filePath}: {ex.Message}");
            }
        }

        LogMessage("System table fields import completed.");
    }

    private bool TableExists(string tableName) {
        var company = connection.GetCompany();
        var rs      = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);

        try {
            rs.DoQuery($"SELECT 1 FROM OUTB WHERE TableName = '{tableName}'");
            bool exists = !rs.EoF;
            return exists;
        }
        finally {
            Shared.ReleaseComObject(rs);
        }
    }

    private bool FieldExists(string tableName, string fieldName) {
        if (tableName.StartsWith("LW_YUVAL08")) {
            // For user-defined tables, check CUFD
            tableName = $"@{tableName}";
        }
        var company = connection.GetCompany();
        var rs      = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);

        try {
            rs.DoQuery($"SELECT 1 FROM CUFD WHERE TableID = '{tableName}' AND AliasID = '{fieldName}'");
            bool exists = !rs.EoF;
            return exists;
        }
        finally {
            Shared.ReleaseComObject(rs);
        }
    }

    private void CreateUserTable(string tableName, string description, string objectType) {
        var company   = connection.GetCompany();
        var userTable = (UserTablesMD)company.GetBusinessObject(BoObjectTypes.oUserTables);

        try {
            LogMessage($"Creating user table: {tableName}");
            userTable.TableName        = tableName;
            userTable.TableDescription = description;
            userTable.TableType        = (BoUTBTableType)int.Parse(objectType);

            int result = userTable.Add();
            if (result != 0) {
                string error = company.GetLastErrorDescription();
                LogMessage($"ERROR creating table {tableName}: {error}");
                throw new Exception($"Failed to create table {tableName}: {error}");
            }

            LogMessage($"Table {tableName} created successfully.");
        }
        finally {
            Shared.ReleaseComObject(userTable);
        }
    }

    private void ProcessField(string tableName, TableFieldInfo fieldInfo) {
        bool fieldExists = FieldExists(tableName, fieldInfo.Name);
        LogMessage($"Field {fieldInfo.Name} exists in table {tableName}: {fieldExists}");

        if (fieldExists) {
            // We can't update most field properties after creation
            LogMessage($"Field {fieldInfo.Name} already exists. Skipping creation.");
            return;
        }

        var company   = connection.GetCompany();
        var userField = (UserFieldsMD)company.GetBusinessObject(BoObjectTypes.oUserFields);

        try {
            LogMessage($"Creating field {fieldInfo.Name} in table {tableName}");

            userField.TableName   = tableName;
            userField.Name        = fieldInfo.Name;
            userField.Description = fieldInfo.Description;
            userField.Type        = GetFieldType(fieldInfo.Type);
            if (userField.Type != BoFieldTypes.db_Memo) {
                var subType = GetFieldSubType(fieldInfo.EditType);
                if (subType.HasValue)
                    userField.SubType = subType.Value;
            }

            if (fieldInfo.Size != 0) {
                userField.Size     = fieldInfo.Size;
                userField.EditSize = fieldInfo.Size;
            }

            if (!string.IsNullOrWhiteSpace(fieldInfo.DefaultValue))
                userField.DefaultValue = fieldInfo.DefaultValue;
            userField.Mandatory = fieldInfo.IsMandatory ? BoYesNoEnum.tYES : BoYesNoEnum.tNO;

            // Add valid values if any
            if (fieldInfo.ValidValues is { Count: > 0 }) {
                foreach (var validValue in fieldInfo.ValidValues) {
                    userField.ValidValues.Value       = validValue.Key;
                    userField.ValidValues.Description = validValue.Value;
                    userField.ValidValues.Add();
                }
            }

            int result = userField.Add();
            if (result != 0) {
                string error = company.GetLastErrorDescription();
                LogMessage($"ERROR creating field {fieldInfo.Name}: {error}");
                throw new Exception($"Failed to create field {fieldInfo.Name}: {error}");
            }

            LogMessage($"Field {fieldInfo.Name} created successfully.");
        }
        finally {
            Shared.ReleaseComObject(userField);
        }
    }

    private BoFieldTypes GetFieldType(string typeName) =>
        typeName switch {
            "A" => BoFieldTypes.db_Alpha,
            "D" => BoFieldTypes.db_Date,
            "N" => BoFieldTypes.db_Numeric,
            "B" => BoFieldTypes.db_Float,
            "M" => BoFieldTypes.db_Memo,
            _   => throw new ArgumentException($"Unsupported field type: {typeName}")
        };

    private BoFldSubTypes? GetFieldSubType(string editType) {
        return editType switch {
            "?" => BoFldSubTypes.st_Address,
            "#" => BoFldSubTypes.st_Phone,
            "I" => BoFldSubTypes.st_Image,
            "%" => BoFldSubTypes.st_Percentage,
            "M" => BoFldSubTypes.st_Measurement,
            "P" => BoFldSubTypes.st_Price,
            "Q" => BoFldSubTypes.st_Quantity,
            "R" => BoFldSubTypes.st_Rate,
            "S" => BoFldSubTypes.st_Sum,
            "B" => BoFldSubTypes.st_Link,
            "T" => BoFldSubTypes.st_Time,
            // "C" => BoFldSubTypes.st_Checkbox,
            _ => null
        };
    }


    private void CreateHumanResourcesRoles() {
        LogMessage("Starting import of user-defined tables...");

        List<string> roles = [
            Const.GoodsReceipt,
            Const.GoodsReceiptSupervisor,
            Const.GoodsReceiptConfirmation,
            Const.GoodsReceiptConfirmationSupervisor,
            Const.Picking,
            Const.PickingSupervisor,
            Const.Counting,
            Const.CountingSupervisor,
            Const.Transfer,
            Const.TransferSupervisor,
            Const.TransferRequest
        ];

        var company        = connection.GetCompany();
        var companyService = company.GetCompanyService();
        var rolesService   = (EmployeeRolesSetupService)companyService.GetBusinessService(ServiceTypes.EmployeeRolesSetupService);

        var existingRoles = rolesService.GetEmployeeRoleSetupList();
        for (int i = 0; i < existingRoles.Count; i++) {
            string roleName = existingRoles.Item(i).Name;
            if (!roles.Contains(roleName))
                continue;
            LogMessage($"Role {roleName} already exists. Skipping creation.");
            roles.Remove(roleName);
        }

        roles.ForEach(roleName => {
            LogMessage($"Creating role {roleName}...");
            try {
                var newRole = (EmployeeRoleSetup)rolesService.GetDataInterface(EmployeeRolesSetupServiceDataInterfaces.erssEmployeeRoleSetup);
                newRole.Name        = roleName;
                newRole.Description = roleName;
                rolesService.AddEmployeeRoleSetup(newRole);
                LogMessage($"Role {roleName} created successfully.");
            }
            catch (Exception e) {
                LogMessage($"ERROR creating role {roleName}: {e.Message}");
            }
        });

        LogMessage("User-defined tables import completed.");
    }

    private void LogMessage(string message) {
        string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        Console.WriteLine(logEntry);
        logBuilder.AppendLine(logEntry);
    }
}