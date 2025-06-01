using Core.Enums;

namespace Core.Models;

public class SessionInfo {
    public required string                     UserId         { get; set; }
    public          bool                       SuperUser      { get; set; }
    public required ICollection<Authorization> Authorizations { get; set; }
    public required string                     Token          { get; set; }
    public          DateTime                   ExpiresAt      { get; set; }
}