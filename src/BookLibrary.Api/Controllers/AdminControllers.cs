using System.Security.Claims;
using BookLibrary.Application.Common;
using BookLibrary.Application.DTOs.Auth;
using BookLibrary.Application.DTOs.Catalog;
using BookLibrary.Application.DTOs.Requests;
using BookLibrary.Application.Interfaces;
using BookLibrary.Domain.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookLibrary.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Admin)]
[Route("api/v1/admin")]
public class AdminController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IAuthorService _authorService;
    private readonly IGenreService _genreService;
    private readonly ILanguageService _languageService;
    private readonly IBookRequestService _requestService;
    private readonly IContactMessageService _contactService;
    private readonly IAuthService _authService;
    private readonly IValidator<UpsertBookRequest> _bookValidator;
    private readonly IValidator<AssociateBookFileRequest> _fileValidator;
    private readonly IValidator<UpsertAuthorRequest> _authorValidator;
    private readonly IValidator<UpsertGenreRequest> _genreValidator;
    private readonly IValidator<UpsertLanguageRequest> _languageValidator;

    public AdminController(
        IBookService bookService,
        IAuthorService authorService,
        IGenreService genreService,
        ILanguageService languageService,
        IBookRequestService requestService,
        IContactMessageService contactService,
        IAuthService authService,
        IValidator<UpsertBookRequest> bookValidator,
        IValidator<AssociateBookFileRequest> fileValidator,
        IValidator<UpsertAuthorRequest> authorValidator,
        IValidator<UpsertGenreRequest> genreValidator,
        IValidator<UpsertLanguageRequest> languageValidator)
    {
        _bookService = bookService;
        _authorService = authorService;
        _genreService = genreService;
        _languageService = languageService;
        _requestService = requestService;
        _contactService = contactService;
        _authService = authService;
        _bookValidator = bookValidator;
        _fileValidator = fileValidator;
        _authorValidator = authorValidator;
        _genreValidator = genreValidator;
        _languageValidator = languageValidator;
    }

    private string ActorUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // --- Statistics ---
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(CatalogStatisticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics(CancellationToken ct)
    {
        return Ok(await _bookService.GetStatisticsAsync(ct));
    }

    // --- Books Management ---
    [HttpGet("books")]
    [ProducesResponseType(typeof(PagedResult<BookSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBooks([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        return Ok(await _bookService.GetAdminBooksAsync(page, pageSize, search, ct));
    }

    [HttpGet("books/{id:guid}")]
    [ProducesResponseType(typeof(BookDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBookById(Guid id, CancellationToken ct)
    {
        return Ok(await _bookService.GetAdminBookByIdAsync(id, ct));
    }

    [HttpPost("books")]
    [ProducesResponseType(typeof(BookDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBook([FromBody] UpsertBookRequest request, CancellationToken ct)
    {
        await _bookValidator.ValidateAndThrowAsync(request, ct);
        var created = await _bookService.CreateBookAsync(request, ActorUserId, ct);
        return CreatedAtAction(nameof(GetBookById), new { id = created.Id }, created);
    }

    [HttpPut("books/{id:guid}")]
    [ProducesResponseType(typeof(BookDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBook(Guid id, [FromBody] UpsertBookRequest request, CancellationToken ct)
    {
        await _bookValidator.ValidateAndThrowAsync(request, ct);
        var updated = await _bookService.UpdateBookAsync(id, request, ActorUserId, ct);
        return Ok(updated);
    }

    [HttpPost("books/{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PublishBook(Guid id, CancellationToken ct)
    {
        await _bookService.PublishBookAsync(id, ActorUserId, ct);
        return NoContent();
    }

    [HttpPost("books/{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ArchiveBook(Guid id, CancellationToken ct)
    {
        await _bookService.ArchiveBookAsync(id, ActorUserId, ct);
        return NoContent();
    }

    [HttpDelete("books/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteBook(Guid id, CancellationToken ct)
    {
        await _bookService.DeleteBookAsync(id, ActorUserId, ct);
        return NoContent();
    }

    // --- Book File Google Drive Associations ---
    [HttpPost("books/{id:guid}/files")]
    [ProducesResponseType(typeof(AdminBookFileDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AssociateBookFile(Guid id, [FromBody] AssociateBookFileRequest request, CancellationToken ct)
    {
        await _fileValidator.ValidateAndThrowAsync(request, ct);
        var created = await _bookService.AssociateBookFileAsync(id, request, ActorUserId, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpDelete("books/{id:guid}/files/{fileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveBookFile(Guid id, Guid fileId, CancellationToken ct)
    {
        await _bookService.RemoveBookFileAsync(id, fileId, ActorUserId, ct);
        return NoContent();
    }

    // --- Authors Management ---
    [HttpPost("authors")]
    [ProducesResponseType(typeof(AuthorDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAuthor([FromBody] UpsertAuthorRequest request, CancellationToken ct)
    {
        await _authorValidator.ValidateAndThrowAsync(request, ct);
        var created = await _authorService.CreateAuthorAsync(request, ActorUserId, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("authors/{id:guid}")]
    [ProducesResponseType(typeof(AuthorDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAuthor(Guid id, [FromBody] UpsertAuthorRequest request, CancellationToken ct)
    {
        await _authorValidator.ValidateAndThrowAsync(request, ct);
        return Ok(await _authorService.UpdateAuthorAsync(id, request, ActorUserId, ct));
    }

    [HttpDelete("authors/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAuthor(Guid id, CancellationToken ct)
    {
        await _authorService.DeleteAuthorAsync(id, ActorUserId, ct);
        return NoContent();
    }

    // --- Genres Management ---
    [HttpPost("genres")]
    [ProducesResponseType(typeof(GenreDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGenre([FromBody] UpsertGenreRequest request, CancellationToken ct)
    {
        await _genreValidator.ValidateAndThrowAsync(request, ct);
        var created = await _genreService.CreateGenreAsync(request, ActorUserId, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("genres/{id:guid}")]
    [ProducesResponseType(typeof(GenreDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateGenre(Guid id, [FromBody] UpsertGenreRequest request, CancellationToken ct)
    {
        await _genreValidator.ValidateAndThrowAsync(request, ct);
        return Ok(await _genreService.UpdateGenreAsync(id, request, ActorUserId, ct));
    }

    [HttpDelete("genres/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGenre(Guid id, CancellationToken ct)
    {
        await _genreService.DeleteGenreAsync(id, ActorUserId, ct);
        return NoContent();
    }

    // --- Languages Management ---
    [HttpGet("languages")]
    [ProducesResponseType(typeof(IReadOnlyList<LanguageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllLanguages(CancellationToken ct)
    {
        return Ok(await _languageService.GetLanguagesAsync(includeInactive: true, ct));
    }

    [HttpPost("languages")]
    [ProducesResponseType(typeof(LanguageDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLanguage([FromBody] UpsertLanguageRequest request, CancellationToken ct)
    {
        await _languageValidator.ValidateAndThrowAsync(request, ct);
        var created = await _languageService.CreateLanguageAsync(request, ActorUserId, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("languages/{id:guid}")]
    [ProducesResponseType(typeof(LanguageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLanguage(Guid id, [FromBody] UpsertLanguageRequest request, CancellationToken ct)
    {
        await _languageValidator.ValidateAndThrowAsync(request, ct);
        return Ok(await _languageService.UpdateLanguageAsync(id, request, ActorUserId, ct));
    }

    // --- Book Requests Review ---
    [HttpGet("book-requests")]
    [ProducesResponseType(typeof(PagedResult<AdminBookRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBookRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, CancellationToken ct = default)
    {
        return Ok(await _requestService.GetAllRequestsAsync(page, pageSize, status, ct));
    }

    [HttpPatch("book-requests/{id:guid}")]
    [ProducesResponseType(typeof(AdminBookRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBookRequest(Guid id, [FromBody] UpdateBookRequestStatusDto dto, CancellationToken ct)
    {
        return Ok(await _requestService.UpdateRequestStatusAsync(id, dto, ActorUserId, ct));
    }

    // --- Contact Messages Review ---
    [HttpGet("contact-messages")]
    [ProducesResponseType(typeof(PagedResult<AdminContactMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContactMessages([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, CancellationToken ct = default)
    {
        return Ok(await _contactService.GetMessagesAsync(page, pageSize, status, ct));
    }

    [HttpPatch("contact-messages/{id:guid}")]
    [ProducesResponseType(typeof(AdminContactMessageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateContactMessage(Guid id, [FromBody] UpdateContactMessageDto dto, CancellationToken ct)
    {
        return Ok(await _contactService.UpdateMessageAsync(id, dto, ActorUserId, ct));
    }

    // --- Users Management ---
    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        return Ok(await _authService.GetUsersAsync(page, pageSize, ct));
    }

    [HttpPut("users/{userId}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateUserRole(string userId, [FromBody] UpdateUserRoleRequest request, CancellationToken ct)
    {
        await _authService.UpdateUserRoleAsync(userId, request.Role, ActorUserId, ct);
        return NoContent();
    }
}
