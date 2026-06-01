namespace Core.Interfaces;

/// <summary>
/// Derives a numeric "walk order" for a bin location purely from its code.
/// Used to guide pickers location-by-location (pick-path routing) instead of item-by-item.
/// Pure and stateless: no database, no external system access.
/// </summary>
public interface IPickPathSequencer {
    /// <summary>
    /// Returns a numeric ordinal for a bin code so that codes sort in physical walk order,
    /// numerically per segment (e.g. P9 &lt; P10 &lt; P11), not alphabetically.
    /// Unparseable or empty codes return a large sentinel so they sort last, deterministically.
    /// </summary>
    int GetSequence(string? binCode);
}
