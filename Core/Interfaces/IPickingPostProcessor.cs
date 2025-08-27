using Core.Models;

namespace Core.Interfaces;

public interface IPickingPostProcessor {
    string Id { get; }
    Task ExecuteAsync(PickingPostProcessorContext context, CancellationToken cancellationToken = default);
    bool IsEnabled(Dictionary<string, object>? configuration);
}