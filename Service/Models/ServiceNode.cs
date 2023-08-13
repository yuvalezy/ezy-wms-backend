using System;
using System.ServiceProcess;
using System.Threading.Tasks;
using Service.Shared;

namespace Service.Models;

public class ServiceNode : Node, IDisposable {
    private readonly CustomServiceController service;
    public ServiceNode(int port, CustomServiceController service) : base(port) => this.service = service;

    public ServiceControllerStatus Status              => service.Status;
    public int                     CurrentTransactions { get; set; }
    public NodeStatus              NodeStatus          { get; private set; } = NodeStatus.Running;

    public void Start() {
        try {
            if (service.Status == ServiceControllerStatus.Running)
                return;
            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running);
        }
        catch (Exception e) {
            //todo handle service not started
        }
    }

    public void Stop() {
        if (service.Status != ServiceControllerStatus.Running)
            return;
        service.Stop();
        service.WaitForStatus(ServiceControllerStatus.Stopped);
    }

    public void Dispose() => service.Dispose();

    public void Pause() => NodeStatus = NodeStatus.Pause;

    public void Restart() {
        NodeStatus = NodeStatus.Restarting;
        while (CurrentTransactions > 0) 
            Task.Delay(1000).Wait();
        Stop();

        if (service.Status != ServiceControllerStatus.Stopped)
            throw new Exception("Cannot restart node: " + Port);
        Start();
        NodeStatus = NodeStatus.Running;
    }
}

public enum NodeStatus {
    Running,
    Pause,
    Restarting
}