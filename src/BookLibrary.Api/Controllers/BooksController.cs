using System.Security.Claims;
using BookLibrary.Application.Common;
using BookLibrary.Application.DTOs.Catalog;
using BookLibrary.Application.DTOs.Requests;
using BookLibrary.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookLibrary.Api.Controllers;

[ApiController]
[Route("api/v1/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BookSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchBooks([FromQuery] BookSearchQuery query, CancellationToken ct)
    {
        var result = await _bookService.SearchPublishedBooksAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("recent")]
    [ProducesResponseType(typeof(IReadOnlyList<BookSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentBooks([FromQuery] int count = 10, CancellationToken ct = default)
    {
        var result = await _bookService.GetRecentBooksAsync(count, ct);
        return Ok(result);
    }

    [HttpGet("{idOrSlug}")]
    [ProducesResponseType(typeof(BookDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookByIdOrSlug(string idOrSlug, CancellationToken ct)
    {
        var book = await _bookService.GetPublishedBookByIdOrSlugAsync(idOrSlug, ct);
        return Ok(book);
    }

    [HttpGet("{idOrSlug}/files")]
    [ProducesResponseType(typeof(IReadOnlyList<BookFileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookFiles(string idOrSlug, CancellationToken ct)
    {
        var files = await _bookService.GetPublishedBookFilesAsync(idOrSlug, ct);
        return Ok(files);
    }

    [HttpGet("{idOrSlug}/files/{fileId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(BookFileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadBookFile(
        string idOrSlug,
        Guid fileId,
        [FromQuery] bool redirect = true,
        CancellationToken ct = default)
    {
        var (downloadUrl, fileDto) = await _bookService.GetFileDownloadRedirectAsync(idOrSlug, fileId, ct);
        if (redirect)
        {
            return Redirect(downloadUrl);
        }
        return Ok(fileDto);
    }

    [HttpPost("{bookId:guid}/files/{fileId:guid}/report-broken")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReportBrokenLink(
        Guid bookId,
        Guid fileId,
        [FromBody] ReportBrokenLinkRequest request,
        CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _bookService.ReportBrokenLinkAsync(bookId, fileId, request.Reason, userId, ct);
        return NoContent();
    }
}
