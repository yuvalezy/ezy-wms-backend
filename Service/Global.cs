// using System;
// using System.Collections.Generic;
// using System.Data;
// using System.Diagnostics;
// using System.Linq;
// using System.Reflection;
// using System.Threading;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.Logging;
// using SAPbobsCOM;
// using Service.Models;
// using Service.Shared;
// using Service.Shared.Company;
// using Service.Shared.Data;
// using Service.Shared.Utils;
// using Connection = Service.Shared.Company.ConnectionController;
// using Path = System.IO.Path;
// using Version = Service.Shared.Utils.Version;
//
// namespace Service
// {
//     public class GlobalService : IGlobalService
//     {
//         private readonly IConfiguration _configuration;
//         private readonly ILogger<GlobalService> _logger;
//         private readonly Mutex _connectionMutex = new(false, "connection");
//         internal readonly Mutex TransactionMutex = new(false, "transaction");
//
//         public GlobalService(IConfiguration configuration, ILogger<GlobalService> logger)
//         {
//             _configuration = configuration;
//             _logger = logger;
//             LoadConfiguration();
//         }
//
//         #region Properties
//
//         public string Database { get; private set; }
//         public string CompanyName { get; private set; }
//         public bool IsMain { get; private set; }
//         public int? Port { get; private set; }
//         public RestAPISettings RestAPISettings { get; private set; }
//         public bool Debug { get; private set; }
//         public BoDataServerTypes ServerType { get; private set; }
//         public string DBServiceVersion { get; private set; }
//         public string User { get; private set; }
//         public string Password { get; private set; }
//         public bool TestHelloWorld { get; private set; }
//         public bool GRPODraft { get; private set; }
//         public bool GRPOModificationsRequiredSupervisor { get; private set; }
//         public bool GRPOCreateSupervisorRequired { get; private set; }
//         public bool TransferTargetItems { get; private set; }
//         public bool PrintThread { get; private set; }
//         public bool Background { get; private set; }
//         public bool Interactive { get; private set; }
//         public bool LoadBalancing { get; private set; }
//         public ServiceNodes Nodes { get; private set; }
//         public Dictionary<int, Authorization> RolesMap { get; } = new();
//         public Dictionary<int, List<Authorization>> UserAuthorizations { get; } = new();
//         public Dictionary<string, List<int>> WarehouseEntryBins { get; } = new();
//         public DataConnector Connector { get; private set; }
//
//         #endregion
//
//         private void LoadConfiguration()
//         {
//             // Load from configuration
//             Port = _configuration.GetValue<int?>("Service:Port");
//             Debug = _configuration.GetValue<bool>("Service:Debug", false);
//             Background = _configuration.GetValue<bool>("Service:Background", false);
//             Interactive = _configuration.GetValue<bool>("Service:Interactive", false);
//             LoadBalancing = _configuration.GetValue<bool>("LoadBalancing:Enabled", false);
//             
//             IsMain = !Background && !Port.HasValue;
//             
//             // Load database configuration
//             Database = _configuration.GetValue<string>("Database:Name");
//             User = _configuration.GetValue<string>("Database:User");
//             Password = _configuration.GetValue<string>("Database:Password");
//             
//             // Load connection from registry or configuration
//             LoadConnectionSettings();
//         }
//
//         private void LoadConnectionSettings()
//         {
//             // This would need to be adapted to load from configuration instead of registry
//             // For now, keeping the structure but would need refactoring
//             Connection.Database = Database;
//             ServerType = Connection.GetServerType();
//         }
//
//         public bool ConnectCompany()
//         {
//             _connectionMutex.WaitOne();
//             try
//             {
//                 try
//                 {
//                     if (Connection.Company is { Connected: true })
//                         return true;
//                 }
//                 catch (Exception)
//                 {
//                     // ignored
//                 }
//
//                 try
//                 {
//                     Connection.ConnectCompany(Connection.Server, ServerType, Connection.Database, User, Password, Connection.Server);
//                 }
//                 catch (Exception ex)
//                 {
//                     _logger.LogError(ex, "DI API connection error");
//                     return false;
//                 }
//
//                 return Connection.Company?.Connected ?? false;
//             }
//             finally
//             {
//                 _connectionMutex.ReleaseMutex();
//             }
//         }
//
//         public void DisconnectCompany()
//         {
//             _connectionMutex.WaitOne();
//             try
//             {
//                 Connection.DisconnectCompany();
//             }
//             finally
//             {
//                 _connectionMutex.ReleaseMutex();
//             }
//         }
//
//         public void LoadDatabaseSettings()
//         {
//             // Implementation would load settings from database
//             // This is a placeholder for the migration
//             _logger.LogInformation("Loading database settings");
//         }
//
//         public void ReloadAPISettings()
//         {
//             // Implementation would reload API settings
//             _logger.LogInformation("Reloading API settings");
//         }
//
//         public bool ValidateAuthorization(int employeeID, params Authorization[] authorizations)
//         {
//             if (!UserAuthorizations.ContainsKey(employeeID))
//                 return false;
//
//             var userAuth = UserAuthorizations[employeeID];
//             return authorizations.Any(a => userAuth.Contains(a) || userAuth.Contains(Authorization.All));
//         }
//     }
//
//     // Static wrapper for backward compatibility during migration
//     public static class Global
//     {
//         private static IGlobalService _service;
//
//         public static void Initialize(IGlobalService service)
//         {
//             _service = service;
//         }
//
//         public static string Database => _service?.Database;
//         public static string CompanyName => _service?.CompanyName;
//         public static bool IsMain => _service?.IsMain ?? false;
//         public static int? Port => _service?.Port;
//         public static RestAPISettings RestAPISettings => _service?.RestAPISettings;
//         public static bool Debug => _service?.Debug ?? false;
//         public static BoDataServerTypes ServerType => _service?.ServerType ?? BoDataServerTypes.dst_MSSQL2019;
//         public static string DBServiceVersion => _service?.DBServiceVersion;
//         public static string User => _service?.User;
//         public static string Password => _service?.Password;
//         public static bool TestHelloWorld => _service?.TestHelloWorld ?? false;
//         public static bool GRPODraft => _service?.GRPODraft ?? false;
//         public static bool GRPOModificationsRequiredSupervisor => _service?.GRPOModificationsRequiredSupervisor ?? false;
//         public static bool GRPOCreateSupervisorRequired => _service?.GRPOCreateSupervisorRequired ?? false;
//         public static bool TransferTargetItems => _service?.TransferTargetItems ?? false;
//         public static bool PrintThread => _service?.PrintThread ?? false;
//         public static bool Background => _service?.Background ?? false;
//         public static bool Interactive => _service?.Interactive ?? false;
//         public static bool LoadBalancing => _service?.LoadBalancing ?? false;
//         public static ServiceNodes Nodes => _service?.Nodes;
//         public static Dictionary<int, Authorization> RolesMap => _service?.RolesMap ?? new();
//         public static Dictionary<int, List<Authorization>> UserAuthorizations => _service?.UserAuthorizations ?? new();
//         public static Dictionary<string, List<int>> WarehouseEntryBins => _service?.WarehouseEntryBins ?? new();
//         public static DataConnector Connector => _service?.Connector;
//
//         public static bool ConnectCompany() => _service?.ConnectCompany() ?? false;
//         public static void DisconnectCompany() => _service?.DisconnectCompany();
//         public static void LoadDatabaseSettings() => _service?.LoadDatabaseSettings();
//         public static void ReloadAPISettings() => _service?.ReloadAPISettings();
//         public static bool ValidateAuthorization(int employeeID, params Authorization[] authorizations) => 
//             _service?.ValidateAuthorization(employeeID, authorizations) ?? false;
//     }
// }