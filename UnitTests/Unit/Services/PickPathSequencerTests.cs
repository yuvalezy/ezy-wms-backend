using Core.Interfaces;
using Infrastructure.Services;

namespace UnitTests.Unit.Services;

[TestFixture]
public class PickPathSequencerTests {
    private IPickPathSequencer _sequencer = null!;

    [SetUp]
    public void SetUp() => _sequencer = new PickPathSequencer();

    [Test]
    public void GetSequence_OrdersNumericallyNotAlphabetically() {
        // The whole point: P10 must sort AFTER P9, unlike a plain string sort.
        int p2 = _sequencer.GetSequence("BIN-P2-A1-N1");
        int p9 = _sequencer.GetSequence("BIN-P9-A1-N1");
        int p10 = _sequencer.GetSequence("BIN-P10-A1-N1");
        int p11 = _sequencer.GetSequence("BIN-P11-A1-N1");

        Assert.That(p2, Is.LessThan(p9));
        Assert.That(p9, Is.LessThan(p10));
        Assert.That(p10, Is.LessThan(p11));
    }

    [Test]
    public void GetSequence_SortsBySegmentSignificance_AisleThenBayThenLevel() {
        // Same aisle: bay is the tie-breaker, then level.
        int p1a1n1 = _sequencer.GetSequence("BIN-P1-A1-N1");
        int p1a1n2 = _sequencer.GetSequence("BIN-P1-A1-N2");
        int p1a2n1 = _sequencer.GetSequence("BIN-P1-A2-N1");
        int p2a1n1 = _sequencer.GetSequence("BIN-P2-A1-N1");

        Assert.Multiple(() => {
            Assert.That(p1a1n1, Is.LessThan(p1a1n2)); // level
            Assert.That(p1a1n2, Is.LessThan(p1a2n1)); // bay outranks level
            Assert.That(p1a2n1, Is.LessThan(p2a1n1)); // aisle outranks bay
        });
    }

    [Test]
    public void GetSequence_MultiDigitBayTieBreak() {
        // A2 must sort before A10 within the same aisle.
        int a2 = _sequencer.GetSequence("BIN-P1-A2-N1");
        int a10 = _sequencer.GetSequence("BIN-P1-A10-N1");

        Assert.That(a2, Is.LessThan(a10));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    [TestCase("STAGING")]   // no numeric segment at all
    [TestCase("BIN-NONE")]  // only a constant prefix, no numbers
    public void GetSequence_UnparseableCodes_SortLastWithSentinel(string? code) {
        Assert.That(_sequencer.GetSequence(code), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void GetSequence_UnparseableCodes_DoNotThrowAndSortAfterRealBins() {
        int real = _sequencer.GetSequence("BIN-P99-A99-N99");
        int junk = _sequencer.GetSequence("not-a-bin");

        Assert.That(real, Is.LessThan(junk));
    }

    [Test]
    public void GetSequence_IsDeterministic() {
        int first = _sequencer.GetSequence("BIN-P3-A4-N5");
        int second = new PickPathSequencer().GetSequence("BIN-P3-A4-N5");
        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void GetSequence_ToleratesMissingTrailingSegments() {
        // Fewer segments should still order sensibly: aisle stays most significant.
        int p1 = _sequencer.GetSequence("BIN-P1");
        int p2 = _sequencer.GetSequence("BIN-P2");
        int p1a1 = _sequencer.GetSequence("BIN-P1-A1");

        Assert.Multiple(() => {
            Assert.That(p1, Is.LessThan(p2));
            Assert.That(p1, Is.LessThanOrEqualTo(p1a1)); // P1 alone <= P1-A1 (level/bay treated as 0)
            Assert.That(p1a1, Is.LessThan(p2));
        });
    }
}
