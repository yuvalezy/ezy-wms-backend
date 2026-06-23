using Adapters.CrossPlatform.SBO.Models;
using Core.Entities;

namespace Adapters.CrossPlatform.SBO.Helpers;

public sealed record PickingSourceSerialUpdate(
    int BaseObjectType,
    int OrderEntry,
    int OrderRowId,
    string SerialValue);

public static class PickingSourceSerialNumberBuilder {
    /// <param name="emptyLineSerial">
    /// Value written to source lines that have picked rows but no package label. When null (default),
    /// an incrementing fallback number is used (after the highest label sequence) — the behaviour used
    /// by the normal pick sync. Pass "" to blank such lines instead, used when resetting/re-pushing
    /// serials during a repack restart.
    /// </param>
    public static IReadOnlyList<PickingSourceSerialUpdate> Build(IEnumerable<PickList> pickRows, IEnumerable<PickListSboLine> pickLines, string? emptyLineSerial = null) {
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
                : emptyLineSerial ?? (nextFallbackSerial++).ToString();

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
