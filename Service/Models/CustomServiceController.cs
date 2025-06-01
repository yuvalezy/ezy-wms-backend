// using System;
// using System.ServiceProcess;
//
// namespace Service.Models;
//
// public abstract class CustomServiceController : IDisposable {
//     public string ID      { get; }
//     public int?   Port    { get; }
//     public int?   Restart { get; }
//
//     public abstract ServiceControllerStatus Status { get; }
//
//     public CustomServiceController(string id, int? port = null, int? restart = null) {
//         ID      = id;
//         Port    = port;
//         Restart = restart;
//     }
//
//     public static CustomServiceController GetController(string id, int? port = null, int? restart = null) =>
//         !Environment.UserInteractive ? new WindowsServiceController(id, port, restart) : new InteractiveServiceController(id, port, restart);
//
//     public abstract void Start();
//     public abstract void WaitForStatus(ServiceControllerStatus running);
//     public abstract void ExecuteCommand(int                    command);
//     public abstract void Stop();
//     public abstract void Dispose();
// }