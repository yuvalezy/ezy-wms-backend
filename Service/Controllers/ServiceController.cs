using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using Service.Models;
using Service.Shared;
using Service.Shared.Company;
using Service.Views;

namespace Service.Controllers;

public class ServiceController {
    private readonly IService view;

    private HelloWorldController    helloWorldController;
    private API.Service             restAPIService;
    private CustomServiceController backgroundController;

    public ServiceController(IService view) => this.view = view;

    public bool Load() {
        if (string.IsNullOrEmpty(ConnectionController.Database)) {
            view.LogError("Missing database parameter. Run Service.exe /db [dbName]");
            return false;
        }

        try {
            Global.LoadRegistrySettings();
        }
        catch (Exception ex) {
            view.LogError("Error loading registry settings: " + ex.Message);
            return false;
        }

        try {
            Global.LoadDatabaseSettings();
        }
        catch (Exception ex) {
            view.LogError($"Error loading database \"{ConnectionController.Database}\": {ex.Message}");
            return false;
        }

        try {
            Global.ConnectCompany();
            CompanySettings.Load();
        }
        catch (Exception ex) {
            view.LogError($"Error loading database company settings \"{ConnectionController.Database}\": {ex.Message}");
            return false;
        }


        try {
            Global.LoadRestAPISettings();
        }
        catch (Exception ex) {
            view.LogError($"Error loading print settings for \"{ConnectionController.Database}\" {ex.Message}");
            return false;
        }

        if (!Global.CheckVersion()) {
            view.LogError($"Database version does not match with service version \"{ConnectionController.Database}\"");
            return false;
        }

#if DEBUG
        if (Global.Interactive)
            return true;
#endif

        return true;
    }


    public bool StartComponents() {
        try {
            if (StartMasterController())
                return true;
            StartRestAPI();
            StartComponentsExecution();
            return true;
        }
        catch (Exception e) {
            view.LogError("Error Starting Components: " + e.Message);
            return false;
        }
    }

    private bool StartMasterController() {
        try {
            if (backgroundController != null) {
                backgroundController.Dispose();
                foreach (var node in Global.Nodes)
                    node.Dispose();
                Global.Nodes.Clear();
            }

            if (!Global.LoadBalancing || Global.Background || Global.Port.HasValue)
                return false;

            view.LogInfo("Starting background service");
            LoadChildServices();
            StartChildServices();
            StartRestAPI();
            Global.Nodes.StartRestartTimer();
            return true;
        }
        catch (Exception e) {
            string errorMessage = $"Start Master Controller Error: {e.Message}";
            view.LogError(errorMessage);
            throw new Exception(errorMessage);
        }
    }

    private void LoadChildServices() {
        try {
            backgroundController = GetController(0, restart: Global.RestAPISettings.OperationsRestart);
            Global.Nodes         = new();
            int nodesCount = Global.RestAPISettings.Nodes.Count;
            view.LogInfo($"Initializing {nodesCount} nodes controllers");
            for (int i = 0; i < nodesCount; i++) {
                int port = Global.RestAPISettings.Nodes[i].Port;
                Global.Nodes.Add(new ServiceNode(port, GetController(i + 1, port)));
            }
        }
        catch (Exception e) {
            view.LogError($"Load Child Services Error: {e.Message}");
            throw;
        }
    }

    private void StartChildServices() {
        var tasks = new List<Task> {
            Task.Run(() => {
                try {
                    if (backgroundController.Status == ServiceControllerStatus.Running)
                        return;
                    if (backgroundController.Status == ServiceControllerStatus.StopPending)
                        backgroundController.WaitForStatus(ServiceControllerStatus.Stopped);
                    if (backgroundController.Status == ServiceControllerStatus.Stopped)
                        backgroundController.Start();
                    backgroundController.WaitForStatus(ServiceControllerStatus.Running);
                }
                catch (Exception e) {
                    view.LogError($"Start Background (Operations) Service Error: {e.Message}");
                    view.StopService();
                }
            })
        };
        tasks.AddRange(Global.Nodes.Select(node => Task.Run(() => {
            try {
                node.Start();
            }
            catch (Exception e) {
                view.LogError($"Start Node {node.Port} Service Error: {e.Message}");
                view.StopService();
            }
        })));
        Task.WaitAll(tasks.ToArray());
    }

    private CustomServiceController GetController(int id, int? port = null, int? restart = null) {
        string serviceName = $"LW-YUVAL08-SERVER|{ConnectionController.Database}|{id}";
        try {
            return CustomServiceController.GetController(serviceName, port, restart);
        }
        catch (Exception e) {
            view.LogError($"Error loading controller: {e.Message}");
            throw;
        }
    }

    private void StartComponentsExecution() {
        try {
            if (Global.LoadBalancing && !Global.Background)
                return;
            Task.Run(() => {
                StartHelloWorld();
                //add additional service here
            });
        }
        catch (Exception e) {
            throw new Exception("Start components execution error: " + e.Message);
        }
    }


    private void StartHelloWorld() {
        switch (Global.TestHelloWorld) {
            case true when helloWorldController == null:
                helloWorldController = new HelloWorldController();
                helloWorldController.Start();
                break;
            case false when helloWorldController != null:
                helloWorldController.Stop();
                helloWorldController.Dispose();
                helloWorldController = null;
                break;
        }
    }

    private void StartRestAPI() {
        try {
            if (Global.LoadBalancing && Global.Background)
                return;
            switch (Global.RestAPISettings.Enabled) {
                case false when restAPIService != null:
                    restAPIService.Stop();
                    restAPIService.Dispose();
                    restAPIService = null;
                    break;
                case true when restAPIService == null:
                    restAPIService = new API.Service();
                    Task.Run(() => restAPIService.Start());
                    break;
                default:
                    restAPIService?.Stop();
                    restAPIService?.Dispose();
                    restAPIService?.Start();
                    break;
            }
        }
        catch (Exception e) {
            throw new Exception("Start Rest API Error: " + e.Message);
        }
    }

    public void StopAll() {
        Global.Nodes?.StopRestartTimer();
        StopBackgroundServices();
        restAPIService?.Stop();
        var tasks = new List<Task>();
        if (backgroundController is { Status: ServiceControllerStatus.Running }) {
            tasks.Add(Task.Run(() => {
                backgroundController.Stop();
                backgroundController.WaitForStatus(ServiceControllerStatus.Stopped);
            }));
        }

        if (Global.Nodes == null)
            return;
        tasks.AddRange(Global.Nodes
            .Where(node => node.Status == ServiceControllerStatus.Running)
            .Select(node => Task.Run(node.Stop)));

        Task.WaitAll(tasks.ToArray());
    }

    private void StopBackgroundServices() {
        StopBackgroundService(helloWorldController);
        //add additional service heere

        void StopBackgroundService(BaseBackgroundController service) {
            if (service == null)
                return;
            while (service.IsRunning) {
                Console.WriteLine($"Waiting for service {service.GetType().Name} to stop running...");
                Task.Delay(1000).Wait();
            }

            service.Stop();
        }
    }


    public bool ExecuteCommand(int command) {
        switch (command) {
            case Const.ReloadSettings:
                try {
                    Global.LoadDatabaseSettings();
                    StartComponents();
                }
                catch (Exception ex) {
                    view.LogError($"Error loading database \"{ConnectionController.Database}\" settings: {ex.Message}");
                    return false;
                }

                break;
            case Const.ReloadRestAPISettings:
                Global.LoadRestAPISettings();
                StartComponents();
                break;
            case Const.ExecuteBackgroundHelloWorld when !Global.LoadBalancing || Global.Background:
                helloWorldController?.Execute();
                break;
        }

        if (!Global.LoadBalancing || Global.Background || backgroundController == null)
            return true;
        if (backgroundController.Status == ServiceControllerStatus.StartPending)
            backgroundController.WaitForStatus(ServiceControllerStatus.Running);
        if (backgroundController.Status == ServiceControllerStatus.Running)
            backgroundController.ExecuteCommand(command);
        return true;
    }
}