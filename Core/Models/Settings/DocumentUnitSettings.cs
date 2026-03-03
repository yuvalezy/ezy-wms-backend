using Core.Enums;

namespace Core.Models.Settings;

public record DocumentUnitSettings
{
    public UnitType? DefaultUnitType { get; set; }
    public bool? EnableUnitSelection { get; set; }
    public bool? EnableUseBaseUn { get; set; }
    public UnitType? MaxUnitLevel { get; set; }
}
