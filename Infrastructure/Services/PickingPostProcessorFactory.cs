using System.Reflection;
using Core.Interfaces;
using Core.Models.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class PickingPostProcessorFactory : IPickingPostProcessorFactory {
    private readonly ISettings settings;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<PickingPostProcessorFactory> logger;
    private readonly Lazy<List<IPickingPostProcessor>> processors;

    public PickingPostProcessorFactory(
        ISettings settings,
        IServiceProvider serviceProvider,
        ILogger<PickingPostProcessorFactory> logger) {
        this.settings = settings;
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.processors = new Lazy<List<IPickingPostProcessor>>(LoadProcessors);
    }

    public IEnumerable<IPickingPostProcessor> GetEnabledProcessors() {
        return processors.Value.Where(p => p.IsEnabled(GetProcessorConfiguration(p.Id)));
    }

    public IPickingPostProcessor? GetProcessor(string id) {
        return processors.Value.FirstOrDefault(p => p.Id == id);
    }

    private Dictionary<string, object>? GetProcessorConfiguration(string processorId) {
        var processorSettings = settings.PickingPostProcessing.Processors
            .FirstOrDefault(p => p.Id == processorId);
        return processorSettings?.Configuration;
    }

    private List<IPickingPostProcessor> LoadProcessors() {
        var loadedProcessors = new List<IPickingPostProcessor>();

        foreach (var processorConfig in settings.PickingPostProcessing.Processors) {
            try {
                if (!processorConfig.Enabled) {
                    logger.LogDebug("Skipping disabled post-processor: {ProcessorId}", processorConfig.Id);
                    continue;
                }

                var processor = LoadProcessor(processorConfig);
                if (processor != null) {
                    loadedProcessors.Add(processor);
                    logger.LogInformation("Loaded post-processor: {ProcessorId} from {Assembly}", 
                        processorConfig.Id, processorConfig.Assembly);
                }
            }
            catch (Exception ex) {
                logger.LogError(ex, "Failed to load post-processor: {ProcessorId} from {Assembly}", 
                    processorConfig.Id, processorConfig.Assembly);
            }
        }

        return loadedProcessors;
    }

    private IPickingPostProcessor? LoadProcessor(PickingPostProcessorSettings config) {
        try {
            // Load the assembly
            var assemblyPath = Path.IsPathRooted(config.Assembly) 
                ? config.Assembly 
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.Assembly);

            if (!File.Exists(assemblyPath)) {
                logger.LogError("Assembly not found: {AssemblyPath}", assemblyPath);
                return null;
            }

            var assembly = Assembly.LoadFrom(assemblyPath);
            var processorType = assembly.GetType(config.TypeName);

            if (processorType == null) {
                logger.LogError("Type {TypeName} not found in assembly {Assembly}", config.TypeName, config.Assembly);
                return null;
            }

            if (!typeof(IPickingPostProcessor).IsAssignableFrom(processorType)) {
                logger.LogError("Type {TypeName} does not implement IPickingPostProcessor", config.TypeName);
                return null;
            }

            // Try to create instance using DI first, fallback to Activator
            var instance = ActivatorUtilities.CreateInstance(serviceProvider, processorType) as IPickingPostProcessor;
            instance ??= Activator.CreateInstance(processorType) as IPickingPostProcessor;

            if (instance == null) {
                logger.LogError("Failed to create instance of {TypeName}", config.TypeName);
                return null;
            }

            return instance;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error loading processor from {Assembly}, type {TypeName}", config.Assembly, config.TypeName);
            return null;
        }
    }
}