using System;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Forms;
using Service.Shared;
using Service.Shared.Data;
using StackExchange.Redis;

namespace Service.Administration.Helpers;

public class ServerValidation {
    private readonly bool            enableRedis;
    private readonly string          redisServer;
    private readonly bool            loadBalancing;
    private readonly RestAPISettings settings;
    private readonly string          dbName;
    private readonly IWin32Window    owner;
    private readonly string          systemID;


    public ServerValidation(string redisServer, bool loadBalancing, IWin32Window owner) {
        enableRedis        = true;
        this.redisServer   = redisServer;
        this.loadBalancing = loadBalancing;
        this.owner         = owner;
    }

    public ServerValidation(string dbName, DataConnector data, RestAPISettings settings, IWin32Window owner) {
        enableRedis   = settings.EnableRedisServer;
        redisServer   = settings.RedisServer;
        loadBalancing = settings.LoadBalancing;
        this.settings = settings;
        this.dbName   = dbName;
        this.owner    = owner;
    }

    public static ServiceController[] GetServices(string dbName) =>
        ServiceController.GetServices().Where(v => v.ServiceName.StartsWith($"{Const.ServiceName}|{dbName}|")).ToArray();

    public static bool ExistsService(string name) => ServiceController.GetServices().Any(v => v.ServiceName.Equals(name));

    public bool Execute() => ValidateRedis() && ValidateRegisteredServices();

    private bool ValidateRegisteredServices() {
        if (!loadBalancing)
            return true;
        var controllers = GetServices(dbName);
        var values      = controllers.Select(v => int.Parse(v.ServiceName.Split('|').Last())).ToList();
        foreach (var service in controllers)
            service.Dispose();
        bool error = false;
        for (int i = 0; i <= settings.Nodes.Count; i++) {
            if (values.Contains(i))
                continue;
            error = true;
            break;
        }

        if (!error)
            return true;
        var registration = new ServiceRegistration(true, dbName, settings,
            message => MessageBox.Show(owner, message, "Service Controllers Validation", MessageBoxButtons.OK, MessageBoxIcon.Error));
        registration.ReinstallNodes();
        return true;
    }

    public bool ValidateRedis() {
        if (!enableRedis || !loadBalancing)
            return true;
        try {
            using var conn = ConnectionMultiplexer.Connect(redisServer);
            return true;
        }
        catch (Exception e) {
            MessageBox.Show(owner, $"Could not establish connection to the Redis Server {redisServer}: {e.Message}", "Redis In-Memory Server", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
    }
}