using BookLibrary.Application.Common;
using BookLibrary.Application.DTOs.Catalog;
using BookLibrary.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookLibrary.Api.Controllers;

[ApiController]
[Route("api/v1/authors")]
public class AuthorsController : ControllerBase
{
    private readonly IAuthorService _authorService;

    public AuthorsController(IAuthorService authorService)
    {
        _authorService = authorService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuthorSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuthors(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        return Ok(await _authorService.GetAuthorsAsync(page, pageSize, search, ct));
    }

    [HttpGet("{idOrSlug}")]
    [ProducesResponseType(typeof(AuthorDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuthor(string idOrSlug, CancellationToken ct)
    {
        return Ok(await _authorService.GetAuthorByIdOrSlugAsync(idOrSlug, ct));
    }

    [HttpGet("{idOrSlug}/books")]
    [ProducesResponseType(typeof(PagedResult<BookSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBooksByAuthor(
        string idOrSlug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Ok(await _authorService.GetPublishedBooksByAuthorAsync(idOrSlug, page, pageSize, ct));
    }
}

[ApiController]
[Route("api/v1/genres")]
public class GenresController : ControllerBase
{
    private readonly IGenreService _genreService;

    public GenresController(IGenreService genreService)
    {
        _genreService = genreService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GenreSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGenres(CancellationToken ct)
    {
        return Ok(await _genreService.GetAllGenresAsync(ct));
    }

    [HttpGet("{idOrSlug}")]
    [ProducesResponseType(typeof(GenreDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGenre(string idOrSlug, CancellationToken ct)
    {
        return Ok(await _genreService.GetGenreByIdOrSlugAsync(idOrSlug, ct));
    }

    [HttpGet("{idOrSlug}/books")]
    [ProducesResponseType(typeof(PagedResult<BookSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBooksByGenre(
        string idOrSlug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Ok(await _genreService.GetPublishedBooksByGenreAsync(idOrSlug, page, pageSize, ct));
    }
}

[ApiController]
[Route("api/v1/languages")]
public class LanguagesController : ControllerBase
{
    private readonly ILanguageService _languageService;

    public LanguagesController(ILanguageService languageService)
    {
        _languageService = languageService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LanguageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLanguages(CancellationToken ct)
    {
        return Ok(await _languageService.GetLanguagesAsync(includeInactive: false, ct));
    }
}
