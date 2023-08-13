namespace Service.Shared.PrintLayout; 

public class LayoutDefinition {
    public int                      ID       { get; set; }
    public string                   Name     { get; set; }
    public LayoutDefinitionSpecific Specific { get; set; }
    public string                   MainKey  { get; set; }
    public LayoutDefinition(int id, string name, bool itemCode = false, bool cardCode = false, bool shipToCode = false, string mainKey = null) {
        ID       = id;
        Name     = name;
        Specific = new LayoutDefinitionSpecific(itemCode, cardCode, shipToCode);
        MainKey  = mainKey;
    }
    public LayoutDefinition(int id, string name, LayoutDefinitionSpecific specific, string mainKey = null) {
        ID       = id;
        Name     = name;
        Specific = specific;
        MainKey  = mainKey;
    }
}
public class LayoutDefinitionSpecific {
    public LayoutDefinitionSpecific() {
    }

    public LayoutDefinitionSpecific(bool itemCode = false, bool cardCode = false, bool shipToCode = false) {
        ItemCode   = itemCode;
        CardCode   = cardCode;
        ShipToCode = shipToCode;
    }

    public bool ItemCode   { get; set; }
    public bool CardCode   { get; set; }
    public bool ShipToCode { get; set; }
}