namespace Core.Interfaces;

public interface IPickingPostProcessorFactory {
    IEnumerable<IPickingPostProcessor> GetEnabledProcessors();
    IPickingPostProcessor? GetProcessor(string id);
}