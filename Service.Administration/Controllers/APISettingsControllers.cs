using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Service.Shared;
using Service.Administration.Helpers;
using Service.Administration.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Service.Shared.Utils;

namespace Service.Administration.Controllers;

public class APISettingsControllers {
    private readonly IAPISettings view;
    private readonly PortsManager ports;
    private readonly IWin32Window owner;

    public APISettingsControllers(IAPISettings view, IWin32Window owner) {
        this.view  = view;
        ports      = LoadPortsManager(view);
        this.owner = owner;
    }

    #region Ports Manager

    private static string PortsManagerFilePath => Path.Combine(Application.StartupPath, "Settings", "ports.json");

    private PortsManager LoadPortsManager(IAPISettings view) {
        try {
            string fileName = PortsManagerFilePath;
            if (File.Exists(fileName)) {
                string content = File.ReadAllText(fileName);
                return JsonConvert.DeserializeObject<PortsManager>(content);
            }

            var value = new PortsManager();
            WritePortsFile(value);
            return value;
        }
        catch (Exception ex) {
            MessageBox.Show(owner, $"Load Ports Manager Error: {ex.Message}", view.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return [];
        }
    }


    private void SavePortsManager() {
        try {
            ports.ClearValues(view.Database);
            ports.SetValue(view.CurrentPort, view.Database);
            if (view.Settings.LoadBalancing)
                view.Settings.Nodes.ForEach(node => ports.SetValue(node.Port, view.Database));
            WritePortsFile(ports);
        }
        catch (Exception ex) {
            ErrorMessage("Save Ports Manager", ex.Message);
        }
    }

    private static void WritePortsFile(PortsManager ports) {
        string fileName  = PortsManagerFilePath;
        string checkPath = Path.GetDirectoryName(fileName);
        if (!Directory.Exists(checkPath))
            Directory.CreateDirectory(checkPath);
        string content = JsonConvert.SerializeObject(ports, new JsonSerializerSettings {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        File.WriteAllText(PortsManagerFilePath, content);
    }

    #endregion

    #region Validation

    public bool ValidateForm() {
        int currentPort = view.CurrentPort;
        if (currentPort != view.Settings.Port && !ValidatePort(currentPort))
            return false;
        return ValidateLoadBalancing(currentPort);
    }

    private bool ValidateLoadBalancing(int currentPort) {
        if (!view.EnableLoadBalancing)
            return true;

        if (!CheckRedisConnection())
            return false;

        var     rows  = view.NodesTable.Rows;
        short[] ports = (from DataRow dr in rows select (short)dr["Port"]).ToArray();
        if (ports.Any(port => view.Settings.Nodes.All(v => v.Port != port) && !ValidatePort(port)))
            return false;

        var check = ports.GroupBy(v => v).FirstOrDefault(v => v.Count() > 1);
        if (check != null) {
            MessageBox.Show(owner, $"Duplicate port {check.Key} found in nodes ports list.", view.Text, MessageBoxButtons.OK, MessageBoxIcon.Stop);
            return false;
        }

        if (ports.Any(v => v == currentPort)) {
            MessageBox.Show(owner, $"Main port {currentPort} found in nodes port list.", view.Text, MessageBoxButtons.OK, MessageBoxIcon.Stop);
            return false;
        }

        if (rows.Count == view.Settings.Nodes.Count || rows.Count <= Environment.ProcessorCount)
            return true;

        string message = $"You have set {rows.Count} nodes and the current machine only has {Environment.ProcessorCount} logical processor units.\nDo you want to continue?";
        return MessageBox.Show(owner, message, view.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }

    private bool CheckRedisConnection() {
        if (!view.EnableRedisServer)
            return true;
        var validation = new ServerValidation(view.RedisServer, true, owner);
        return validation.ValidateRedis();
    }

    private bool ValidatePort(int port) {
        if (!ports.IsAvailable(port, view.Database, out string usedBy)) {
            MessageBox.Show(owner, $"Port {port} is already used by database {usedBy}.", view.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        if (!Remoting.IsPortAvailable(port)) {
            MessageBox.Show(owner, $"Port {port} is currently used by another service.", view.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        return true;
    }

    #endregion

    #region Settings

    public void LoadSettings() {
        try {
            view.Settings = RestAPISettings.Load(APISettings.SettingsFilePath(view.Database));
            LoadUsers();
            view.Active              = view.Settings.Enabled;
            view.CurrentPort         = view.Settings.Port != 0 ? view.Settings.Port : NextPort;
            view.EnableLoadBalancing = view.Settings.LoadBalancing;
            view.EnableRedisServer   = view.Settings.EnableRedisServer;
            view.RedisServer         = view.Settings.RedisServer;
            view.Nodes               = view.Settings.Nodes?.Count ?? 2;
            view.NodesRestart        = view.Settings.NodesRestart;
            view.OperationsRestart   = view.Settings.OperationsRestart;
            DisplayLoadBalancing();
            EnableRedisChanged();
            view.Loaded = true;
        }
        catch (Exception ex) {
            ErrorMessage("Load Settings", ex.Message);
        }
    }

    private void LoadUsers() {
    }

    public void SaveSettings() {
        try {
            //remove empty printer for selection
            view.Settings.Enabled           = view.Active;
            view.Settings.Port              = view.CurrentPort;
            view.Settings.LoadBalancing     = view.EnableLoadBalancing;
            view.Settings.EnableRedisServer = view.EnableRedisServer;
            view.Settings.RedisServer       = view.RedisServer;
            view.Settings.Nodes             = view.EnableLoadBalancing ? view.NodesTable.Rows.Cast<DataRow>().Select(v => new Node((short)v["Port"])).ToList() : null;
            view.Settings.NodesRestart      = view.NodesRestart;
            view.Settings.OperationsRestart = view.OperationsRestart;
            view.Settings.Save();
            SavePortsManager();
        }
        catch (Exception ex) {
            ErrorMessage("Save Ports Manager", ex.Message);
        }
    }

    public void EnableRedisChanged() => view.EnableRedisServerName = view.EnableRedisServer;

    #endregion

    #region Load Balancing

    public void LoadBalancingChanged() {
        if (!view.Loaded)
            return;
        view.Settings.Nodes = null;
        DisplayLoadBalancing();
    }

    private void DisplayLoadBalancing() {
        if (view.EnableLoadBalancing) {
            ShowLoadBalancing();
            return;
        }

        view.DisplayLoadBalancing();
    }

    private void ShowLoadBalancing() {
        view.DisplayLoadBalancing();
        int port = NextPort;
        if (port <= view.CurrentPort)
            port++;
        view.Settings.Nodes ??= [new(port), new(port + 1)];
        SetNodesDataSource();
    }


    public void AddRemoveNodes() {
        if (!view.Loaded)
            return;
        int value = view.Nodes;
        var rows  = view.NodesTable.Rows;
        if (value < rows.Count) {
            while (rows.Count > value)
                rows.RemoveAt(rows.Count - 1);
        }
        else {
            while (rows.Count < value) {
                var dr = view.NodesTable.NewRow();
                dr["ID"]   = rows.Count + 1;
                dr["Port"] = NextPort;
                view.NodesTable.Rows.Add(dr);
            }
        }
    }

    private int NextPort {
        get {
            int value = ports.LastUsedPort + 1;
            if (!view.EnableLoadBalancing)
                return value;
            foreach (DataRow dr in view.NodesTable.Rows) {
                short port = (short)dr["Port"];
                if (port >= value)
                    value = port + 1;
            }

            return value;
        }
    }

    private void SetNodesDataSource() {
        view.NodesTable.Clear();
        var nodes = view.Settings.Nodes;
        for (int i = 0; i < nodes.Count; i++) {
            var dr = view.NodesTable.NewRow();
            dr["ID"]   = i + 1;
            dr["Port"] = nodes[i].Port;
            view.NodesTable.Rows.Add(dr);
        }
    }

    #endregion

    private void ErrorMessage(string id, string message) => MessageBox.Show(owner, $"{id} Error: {message}", view.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
}