using Adapters.CrossPlatform.SBO.Helpers;
using Adapters.CrossPlatform.SBO.Models;
using Core.Entities;
using Core.Enums;

namespace UnitTests.Unit.Services;

[TestFixture]
public class PickingSourceSerialNumberBuilderTests {
    [Test]
    public void Build_GroupsDistinctLabelsBySourceLine() {
        var updates = PickingSourceSerialNumberBuilder.Build([
            PickRow(0, Label("R1", 1)),
            PickRow(0, Label("R1", 1)),
            PickRow(0, Label("R3", 3))
        ], [
            PickLine(0, 17, 498, 0)
        ]);

        Assert.Multiple(() => {
            Assert.That(updates, Has.Count.EqualTo(1));
            Assert.That(updates[0].BaseObjectType, Is.EqualTo(17));
            Assert.That(updates[0].OrderEntry, Is.EqualTo(498));
            Assert.That(updates[0].OrderRowId, Is.EqualTo(0));
            Assert.That(updates[0].SerialValue, Is.EqualTo("R1,R3"));
        });
    }

    [Test]
    public void Build_AssignsFallbackSerialsAfterHighestLabelSequenceInPickLineOrder() {
        var updates = PickingSourceSerialNumberBuilder.Build([
            PickRow(0, Label("R1", 1)),
            PickRow(1, Label("R3", 3)),
            PickRow(2),
            PickRow(3, Label("R2", 2)),
            PickRow(4),
            PickRow(5, Label("R4", 4)),
            PickRow(6)
        ], [
            PickLine(0, 17, 800, 0),
            PickLine(1, 17, 800, 1),
            PickLine(2, 17, 800, 2),
            PickLine(3, 17, 800, 3),
            PickLine(4, 17, 800, 4),
            PickLine(5, 1250000001, 900, 0),
            PickLine(6, 1250000001, 900, 1)
        ]);

        Assert.Multiple(() => {
            Assert.That(updates.Single(update => update.BaseObjectType == 17 && update.OrderEntry == 800 && update.OrderRowId == 2).SerialValue, Is.EqualTo("5"));
            Assert.That(updates.Single(update => update.BaseObjectType == 17 && update.OrderEntry == 800 && update.OrderRowId == 4).SerialValue, Is.EqualTo("6"));
            Assert.That(updates.Single(update => update.BaseObjectType == 1250000001 && update.OrderEntry == 900 && update.OrderRowId == 1).SerialValue, Is.EqualTo("7"));
        });
    }

    [Test]
    public void Build_UsesLabelsOnlyWhenSourceLineHasMixedLabeledAndUnlabeledRows() {
        var updates = PickingSourceSerialNumberBuilder.Build([
            PickRow(0),
            PickRow(0, Label("R2", 2))
        ], [
            PickLine(0, 17, 498, 1)
        ]);

        Assert.Multiple(() => {
            Assert.That(updates, Has.Count.EqualTo(1));
            Assert.That(updates[0].SerialValue, Is.EqualTo("R2"));
        });
    }

    [Test]
    public void Build_WithEmptyLineSerial_BlanksUnlabeledLinesAndKeepsLabels() {
        var updates = PickingSourceSerialNumberBuilder.Build([
            PickRow(0, Label("R1", 1)),
            PickRow(1),
            PickRow(2, Label("R2", 2))
        ], [
            PickLine(0, 17, 800, 0),
            PickLine(1, 17, 800, 1),
            PickLine(2, 1250000001, 900, 0)
        ], emptyLineSerial: "");

        Assert.Multiple(() => {
            Assert.That(updates.Single(u => u.OrderEntry == 800 && u.OrderRowId == 0).SerialValue, Is.EqualTo("R1"));
            // Unlabeled line is blanked instead of receiving a fallback number.
            Assert.That(updates.Single(u => u.OrderEntry == 800 && u.OrderRowId == 1).SerialValue, Is.EqualTo(""));
            Assert.That(updates.Single(u => u.OrderEntry == 900 && u.OrderRowId == 0).SerialValue, Is.EqualTo("R2"));
        });
    }

    [Test]
    public void Build_WithEmptyLineSerial_BlanksEveryLineWhenNoLabelsExist() {
        var updates = PickingSourceSerialNumberBuilder.Build([
            PickRow(0),
            PickRow(1)
        ], [
            PickLine(0, 17, 498, 0),
            PickLine(1, 17, 498, 1)
        ], emptyLineSerial: "");

        Assert.Multiple(() => {
            Assert.That(updates, Has.Count.EqualTo(2));
            Assert.That(updates, Has.All.Property("SerialValue").EqualTo(""));
        });
    }

    [Test]
    public void Build_WhenNoLabelsExist_StartsFallbackSerialsAtOne() {
        var updates = PickingSourceSerialNumberBuilder.Build([
            PickRow(0),
            PickRow(1)
        ], [
            PickLine(0, 17, 498, 0),
            PickLine(1, 17, 498, 1)
        ]);

        Assert.Multiple(() => {
            Assert.That(updates[0].SerialValue, Is.EqualTo("1"));
            Assert.That(updates[1].SerialValue, Is.EqualTo("2"));
        });
    }

    private static PickList PickRow(int pickEntry, PickingPackageLabel? label = null) => new() {
        AbsEntry = 1,
        ItemCode = "BOX1",
        PickEntry = pickEntry,
        Quantity = 1,
        PickingPackageLabel = label,
        PickingPackageLabelId = label?.Id,
        Unit = UnitType.Unit
    };

    private static PickingPackageLabel Label(string code, int sequence) => new() {
        AbsEntry = 1,
        WhsCode = "BIN",
        Code = code,
        Sequence = sequence
    };

    private static PickListSboLine PickLine(int lineNumber, int baseObjectType, int orderEntry, int orderRowId) => new() {
        LineNumber = lineNumber,
        BaseObjectType = baseObjectType,
        OrderEntry = orderEntry,
        OrderRowID = orderRowId,
        DocumentLinesBinAllocations = []
    };
}
