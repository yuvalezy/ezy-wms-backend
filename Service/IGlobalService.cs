// using System.Collections.Generic;
// using SAPbobsCOM;
// using Service.Models;
// using Service.Shared;
// using Service.Shared.Data;
//
// namespace Service;
//
// public interface IGlobalService
// {
//     string                               Database                            { get; }
//     string                               CompanyName                         { get; }
//     bool                                 Debug                               { get; }
//     BoDataServerTypes                    ServerType                          { get; }
//     string                               DBServiceVersion                    { get; }
//     string                               User                                { get; }
//     string                               Password                            { get; }
//     bool                                 TestHelloWorld                      { get; }
//     bool                                 GRPODraft                           { get; }
//     bool                                 GRPOModificationsRequiredSupervisor { get; }
//     bool                                 GRPOCreateSupervisorRequired        { get; }
//     bool                                 TransferTargetItems                 { get; }
//     bool                                 PrintThread                         { get; }
//     bool                                 Background                          { get; }
//     bool                                 Interactive                         { get; }
//     Dictionary<int, Authorization>       RolesMap                            { get; }
//     Dictionary<int, List<Authorization>> UserAuthorizations                  { get; }
//     Dictionary<string, List<int>>        WarehouseEntryBins                  { get; }
//     DataConnector                        Connector                           { get; }
//         
//     bool ConnectCompany();
//     void DisconnectCompany();
//     void LoadDatabaseSettings();
//     void ReloadAPISettings();
//     bool ValidateAuthorization(int employeeID, params Authorization[] authorizations);
// }