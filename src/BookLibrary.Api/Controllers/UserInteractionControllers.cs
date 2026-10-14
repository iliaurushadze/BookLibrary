using System.Security.Claims;
using BookLibrary.Application.Common;
using BookLibrary.Application.DTOs.Catalog;
using BookLibrary.Application.DTOs.Requests;
using BookLibrary.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookLibrary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/book-requests")]
public class BookRequestsController : ControllerBase
{
    private readonly IBookRequestService _requestService;
    private readonly IValidator<CreateBookRequestDto> _validator;

    public BookRequestsController(IBookRequestService requestService, IValidator<CreateBookRequestDto> validator)
    {
        _requestService = requestService;
        _validator = validator;
    }

    [HttpPost]
    [EnableRateLimiting("SubmissionPolicy")]
    [ProducesResponseType(typeof(UserBookRequestDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRequest([FromBody] CreateBookRequestDto dto, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(dto, ct);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var created = await _requestService.CreateRequestAsync(userId, dto, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(PagedResult<UserBookRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _requestService.GetMyRequestsAsync(userId, page, pageSize, ct));
    }

    [HttpGet("me/{requestId:guid}")]
    [ProducesResponseType(typeof(UserBookRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRequestById(Guid requestId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _requestService.GetMyRequestByIdAsync(userId, requestId, ct));
    }

    [HttpPost("me/{requestId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelMyRequest(Guid requestId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _requestService.CancelMyRequestAsync(userId, requestId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/contact")]
public class ContactController : ControllerBase
{
    private readonly IContactMessageService _contactService;
    private readonly IValidator<CreateContactMessageDto> _validator;

    public ContactController(IContactMessageService contactService, IValidator<CreateContactMessageDto> validator)
    {
        _contactService = contactService;
        _validator = validator;
    }

    [HttpPost]
    [EnableRateLimiting("SubmissionPolicy")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> SubmitMessage([FromBody] CreateContactMessageDto dto, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(dto, ct);
        await _contactService.SubmitMessageAsync(dto, ct);
        return Accepted(new { message = "Your message has been received." });
    }
}

[ApiController]
[Authorize]
[Route("api/v1/bookmarks")]
public class BookmarksController : ControllerBase
{
    private readonly IBookmarkService _bookmarkService;

    public BookmarksController(IBookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BookSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBookmarks([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _bookmarkService.GetUserBookmarksAsync(userId, page, pageSize, ct));
    }

    [HttpPost("{bookId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddBookmark(Guid bookId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _bookmarkService.AddBookmarkAsync(userId, bookId, ct);
        return NoContent();
    }

    [HttpDelete("{bookId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveBookmark(Guid bookId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _bookmarkService.RemoveBookmarkAsync(userId, bookId, ct);
        return NoContent();
    }
}
