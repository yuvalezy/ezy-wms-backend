using Core.DTOs.Auth;
using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Unit.Services;

public class PickListCheckServiceTests
{
    private readonly Mock<IPickingListRepository> _mockPickingRepository;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<ILogger<PickListCheckService>> _mockLogger;
    private readonly PickListCheckService _service;
    private readonly SessionInfo _sessionInfo;

    public PickListCheckServiceTests()
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
    public async Task StartCheck_WithValidPickList_ShouldCreateSession()
    {
        // Arrange
        var pickListId = 123;
        var pickList = new PickingDocument
        {
            Entry = pickListId,
            Status = PickStatus.Picked,
            Detail = new List<PickingDocumentDetail>
            {
                new PickingDocumentDetail
                {
                    Items = new List<PickingDocumentDetailItem>
                    {
                        new PickingDocumentDetailItem { ItemCode = "ITEM001", Picked = 10 },
                        new PickingDocumentDetailItem { ItemCode = "ITEM002", Picked = 5 }
                    }
                }
            }
        };

        _mockPickingRepository.Setup(x => x.GetPicking(It.IsAny<PickingParameters>()))
            .ReturnsAsync(pickList);

        object cacheValue = null;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);
        
        var cacheEntry = new Mock<ICacheEntry>();
        _mockCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntry.Object);

        // Act
        var result = await _service.StartCheck(pickListId, _sessionInfo);

        // Assert
        result.Should().NotBeNull();
        result!.PickListId.Should().Be(pickListId);
        result.StartedByUserName.Should().Be(_sessionInfo.UserName);
        result.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task StartCheck_WithExistingSession_ShouldReturnExisting()
    {
        // Arrange
        var pickListId = 123;
        var existingSession = new PickListCheckSession
        {
            PickListId = pickListId,
            StartedByUserName = "Other User",
            StartedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        object cacheValue = existingSession;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);

        // Act
        var result = await _service.StartCheck(pickListId, _sessionInfo);

        // Assert
        result.Should().NotBeNull();
        result!.StartedByUserName.Should().Be("Other User");
        _mockPickingRepository.Verify(x => x.GetPicking(It.IsAny<PickingParameters>()), Times.Never);
    }

    [Fact]
    public async Task CheckItem_WithValidItem_ShouldUpdateSession()
    {
        // Arrange
        var pickListId = 123;
        var itemCode = "ITEM001";
        var checkedQuantity = 10m;
        
        var session = new PickListCheckSession
        {
            PickListId = pickListId,
            CheckedItems = new Dictionary<string, CheckedItemInfo>(),
            PickListItems = new Dictionary<string, decimal>
            {
                { itemCode, 10m }
            }
        };

        object cacheValue = session;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);

        var cacheEntry = new Mock<ICacheEntry>();
        _mockCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntry.Object);

        var request = new PickListCheckItemRequest
        {
            ItemCode = itemCode,
            CheckedQuantity = checkedQuantity,
            Unit = UnitType.Unit
        };

        // Act
        var result = await _service.CheckItem(pickListId, request);

        // Assert
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.ItemsChecked.Should().Be(1);
        session.CheckedItems.Should().ContainKey(itemCode);
    }

    [Fact]
    public async Task CheckItem_WithNoSession_ShouldReturnError()
    {
        // Arrange
        var pickListId = 999;
        object cacheValue = null;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);

        var request = new PickListCheckItemRequest
        {
            ItemCode = "ITEM001",
            CheckedQuantity = 10,
            Unit = UnitType.Unit
        };

        // Act
        var result = await _service.CheckItem(pickListId, request);

        // Assert
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No active check session");
    }

    [Fact]
    public async Task GetSummary_WithDiscrepancies_ShouldCalculateCorrectly()
    {
        // Arrange
        var pickListId = 123;
        var session = new PickListCheckSession
        {
            PickListId = pickListId,
            CheckedItems = new Dictionary<string, CheckedItemInfo>
            {
                { "ITEM001", new CheckedItemInfo { ItemCode = "ITEM001", CheckedQuantity = 8, Unit = UnitType.Unit } },
                { "ITEM002", new CheckedItemInfo { ItemCode = "ITEM002", CheckedQuantity = 5, Unit = UnitType.Unit } }
            },
            PickListItems = new Dictionary<string, decimal>
            {
                { "ITEM001", 10m }, // Discrepancy: checked 8, picked 10
                { "ITEM002", 5m },  // Match
                { "ITEM003", 3m }   // Not checked yet
            },
            ItemDetails = new Dictionary<string, ItemDetails>
            {
                { "ITEM001", new ItemDetails { Code = "ITEM001", Name = "Item 1" } },
                { "ITEM002", new ItemDetails { Code = "ITEM002", Name = "Item 2" } },
                { "ITEM003", new ItemDetails { Code = "ITEM003", Name = "Item 3" } }
            }
        };

        object cacheValue = session;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);

        // Act
        var result = await _service.GetCheckSummary(pickListId);

        // Assert
        result.Should().NotBeNull();
        result!.TotalItems.Should().Be(3);
        result.ItemsChecked.Should().Be(2);
        result.DiscrepancyCount.Should().Be(1); // Only ITEM001 has discrepancy
        
        var item1 = result.Items.First(x => x.ItemCode == "ITEM001");
        item1.PickedQuantity.Should().Be(10);
        item1.CheckedQuantity.Should().Be(8);
        item1.Difference.Should().Be(-2);
    }

    [Fact]
    public async Task CompleteCheck_WithActiveSession_ShouldMarkCompleted()
    {
        // Arrange
        var pickListId = 123;
        var session = new PickListCheckSession
        {
            PickListId = pickListId,
            IsCompleted = false
        };

        object cacheValue = session;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);

        var cacheEntry = new Mock<ICacheEntry>();
        _mockCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntry.Object);

        // Act
        var result = await _service.CompleteCheck(pickListId);

        // Assert
        result.Should().BeTrue();
        session.IsCompleted.Should().BeTrue();
        session.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckItem_WithItemNotInPickList_ShouldReturnError()
    {
        // Arrange
        var pickListId = 123;
        var session = new PickListCheckSession
        {
            PickListId = pickListId,
            CheckedItems = new Dictionary<string, CheckedItemInfo>(),
            PickListItems = new Dictionary<string, decimal>
            {
                { "ITEM001", 10m }
            }
        };

        object cacheValue = session;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);

        var request = new PickListCheckItemRequest
        {
            ItemCode = "ITEM999", // Not in pick list
            CheckedQuantity = 5,
            Unit = UnitType.Unit
        };

        // Act
        var result = await _service.CheckItem(pickListId, request);

        // Assert
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found in pick list");
    }
}