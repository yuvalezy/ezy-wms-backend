using Service.Shared;

namespace Service.API.Print; 

public class PrintParameters {
    public PrintObjectType Type    { get; }
    public int?            ID      { get; }
    public int             Entry   { get; }
    public string          Printer { get; }

    public PrintParameters(PrintObjectType type, int entry, int? id = null, string printer = null) {
        Type    = type;
        Entry   = entry;
        ID      = id;
        Printer = printer;
    }
}