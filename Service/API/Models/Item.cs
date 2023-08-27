using Newtonsoft.Json;

namespace Service.API.Models;

public class Item {
    public string Code { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Father { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? BoxNumber { get; set; }

    public Item() {
    }

    public Item(string code) => Code = code;
}