namespace Core.DTOs.Docs;

public sealed record DocumentationArticleSummary(
    string Module,
    string Slug,
    string Title,
    string Description,
    string Section,
    string? Route,
    IReadOnlyCollection<string> Roles,
    bool SuperUserOnly,
    string? Feature,
    int Order);

public sealed record DocumentationArticle(
    DocumentationArticleSummary Summary,
    string Content);

public sealed record DocumentationIndex(
    string Locale,
    IReadOnlyCollection<DocumentationArticleSummary> Articles);

