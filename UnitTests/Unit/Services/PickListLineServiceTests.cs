using Core.DTOs.PickList;
using Core.Enums;
using Core.Models;
using Infrastructure.Services;

namespace UnitTests.Unit.Services;

[TestFixture]
public class PickListLineServiceTests {
    [TestCase(0)]
    [TestCase(-1)]
    public async Task AddItem_RejectsNonPositiveQuantity(decimal quantity) {
        var service = new PickListLineService(null!, null!, null!, null!, null!, null!);
        var request = new PickListAddItemRequest {
            ID = 1,
            Type = 17,
            Entry = 100,
            ItemCode = "A0001",
            Quantity = quantity,
            Unit = UnitType.Unit
        };

        var result = await service.AddItem(CreateSession(), request);

        Assert.Multiple(() => {
            Assert.That(result.Status, Is.EqualTo(ResponseStatus.Error));
            Assert.That(result.ErrorMessage, Is.EqualTo("Quantity must be greater than zero"));
        });
    }

    private static SessionInfo CreateSession() => new() {
        UserId = Guid.NewGuid().ToString(),
        Name = "Tester",
        Warehouse = "01",
        Roles = [RoleType.Picking],
        Token = string.Empty
    };
}
