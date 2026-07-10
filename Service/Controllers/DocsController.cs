using System.Collections.Generic;
using Core.DTOs.Docs;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Middlewares;

namespace Service.Controllers;

[ApiController]
[Route("api/docs")]
[Authorize]
public sealed class DocsController(IWmsDocumentationService documentationService) : ControllerBase {
    [HttpGet]
    public ActionResult<DocumentationIndex> Index([FromQuery] string? locale) =>
        Ok(documentationService.GetIndex(locale, HttpContext.GetSession()));

    [HttpGet("search")]
    public ActionResult<IReadOnlyCollection<DocumentationArticleSummary>> Search(
        [FromQuery] string q, [FromQuery] string? locale, [FromQuery] int limit = 20) =>
        Ok(documentationService.Search(q, locale, limit, HttpContext.GetSession()));

    [HttpGet("{module}/{**slug}")]
    public ActionResult<DocumentationArticle> Article(string module, string slug, [FromQuery] string? locale) {
        var article = documentationService.GetArticle(module, slug, locale, HttpContext.GetSession());
        return article is null ? NotFound() : Ok(article);
    }
}
