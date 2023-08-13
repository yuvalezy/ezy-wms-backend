using System.Data;
using Service.Shared;

namespace Service.Administration.Views;

public interface IAPISettings {
    bool            Loaded                { get; set; }
    string          Text                  { get; set; }
    string          Database              { get; set; }
    bool            Active                { get; set; }
    int             CurrentPort           { get; set; }
    bool            EnableLoadBalancing   { get; set; }
    bool            EnableRedisServer     { get; set; }
    string          RedisServer           { get; set; }
    int             Nodes                 { get; set; }
    int             NodesRestart          { get; set; }
    int             OperationsRestart     { get; set; }
    RestAPISettings Settings              { get; set; }
    DataTable       NodesTable            { get; }
    bool            EnableRedisServerName { set; }
    void            DisplayLoadBalancing();
}