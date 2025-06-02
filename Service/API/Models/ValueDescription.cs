namespace Service.API.Models; 

public class ValueDescription<T> {
    public T      Value       { get; set; }
    public string Description { get; set; }
    public ValueDescription(T value, string description) {
        Value       = value;
        Description = description;
    }
}