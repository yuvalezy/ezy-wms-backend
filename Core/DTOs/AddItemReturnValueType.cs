using Core.Enums;

namespace Core.DTOs;

public abstract class AddItemRequestBase {
    public Guid      ID       { get; set; }
    public string    ItemCode { get; set; }
    public string    BarCode  { get; set; }
    public int?      BinEntry { get; set; }
    public UnitType? Unit     { get; set; }
}

// Custom exception for API errors
public class ApiErrorException(int errorId, object errorData) : Exception {
    public int    ErrorId   { get; set; } = errorId;
    public object ErrorData { get; set; } = errorData;
}
