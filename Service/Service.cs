using System;
using System.Diagnostics;
using System.ServiceProcess;
using Service.Views;
using ServiceController = Service.Controllers.ServiceController;

namespace Service; 

public partial class Service : ServiceBase, IService {
    private readonly ServiceController controller;

    public Service() {
        InitializeComponent();
        eventLog.Source = $"Light WMS Service ({Global.Database})";
        controller      = new ServiceController(this);
    }

    protected override void OnStart(string[] args) {
        Global.Service = this;
        if (!controller.Load()) {
            StopService();
            return;
        }

        if (!controller.StartComponents()) {
            StopService();
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) => HandleUnhandledException(e.ExceptionObject as Exception);
    }

    private static void HandleUnhandledException(Exception ex) {
        if (Environment.UserInteractive)
            WriteConsoleMessage("Unhandled exception: " + ex.Message, ConsoleColor.White);
    }

    protected override void OnStop() => controller.StopAll();

    #region Overrides of ServiceBase

    protected override void OnCustomCommand(int command) {
        if (!controller.ExecuteCommand(command)) 
            StopService();

        base.OnCustomCommand(command);
    }

    #endregion

    public void StopService() {
        if (Environment.UserInteractive) {
            Console.WriteLine("Stop Service has been triggered.");
            Console.WriteLine("Press any key to exit!");
            Console.ReadLine();
            Environment.Exit(1);
        }
        else {
            Stop();
        }
    }

    public void LogInfo(string message) {
#if DEBUG
        if (Environment.UserInteractive)
            WriteConsoleMessage(message, ConsoleColor.White);
        else
            eventLog.WriteEntry(message, EventLogEntryType.Information, 1);
#endif
    }

    public void LogError(string message) {
        if (Environment.UserInteractive)
            WriteConsoleMessage(message, ConsoleColor.Red);
        else
            eventLog.WriteEntry(message, EventLogEntryType.Error, 1);
    }

    public void LogWarning(string message) {
        if (Environment.UserInteractive)
            WriteConsoleMessage(message, ConsoleColor.Green);
        else
            eventLog.WriteEntry(message, EventLogEntryType.Warning, 1);
    }

    private static void WriteConsoleMessage(string message, ConsoleColor color) {
        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = prevColor;
    }
}