using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Service.Shared; 

public class RestAPISettings {
    public   bool              Enabled           { get; set; }
    public   int               Port              { get; set; }
    public   List<Node>        Nodes             { get; set; }
    public   int               NodesRestart      { get; set; } = 4;
    public   int               OperationsRestart { get; set; } = 24;
    public   bool              LoadBalancing     { get; set; }
    public   bool              EnableRedisServer { get; set; }
    public   string            RedisServer       { get; set; } = "127.0.0.1:6379";
    public   string            DefaultPrinter    { get; set; }
    public   List<string>      Printers          { get; } = new();
    public   List<Object>      Objects           { get; } = new();
    public   List<AccessToken> AccessUsers      { get; } = new();
    internal string            FilePath          { get; set; }
    public   AccountInfo       AccountInfo       { get; set; }

    public string GetDefaultPrinter(int type, int id) {
        string printer = DefaultPrinter;
        var    @object = Objects.FirstOrDefault(o => o.ID == type);
        if (@object == null)
            return printer;
        if (!string.IsNullOrWhiteSpace(@object.DefaultPrinter))
            printer = @object.DefaultPrinter;
        var layout = @object.Layouts.FirstOrDefault(l => l.ID == id);
        if (layout != null && !string.IsNullOrWhiteSpace(layout.DefaultPrinter))
            printer = layout.DefaultPrinter;
        return printer;
    }

    public bool ValidateAccess(string username, string password) => AccessUsers.Any(a => a.ID == username && a.Password == password);

    public static RestAPISettings Load(string path) {
        RestAPISettings settings;
        if (File.Exists(path)) {
            string content = File.ReadAllText(path);
            settings          = JsonConvert.DeserializeObject<RestAPISettings>(content);
            settings.FilePath = path;
        }
        else {
            settings = new RestAPISettings {
                FilePath = path
            };
            settings.Save();
        }

        return settings;
    }

    public void Save() {
        string fileName  = FilePath;
        string checkPath = Path.GetDirectoryName(fileName);
        if (!Directory.Exists(checkPath))
            Directory.CreateDirectory(checkPath);
        string content = JsonConvert.SerializeObject(this, new JsonSerializerSettings {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        File.WriteAllText(FilePath, content);
    }
}