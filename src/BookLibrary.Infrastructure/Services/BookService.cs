using BookLibrary.Application.Common;
using BookLibrary.Application.DTOs.Catalog;
using BookLibrary.Application.DTOs.Requests;
using BookLibrary.Application.Exceptions;
using BookLibrary.Application.Interfaces;
using BookLibrary.Domain.Entities;
using BookLibrary.Domain.Enums;
using BookLibrary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Infrastructure.Services;

public class BookService : IBookService
{
    private readonly AppDbContext _db;
    private readonly IBookFileLinkService _fileLinkService;

    public BookService(AppDbContext db, IBookFileLinkService fileLinkService)
    {
        _db = db;
        _fileLinkService = fileLinkService;
    }

    public async Task<PagedResult<BookSummaryDto>> SearchPublishedBooksAsync(BookSearchQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var booksQuery = _db.Books
            .AsNoTracking()
            .Where(b => b.Status == BookStatus.Published);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            booksQuery = booksQuery.Where(b =>
                b.Title.ToLower().Contains(term) ||
                b.Synopsis.ToLower().Contains(term) ||
                (b.Isbn13 != null && b.Isbn13.ToLower().Contains(term)) ||
                (b.Isbn10 != null && b.Isbn10.ToLower().Contains(term)) ||
                b.BookAuthors.Any(ba => ba.Author!.DisplayName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(query.Genre))
        {
            var genreFilter = query.Genre.Trim().ToLower();
            if (Guid.TryParse(genreFilter, out var genreId))
            {
                booksQuery = booksQuery.Where(b => b.BookGenres.Any(bg => bg.GenreId == genreId));
            }
            else
            {
                booksQuery = booksQuery.Where(b => b.BookGenres.Any(bg => bg.Genre!.Slug.ToLower() == genreFilter));
            }
        }

        if (!string.IsNullOrWhiteSpace(query.Author))
        {
            var authorFilter = query.Author.Trim().ToLower();
            if (Guid.TryParse(authorFilter, out var authorId))
            {
                booksQuery = booksQuery.Where(b => b.BookAuthors.Any(ba => ba.AuthorId == authorId));
            }
            else
            {
                booksQuery = booksQuery.Where(b => b.BookAuthors.Any(ba => ba.Author!.Slug.ToLower() == authorFilter));
            }
        }

        if (!string.IsNullOrWhiteSpace(query.Language))
        {
            var langFilter = query.Language.Trim().ToLower();
            booksQuery = booksQuery.Where(b =>
                b.OriginalLanguage!.Code.ToLower() == langFilter ||
                b.BookFiles.Any(bf => bf.IsActive && bf.Language != null && bf.Language.Code.ToLower() == langFilter));
        }

        if (query.Format.HasValue)
        {
            var fmt = query.Format.Value;
            booksQuery = booksQuery.Where(b => b.BookFiles.Any(bf => bf.IsActive && bf.Format == fmt));
        }

        if (query.PublicationYear.HasValue)
        {
            booksQuery = booksQuery.Where(b => b.PublicationYear == query.PublicationYear.Value);
        }

        var isAsc = string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        booksQuery = query.SortBy?.ToLowerInvariant() switch
        {
            "title" => isAsc ? booksQuery.OrderBy(b => b.Title) : booksQuery.OrderByDescending(b => b.Title),
            "publicationdate" or "publicationyear" => isAsc
                ? booksQuery.OrderBy(b => b.PublicationYear).ThenBy(b => b.Title)
                : booksQuery.OrderByDescending(b => b.PublicationYear).ThenBy(b => b.Title),
            _ => isAsc ? booksQuery.OrderBy(b => b.CreatedAtUtc) : booksQuery.OrderByDescending(b => b.CreatedAtUtc)
        };

        var totalCount = await booksQuery.CountAsync(ct);

        var items = await booksQuery
            .Include(b => b.OriginalLanguage)
            .Include(b => b.BookAuthors.OrderBy(ba => ba.DisplayOrder)).ThenInclude(ba => ba.Author)
            .Include(b => b.BookGenres).ThenInclude(bg => bg.Genre)
            .Include(b => b.BookFiles.Where(bf => bf.IsActive))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<BookSummaryDto>
        {
            Items = items.Select(MapToSummaryDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IReadOnlyList<BookSummaryDto>> GetRecentBooksAsync(int count = 10, CancellationToken ct = default)
    {
        var safeCount = Math.Clamp(count, 1, 50);
        var books = await _db.Books
            .AsNoTracking()
            .Where(b => b.Status == BookStatus.Published)
            .Include(b => b.OriginalLanguage)
            .Include(b => b.BookAuthors.OrderBy(ba => ba.DisplayOrder)).ThenInclude(ba => ba.Author)
            .Include(b => b.BookGenres).ThenInclude(bg => bg.Genre)
            .Include(b => b.BookFiles.Where(bf => bf.IsActive))
            .OrderByDescending(b => b.PublishedAtUtc ?? b.CreatedAtUtc)
            .Take(safeCount)
            .ToListAsync(ct);

        return books.Select(MapToSummaryDto).ToList();
    }

    public async Task<BookDetailDto> GetPublishedBookByIdOrSlugAsync(string idOrSlug, CancellationToken ct = default)
    {
        var book = await FindBookQuery(includeInactiveFiles: false)
            .AsNoTracking()
            .Where(b => b.Status == BookStatus.Published)
            .FirstOrDefaultAsync(BuildIdOrSlugPredicate(idOrSlug), ct);

        if (book == null)
            throw new NotFoundException($"Published book '{idOrSlug}' was not found.");

        var relatedBooks = await GetRelatedBooksAsync(book, ct);
        return MapToDetailDto(book, relatedBooks);
    }

    public async Task<IReadOnlyList<BookFileDto>> GetPublishedBookFilesAsync(string idOrSlug, CancellationToken ct = default)
    {
        var book = await _db.Books
            .AsNoTracking()
            .Include(b => b.BookFiles.Where(bf => bf.IsActive)).ThenInclude(bf => bf.Language)
            .Where(b => b.Status == BookStatus.Published)
            .FirstOrDefaultAsync(BuildIdOrSlugPredicate(idOrSlug), ct);

        if (book == null)
            throw new NotFoundException($"Published book '{idOrSlug}' was not found.");

        return book.BookFiles.Select(MapToBookFileDto).ToList();
    }

    public async Task<(string DownloadUrl, BookFileDto File)> GetFileDownloadRedirectAsync(string idOrSlug, Guid fileId, CancellationToken ct = default)
    {
        var book = await _db.Books
            .AsNoTracking()
            .Include(b => b.BookFiles).ThenInclude(bf => bf.Language)
            .FirstOrDefaultAsync(BuildIdOrSlugPredicate(idOrSlug), ct);

        if (book == null || book.Status != BookStatus.Published)
            throw new NotFoundException($"Published book '{idOrSlug}' was not found.");

        var bookFile = book.BookFiles.FirstOrDefault(f => f.Id == fileId && f.IsActive);
        if (bookFile == null)
            throw new NotFoundException($"Active file '{fileId}' was not found for book '{book.Title}'.");

        var dto = MapToBookFileDto(bookFile);
        return (dto.DownloadUrl, dto);
    }

    public async Task<PagedResult<BookSummaryDto>> GetAdminBooksAsync(int page, int pageSize, string? search, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Books.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(term) || b.Slug.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Include(b => b.OriginalLanguage)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.BookGenres).ThenInclude(bg => bg.Genre)
            .Include(b => b.BookFiles)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<BookSummaryDto>
        {
            Items = items.Select(MapToSummaryDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BookDetailDto> GetAdminBookByIdAsync(Guid id, CancellationToken ct = default)
    {
        var book = await FindBookQuery(includeInactiveFiles: true)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (book == null)
            throw new NotFoundException($"Book '{id}' was not found.");

        var related = await GetRelatedBooksAsync(book, ct);
        return MapToDetailDto(book, related);
    }

    public async Task<BookDetailDto> CreateBookAsync(UpsertBookRequest request, string actorUserId, CancellationToken ct = default)
    {
        await ValidateBookRelationsAndDuplicatesAsync(null, request, ct);

        var slug = await GenerateUniqueBookSlugAsync(null, request.Slug ?? request.Title, ct);

        var book = new Book
        {
            Title = request.Title.Trim(),
            Slug = slug,
            Synopsis = request.Synopsis.Trim(),
            Isbn10 = request.Isbn10?.Trim(),
            Isbn13 = request.Isbn13?.Trim(),
            CoverImageUrl = request.CoverImageUrl?.Trim(),
            PublicationYear = request.PublicationYear ?? request.PublicationDate?.Year,
            PublicationDate = request.PublicationDate,
            Publisher = request.Publisher?.Trim(),
            OriginalLanguageId = request.OriginalLanguageId,
            SeriesName = request.SeriesName?.Trim(),
            SeriesOrder = request.SeriesOrder,
            Edition = request.Edition?.Trim(),
            Status = BookStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var order = 1;
        foreach (var authorId in request.AuthorIds.Distinct())
        {
            book.BookAuthors.Add(new BookAuthor { BookId = book.Id, AuthorId = authorId, DisplayOrder = order++ });
        }

        foreach (var genreId in request.GenreIds.Distinct())
        {
            book.BookGenres.Add(new BookGenre { BookId = book.Id, GenreId = genreId });
        }

        _db.Books.Add(book);
        LogAudit(actorUserId, "CreateBook", nameof(Book), book.Id.ToString(), $"Created book '{book.Title}'");
        await _db.SaveChangesAsync(ct);

        return await GetAdminBookByIdAsync(book.Id, ct);
    }

    public async Task<BookDetailDto> UpdateBookAsync(Guid id, UpsertBookRequest request, string actorUserId, CancellationToken ct = default)
    {
        var book = await _db.Books
            .Include(b => b.BookAuthors)
            .Include(b => b.BookGenres)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (book == null)
            throw new NotFoundException($"Book '{id}' was not found.");

        await ValidateBookRelationsAndDuplicatesAsync(id, request, ct);

        var slug = await GenerateUniqueBookSlugAsync(id, request.Slug ?? request.Title, ct);

        book.Title = request.Title.Trim();
        book.Slug = slug;
        book.Synopsis = request.Synopsis.Trim();
        book.Isbn10 = request.Isbn10?.Trim();
        book.Isbn13 = request.Isbn13?.Trim();
        book.CoverImageUrl = request.CoverImageUrl?.Trim();
        book.PublicationYear = request.PublicationYear ?? request.PublicationDate?.Year;
        book.PublicationDate = request.PublicationDate;
        book.Publisher = request.Publisher?.Trim();
        book.OriginalLanguageId = request.OriginalLanguageId;
        book.SeriesName = request.SeriesName?.Trim();
        book.SeriesOrder = request.SeriesOrder;
        book.Edition = request.Edition?.Trim();
        book.UpdatedAtUtc = DateTime.UtcNow;

        _db.BookAuthors.RemoveRange(book.BookAuthors);
        _db.BookGenres.RemoveRange(book.BookGenres);

        var order = 1;
        foreach (var authorId in request.AuthorIds.Distinct())
        {
            book.BookAuthors.Add(new BookAuthor { BookId = book.Id, AuthorId = authorId, DisplayOrder = order++ });
        }

        foreach (var genreId in request.GenreIds.Distinct())
        {
            book.BookGenres.Add(new BookGenre { BookId = book.Id, GenreId = genreId });
        }

        LogAudit(actorUserId, "UpdateBook", nameof(Book), book.Id.ToString(), $"Updated book '{book.Title}'");
        await _db.SaveChangesAsync(ct);

        return await GetAdminBookByIdAsync(book.Id, ct);
    }

    public async Task PublishBookAsync(Guid id, string actorUserId, CancellationToken ct = default)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (book == null)
            throw new NotFoundException($"Book '{id}' was not found.");

        book.Status = BookStatus.Published;
        book.PublishedAtUtc ??= DateTime.UtcNow;
        book.UpdatedAtUtc = DateTime.UtcNow;

        LogAudit(actorUserId, "PublishBook", nameof(Book), book.Id.ToString(), $"Published book '{book.Title}'");
        await _db.SaveChangesAsync(ct);
    }

    public async Task ArchiveBookAsync(Guid id, string actorUserId, CancellationToken ct = default)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (book == null)
            throw new NotFoundException($"Book '{id}' was not found.");

        book.Status = BookStatus.Archived;
        book.UpdatedAtUtc = DateTime.UtcNow;

        LogAudit(actorUserId, "ArchiveBook", nameof(Book), book.Id.ToString(), $"Archived book '{book.Title}'");
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteBookAsync(Guid id, string actorUserId, CancellationToken ct = default)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (book == null)
            throw new NotFoundException($"Book '{id}' was not found.");

        _db.Books.Remove(book);
        LogAudit(actorUserId, "DeleteBook", nameof(Book), book.Id.ToString(), $"Deleted book '{book.Title}'");
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AdminBookFileDto> AssociateBookFileAsync(Guid bookId, AssociateBookFileRequest request, string actorUserId, CancellationToken ct = default)
    {
        var bookExists = await _db.Books.AnyAsync(b => b.Id == bookId, ct);
        if (!bookExists)
            throw new NotFoundException($"Book '{bookId}' was not found.");

        var parseResult = _fileLinkService.ParseAndValidateSharingUrl(request.SharingUrl);
        if (!parseResult.IsValid || string.IsNullOrWhiteSpace(parseResult.FileId))
        {
            throw new BadRequestException(parseResult.ErrorMessage ?? "Invalid Google Drive sharing URL.");
        }

        Language? language = null;
        if (request.LanguageId.HasValue)
        {
            language = await _db.Languages.FirstOrDefaultAsync(l => l.Id == request.LanguageId.Value, ct);
            if (language == null)
                throw new BadRequestException($"Language '{request.LanguageId}' does not exist.");
        }

        var duplicateExists = await _db.BookFiles.AnyAsync(
            bf => bf.BookId == bookId && bf.GoogleDriveFileId == parseResult.FileId, ct);
        if (duplicateExists)
        {
            throw new ConflictException($"Google Drive file '{parseResult.FileId}' is already associated with this book.");
        }

        var bookFile = new BookFile
        {
            BookId = bookId,
            Format = request.Format,
            GoogleDriveFileId = parseResult.FileId,
            OriginalSharingUrl = request.SharingUrl.Trim(),
            OriginalFileName = request.OriginalFileName?.Trim(),
            FileSizeBytes = request.FileSizeBytes,
            LanguageId = request.LanguageId,
            EditionNote = request.EditionNote?.Trim(),
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.BookFiles.Add(bookFile);
        LogAudit(actorUserId, "AssociateBookFile", nameof(BookFile), bookFile.Id.ToString(),
            $"Associated {bookFile.Format} Drive ID '{bookFile.GoogleDriveFileId}' with book '{bookId}'");
        await _db.SaveChangesAsync(ct);

        return new AdminBookFileDto(
            bookFile.Id,
            bookFile.Format,
            bookFile.GoogleDriveFileId,
            bookFile.OriginalSharingUrl,
            bookFile.OriginalFileName,
            bookFile.FileSizeBytes,
            bookFile.LanguageId,
            language?.Code,
            bookFile.EditionNote,
            bookFile.IsActive,
            _fileLinkService.BuildDownloadUrl(bookFile.GoogleDriveFileId),
            bookFile.CreatedAtUtc,
            bookFile.UpdatedAtUtc);
    }

    public async Task RemoveBookFileAsync(Guid bookId, Guid fileId, string actorUserId, CancellationToken ct = default)
    {
        var bookFile = await _db.BookFiles.FirstOrDefaultAsync(bf => bf.Id == fileId && bf.BookId == bookId, ct);
        if (bookFile == null)
            throw new NotFoundException($"File '{fileId}' was not found for book '{bookId}'.");

        _db.BookFiles.Remove(bookFile);
        LogAudit(actorUserId, "RemoveBookFile", nameof(BookFile), fileId.ToString(), $"Removed file '{fileId}' from book '{bookId}'");
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReportBrokenLinkAsync(Guid bookId, Guid fileId, string reason, string? reporterUserId, CancellationToken ct = default)
    {
        var bookFile = await _db.BookFiles.FirstOrDefaultAsync(bf => bf.Id == fileId && bf.BookId == bookId, ct);
        if (bookFile == null)
            throw new NotFoundException($"File '{fileId}' was not found for book '{bookId}'.");

        var report = new BrokenLinkReport
        {
            BookFileId = fileId,
            ReporterUserId = reporterUserId,
            Reason = reason.Trim(),
            ReportedAtUtc = DateTime.UtcNow
        };

        _db.BrokenLinkReports.Add(report);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CatalogStatisticsDto> GetStatisticsAsync(CancellationToken ct = default)
    {
        var published = await _db.Books.CountAsync(b => b.Status == BookStatus.Published, ct);
        var draft = await _db.Books.CountAsync(b => b.Status == BookStatus.Draft, ct);
        var archived = await _db.Books.CountAsync(b => b.Status == BookStatus.Archived, ct);
        var authors = await _db.Authors.CountAsync(ct);
        var genres = await _db.Genres.CountAsync(ct);
        var files = await _db.BookFiles.CountAsync(bf => bf.IsActive, ct);
        var pendingReqs = await _db.BookRequests.CountAsync(r => r.Status == BookRequestStatus.Pending, ct);
        var unresolvedContacts = await _db.ContactMessages.CountAsync(c => c.Status == ContactMessageStatus.New || c.Status == ContactMessageStatus.InProgress, ct);
        var brokenLinks = await _db.BrokenLinkReports.CountAsync(b => !b.IsResolved, ct);

        return new CatalogStatisticsDto(
            published, draft, archived, authors, genres, files, pendingReqs, unresolvedContacts, brokenLinks);
    }

    private IQueryable<Book> FindBookQuery(bool includeInactiveFiles)
    {
        return _db.Books
            .Include(b => b.OriginalLanguage)
            .Include(b => b.BookAuthors.OrderBy(ba => ba.DisplayOrder)).ThenInclude(ba => ba.Author)
            .Include(b => b.BookGenres).ThenInclude(bg => bg.Genre)
            .Include(b => b.BookFiles.Where(bf => includeInactiveFiles || bf.IsActive)).ThenInclude(bf => bf.Language);
    }

    private static System.Linq.Expressions.Expression<Func<Book, bool>> BuildIdOrSlugPredicate(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out var id))
            return b => b.Id == id;

        var normalizedSlug = idOrSlug.Trim().ToLower();
        return b => b.Slug.ToLower() == normalizedSlug;
    }

    private async Task<IReadOnlyList<BookSummaryDto>> GetRelatedBooksAsync(Book book, CancellationToken ct)
    {
        var authorIds = book.BookAuthors.Select(ba => ba.AuthorId).ToList();
        var genreIds = book.BookGenres.Select(bg => bg.GenreId).ToList();

        var related = await _db.Books
            .AsNoTracking()
            .Where(b => b.Id != book.Id && b.Status == BookStatus.Published)
            .Where(b => b.BookAuthors.Any(ba => authorIds.Contains(ba.AuthorId)) ||
                        b.BookGenres.Any(bg => genreIds.Contains(bg.GenreId)))
            .Include(b => b.OriginalLanguage)
            .Include(b => b.BookAuthors.OrderBy(ba => ba.DisplayOrder)).ThenInclude(ba => ba.Author)
            .Include(b => b.BookGenres).ThenInclude(bg => bg.Genre)
            .Include(b => b.BookFiles.Where(bf => bf.IsActive))
            .OrderByDescending(b => b.BookAuthors.Count(ba => authorIds.Contains(ba.AuthorId)))
            .ThenByDescending(b => b.CreatedAtUtc)
            .Take(5)
            .ToListAsync(ct);

        return related.Select(MapToSummaryDto).ToList();
    }

    private async Task ValidateBookRelationsAndDuplicatesAsync(Guid? existingBookId, UpsertBookRequest request, CancellationToken ct)
    {
        var langExists = await _db.Languages.AnyAsync(l => l.Id == request.OriginalLanguageId, ct);
        if (!langExists)
            throw new BadRequestException($"Original language '{request.OriginalLanguageId}' does not exist.");

        var distinctAuthors = request.AuthorIds.Distinct().ToList();
        var foundAuthors = await _db.Authors.CountAsync(a => distinctAuthors.Contains(a.Id), ct);
        if (foundAuthors != distinctAuthors.Count)
            throw new BadRequestException("One or more specified Author IDs do not exist.");

        var distinctGenres = request.GenreIds.Distinct().ToList();
        var foundGenres = await _db.Genres.CountAsync(g => distinctGenres.Contains(g.Id), ct);
        if (foundGenres != distinctGenres.Count)
            throw new BadRequestException("One or more specified Genre IDs do not exist.");

        if (!string.IsNullOrWhiteSpace(request.Isbn13))
        {
            var isbn13 = request.Isbn13.Trim();
            var isbnExists = await _db.Books.AnyAsync(b => b.Id != existingBookId && b.Isbn13 == isbn13, ct);
            if (isbnExists)
                throw new ConflictException($"A book with ISBN-13 '{isbn13}' already exists.");
        }

        if (!string.IsNullOrWhiteSpace(request.Isbn10))
        {
            var isbn10 = request.Isbn10.Trim();
            var isbnExists = await _db.Books.AnyAsync(b => b.Id != existingBookId && b.Isbn10 == isbn10, ct);
            if (isbnExists)
                throw new ConflictException($"A book with ISBN-10 '{isbn10}' already exists.");
        }
    }

    private async Task<string> GenerateUniqueBookSlugAsync(Guid? existingId, string rawInput, CancellationToken ct)
    {
        var baseSlug = SlugGenerator.Generate(rawInput);
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "book";

        var candidate = baseSlug;
        var counter = 2;
        while (await _db.Books.AnyAsync(b => b.Id != existingId && b.Slug == candidate, ct))
        {
            candidate = $"{baseSlug}-{counter++}";
        }
        return candidate;
    }

    private BookSummaryDto MapToSummaryDto(Book book)
    {
        return new BookSummaryDto(
            book.Id,
            book.Title,
            book.Slug,
            book.CoverImageUrl,
            book.PublicationYear,
            new LanguageDto(
                book.OriginalLanguage!.Id,
                book.OriginalLanguage.Name,
                book.OriginalLanguage.Code,
                book.OriginalLanguage.NativeName,
                book.OriginalLanguage.IsActive),
            book.BookAuthors
                .OrderBy(ba => ba.DisplayOrder)
                .Select(ba => new AuthorSummaryDto(ba.Author!.Id, ba.Author.DisplayName, ba.Author.Slug, ba.Author.PortraitImageUrl))
                .ToList(),
            book.BookGenres
                .Select(bg => new GenreSummaryDto(bg.Genre!.Id, bg.Genre.Name, bg.Genre.Slug, bg.Genre.ParentGenreId))
                .ToList(),
            book.BookFiles
                .Where(bf => bf.IsActive)
                .Select(bf => bf.Format.ToString().ToUpperInvariant())
                .Distinct()
                .ToList(),
            book.CreatedAtUtc);
    }

    private BookDetailDto MapToDetailDto(Book book, IReadOnlyList<BookSummaryDto> relatedBooks)
    {
        return new BookDetailDto(
            book.Id,
            book.Title,
            book.Slug,
            book.Synopsis,
            book.Isbn10,
            book.Isbn13,
            book.CoverImageUrl,
            book.PublicationYear,
            book.PublicationDate,
            book.Publisher,
            new LanguageDto(
                book.OriginalLanguage!.Id,
                book.OriginalLanguage.Name,
                book.OriginalLanguage.Code,
                book.OriginalLanguage.NativeName,
                book.OriginalLanguage.IsActive),
            book.SeriesName,
            book.SeriesOrder,
            book.Edition,
            book.Status,
            book.BookAuthors
                .OrderBy(ba => ba.DisplayOrder)
                .Select(ba => new AuthorSummaryDto(ba.Author!.Id, ba.Author.DisplayName, ba.Author.Slug, ba.Author.PortraitImageUrl))
                .ToList(),
            book.BookGenres
                .Select(bg => new GenreSummaryDto(bg.Genre!.Id, bg.Genre.Name, bg.Genre.Slug, bg.Genre.ParentGenreId))
                .ToList(),
            book.BookFiles.Select(MapToBookFileDto).ToList(),
            relatedBooks,
            book.CreatedAtUtc,
            book.UpdatedAtUtc);
    }

    private BookFileDto MapToBookFileDto(BookFile bf)
    {
        return new BookFileDto(
            bf.Id,
            bf.Format,
            bf.Format.ToString().ToUpperInvariant(),
            bf.OriginalFileName,
            bf.FileSizeBytes,
            bf.LanguageId,
            bf.Language?.Code,
            bf.Language?.Name,
            bf.EditionNote,
            _fileLinkService.BuildDownloadUrl(bf.GoogleDriveFileId),
            _fileLinkService.BuildDirectViewUrl(bf.GoogleDriveFileId),
            bf.CreatedAtUtc);
    }

    private void LogAudit(string actorUserId, string action, string entityType, string entityId, string details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
