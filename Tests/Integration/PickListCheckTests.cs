using System.Net;
using System.Net.Http.Json;
using Core.DTOs.Auth;
using Core.DTOs.PickList;
using Core.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tests.Common;
using Xunit;

namespace Tests.Integration;

public class PickListCheckTests : IntegrationTestBase
{
    private const string BaseUrl = "/api/picking";

    [Fact]
    public async Task StartCheck_WithValidPickList_ShouldCreateSession()
    {
        // Arrange
        await AuthenticateAsync(RoleType.PickingSupervisor);
        var pickListId = 123;

        // Act
        var response = await Client.PostAsync($"{BaseUrl}/{pickListId}/check/start", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await response.Content.ReadFromJsonAsync<PickListCheckSession>();
        session.Should().NotBeNull();
        session!.PickListId.Should().Be(pickListId);
        session.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task StartCheck_WithoutSupervisorRole_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticateAsync(RoleType.PickingCheck); // Not supervisor
        var pickListId = 123;

        // Act
        var response = await Client.PostAsync($"{BaseUrl}/{pickListId}/check/start", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CheckItem_WithValidItem_ShouldUpdateSession()
    {
        // Arrange
        await AuthenticateAsync(RoleType.PickingCheck);
        var pickListId = 123;
        
        // First start a check as supervisor
        await AuthenticateAsync(RoleType.PickingSupervisor);
        await Client.PostAsync($"{BaseUrl}/{pickListId}/check/start", null);
        
        // Switch to checker role
        await AuthenticateAsync(RoleType.PickingCheck);
        
        var request = new PickListCheckItemRequest
        {
            ItemCode = "ITEM001",
            CheckedQuantity = 10,
            Unit = UnitType.Unit
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseUrl}/{pickListId}/check/item", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PickListCheckItemResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.ItemsChecked.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetSummary_WithActiveSession_ShouldReturnSummary()
    {
        // Arrange
        await AuthenticateAsync(RoleType.PickingSupervisor);
        var pickListId = 123;
        await Client.PostAsync($"{BaseUrl}/{pickListId}/check/start", null);

        // Act
        var response = await Client.GetAsync($"{BaseUrl}/{pickListId}/check/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<PickListCheckSummaryResponse>();
        summary.Should().NotBeNull();
        summary!.PickListId.Should().Be(pickListId);
    }

    [Fact]
    public async Task CompleteCheck_WithActiveSession_ShouldCompleteSuccessfully()
    {
        // Arrange
        await AuthenticateAsync(RoleType.PickingSupervisor);
        var pickListId = 123;
        await Client.PostAsync($"{BaseUrl}/{pickListId}/check/start", null);

        // Act
        var response = await Client.PostAsync($"{BaseUrl}/{pickListId}/check/complete", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CheckItem_WithNoActiveSession_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync(RoleType.PickingCheck);
        var pickListId = 999; // No session for this ID
        
        var request = new PickListCheckItemRequest
        {
            ItemCode = "ITEM001",
            CheckedQuantity = 10,
            Unit = UnitType.Unit
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseUrl}/{pickListId}/check/item", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StartCheck_WithExistingActiveSession_ShouldReturnExistingSession()
    {
        // Arrange
        await AuthenticateAsync(RoleType.PickingSupervisor);
        var pickListId = 123;
        
        // Start first session
        var firstResponse = await Client.PostAsync($"{BaseUrl}/{pickListId}/check/start", null);
        var firstSession = await firstResponse.Content.ReadFromJsonAsync<PickListCheckSession>();

        // Act - Try to start another session
        var secondResponse = await Client.PostAsync($"{BaseUrl}/{pickListId}/check/start", null);
        var secondSession = await secondResponse.Content.ReadFromJsonAsync<PickListCheckSession>();

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondSession!.StartedAt.Should().Be(firstSession!.StartedAt);
    }

    [Fact]
    public async Task GetPickings_WithIncludeForCheck_ShouldReturnAllPickLists()
    {
        // Arrange
        await AuthenticateAsync(RoleType.PickingSupervisor);

        // Act
        var response = await Client.GetAsync($"{BaseUrl}?includeForCheck=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pickings = await response.Content.ReadFromJsonAsync<List<object>>();
        pickings.Should().NotBeNull();
        // Should include both partial and complete pick lists
    }

    [Fact]
    public async Task CheckWorkflow_FullScenario_ShouldWork()
    {
        // Arrange
        var pickListId = 123;
        
        // 1. Supervisor starts check
        await AuthenticateAsync(RoleType.PickingSupervisor);
        var startResponse = await Client.PostAsync($"{BaseUrl}/{pickListId}/check/start", null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Checker performs checks
        await AuthenticateAsync(RoleType.PickingCheck);
        
        var checkItems = new[]
        {
            new PickListCheckItemRequest { ItemCode = "ITEM001", CheckedQuantity = 10, Unit = UnitType.Unit },
            new PickListCheckItemRequest { ItemCode = "ITEM002", CheckedQuantity = 5, Unit = UnitType.Unit },
            new PickListCheckItemRequest { ItemCode = "ITEM003", CheckedQuantity = 8, Unit = UnitType.Unit }
        };

        foreach (var item in checkItems)
        {
            var checkResponse = await Client.PostAsJsonAsync($"{BaseUrl}/{pickListId}/check/item", item);
            checkResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // 3. Get summary to verify
        var summaryResponse = await Client.GetAsync($"{BaseUrl}/{pickListId}/check/summary");
        summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await summaryResponse.Content.ReadFromJsonAsync<PickListCheckSummaryResponse>();
        summary!.ItemsChecked.Should().BeGreaterThan(0);

        // 4. Supervisor completes check
        await AuthenticateAsync(RoleType.PickingSupervisor);
        var completeResponse = await Client.PostAsync($"{BaseUrl}/{pickListId}/check/complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}