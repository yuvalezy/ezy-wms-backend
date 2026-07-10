using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Core.DTOs.Docs;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public sealed class WmsDocumentationService(ISettings settings, ILogger<WmsDocumentationService> logger) : IWmsDocumentationService {
    private static readonly string[] SupportedLocales = ["en", "es"];
    private readonly Lazy<IReadOnlyCollection<ManifestArticle>> manifest = new(LoadManifest);
    private readonly Dictionary<string, string> contentCache = new(StringComparer.OrdinalIgnoreCase);

    public DocumentationIndex GetIndex(string? locale, SessionInfo session) {
        string normalizedLocale = NormalizeLocale(locale);
        var articles = manifest.Value
            .Where(article => article.Locale == normalizedLocale && CanView(article, session))
            .OrderBy(article => article.Order)
            .Select(ToSummary)
            .ToArray();
        return new DocumentationIndex(normalizedLocale, articles);
    }

    public DocumentationArticle? GetArticle(string module, string slug, string? locale, SessionInfo session) {
        string normalizedLocale = NormalizeLocale(locale);
        var article = manifest.Value.FirstOrDefault(value =>
            value.Locale == normalizedLocale &&
            value.Module.Equals(module, StringComparison.OrdinalIgnoreCase) &&
            value.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

        if (article is null || !CanView(article, session)) {
            return null;
        }

        string path = ResolveContentPath(article.File);
        if (!File.Exists(path)) {
            logger.LogError("Documentation article {File} is missing from the published corpus", article.File);
            return null;
        }

        if (!contentCache.TryGetValue(path, out string? content)) {
            content = File.ReadAllText(path);
            contentCache[path] = content;
        }

        return new DocumentationArticle(ToSummary(article), content);
    }

    public IReadOnlyCollection<DocumentationArticleSummary> Search(string query, string? locale, int limit, SessionInfo session) {
        if (string.IsNullOrWhiteSpace(query)) {
            return [];
        }

        string normalizedQuery = query.Trim();
        int safeLimit = Math.Clamp(limit, 1, 50);
        return manifest.Value
            .Where(article => article.Locale == NormalizeLocale(locale) && CanView(article, session))
            .Select(article => new { Article = article, Score = Score(article, normalizedQuery) })
            .Where(value => value.Score > 0)
            .OrderByDescending(value => value.Score)
            .ThenBy(value => value.Article.Order)
            .Take(safeLimit)
            .Select(value => ToSummary(value.Article))
            .ToArray();
    }

    private bool CanView(ManifestArticle article, SessionInfo session) {
        if (article.SuperUserOnly && !session.SuperUser) {
            return false;
        }

        if (article.Roles.Count > 0 && !session.SuperUser) {
            bool hasRole = article.Roles.Any(role => Enum.TryParse<RoleType>(role, true, out var parsed) && session.Roles.Contains(parsed));
            if (!hasRole) {
                return false;
            }
        }

        return article.Feature switch {
            "BinLocation" => session.EnableBinLocations,
            "GoodsReceiptTransactional" => settings.Options.GoodsReceiptType is GoodsReceiptDocumentType.Transactional or GoodsReceiptDocumentType.Both,
            "GoodsReceiptConfirmation" => settings.Options.GoodsReceiptType is GoodsReceiptDocumentType.Confirmation or GoodsReceiptDocumentType.Both,
            "TransferRequest" => settings.Options.EnableTransferRequest,
            "EnableInventoryTransfer" => settings.Options.EnableWarehouseTransfer,
            "EnableTransferConfirm" => settings.Options.EnableTransferConfirm,
            _ => true
        };
    }

    private static int Score(ManifestArticle article, string query) {
        string haystack = $"{article.Title} {article.Description} {article.Module} {article.Slug}";
        int score = 0;
        foreach (string token in Regex.Split(query.ToLowerInvariant(), "\\s+")) {
            if (token.Length == 0) continue;
            string normalized = haystack.ToLowerInvariant();
            if (normalized.Contains(token, StringComparison.Ordinal)) score += 1;
            if (article.Title.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 4;
            if (article.Slug.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 3;
        }
        return score;
    }

    private static string NormalizeLocale(string? locale) =>
        SupportedLocales.Contains(locale?.ToLowerInvariant(), StringComparer.Ordinal) ? locale!.ToLowerInvariant() : "en";

    private static DocumentationArticleSummary ToSummary(ManifestArticle article) => new(
        article.Module, article.Slug, article.Title, article.Description, article.Section,
        article.Route, article.Roles, article.SuperUserOnly, article.Feature, article.Order);

    private static IReadOnlyCollection<ManifestArticle> LoadManifest() {
        string path = Path.Combine(AppContext.BaseDirectory, "Docs", "docs-manifest.json");
        if (!File.Exists(path)) {
            throw new FileNotFoundException("The WMS documentation manifest was not published.", path);
        }
        var result = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return result?.Articles ?? [];
    }

    private static string ResolveContentPath(string relativePath) {
        string root = Path.Combine(AppContext.BaseDirectory, "Docs", "Content", "wms");
        string candidate = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!candidate.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException("Invalid documentation path.");
        }
        return candidate;
    }

    private sealed class Manifest { public List<ManifestArticle> Articles { get; set; } = []; }

    private sealed class ManifestArticle {
        public string Locale { get; set; } = "en";
        public string Module { get; set; } = "";
        public string Slug { get; set; } = "";
        public string File { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Section { get; set; } = "";
        public string? Route { get; set; }
        public List<string> Roles { get; set; } = [];
        public bool SuperUserOnly { get; set; }
        public string? Feature { get; set; }
        public int Order { get; set; }
    }
}
