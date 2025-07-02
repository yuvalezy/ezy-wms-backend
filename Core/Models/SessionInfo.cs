using System.Text.Json.Serialization;
using Core.Enums;

namespace Core.Models;

public class SessionInfo {
    public required string UserId { get; set; }

    [JsonIgnore]
    public Guid Guid => Guid.Parse(UserId);

    public required string                Name               { get; set; }
    public          bool                  SuperUser          { get; set; }
    public required string                Warehouse          { get; set; }
    public          bool                  EnableBinLocations { get; set; }
    public          int?                  DefaultBinLocation { get; set; }
    public required ICollection<RoleType> Roles              { get; set; }
    public          DateTime              ExpiresAt          { get; set; }
    public          string                Token              { get; set; }
}