// using System;
// using System.Timers;
// using Service.Shared.Data;
//
// namespace Service.Controllers;
//
// public abstract class BaseBackgroundController : IDisposable {
//     internal readonly DataConnector Data = Global.Connector;
//
//     internal readonly Timer Timer = new() {
//         Interval = 1000 * 60,
//     };
//
//     private void OnTimerOnElapsed(object o, ElapsedEventArgs e) {
//         Timer.Stop();
//         IsRunning = true;
//         try {
//             Elapsed();
//         }
//         finally {
//             IsRunning = false;
//             Timer.Start();
//         }
//     }
//
//     public abstract void Elapsed();
//
//
//     public bool IsRunning { get; private set; }
//
//     public void Start() {
//         Timer.Elapsed += OnTimerOnElapsed;
//         Timer.Start();
//         OnTimerOnElapsed(null, null);
//     }
//
//     public void Stop() {
//         Timer.Elapsed -= OnTimerOnElapsed;
//         Timer.Stop();
//     }
//
//
//     public void Dispose() {
//         Data?.Dispose();
//         Timer.Dispose();
//         GC.SuppressFinalize(this);
//     }
// }