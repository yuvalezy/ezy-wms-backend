using Core.Utils;

namespace UnitTests.Unit.Utils;

[TestFixture]
public class MetadataFormulaEvaluatorTests {
    private const string VolumeFormula = "{PurchaseUnitLength} * {PurchaseUnitWidth} * {PurchaseUnitHeight}";

    private static Dictionary<string, object?> Dims(object? l, object? w, object? h) => new() {
        ["PurchaseUnitLength"] = l,
        ["PurchaseUnitWidth"] = w,
        ["PurchaseUnitHeight"] = h
    };

    [Test]
    public void Evaluate_Volume_MultipliesDimensions() {
        var result = MetadataFormulaEvaluator.Evaluate(VolumeFormula, Dims(1.25m, 2.5m, 3.75m), 6);

        Assert.That(result, Is.EqualTo(11.71875m));
    }

    [Test]
    public void Evaluate_TinyDimensions_KeepsSixDecimals() {
        // The case that motivated this: SAP loses these once divided per unit; the backend must not.
        var result = MetadataFormulaEvaluator.Evaluate(VolumeFormula, Dims(0.022917m, 0.019167m, 0.015833m), 6);

        Assert.That(result, Is.EqualTo(0.000007m));
    }

    [Test]
    public void Evaluate_MissingDependency_ReturnsNull() {
        var result = MetadataFormulaEvaluator.Evaluate(VolumeFormula, Dims(1m, null, 3m), 6);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Evaluate_UnknownToken_ReturnsNull() {
        var result = MetadataFormulaEvaluator.Evaluate("{Nope} * 2", Dims(1m, 2m, 3m), 2);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Evaluate_ZeroDimension_ReturnsZeroNotNull() {
        var result = MetadataFormulaEvaluator.Evaluate(VolumeFormula, Dims(0m, 2m, 3m), 6);

        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public void Evaluate_Precedence_MultiplicationBeforeAddition() {
        var result = MetadataFormulaEvaluator.Evaluate("{A} + {B} * {C}", new Dictionary<string, object?> { ["A"] = 2m, ["B"] = 3m, ["C"] = 4m }, 0);

        Assert.That(result, Is.EqualTo(14m));
    }

    [Test]
    public void Evaluate_Parentheses_OverridePrecedence() {
        var result = MetadataFormulaEvaluator.Evaluate("({A} + {B}) * {C}", new Dictionary<string, object?> { ["A"] = 2m, ["B"] = 3m, ["C"] = 4m }, 0);

        Assert.That(result, Is.EqualTo(20m));
    }

    [Test]
    public void Evaluate_NegativeValueAndUnaryMinus_Works() {
        var result = MetadataFormulaEvaluator.Evaluate("-{A} * {B}", new Dictionary<string, object?> { ["A"] = -2m, ["B"] = 3m }, 0);

        Assert.That(result, Is.EqualTo(6m));
    }

    [Test]
    public void Evaluate_DivisionByZero_ReturnsNull() {
        var result = MetadataFormulaEvaluator.Evaluate("{A} / {B}", new Dictionary<string, object?> { ["A"] = 2m, ["B"] = 0m }, 2);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Evaluate_Precision_RoundsAwayFromZero() {
        var result = MetadataFormulaEvaluator.Evaluate("{A} / {B}", new Dictionary<string, object?> { ["A"] = 1m, ["B"] = 3m }, 2);

        Assert.That(result, Is.EqualTo(0.33m));
    }

    [Test]
    public void Evaluate_IntegerAndStringValues_AreAccepted() {
        var values = new Dictionary<string, object?> { ["A"] = 2, ["B"] = "3.5" };

        var result = MetadataFormulaEvaluator.Evaluate("{A} * {B}", values, 2);

        Assert.That(result, Is.EqualTo(7m));
    }

    [Test]
    public void Evaluate_CaseInsensitiveFieldLookup() {
        var values = new Dictionary<string, object?> { ["purchaseunitlength"] = 2m };

        var result = MetadataFormulaEvaluator.Evaluate("{PurchaseUnitLength} * 2", values, 0);

        Assert.That(result, Is.EqualTo(4m));
    }

    [Test]
    public void Evaluate_MalformedExpression_ReturnsNull() {
        var result = MetadataFormulaEvaluator.Evaluate("({A} * 2", new Dictionary<string, object?> { ["A"] = 2m }, 0);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Evaluate_EmptyFormula_ReturnsNull() {
        Assert.That(MetadataFormulaEvaluator.Evaluate("", Dims(1m, 2m, 3m), 0), Is.Null);
        Assert.That(MetadataFormulaEvaluator.Evaluate(null, Dims(1m, 2m, 3m), 0), Is.Null);
    }

    [Test]
    public void GetReferencedFields_ReturnsDistinctTrimmedTokens() {
        var refs = MetadataFormulaEvaluator.GetReferencedFields("{ A } * {B} + {A}");

        Assert.That(refs, Is.EquivalentTo(new[] { "A", "B" }));
    }
}
