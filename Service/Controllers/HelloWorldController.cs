// using System;
// using Microsoft.Extensions.Logging;
// using Console = System.Console;
//
// namespace Service.Controllers;
//
// public class HelloWorldController(ILogger<HelloWorldController> logger) : BaseBackgroundController {
//     public override void Elapsed() {
//         Execute();
//     }
//
//     public void Execute() {
//         try {
//             Console.WriteLine("Hello World Controller has executed...");
//         }
//         catch (Exception ex) {
//             logger.LogError($"Error executing Clear Expired Bin Locks: ${ex.Message}");
//         }
//     }
// }