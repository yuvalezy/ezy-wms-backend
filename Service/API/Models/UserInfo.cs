using System.Collections.Generic;
using Service.Shared;

namespace Service.API.Models;

public class UserInfo {
    public int               ID    { get; set; }
    public string            Name  { get; set; }
    public IEnumerable<Role> Roles { get; set; }
}