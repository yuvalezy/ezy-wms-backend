using Core.DTOs;
using Core.DTOs.General;
using Core.Entities;

namespace Core.Extensions;

public static class UserExtensions {
    public static UserAuditResponse? ToDto(this User? user) => user != null ? new UserAuditResponse(user.Id, user.FullName) : null;
}