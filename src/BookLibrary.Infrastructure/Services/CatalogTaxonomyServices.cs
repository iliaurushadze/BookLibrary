using BookLibrary.Application.Common;
using BookLibrary.Application.DTOs.Catalog;
using BookLibrary.Application.Exceptions;
using BookLibrary.Application.Interfaces;
using BookLibrary.Domain.Entities;
using BookLibrary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Infrastructure.Services;

public class AuthorService : IAuthorService
{
    private readonly AppDbContext _db;
    private readonly IBookService _bookService;

    public AuthorService(AppDbContext db, IBookService bookService)
    {
        _db = db;
        _bookService = bookService;
    }

    public async Task<PagedResult<AuthorSummaryDto>> GetAuthorsAsync(int page, int pageSize, string? search, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Authors.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(a => a.DisplayName.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(a => a.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuthorSummaryDto(a.Id, a.DisplayName, a.Slug, a.PortraitImageUrl))
            .ToListAsync(ct);

        return new PagedResult<AuthorSummaryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<AuthorDetailDto> GetAuthorByIdOrSlugAsync(string idOrSlug, CancellationToken ct = default)
    {
        var author = await FindAuthorAsync(idOrSlug, ct);
        if (author == null)
            throw new NotFoundException($"Author '{idOrSlug}' was not found.");

        return MapToDetail(author);
    }

    public async Task<PagedResult<BookSummaryDto>> GetPublishedBooksByAuthorAsync(string idOrSlug, int page, int pageSize, CancellationToken ct = default)
    {
        var author = await FindAuthorAsync(idOrSlug, ct);
        if (author == null)
            throw new NotFoundException($"Author '{idOrSlug}' was not found.");

        return await _bookService.SearchPublishedBooksAsync(new BookSearchQuery(
            Page: page,
            PageSize: pageSize,
            Author: author.Slug), ct);
    }

    public async Task<AuthorDetailDto> CreateAuthorAsync(UpsertAuthorRequest request, string actorUserId, CancellationToken ct = default)
    {
        var slug = await EnsureUniqueSlugAsync(null, request.Slug ?? request.DisplayName, ct);
        var author = new Author
        {
            DisplayName = request.DisplayName.Trim(),
            Slug = slug,
            Biography = request.Biography?.Trim(),
            PortraitImageUrl = request.PortraitImageUrl?.Trim(),
            BirthDate = request.BirthDate,
            DeathDate = request.DeathDate,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Authors.Add(author);
        await _db.SaveChangesAsync(ct);
        return MapToDetail(author);
    }

    public async Task<AuthorDetailDto> UpdateAuthorAsync(Guid id, UpsertAuthorRequest request, string actorUserId, CancellationToken ct = default)
    {
        var author = await _db.Authors.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (author == null)
            throw new NotFoundException($"Author '{id}' was not found.");

        author.DisplayName = request.DisplayName.Trim();
        author.Slug = await EnsureUniqueSlugAsync(id, request.Slug ?? request.DisplayName, ct);
        author.Biography = request.Biography?.Trim();
        author.PortraitImageUrl = request.PortraitImageUrl?.Trim();
        author.BirthDate = request.BirthDate;
        author.DeathDate = request.DeathDate;
        author.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return MapToDetail(author);
    }

    public async Task DeleteAuthorAsync(Guid id, string actorUserId, CancellationToken ct = default)
    {
        var author = await _db.Authors.Include(a => a.BookAuthors).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (author == null)
            throw new NotFoundException($"Author '{id}' was not found.");

        if (author.BookAuthors.Any())
            throw new ConflictException("Cannot delete an author that is associated with existing books.");

        _db.Authors.Remove(author);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<Author?> FindAuthorAsync(string idOrSlug, CancellationToken ct)
    {
        if (Guid.TryParse(idOrSlug, out var id))
            return await _db.Authors.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

        var slug = idOrSlug.Trim().ToLower();
        return await _db.Authors.AsNoTracking().FirstOrDefaultAsync(a => a.Slug.ToLower() == slug, ct);
    }

    private async Task<string> EnsureUniqueSlugAsync(Guid? existingId, string raw, CancellationToken ct)
    {
        var baseSlug = SlugGenerator.Generate(raw);
        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "author";

        var candidate = baseSlug;
        var i = 2;
        while (await _db.Authors.AnyAsync(a => a.Id != existingId && a.Slug == candidate, ct))
        {
            candidate = $"{baseSlug}-{i++}";
        }
        return candidate;
    }

    private static AuthorDetailDto MapToDetail(Author a) =>
        new(a.Id, a.DisplayName, a.Slug, a.Biography, a.PortraitImageUrl, a.BirthDate, a.DeathDate, a.CreatedAtUtc, a.UpdatedAtUtc);
}

public class GenreService : IGenreService
{
    private readonly AppDbContext _db;
    private readonly IBookService _bookService;

    public GenreService(AppDbContext db, IBookService bookService)
    {
        _db = db;
        _bookService = bookService;
    }

    public async Task<IReadOnlyList<GenreSummaryDto>> GetAllGenresAsync(CancellationToken ct = default)
    {
        return await _db.Genres
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new GenreSummaryDto(g.Id, g.Name, g.Slug, g.ParentGenreId))
            .ToListAsync(ct);
    }

    public async Task<GenreDetailDto> GetGenreByIdOrSlugAsync(string idOrSlug, CancellationToken ct = default)
    {
        var genre = await FindGenreAsync(idOrSlug, ct);
        if (genre == null)
            throw new NotFoundException($"Genre '{idOrSlug}' was not found.");

        return MapToDetail(genre);
    }

    public async Task<PagedResult<BookSummaryDto>> GetPublishedBooksByGenreAsync(string idOrSlug, int page, int pageSize, CancellationToken ct = default)
    {
        var genre = await FindGenreAsync(idOrSlug, ct);
        if (genre == null)
            throw new NotFoundException($"Genre '{idOrSlug}' was not found.");

        return await _bookService.SearchPublishedBooksAsync(new BookSearchQuery(
            Page: page,
            PageSize: pageSize,
            Genre: genre.Slug), ct);
    }

    public async Task<GenreDetailDto> CreateGenreAsync(UpsertGenreRequest request, string actorUserId, CancellationToken ct = default)
    {
        var slug = SlugGenerator.Generate(request.Slug ?? request.Name);
        if (await _db.Genres.AnyAsync(g => g.Slug == slug, ct))
            throw new ConflictException($"Genre with slug '{slug}' already exists.");

        if (request.ParentGenreId.HasValue && !await _db.Genres.AnyAsync(g => g.Id == request.ParentGenreId.Value, ct))
            throw new BadRequestException($"Parent genre '{request.ParentGenreId}' was not found.");

        var genre = new Genre
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description?.Trim(),
            ParentGenreId = request.ParentGenreId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Genres.Add(genre);
        await _db.SaveChangesAsync(ct);
        return await GetGenreByIdOrSlugAsync(genre.Id.ToString(), ct);
    }

    public async Task<GenreDetailDto> UpdateGenreAsync(Guid id, UpsertGenreRequest request, string actorUserId, CancellationToken ct = default)
    {
        var genre = await _db.Genres.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (genre == null)
            throw new NotFoundException($"Genre '{id}' was not found.");

        var slug = SlugGenerator.Generate(request.Slug ?? request.Name);
        if (await _db.Genres.AnyAsync(g => g.Id != id && g.Slug == slug, ct))
            throw new ConflictException($"Genre with slug '{slug}' already exists.");

        if (request.ParentGenreId == id)
            throw new BadRequestException("A genre cannot be its own parent.");

        genre.Name = request.Name.Trim();
        genre.Slug = slug;
        genre.Description = request.Description?.Trim();
        genre.ParentGenreId = request.ParentGenreId;
        genre.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetGenreByIdOrSlugAsync(genre.Id.ToString(), ct);
    }

    public async Task DeleteGenreAsync(Guid id, string actorUserId, CancellationToken ct = default)
    {
        var genre = await _db.Genres
            .Include(g => g.BookGenres)
            .Include(g => g.SubGenres)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (genre == null)
            throw new NotFoundException($"Genre '{id}' was not found.");

        if (genre.BookGenres.Any() || genre.SubGenres.Any())
            throw new ConflictException("Cannot delete a genre that has books or sub-genres assigned.");

        _db.Genres.Remove(genre);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<Genre?> FindGenreAsync(string idOrSlug, CancellationToken ct)
    {
        var query = _db.Genres
            .AsNoTracking()
            .Include(g => g.ParentGenre)
            .Include(g => g.SubGenres);

        if (Guid.TryParse(idOrSlug, out var id))
            return await query.FirstOrDefaultAsync(g => g.Id == id, ct);

        var slug = idOrSlug.Trim().ToLower();
        return await query.FirstOrDefaultAsync(g => g.Slug.ToLower() == slug, ct);
    }

    private static GenreDetailDto MapToDetail(Genre g) =>
        new(
            g.Id,
            g.Name,
            g.Slug,
            g.Description,
            g.ParentGenreId,
            g.ParentGenre?.Name,
            g.SubGenres.Select(sg => new GenreSummaryDto(sg.Id, sg.Name, sg.Slug, sg.ParentGenreId)).ToList());
}

public class LanguageService : ILanguageService
{
    private readonly AppDbContext _db;

    public LanguageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LanguageDto>> GetLanguagesAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.Languages.AsNoTracking();
        if (!includeInactive)
            query = query.Where(l => l.IsActive);

        return await query
            .OrderBy(l => l.Name)
            .Select(l => new LanguageDto(l.Id, l.Name, l.Code, l.NativeName, l.IsActive))
            .ToListAsync(ct);
    }

    public async Task<LanguageDto> CreateLanguageAsync(UpsertLanguageRequest request, string actorUserId, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToLowerInvariant();
        if (await _db.Languages.AnyAsync(l => l.Code.ToLower() == code, ct))
            throw new ConflictException($"Language with code '{code}' already exists.");

        var language = new Language
        {
            Name = request.Name.Trim(),
            Code = code,
            NativeName = request.NativeName?.Trim(),
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Languages.Add(language);
        await _db.SaveChangesAsync(ct);

        return new LanguageDto(language.Id, language.Name, language.Code, language.NativeName, language.IsActive);
    }

    public async Task<LanguageDto> UpdateLanguageAsync(Guid id, UpsertLanguageRequest request, string actorUserId, CancellationToken ct = default)
    {
        var language = await _db.Languages.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (language == null)
            throw new NotFoundException($"Language '{id}' was not found.");

        var code = request.Code.Trim().ToLowerInvariant();
        if (await _db.Languages.AnyAsync(l => l.Id != id && l.Code.ToLower() == code, ct))
            throw new ConflictException($"Language with code '{code}' already exists.");

        language.Name = request.Name.Trim();
        language.Code = code;
        language.NativeName = request.NativeName?.Trim();
        language.IsActive = request.IsActive;
        language.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return new LanguageDto(language.Id, language.Name, language.Code, language.NativeName, language.IsActive);
    }
}
