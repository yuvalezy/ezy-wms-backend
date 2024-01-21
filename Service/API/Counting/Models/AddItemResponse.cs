namespace Service.API.Counting.Models;

public class AddItemResponse {
    public int? LineID { get; set; }
    public bool   ClosedCounting { get; set; }
    public string ErrorMessage   { get; set; }
}