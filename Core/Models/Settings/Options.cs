namespace Core.Models.Settings;

public class Options {
    bool GRPODraft                           { get; }
    bool GRPOModificationsRequiredSupervisor { get; }
    bool GRPOCreateSupervisorRequired        { get; }
    bool TransferTargetItems                 { get; }
}