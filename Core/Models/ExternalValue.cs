namespace Core.Models;

public class ExternalValue<T> {
    public required T Id       { get; set; }
    public required string Name { get; set; }
}