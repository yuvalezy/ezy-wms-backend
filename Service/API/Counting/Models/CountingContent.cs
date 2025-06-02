namespace Service.API.Counting.Models;

public class CountingContent {
    public string Code     { get; set; }
    public string Name     { get; set; }
    public int    Quantity { get; set; }
    public int    Unit     { get; set; }
    public int    Dozen    { get; set; }
    public int    Pack     { get; set; }
    public int?   BinEntry { get; set; }
}