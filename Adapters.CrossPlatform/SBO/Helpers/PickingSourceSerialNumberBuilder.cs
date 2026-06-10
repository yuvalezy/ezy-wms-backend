using Adapters.CrossPlatform.SBO.Models;
using Core.Entities;

namespace Adapters.CrossPlatform.SBO.Helpers;

public sealed record PickingSourceSerialUpdate(
    int BaseObjectType,
    int OrderEntry,
    int OrderRowId,
    string SerialValue);

public static class PickingSourceSerialNumberBuilder {
    public static IReadOnlyList<PickingSourceSerialUpdate> Build(IEnumerable<PickList> pickRows, IEnumerable<PickListSboLine> pickLines) {
        var orderedPickLines = pickLines.ToArray();
        var pickLineLookup = orderedPickLines.ToDictionary(line => line.LineNumber);
        var sourceOrder = new Dictionary<SourceLineKey, int>();

        foreach (var pickLine in orderedPickLines) {
            var key = SourceLineKey.FromPickLine(pickLine);
            if (!sourceOrder.ContainsKey(key)) {
                sourceOrder[key] = sourceOrder.Count;
            }
        }

        var rowsBySourceLine = new Dictionary<SourceLineKey, List<PickList>>();
        var maxLabelSequence = 0;

        foreach (var row in pickRows) {
            if (row.PickingPackageLabel != null && row.PickingPackageLabel.Sequence > maxLabelSequence) {
                maxLabelSequence = row.PickingPackageLabel.Sequence;
            }

            if (!pickLineLookup.TryGetValue(row.PickEntry, out var pickLine)) {
                continue;
            }

            var key = SourceLineKey.FromPickLine(pickLine);
            if (!rowsBySourceLine.TryGetValue(key, out var sourceRows)) {
                sourceRows = [];
                rowsBySourceLine[key] = sourceRows;
            }

            sourceRows.Add(row);
        }

        var nextFallbackSerial = maxLabelSequence + 1;
        return rowsBySourceLine
        .OrderBy(group => sourceOrder.GetValueOrDefault(group.Key, int.MaxValue))
        .Select(group => {
            var labels = group.Value
            .Select(row => row.PickingPackageLabel)
            .Where(label => label != null)
            .GroupBy(label => label!.Code)
            .Select(labelGroup => labelGroup
                .OrderBy(label => label!.Sequence)
                .ThenBy(label => label!.Code)
                .First()!)
            .OrderBy(label => label.Sequence)
            .ThenBy(label => label.Code)
            .Select(label => label.Code)
            .ToArray();

            var serialValue = labels.Length > 0
                ? string.Join(",", labels)
                : (nextFallbackSerial++).ToString();

            return new PickingSourceSerialUpdate(
                group.Key.BaseObjectType,
                group.Key.OrderEntry,
                group.Key.OrderRowId,
                serialValue);
        })
        .ToArray();
    }

    private sealed record SourceLineKey(int BaseObjectType, int OrderEntry, int OrderRowId) {
        public static SourceLineKey FromPickLine(PickListSboLine pickLine) => new(
            pickLine.BaseObjectType,
            pickLine.OrderEntry,
            pickLine.OrderRowID);
    }
}
