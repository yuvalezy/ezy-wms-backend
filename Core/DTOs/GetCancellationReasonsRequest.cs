using Core.Enums;

namespace Core.DTOs;

public class GetCancellationReasonsRequest {
    public ObjectType? ObjectType      { get; set; }
    public bool        IncludeDisabled { get; set; } = false;
    public string?     SearchTerm      { get; set; }
}