using System.Data;
using Service.Shared.Data;

namespace Service.Shared.PrintLayout.Models; 

public class PrintLayoutVariable {
    // Required for DI API Data Reader
    // ReSharper disable once UnusedMember.Global
    public PrintLayoutVariable() {
    }
    public PrintLayoutVariable(DataRow dr) {
        Variable = (string)dr["Variable"];
        Value    = (string)dr["Value"];
    }

    [RecordsetReaderColumn] public string Variable { get; set; }
    [RecordsetReaderColumn] public string Value    { get; set; }
}