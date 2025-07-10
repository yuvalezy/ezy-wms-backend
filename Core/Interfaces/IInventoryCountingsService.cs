using Core.DTOs.InventoryCounting;
using Core.Entities;
using Core.Models;

namespace Core.Interfaces;

public interface IInventoryCountingsService {
    Task<InventoryCountingResponse>                     CreateCounting(CreateInventoryCountingRequest      request, SessionInfo sessionInfo);
    Task<IEnumerable<InventoryCountingResponse>>        GetCountings(InventoryCountingsRequest             request, string      warehouse);
    Task<InventoryCountingResponse>                     GetCounting(Guid                                   id);
    Task<bool>                                          CancelCounting(Guid                                id,          SessionInfo                        sessionInfo);
    Task<ProcessInventoryCountingResponse>              ProcessCounting(Guid                               id,          SessionInfo                        sessionInfo);
    Task<IEnumerable<InventoryCountingContentResponse>> GetCountingContent(InventoryCountingContentRequest request);
    Task<InventoryCountingSummaryResponse>              GetCountingSummaryReport(Guid                      id);
}