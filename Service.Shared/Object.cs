using System.Collections.Generic;

namespace Service.Shared;

public class Object {
    public int                    ID             { get; }
    public string                 DefaultPrinter { get; set; }
    public List<LayoutDefinition> Layouts        { get; set; } = new();

    public Object(int id) => ID = id;
}