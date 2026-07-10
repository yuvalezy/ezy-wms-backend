using Core.DTOs.Docs;
using Core.Models;

namespace Core.Interfaces;

public interface IWmsDocumentationService {
    DocumentationIndex GetIndex(string? locale, SessionInfo session);
    DocumentationArticle? GetArticle(string module, string slug, string? locale, SessionInfo session);
    IReadOnlyCollection<DocumentationArticleSummary> Search(string query, string? locale, int limit, SessionInfo session);
}

