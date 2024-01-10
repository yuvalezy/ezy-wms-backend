using System.Collections.Generic;
using Newtonsoft.Json;
using Service.Shared;

namespace Service.API.Models;

public class UserInfo {
    public int    ID   { get; set; }
    public string Name { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IEnumerable<Authorization> Authorizations { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Branch { get; set; }
    public bool BinLocations { get; set; }

    public UserInfo() {
    }

    public UserInfo(int id, string name) {
        ID   = id;
        Name = name;
    }
}