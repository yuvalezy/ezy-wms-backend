namespace Core.Enums;

/// <summary>
/// Whether a report column's cells render as a hyperlink, and of which kind.
/// Serialized as a string (member name is the wire contract).
/// </summary>
public enum ReportColumnLinkType {
    /// <summary>
    /// Plain text. The default — a column only links when its author says so.
    /// </summary>
    None = 0,

    /// <summary>
    /// A route within this SPA, navigated client-side (react-router), so the app never reloads.
    /// The template must be root-relative (<c>/itemCheck/{ItemCode}</c>).
    /// </summary>
    Internal = 1,

    /// <summary>
    /// An absolute <c>http</c>/<c>https</c> URL, rendered as an <c>&lt;a&gt;</c> opening in a new tab.
    /// </summary>
    External = 2
}
