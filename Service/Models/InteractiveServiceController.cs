using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;
using Service.Shared.Company;

namespace Service.Models;

public class InteractiveServiceController : CustomServiceController {
    private readonly Process proc;
    private readonly string  type;

    private ServiceControllerStatus status = ServiceControllerStatus.Stopped;

    public InteractiveServiceController(string id, int? port, int? restart) : base(id, port, restart) {
        string args = $"db: {ConnectionController.Database} interactive ";
        type =  !port.HasValue ? "background" : $"port: {port}";
        args += type;
        var info = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location) {
            Arguments              = args,
            WindowStyle            = ProcessWindowStyle.Hidden,
            UseShellExecute        = false,
            RedirectStandardOutput = true
        };

        proc           = new Process();
        proc.StartInfo = info;
    }

    public override ServiceControllerStatus Status => status;

    public override void Start() {
        proc.Start();
        proc.BeginOutputReadLine();
        proc.OutputDataReceived += (_, args) => {
            if (Port.HasValue)
                Console.WriteLine("{0} Output: {1}", type, args.Data);
        };
        status = ServiceControllerStatus.Running;
        Console.WriteLine("Child process {0} started, process ID: {1}", type, proc.Id);
    }

    public override void WaitForStatus(ServiceControllerStatus status) {
        //ignore
    }

    public override void ExecuteCommand(int command) {
        //ignore
    }

    public override void Stop() {
        proc.Kill();
        proc.WaitForExit();
        status = ServiceControllerStatus.Stopped;
    }

    public override void Dispose() => proc.Dispose();
}