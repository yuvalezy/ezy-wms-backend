using System.Collections.Generic;
using System.Linq;

namespace Service.Shared; 

public class PortsManager : List<PortData> {
    public bool IsAvailable(int port, string database, out string usedBy) {
        usedBy = string.Empty;
        var check = this.FirstOrDefault(p => p.Port == port && p.Database != database);
        if (check == null)
            return true;
        usedBy = check.Database;
        return false;
    }

    public void ClearValues(string database) => RemoveAll(v => v.Database == database);

    public void SetValue(int port, string database) {
        if (!this.Any(v => v.Database == database && v.Port == port))
            Add(new PortData { Port = port, Database = database });
    }

    public int LastUsedPort => this.Select(p => p.Port).DefaultIfEmpty(8999).Max();
}

public class PortData {
    public int    Port     { get; set; }
    public string Database { get; set; }
}