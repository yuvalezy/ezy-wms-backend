// using System;
// using System.Diagnostics;
// using System.Linq;
// using System.Net;
// using System.Net.Sockets;
// using Microsoft.Owin.Hosting;
//
// namespace Service.API;
//
// public class Service : IDisposable {
//     private IDisposable service;
//
//     public void Start() {
// #if DEBUG
//         if (!Global.Interactive)
//             Console.WriteLine("Starting Rest API Service");
// #endif
//         try {
//             int port    = Global.Port ?? Global.RestAPISettings.Port;
//             var options = new StartOptions();
//             options.Urls.Add($"http://localhost:{port}");
//             options.Urls.Add($"http://127.0.0.1:{port}");
//             options.Urls.Add($"http://{Environment.MachineName}:{port}");
//             foreach (var ipAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(address => address.AddressFamily == AddressFamily.InterNetwork))
//                 options.Urls.Add($"http://{ipAddress}:{port}");
//             service = WebApp.Start<Startup>(options);
// #if DEBUG
//             Console.WriteLine($"Rest API Service Started in Port {port}");
//             Console.WriteLine($"File Service http://localhost:{port}/");
// #endif
//         }
//         catch (Exception ex) {
//             string errorMessage = ex.Message;
//             if (ex.InnerException != null) {
//                 errorMessage += " Inner exception: " + ex.InnerException.Message;
//             }
//
//             Debug.WriteLine(errorMessage);
//             Console.Error.WriteLine("Start Rest API Service Error: " + errorMessage);
//             // Consider logging the full stack trace as well
//             Console.Error.WriteLine(ex.StackTrace);
//         }
//     }
//
//     public void Stop() => Console.WriteLine("Stop Rest API Service");
//
//     public void Dispose() {
//         service.Dispose();
//         GC.SuppressFinalize(this);
//     }
// }