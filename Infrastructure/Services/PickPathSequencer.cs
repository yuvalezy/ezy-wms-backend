using System.Text.RegularExpressions;
using Core.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Computes a numeric walk-order from a structured bin code such as <c>BIN-P1-A1-N1</c>
/// (Pasillo/aisle, bay, Nivel/level). The numeric tail of each segment is read so that
/// P9 &lt; P10 &lt; P11 (numeric), not P10 &lt; P2 (alphabetical).
///
/// The first three numeric segments are packed into a single composite key with the
/// left-most segment being the most significant (aisle &gt; bay &gt; level). Constant prefixes
/// without a number (e.g. the literal "BIN") are ignored.
/// </summary>
public partial class PickPathSequencer : IPickPathSequencer {
    // Each segment is clamped to 0..999 so the composite key (s0*1_000_000 + s1*1_000 + s2)
    // always stays below int.MaxValue. Aisle/bay/level numbers are far smaller in practice.
    private const int SegmentMax = 999;
    private const int Sentinel = int.MaxValue;

    [GeneratedRegex(@"(\d+)\s*$")]
    private static partial Regex TrailingNumber();

    public int GetSequence(string? binCode) {
        if (string.IsNullOrWhiteSpace(binCode)) {
            return Sentinel;
        }

        var segments = binCode.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        int packed = 0;
        int taken = 0;
        bool found = false;

        foreach (var segment in segments) {
            if (taken >= 3) {
                break;
            }

            var match = TrailingNumber().Match(segment);
            if (!match.Success) {
                // Constant prefix segment (e.g. "BIN"): skip, do not consume a slot.
                continue;
            }

            found = true;
            int value = int.TryParse(match.Groups[1].Value, out var parsed) ? Math.Clamp(parsed, 0, SegmentMax) : 0;
            packed = packed * 1_000 + value;
            taken++;
        }

        if (!found) {
            // No numeric segment at all: not a structured code, sort last but deterministically.
            return Sentinel;
        }

        // Left-align so that a 2-segment code (aisle, bay) outranks a 3-segment code only by value,
        // keeping the most significant segment in the millions place regardless of segment count.
        while (taken < 3) {
            packed *= 1_000;
            taken++;
        }

        return packed;
    }
}
