using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using Service.Shared;
using Service.Shared.Data;
using Console = System.Console;

namespace Service; 

public static class Program {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    private static void Main() {
        SBOAssembly.RedirectAssembly();
        RunService();
    }

    private static void RunService() {
        Global.LoadArguments();
        var servicesToRun = new ServiceBase[] { new Service() };
        if (Environment.UserInteractive)
            RunInteractive(servicesToRun);
        else
            ServiceBase.Run(servicesToRun);
    }

    private static void RunInteractive(ServiceBase[] servicesToRun) {
        if (Global.IsMain) {
            Console.WriteLine("Services running in interactive mode.");
            Console.WriteLine();
        }

        string serviceName   = string.Empty;
        var    onStartMethod = typeof(ServiceBase).GetMethod("OnStart", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var service in servicesToRun) {
            serviceName = service.ServiceName;
            if (Global.Background)
                serviceName += " Background ";
            if (Global.Port.HasValue)
                serviceName += $" Node Port {Global.Port}";
            if (!Global.Interactive)
                Console.WriteLine("Starting {0}...", serviceName);
            onStartMethod.Invoke(service, new object[] { new string[] { } });
            if (!Global.Interactive)
                Console.WriteLine("{0} Started", serviceName);
        }

        if (Global.IsMain) {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Press any key to stop the services and end the process...");
            Console.WriteLine("Press \"R\" to process Re-Test...");
            Console.WriteLine("Press \"S\" to process Schedule Planning...");
            Console.WriteLine("Press \"P\" to reload Print Settings...");
            Console.WriteLine("Press \"L\" to reload Settings...");
            Console.WriteLine();
            CheckProcess(servicesToRun, Console.ReadKey());
            Console.WriteLine();
        }
        else {
            Console.ReadKey();
        }

        var onStopMethod = typeof(ServiceBase).GetMethod("OnStop", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var service in servicesToRun) {
            if (!Global.Interactive)
                Console.WriteLine("Stopping {0}...", serviceName);
            onStopMethod.Invoke(service, null);
            if (!Global.Interactive)
                Console.WriteLine("{0} Stopped...", serviceName);
        }

        if (!Global.IsMain)
            return;
        if (!Global.Interactive)
            Console.WriteLine("All services stopped.");
        // Keep the console alive for a second to allow the user to see the message.
        Thread.Sleep(1000);
    }

    private static void CheckProcess(IEnumerable<ServiceBase> servicesToRun, ConsoleKeyInfo readKey) {
        int commandNumber = readKey.Key switch {
            ConsoleKey.L => Const.ReloadSettings,
            ConsoleKey.A => Const.ReloadRestAPISettings,
            ConsoleKey.R => Const.ExecuteBackgroundHelloWorld,
            _            => 0
        };

        if (commandNumber == 0)
            return;
        var onCustomCommandMethod = typeof(ServiceBase).GetMethod("OnCustomCommand", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var service in servicesToRun) {
            Console.WriteLine();
            Console.WriteLine("Custom Command {0}: {1}...", service.ServiceName, readKey.Key);
            onCustomCommandMethod.Invoke(service, new object[] { commandNumber });
            Console.WriteLine("Custom Command {0}: {1} Executed", service.ServiceName, readKey.Key);
        }

        CheckProcess(servicesToRun, Console.ReadKey());
    }
}