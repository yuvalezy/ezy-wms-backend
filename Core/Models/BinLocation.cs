namespace Core.Models;

public class BinLocation {
    public int    Entry { get; set; }
    public string Code  { get; set; }
}


public class BinLocationQuantity : BinLocation {
    public int Quantity { get; set; }
}