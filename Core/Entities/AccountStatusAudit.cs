using Core.Enums;

namespace Core.Entities;

public class AccountStatusAudit : BaseEntity {
    public Guid         AccountStatusId { get; set; }
    public AccountState PreviousStatus  { get; set; }
    public AccountState NewStatus       { get; set; }
    public string?      Reason          { get; set; }
    
    public virtual AccountStatus AccountStatus { get; set; } = null!;
}