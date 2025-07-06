using Core.Enums;

namespace Core.Entities;

public class AccountStatusAudit : BaseEntity {
    public AccountState PreviousStatus  { get; set; }
    public AccountState NewStatus       { get; set; }
    public string?      Reason          { get; set; }
}