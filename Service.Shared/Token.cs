using Newtonsoft.Json;

namespace Service.Shared; 

public class Token {
    [JsonProperty("access_token")] public string AccessToken { get; set; }
    [JsonProperty("token_type")]   public string TokenType   { get; set; }
    [JsonProperty("expired_in")]   public int    ExpiresIn   { get; set; }
}