namespace Core.DTOs.Settings;

public class UserResponse {
    public required Guid                Id                     { get; set; }
    public required string              FullName               { get; set; }
    public          string?             Email                  { get; set; }
    public          string?             Position               { get; set; }
    public          bool                SuperUser              { get; set; }
    public          bool                Active                 { get; set; }
    public          Guid?               AuthorizationGroupId   { get; set; }
    public          string?             AuthorizationGroupName { get; set; }
    public          DateTime            CreatedAt              { get; set; }
    public          DateTime?           UpdatedAt              { get; set; }
    public          ICollection<string> Warehouses             { get; set; } = new List<string>();
    public          string?             ExternalId             { get; set; }
}