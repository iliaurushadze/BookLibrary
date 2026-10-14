using BookLibrary.Application.Common;
using BookLibrary.Application.DTOs.Catalog;
using BookLibrary.Application.DTOs.Requests;
using BookLibrary.Application.Exceptions;
using BookLibrary.Application.Interfaces;
using BookLibrary.Domain.Entities;
using BookLibrary.Domain.Enums;
using BookLibrary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookLibrary.Infrastructure.Services;

public class BookRequestService : IBookRequestService
{
    private readonly AppDbContext _db;

    public BookRequestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserBookRequestDto> CreateRequestAsync(string userId, CreateBookRequestDto request, CancellationToken ct = default)
    {
        var entity = new BookRequest
        {
            BookTitle = request.BookTitle.Trim(),
            AuthorName = request.AuthorName?.Trim(),
            PreferredFormat = request.PreferredFormat,
            Language = request.Language?.Trim(),
            IsbnOrEdition = request.IsbnOrEdition?.Trim(),
            AdditionalDetails = request.AdditionalDetails?.Trim(),
            RequesterId = userId,
            Status = BookRequestStatus.Pending,
            SubmittedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.BookRequests.Add(entity);
        await _db.SaveChangesAsync(ct);

        return MapToUserDto(entity);
    }

    public async Task<PagedResult<UserBookRequestDto>> GetMyRequestsAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.BookRequests
            .AsNoTracking()
            .Where(r => r.RequesterId == userId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.SubmittedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<UserBookRequestDto>
        {
            Items = items.Select(MapToUserDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<UserBookRequestDto> GetMyRequestByIdAsync(string userId, Guid requestId, CancellationToken ct = default)
    {
        var req = await _db.BookRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (req == null)
            throw new NotFoundException($"Book request '{requestId}' was not found.");

        // Protect against IDOR: never allow viewing another user's request
        if (req.RequesterId != userId)
            throw new ForbiddenException("You do not have permission to view this book request.");

        return MapToUserDto(req);
    }

    public async Task CancelMyRequestAsync(string userId, Guid requestId, CancellationToken ct = default)
    {
        var req = await _db.BookRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (req == null)
            throw new NotFoundException($"Book request '{requestId}' was not found.");

        if (req.RequesterId != userId)
            throw new ForbiddenException("You do not have permission to cancel this book request.");

        if (req.Status != BookRequestStatus.Pending)
            throw new BadRequestException($"Only Pending requests can be cancelled. Current status: {req.Status}.");

        req.Status = BookRequestStatus.Cancelled;
        req.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AdminBookRequestDto>> GetAllRequestsAsync(int page, int pageSize, string? status, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.BookRequests.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookRequestStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(r => r.Status == parsedStatus);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.SubmittedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AdminBookRequestDto>
        {
            Items = items.Select(MapToAdminDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<AdminBookRequestDto> UpdateRequestStatusAsync(Guid requestId, UpdateBookRequestStatusDto dto, string actorUserId, CancellationToken ct = default)
    {
        var req = await _db.BookRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (req == null)
            throw new NotFoundException($"Book request '{requestId}' was not found.");

        req.Status = dto.Status;
        req.UserVisibleResponse = dto.UserVisibleResponse?.Trim();
        req.AdminNotes = dto.AdminNotes?.Trim();
        req.UpdatedAtUtc = DateTime.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = "UpdateBookRequestStatus",
            EntityType = nameof(BookRequest),
            EntityId = req.Id.ToString(),
            Details = $"Updated request status to {dto.Status}",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return MapToAdminDto(req);
    }

    private static UserBookRequestDto MapToUserDto(BookRequest r) =>
        new(r.Id, r.BookTitle, r.AuthorName, r.PreferredFormat, r.Language, r.IsbnOrEdition, r.AdditionalDetails, r.Status, r.UserVisibleResponse, r.SubmittedAtUtc, r.UpdatedAtUtc);

    private static AdminBookRequestDto MapToAdminDto(BookRequest r) =>
        new(r.Id, r.BookTitle, r.AuthorName, r.PreferredFormat, r.Language, r.IsbnOrEdition, r.AdditionalDetails, r.RequesterId, r.Status, r.UserVisibleResponse, r.AdminNotes, r.SubmittedAtUtc, r.UpdatedAtUtc);
}

public class ContactMessageService : IContactMessageService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;

    public ContactMessageService(AppDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    public async Task SubmitMessageAsync(CreateContactMessageDto dto, CancellationToken ct = default)
    {
        var msg = new ContactMessage
        {
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim(),
            Subject = dto.Subject.Trim(),
            Message = dto.Message.Trim(),
            Status = ContactMessageStatus.New,
            SubmittedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.ContactMessages.Add(msg);
        await _db.SaveChangesAsync(ct);

        await _emailService.SendEmailAsync(
            msg.Email,
            $"We received your message: {msg.Subject}",
            "Thank you for contacting the Digital Book Library. Our administrators will review your inquiry.",
            ct);
    }

    public async Task<PagedResult<AdminContactMessageDto>> GetMessagesAsync(int page, int pageSize, string? status, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.ContactMessages.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ContactMessageStatus>(status, true, out var parsed))
        {
            query = query.Where(c => c.Status == parsed);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.SubmittedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AdminContactMessageDto(c.Id, c.Name, c.Email, c.Subject, c.Message, c.Status, c.AdminNotes, c.SubmittedAtUtc, c.UpdatedAtUtc))
            .ToListAsync(ct);

        return new PagedResult<AdminContactMessageDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<AdminContactMessageDto> UpdateMessageAsync(Guid id, UpdateContactMessageDto dto, string actorUserId, CancellationToken ct = default)
    {
        var msg = await _db.ContactMessages.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (msg == null)
            throw new NotFoundException($"Contact message '{id}' was not found.");

        msg.Status = dto.Status;
        msg.AdminNotes = dto.AdminNotes?.Trim();
        msg.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return new AdminContactMessageDto(msg.Id, msg.Name, msg.Email, msg.Subject, msg.Message, msg.Status, msg.AdminNotes, msg.SubmittedAtUtc, msg.UpdatedAtUtc);
    }
}

public class BookmarkService : IBookmarkService
{
    private readonly AppDbContext _db;

    public BookmarkService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<BookSummaryDto>> GetUserBookmarksAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.UserBookmarks
            .AsNoTracking()
            .Where(ub => ub.UserId == userId && ub.Book!.Status == BookStatus.Published);

        var total = await query.CountAsync(ct);
        var bookmarks = await query
            .Include(ub => ub.Book!).ThenInclude(b => b.OriginalLanguage)
            .Include(ub => ub.Book!).ThenInclude(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(ub => ub.Book!).ThenInclude(b => b.BookGenres).ThenInclude(bg => bg.Genre)
            .Include(ub => ub.Book!).ThenInclude(b => b.BookFiles.Where(bf => bf.IsActive))
            .OrderByDescending(ub => ub.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = bookmarks.Select(ub =>
        {
            var b = ub.Book!;
            return new BookSummaryDto(
                b.Id,
                b.Title,
                b.Slug,
                b.CoverImageUrl,
                b.PublicationYear,
                new LanguageDto(b.OriginalLanguage!.Id, b.OriginalLanguage.Name, b.OriginalLanguage.Code, b.OriginalLanguage.NativeName, b.OriginalLanguage.IsActive),
                b.BookAuthors.OrderBy(ba => ba.DisplayOrder).Select(ba => new AuthorSummaryDto(ba.Author!.Id, ba.Author.DisplayName, ba.Author.Slug, ba.Author.PortraitImageUrl)).ToList(),
                b.BookGenres.Select(bg => new GenreSummaryDto(bg.Genre!.Id, bg.Genre.Name, bg.Genre.Slug, bg.Genre.ParentGenreId)).ToList(),
                b.BookFiles.Where(bf => bf.IsActive).Select(bf => bf.Format.ToString().ToUpperInvariant()).Distinct().ToList(),
                b.CreatedAtUtc);
        }).ToList();

        return new PagedResult<BookSummaryDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task AddBookmarkAsync(string userId, Guid bookId, CancellationToken ct = default)
    {
        var bookExists = await _db.Books.AnyAsync(b => b.Id == bookId && b.Status == BookStatus.Published, ct);
        if (!bookExists)
            throw new NotFoundException($"Published book '{bookId}' was not found.");

        var alreadyExists = await _db.UserBookmarks.AnyAsync(ub => ub.UserId == userId && ub.BookId == bookId, ct);
        if (alreadyExists)
            return;

        _db.UserBookmarks.Add(new UserBookmark
        {
            UserId = userId,
            BookId = bookId,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveBookmarkAsync(string userId, Guid bookId, CancellationToken ct = default)
    {
        var bookmark = await _db.UserBookmarks.FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BookId == bookId, ct);
        if (bookmark != null)
        {
            _db.UserBookmarks.Remove(bookmark);
            await _db.SaveChangesAsync(ct);
        }
    }
}

public class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("Simulated Email Dispatch -> To: {ToEmail} | Subject: {Subject}", toEmail, subject);
        return Task.CompletedTask;
    }
}
