using Core.DTOs.PickList;

namespace Core.Extensions;

public static class PickListExtensions {
    public static ProcessPickListCancelResponse ToDto(this ProcessPickListResponse source, Guid? transferId = null) {
        return new ProcessPickListCancelResponse {
            DocumentNumber = source.DocumentNumber,
            ErrorMessage   = source.ErrorMessage,
            Message        = source.Message,
            TransferId     = transferId,
            Status         = source.Status,
        };
    }
}