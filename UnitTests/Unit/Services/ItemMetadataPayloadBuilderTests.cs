using System.Text.Json;
using Adapters.CrossPlatform.SBO.Helpers;
using Core.Models.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Unit.Services;

/// <summary>
/// Covers the storage model that keeps L/W/H out of SAP: dimensions live in a JSON UDF, only the
/// calculated volume (÷ pack qty) and a fixed volume unit code are PATCHed to SAP.
/// </summary>
[TestFixture]
public class ItemMetadataPayloadBuilderTests {
    private const string Udf = "U_EZY_WMS_DATA";
    private const string Item = "BOX24";
    private static readonly NullLogger Logger = NullLogger.Instance;

    private static MetadataDefinition Dim(string id) => new() {
        Id = id, Description = id, Type = MetadataFieldType.Decimal, Step = 6,
        StoreIn = Udf, ScaleByField = "PurchaseQtyPerPackUnit", ClearSapField = true,
        MirrorTo = "Sales" + id["Purchase".Length..]
    };

    private static MetaDataDefinitions Definitions() => new() {
        MetadataDefinition = [
            new MetadataDefinition { Id = "ItemCode", Description = "Item Code", Type = MetadataFieldType.String, ReadOnly = true },
            Dim("PurchaseUnitLength"),
            Dim("PurchaseUnitWidth"),
            Dim("PurchaseUnitHeight"),
            new MetadataDefinition {
                Id = "PurchaseUnitVolume", Description = "Unit Volume (m³)", Type = MetadataFieldType.Decimal, Step = 6,
                ReadOnly = true, StoreIn = Udf, SyncToSap = true, ScaleByField = "PurchaseQtyPerPackUnit",
                MirrorTo = "SalesUnitVolume",
                Calculated = new Calculated {
                    Formula = "{PurchaseUnitLength} * {PurchaseUnitWidth} * {PurchaseUnitHeight}",
                    Dependencies = ["PurchaseUnitLength", "PurchaseUnitWidth", "PurchaseUnitHeight"],
                    Precision = 6, ClearDependenciesOnManualEdit = true
                }
            },
            new MetadataDefinition {
                Id = "PurchaseVolumeUnit", Description = "Volume Unit", Type = MetadataFieldType.Integer,
                ReadOnly = true, Hidden = true, FixedValue = "4", MirrorTo = "SalesVolumeUnit"
            },
            new MetadataDefinition {
                Id = "PurchaseUnitWeight", Description = "Peso KG", Type = MetadataFieldType.Decimal, Required = true,
                MirrorTo = "SalesUnitWeight", ScaleByField = "PurchaseQtyPerPackUnit"
            },
            new MetadataDefinition { Id = "PurchaseQtyPerPackUnit", Description = "Pack Qty", Type = MetadataFieldType.Decimal, ReadOnly = true }
        ]
    };

    private static JsonElement Sap(string? blob, decimal packQty = 12m, decimal sapLength = 0.1m) {
        var obj = new Dictionary<string, object?> {
            ["ItemCode"] = Item,
            ["PurchaseUnitLength"] = sapLength,
            ["PurchaseUnitWidth"] = 0.2m,
            ["PurchaseUnitHeight"] = 0.3m,
            ["PurchaseUnitVolume"] = 0.000001m,
            ["PurchaseVolumeUnit"] = 6,
            ["PurchaseUnitWeight"] = 0.5m,
            ["PurchaseQtyPerPackUnit"] = packQty,
            [Udf] = blob
        };
        return JsonSerializer.SerializeToElement(obj);
    }


    private static Dictionary<string, object?> Request(params (string key, object? value)[] fields) =>
        fields.ToDictionary(f => f.key, f => (object?)JsonSerializer.SerializeToElement(f.value));

    private static Dictionary<string, object?> BuildPayload(MetaDataDefinitions defs, JsonElement sap, Dictionary<string, object?> request) {
        var scale = ItemMetadataPayloadBuilder.BuildScaleValueMap(sap, defs);
        var blobs = ItemMetadataPayloadBuilder.ParseBlobs(sap, defs, Logger);
        var current = ItemMetadataPayloadBuilder.ExtractValues(sap, defs, blobs, scale, Logger);
        var writable = ItemMetadataPayloadBuilder.FilterWritableFields(request, defs, Logger);
        return ItemMetadataPayloadBuilder.BuildSapPayload(defs, current, blobs, writable, scale, Item, Logger);
    }

    // ---------- select ----------

    [Test]
    public void BuildSelectFields_IncludesStoreInUdfAndScaleField() {
        var select = ItemMetadataPayloadBuilder.BuildSelectFields(Definitions()).Split(',');

        Assert.That(select, Does.Contain(Udf));
        Assert.That(select, Does.Contain("PurchaseQtyPerPackUnit"));
        Assert.That(select, Does.Contain("PurchaseUnitLength"));
        Assert.That(select.Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(select.Length));
    }

    // ---------- read ----------

    [Test]
    public void ExtractValues_BlobPresent_ReadsUnscaledFromBlob() {
        var defs = Definitions();
        var sap = Sap("""{"PurchaseUnitLength":1.25,"PurchaseUnitWidth":2.5,"PurchaseUnitHeight":3.75,"PurchaseUnitVolume":11.71875}""");
        var scale = ItemMetadataPayloadBuilder.BuildScaleValueMap(sap, defs);
        var blobs = ItemMetadataPayloadBuilder.ParseBlobs(sap, defs, Logger);

        var values = ItemMetadataPayloadBuilder.ExtractValues(sap, defs, blobs, scale, Logger);

        Assert.Multiple(() => {
            Assert.That(values["PurchaseUnitLength"], Is.EqualTo(1.25m));
            Assert.That(values["PurchaseUnitVolume"], Is.EqualTo(11.71875m));
            // Not in blob → SAP property × scale, exactly as before this feature.
            Assert.That(values["PurchaseUnitWeight"], Is.EqualTo(0.5m * 12m));
            // Fixed value wins over whatever SAP holds (6).
            Assert.That(values["PurchaseVolumeUnit"], Is.EqualTo(4));
        });
    }

    [Test]
    public void ExtractValues_NoBlob_FallsBackToSapTimesScale() {
        var defs = Definitions();
        var sap = Sap(null);
        var scale = ItemMetadataPayloadBuilder.BuildScaleValueMap(sap, defs);
        var blobs = ItemMetadataPayloadBuilder.ParseBlobs(sap, defs, Logger);

        var values = ItemMetadataPayloadBuilder.ExtractValues(sap, defs, blobs, scale, Logger);

        Assert.That(values["PurchaseUnitLength"], Is.EqualTo(0.1m * 12m));
        Assert.That(values["PurchaseUnitVolume"], Is.EqualTo(0.000001m * 12m));
    }

    [Test]
    public void ExtractValues_MalformedBlob_TreatedAsEmpty() {
        var defs = Definitions();
        var sap = Sap("not json at all {");
        var scale = ItemMetadataPayloadBuilder.BuildScaleValueMap(sap, defs);
        var blobs = ItemMetadataPayloadBuilder.ParseBlobs(sap, defs, Logger);

        var values = ItemMetadataPayloadBuilder.ExtractValues(sap, defs, blobs, scale, Logger);

        Assert.That(blobs[Udf].Entries, Is.Empty);
        Assert.That(values["PurchaseUnitLength"], Is.EqualTo(0.1m * 12m));
    }

    [Test]
    public void VisibleValues_StripsHiddenDefinitions() {
        var defs = Definitions();
        var all = new Dictionary<string, object?> { ["PurchaseUnitVolume"] = 1m, ["PurchaseVolumeUnit"] = 4 };

        var visible = ItemMetadataPayloadBuilder.VisibleValues(all, defs);

        Assert.That(visible.Keys, Is.EquivalentTo(new[] { "PurchaseUnitVolume" }));
    }

    [Test]
    public void VisibleDefinitions_StripsHidden() {
        Assert.That(Definitions().VisibleDefinitions().Select(d => d.Id), Does.Not.Contain("PurchaseVolumeUnit"));
    }

    // ---------- write ----------

    [Test]
    public void BuildSapPayload_DimensionsGoToBlobOnly_VolumeAndUnitGoToSap() {
        var payload = BuildPayload(Definitions(), Sap(null), Request(
            ("PurchaseUnitLength", 1.25m), ("PurchaseUnitWidth", 2.5m), ("PurchaseUnitHeight", 3.75m),
            ("PurchaseUnitWeight", 6m)));

        Assert.Multiple(() => {
            // The whole point: SAP must never see real dimensions again — they are zeroed (twins too)
            // because SAP re-derives volume from any non-zero dimension and ignores the patched value.
            Assert.That(payload["PurchaseUnitLength"], Is.EqualTo(0m));
            Assert.That(payload["PurchaseUnitWidth"], Is.EqualTo(0m));
            Assert.That(payload["PurchaseUnitHeight"], Is.EqualTo(0m));
            Assert.That(payload["SalesUnitLength"], Is.EqualTo(0m));
            Assert.That(payload["SalesUnitWidth"], Is.EqualTo(0m));
            Assert.That(payload["SalesUnitHeight"], Is.EqualTo(0m));

            // Volume = 11.71875 per pack, ÷ 12 per unit, mirrored to sales.
            Assert.That(payload["PurchaseUnitVolume"], Is.EqualTo(11.71875m / 12m));
            Assert.That(payload["SalesUnitVolume"], Is.EqualTo(11.71875m / 12m));

            // Fixed unit code always present, mirrored.
            Assert.That(payload["PurchaseVolumeUnit"], Is.EqualTo(4));
            Assert.That(payload["SalesVolumeUnit"], Is.EqualTo(4));

            // Ordinary scaled + mirrored field still works.
            Assert.That(payload["PurchaseUnitWeight"], Is.EqualTo(0.5m));
            Assert.That(payload["SalesUnitWeight"], Is.EqualTo(0.5m));
        });

        var blob = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>((string)payload[Udf]!)!;
        Assert.Multiple(() => {
            Assert.That(blob["PurchaseUnitLength"].GetDecimal(), Is.EqualTo(1.25m));
            Assert.That(blob["PurchaseUnitWidth"].GetDecimal(), Is.EqualTo(2.5m));
            Assert.That(blob["PurchaseUnitHeight"].GetDecimal(), Is.EqualTo(3.75m));
            Assert.That(blob["PurchaseUnitVolume"].GetDecimal(), Is.EqualTo(11.71875m));
            Assert.That(blob.Keys, Does.Not.Contain("PurchaseUnitWeight"));
        });
    }

    [Test]
    public void BuildSapPayload_TinyDimensions_VolumeKeepsPrecisionInBlob() {
        var payload = BuildPayload(Definitions(), Sap(null, packQty: 1m), Request(
            ("PurchaseUnitLength", 0.022917m), ("PurchaseUnitWidth", 0.019167m), ("PurchaseUnitHeight", 0.015833m)));

        var blob = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>((string)payload[Udf]!)!;
        Assert.That(blob["PurchaseUnitVolume"].GetDecimal(), Is.EqualTo(0.000007m));
        Assert.That(payload["PurchaseUnitVolume"], Is.EqualTo(0.000007m));
    }

    [Test]
    public void BuildSapPayload_ClientVolumeIsIgnored_BackendRecalculates() {
        // Volume is ReadOnly → dropped from the request; even if it slipped through it must be recomputed.
        var payload = BuildPayload(Definitions(), Sap(null, packQty: 1m), Request(
            ("PurchaseUnitLength", 2m), ("PurchaseUnitWidth", 2m), ("PurchaseUnitHeight", 2m),
            ("PurchaseUnitVolume", 999m)));

        Assert.That(payload["PurchaseUnitVolume"], Is.EqualTo(8m));
    }

    [Test]
    public void BuildSapPayload_PartialDimensionUpdate_MergesWithBlob() {
        var existing = """{"PurchaseUnitLength":1,"PurchaseUnitWidth":2,"PurchaseUnitHeight":3,"PurchaseUnitVolume":6,"SomeOtherKey":"kept"}""";

        var payload = BuildPayload(Definitions(), Sap(existing, packQty: 1m), Request(("PurchaseUnitHeight", 4m)));

        var blob = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>((string)payload[Udf]!)!;
        Assert.Multiple(() => {
            Assert.That(blob["PurchaseUnitLength"].GetDecimal(), Is.EqualTo(1m));
            Assert.That(blob["PurchaseUnitHeight"].GetDecimal(), Is.EqualTo(4m));
            Assert.That(blob["PurchaseUnitVolume"].GetDecimal(), Is.EqualTo(8m));
            Assert.That(blob["SomeOtherKey"].GetString(), Is.EqualTo("kept"));
            Assert.That(payload["PurchaseUnitVolume"], Is.EqualTo(8m));
        });
    }

    [Test]
    public void BuildSapPayload_DependencyCleared_VolumeNotRecomputedBlobKeepsNull() {
        var existing = """{"PurchaseUnitLength":1,"PurchaseUnitWidth":2,"PurchaseUnitHeight":3,"PurchaseUnitVolume":6}""";

        var payload = BuildPayload(Definitions(), Sap(existing, packQty: 1m), Request(("PurchaseUnitWidth", null)));

        var blob = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>((string)payload[Udf]!)!;
        Assert.Multiple(() => {
            Assert.That(blob["PurchaseUnitWidth"].ValueKind, Is.EqualTo(JsonValueKind.Null));
            // Volume untouched: not in payload, blob keeps the previous value.
            Assert.That(payload.Keys, Does.Not.Contain("PurchaseUnitVolume"));
            Assert.That(blob["PurchaseUnitVolume"].GetDecimal(), Is.EqualTo(6m));
        });
    }

    [Test]
    public void BuildSapPayload_NonDimensionOnlyUpdate_DoesNotTouchBlob() {
        var payload = BuildPayload(Definitions(), Sap(null), Request(("PurchaseUnitWeight", 12m)));

        Assert.That(payload.Keys, Does.Not.Contain(Udf));
        Assert.That(payload["PurchaseUnitWeight"], Is.EqualTo(1m));
        // Fixed values are always written, even on unrelated saves.
        Assert.That(payload["PurchaseVolumeUnit"], Is.EqualTo(4));
    }

    [Test]
    public void BuildSapPayload_ScaleFactorMissing_Throws() {
        var defs = Definitions();
        var sap = Sap(null, packQty: 0m);

        Assert.Throws<InvalidOperationException>(() => BuildPayload(defs, sap, Request(
            ("PurchaseUnitLength", 1m), ("PurchaseUnitWidth", 1m), ("PurchaseUnitHeight", 1m))));
    }

    [Test]
    public void BuildSapPayload_PayloadKeysUseConfiguredCasing() {
        var payload = BuildPayload(Definitions(), Sap(null), Request(("purchaseunitweight", 12m)));

        Assert.That(payload.Keys, Does.Contain("PurchaseUnitWeight"));
        Assert.That(payload.Keys, Does.Not.Contain("purchaseunitweight"));
    }

    [Test]
    public void FilterWritableFields_DropsReadOnlyHiddenFixedAndUnknown() {
        var writable = ItemMetadataPayloadBuilder.FilterWritableFields(Request(
            ("ItemCode", "X"), ("PurchaseVolumeUnit", 6), ("PurchaseUnitVolume", 1m), ("Bogus", 1), ("PurchaseUnitWeight", 1m)),
            Definitions(), Logger);

        Assert.That(writable.Keys, Is.EquivalentTo(new[] { "PurchaseUnitWeight" }));
    }

    // ---------- config validation ----------

    [Test]
    public void ValidateMetadataDefinitions_TargetConfig_IsValid() {
        Assert.That(Definitions().ValidateMetadataDefinitions(), Is.Empty);
    }

    [Test]
    public void ValidateMetadataDefinitions_RejectsBadCombinations() {
        var defs = new MetaDataDefinitions {
            MetadataDefinition = [
                new MetadataDefinition { Id = "A", Description = "a", Type = MetadataFieldType.Decimal, SyncToSap = true },
                new MetadataDefinition { Id = "B", Description = "b", Type = MetadataFieldType.Integer, FixedValue = "abc" },
                new MetadataDefinition { Id = "C", Description = "c", Type = MetadataFieldType.Decimal, Hidden = true, Required = true },
                new MetadataDefinition { Id = "D", Description = "d", Type = MetadataFieldType.Decimal, StoreIn = "bad name" },
                new MetadataDefinition { Id = "F", Description = "f", Type = MetadataFieldType.Decimal, ClearSapField = true },
                new MetadataDefinition {
                    Id = "E", Description = "e", Type = MetadataFieldType.Decimal,
                    Calculated = new Calculated { Formula = "{Missing} * {A}", Dependencies = ["Nope"] }
                }
            ]
        };

        var errors = defs.ValidateMetadataDefinitions().ToList();

        Assert.Multiple(() => {
            Assert.That(errors, Has.Some.Contains("SyncToSap"));
            Assert.That(errors, Has.Some.Contains("FixedValue"));
            Assert.That(errors, Has.Some.Contains("Hidden and Required"));
            Assert.That(errors, Has.Some.Contains("StoreIn"));
            Assert.That(errors, Has.Some.Contains("ClearSapField"));
            Assert.That(errors, Has.Some.Contains("unknown field 'Nope'"));
            Assert.That(errors, Has.Some.Contains("unknown field 'Missing'"));
        });
    }
}
