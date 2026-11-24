using Core.DTOs.ReAllocate;
using Core.Models;

namespace Core.Services;

public interface IReAllocateService {
    Task<ReAllocateSourcesResponse> GetSourcesAsync(string itemCode, SessionInfo sessionInfo);
    Task<ReAllocateResponse> ExecuteAsync(ReAllocateRequest request, SessionInfo sessionInfo);
}
