using Core.Entities;
using Microsoft.Extensions.Logging;

namespace Core.Models;

public class PickingPostProcessorContext {
    public required int AbsEntry { get; init; }
    public required List<PickList> ProcessedData { get; init; }
    public required Dictionary<string, object>? Configuration { get; init; }
    public required ILogger Logger { get; init; }
    public required IServiceProvider ServiceProvider { get; init; }
}