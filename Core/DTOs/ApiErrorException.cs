namespace Core.DTOs;

public class ApiErrorException(int errorId, object errorData) : Exception {
    public int    ErrorId   { get; set; } = errorId;
    public object ErrorData { get; set; } = errorData;
}