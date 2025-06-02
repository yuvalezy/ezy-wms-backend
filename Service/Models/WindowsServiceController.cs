// using System.ServiceProcess;
// using System.Threading;
// using System.Threading.Tasks;
//
// namespace Service.Models;
//
// public class WindowsServiceController : CustomServiceController {
//     private readonly ServiceController       service;
//     private readonly CancellationTokenSource token = new();
//
//
//     public WindowsServiceController(string id, int? port, int? restart) : base(id, port, restart) => service = new ServiceController(id);
//
//     public override ServiceControllerStatus Status => service.Status;
//
//     public override void Start() {
//         service.Start();
//         StartRestartTimer();
//     }
//
//     private void StartRestartTimer() {
//         if (!Restart.HasValue)
//             return;
//         Task.Delay(Restart.Value * 60 * 1000, token.Token).ContinueWith(_ => RestartService());
//     }
//
//     private void RestartService() {
//         if (token.IsCancellationRequested)
//             return;
//         if (service.Status == ServiceControllerStatus.Running) {
//             service.Stop();
//             service.WaitForStatus(ServiceControllerStatus.Stopped);
//         }
//
//         if (service.Status == ServiceControllerStatus.Running)
//             return;
//         service.Start();
//         service.WaitForStatus(ServiceControllerStatus.Running);
//         StartRestartTimer();
//     }
//
//     private void StopRestartTimer() => token?.Cancel();
//
//     public override void WaitForStatus(ServiceControllerStatus status)  => service.WaitForStatus(status);
//     public override void ExecuteCommand(int                    command) => service.ExecuteCommand(command);
//
//     public override void Stop() {
//         StopRestartTimer();
//         if (service.Status == ServiceControllerStatus.StartPending)
//             service.WaitForStatus(ServiceControllerStatus.Running);
//         if (service.Status == ServiceControllerStatus.Running)
//             service.Stop();
//     }
//
//     public override void Dispose() => service?.Dispose();
// }