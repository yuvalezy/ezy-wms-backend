namespace Service.API.General.Models;

public class ApplicationSettings {
    public bool GRPOModificationSupervisor   { get; set; }
    public bool GRPOCreateSupervisorRequired { get; set; }
    public bool TransferTargetItems          { get; set; }
}