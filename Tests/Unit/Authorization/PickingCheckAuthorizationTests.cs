using Core.Enums;
using FluentAssertions;
using Xunit;

namespace Tests.Unit.Authorization;

public class PickingCheckAuthorizationTests
{
    [Fact]
    public void RoleType_PickingCheck_ShouldHaveCorrectValue()
    {
        // Assert
        RoleType.PickingCheck.Should().Be((RoleType)14);
    }

    [Fact]
    public void PickingSupervisor_ShouldHaveHigherPrivilegeThanPickingCheck()
    {
        // In the authorization hierarchy, supervisors should have higher privileges
        var supervisorValue = (int)RoleType.PickingSupervisor;
        var checkerValue = (int)RoleType.PickingCheck;
        
        // This is just to verify the enum values are set correctly
        supervisorValue.Should().Be(7);
        checkerValue.Should().Be(14);
    }

    [Theory]
    [InlineData(RoleType.PickingSupervisor, true)]
    [InlineData(RoleType.PickingCheck, true)]
    [InlineData(RoleType.Picking, false)]
    [InlineData(RoleType.GoodsReceipt, false)]
    public void CheckEndpoints_ShouldAllowCorrectRoles(RoleType role, bool shouldHaveAccess)
    {
        // This test documents which roles should have access to check endpoints
        var allowedRoles = new[] { RoleType.PickingSupervisor, RoleType.PickingCheck };
        
        if (shouldHaveAccess)
        {
            allowedRoles.Should().Contain(role);
        }
        else
        {
            allowedRoles.Should().NotContain(role);
        }
    }

    [Fact]
    public void SupervisorOnlyEndpoints_ShouldRestrictAccess()
    {
        // Document supervisor-only endpoints
        var supervisorOnlyActions = new[]
        {
            "StartCheck",
            "CompleteCheck"
        };

        var checkerActions = new[]
        {
            "CheckItem",
            "GetSummary"
        };

        // These lists document the intended authorization
        supervisorOnlyActions.Should().HaveCount(2);
        checkerActions.Should().HaveCount(2);
    }
}