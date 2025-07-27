using Core.DTOs.General;

namespace Core.DTOs.PickList;

public class PickListAddItemResponse : ResponseBase {
    public bool ClosedDocument { get; set; }
    
    public static PickListAddItemResponse OkResponse => new() { 
        Status = Core.Enums.ResponseStatus.Ok,
        ClosedDocument = false 
    };

    public static PickListAddItemResponse Error(string errorMessage) => new() {
        Status = Enums.ResponseStatus.Error,
        ErrorMessage = errorMessage,
    };
}