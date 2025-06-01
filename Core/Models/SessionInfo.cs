using Core.Enums;

namespace Core.Models;

public class SessionInfo {
    public required string                     UserId      { get; set; }
    public required string                     AccountId   { get; set; }
    public          bool                       SuperUser   { get; set; }
    public required ICollection<Authorization> Permissions { get; set; }

}