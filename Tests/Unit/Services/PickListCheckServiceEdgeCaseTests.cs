using Core.DTOs.Auth;
using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Unit.Services;

public class PickListCheckServiceEdgeCaseTests
{
    private readonly Mock<IPickingListRepository> _mockPickingRepository;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<ILogger<PickListCheckService>> _mockLogger;
    private readonly PickListCheckService _service;
    private readonly SessionInfo _sessionInfo;

    public PickListCheckServiceEdgeCaseTests()
    {
        _mockPickingRepository = new Mock<IPickingListRepository>();
        _mockCache = new Mock<IMemoryCache>();
        _mockLogger = new Mock<ILogger<PickListCheckService>>();
        
        _service = new PickListCheckService(
            _mockPickingRepository.Object,
            _mockCache.Object,
            _mockLogger.Object
        );

        _sessionInfo = new SessionInfo
        {
            UserId = "user123",
            UserName = "Test User",
            WarehouseId = 1
        };
    }

    [Fact]
    public async Task CheckItem_WithZeroQuantity_ShouldAccept()
    {
        // Arrange - Item was picked but checker finds nothing
        var pickListId = 123;
        var session = CreateActiveSession(pickListId);
        SetupCache(session);

        var request = new PickListCheckItemRequest
        {
            ItemCode = "ITEM001",
            CheckedQuantity = 0, // Zero quantity check
            Unit = UnitType.Unit
        };

        // Act
        var result = await _service.CheckItem(pickListId, request);

        // Assert
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        session.CheckedItems["ITEM001"].CheckedQuantity.Should().Be(0);
    }

    [Fact]
    public async Task CheckItem_WithQuantityExceedingPicked_ShouldAccept()
    {
        // Arrange - Checker finds more than what was picked
        var pickListId = 123;
        var session = CreateActiveSession(pickListId);
        SetupCache(session);

        var request = new PickListCheckItemRequest
        {
            ItemCode = "ITEM001",
            CheckedQuantity = 20, // Picked was 10, but checker found 20
            Unit = UnitType.Unit
        };

        // Act
        var result = await _service.CheckItem(pickListId, request);

        // Assert
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        session.CheckedItems["ITEM001"].CheckedQuantity.Should().Be(20);
    }

    [Fact]
    public async Task CheckItem_MultipleTimesForSameItem_ShouldUpdateLastValue()
    {
        // Arrange
        var pickListId = 123;
        var session = CreateActiveSession(pickListId);
        SetupCache(session);

        var request1 = new PickListCheckItemRequest
        {
            ItemCode = "ITEM001",
            CheckedQuantity = 5,
            Unit = UnitType.Unit
        };

        var request2 = new PickListCheckItemRequest
        {
            ItemCode = "ITEM001",
            CheckedQuantity = 8, // Recount
            Unit = UnitType.Unit
        };

        // Act
        await _service.CheckItem(pickListId, request1);
        var result = await _service.CheckItem(pickListId, request2);

        // Assert
        result!.Success.Should().BeTrue();
        session.CheckedItems["ITEM001"].CheckedQuantity.Should().Be(8);
        result.ItemsChecked.Should().Be(1); // Still counts as 1 item checked
    }

    [Fact]
    public async Task StartCheck_WithNullPickList_ShouldReturnNull()
    {
        // Arrange
        var pickListId = 999;
        _mockPickingRepository.Setup(x => x.GetPicking(It.IsAny<PickingParameters>()))
            .ReturnsAsync((PickingDocument)null);

        object cacheValue = null;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);

        // Act
        var result = await _service.StartCheck(pickListId, _sessionInfo);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSummary_WithNoCheckedItems_ShouldShowAllUnchecked()
    {
        // Arrange
        var pickListId = 123;
        var session = CreateActiveSession(pickListId);
        session.CheckedItems.Clear(); // No items checked yet
        SetupCache(session);

        // Act
        var result = await _service.GetCheckSummary(pickListId);

        // Assert
        result.Should().NotBeNull();
        result!.ItemsChecked.Should().Be(0);
        result.DiscrepancyCount.Should().Be(0);
        result.Items.Should().HaveCount(2); // All items shown
        result.Items.All(x => x.CheckedQuantity == 0).Should().BeTrue();
    }

    [Fact]
    public async Task CompleteCheck_OnAlreadyCompletedSession_ShouldStillReturnTrue()
    {
        // Arrange
        var pickListId = 123;
        var session = CreateActiveSession(pickListId);
        session.IsCompleted = true;
        session.CompletedAt = DateTime.UtcNow.AddMinutes(-10);
        SetupCache(session);

        // Act
        var result = await _service.CompleteCheck(pickListId);

        // Assert
        result.Should().BeTrue(); // Idempotent operation
    }

    [Fact]
    public async Task CheckItem_WithDifferentUnits_ShouldTrackSeparately()
    {
        // Arrange
        var pickListId = 123;
        var session = CreateActiveSession(pickListId);
        SetupCache(session);

        var requestUnit = new PickListCheckItemRequest
        {
            ItemCode = "ITEM001",
            CheckedQuantity = 10,
            Unit = UnitType.Unit
        };

        var requestBox = new PickListCheckItemRequest
        {
            ItemCode = "ITEM001",
            CheckedQuantity = 1,
            Unit = UnitType.Box
        };

        // Act
        await _service.CheckItem(pickListId, requestUnit);
        var result = await _service.CheckItem(pickListId, requestBox);

        // Assert
        // Last unit type wins (business decision)
        session.CheckedItems["ITEM001"].Unit.Should().Be(UnitType.Box);
        session.CheckedItems["ITEM001"].CheckedQuantity.Should().Be(1);
    }

    [Fact]
    public async Task StartCheck_WithEmptyPickList_ShouldStillCreateSession()
    {
        // Arrange
        var pickListId = 123;
        var pickList = new PickingDocument
        {
            Entry = pickListId,
            Status = PickStatus.Picked,
            Detail = new List<PickingDocumentDetail>() // Empty
        };

        _mockPickingRepository.Setup(x => x.GetPicking(It.IsAny<PickingParameters>()))
            .ReturnsAsync(pickList);

        object cacheValue = null;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);
        
        SetupCacheEntry();

        // Act
        var result = await _service.StartCheck(pickListId, _sessionInfo);

        // Assert
        result.Should().NotBeNull();
        result!.PickListId.Should().Be(pickListId);
    }

    [Fact]
    public async Task ConcurrentCheckSessions_ShouldNotInterfere()
    {
        // Arrange
        var pickList1 = 123;
        var pickList2 = 456;
        
        var session1 = CreateActiveSession(pickList1);
        var session2 = CreateActiveSession(pickList2);
        
        // Setup cache to return different sessions for different keys
        _mockCache.Setup(x => x.TryGetValue($"picklist_check_{pickList1}", out It.Ref<object>.IsAny))
            .Returns((object key, out object value) => { value = session1; return true; });
        
        _mockCache.Setup(x => x.TryGetValue($"picklist_check_{pickList2}", out It.Ref<object>.IsAny))
            .Returns((object key, out object value) => { value = session2; return true; });

        SetupCacheEntry();

        var request = new PickListCheckItemRequest
        {
            ItemCode = "ITEM001",
            CheckedQuantity = 5,
            Unit = UnitType.Unit
        };

        // Act
        await _service.CheckItem(pickList1, request);
        await _service.CheckItem(pickList2, request);

        // Assert
        session1.CheckedItems["ITEM001"].CheckedQuantity.Should().Be(5);
        session2.CheckedItems["ITEM001"].CheckedQuantity.Should().Be(5);
    }

    private PickListCheckSession CreateActiveSession(int pickListId)
    {
        return new PickListCheckSession
        {
            PickListId = pickListId,
            StartedByUserName = _sessionInfo.UserName,
            StartedAt = DateTime.UtcNow,
            CheckedItems = new Dictionary<string, CheckedItemInfo>(),
            PickListItems = new Dictionary<string, decimal>
            {
                { "ITEM001", 10m },
                { "ITEM002", 5m }
            },
            ItemDetails = new Dictionary<string, ItemDetails>
            {
                { "ITEM001", new ItemDetails { Code = "ITEM001", Name = "Item 1" } },
                { "ITEM002", new ItemDetails { Code = "ITEM002", Name = "Item 2" } }
            }
        };
    }

    private void SetupCache(PickListCheckSession session)
    {
        object cacheValue = session;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);
        SetupCacheEntry();
    }

    private void SetupCacheEntry()
    {
        var cacheEntry = new Mock<ICacheEntry>();
        _mockCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntry.Object);
    }
}