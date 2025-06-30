using Core.DTOs.PickList;

namespace Core.Mappers.PickList;

public static class ProcessPickListResponseMapper {
    public static ProcessPickListCancelResponse ToCancelResponse(
        this ProcessPickListResponse source,
        Guid?                   transferId = null) {
        return new ProcessPickListCancelResponse {
            DocumentNumber = source.DocumentNumber,
            ErrorMessage   = source.ErrorMessage,
            Message        = source.Message,
            TransferId     = transferId,
            Status         = source.Status,
        };
    }
}