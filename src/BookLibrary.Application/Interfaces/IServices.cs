using BookLibrary.Application.Common;
using BookLibrary.Application.DTOs.Auth;
using BookLibrary.Application.DTOs.Catalog;
using BookLibrary.Application.DTOs.Requests;

namespace BookLibrary.Application.Interfaces;

public interface IBookService
{
    Task<PagedResult<BookSummaryDto>> SearchPublishedBooksAsync(BookSearchQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<BookSummaryDto>> GetRecentBooksAsync(int count = 10, CancellationToken ct = default);
    Task<BookDetailDto> GetPublishedBookByIdOrSlugAsync(string idOrSlug, CancellationToken ct = default);
    Task<IReadOnlyList<BookFileDto>> GetPublishedBookFilesAsync(string idOrSlug, CancellationToken ct = default);
    Task<(string DownloadUrl, BookFileDto File)> GetFileDownloadRedirectAsync(string idOrSlug, Guid fileId, CancellationToken ct = default);

    // Admin operations
    Task<PagedResult<BookSummaryDto>> GetAdminBooksAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<BookDetailDto> GetAdminBookByIdAsync(Guid id, CancellationToken ct = default);
    Task<BookDetailDto> CreateBookAsync(UpsertBookRequest request, string actorUserId, CancellationToken ct = default);
    Task<BookDetailDto> UpdateBookAsync(Guid id, UpsertBookRequest request, string actorUserId, CancellationToken ct = default);
    Task PublishBookAsync(Guid id, string actorUserId, CancellationToken ct = default);
    Task ArchiveBookAsync(Guid id, string actorUserId, CancellationToken ct = default);
    Task DeleteBookAsync(Guid id, string actorUserId, CancellationToken ct = default);
    Task<AdminBookFileDto> AssociateBookFileAsync(Guid bookId, AssociateBookFileRequest request, string actorUserId, CancellationToken ct = default);
    Task RemoveBookFileAsync(Guid bookId, Guid fileId, string actorUserId, CancellationToken ct = default);
    Task ReportBrokenLinkAsync(Guid bookId, Guid fileId, string reason, string? reporterUserId, CancellationToken ct = default);
    Task<CatalogStatisticsDto> GetStatisticsAsync(CancellationToken ct = default);
}

public interface IAuthorService
{
    Task<PagedResult<AuthorSummaryDto>> GetAuthorsAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<AuthorDetailDto> GetAuthorByIdOrSlugAsync(string idOrSlug, CancellationToken ct = default);
    Task<PagedResult<BookSummaryDto>> GetPublishedBooksByAuthorAsync(string idOrSlug, int page, int pageSize, CancellationToken ct = default);
    Task<AuthorDetailDto> CreateAuthorAsync(UpsertAuthorRequest request, string actorUserId, CancellationToken ct = default);
    Task<AuthorDetailDto> UpdateAuthorAsync(Guid id, UpsertAuthorRequest request, string actorUserId, CancellationToken ct = default);
    Task DeleteAuthorAsync(Guid id, string actorUserId, CancellationToken ct = default);
}

public interface IGenreService
{
    Task<IReadOnlyList<GenreSummaryDto>> GetAllGenresAsync(CancellationToken ct = default);
    Task<GenreDetailDto> GetGenreByIdOrSlugAsync(string idOrSlug, CancellationToken ct = default);
    Task<PagedResult<BookSummaryDto>> GetPublishedBooksByGenreAsync(string idOrSlug, int page, int pageSize, CancellationToken ct = default);
    Task<GenreDetailDto> CreateGenreAsync(UpsertGenreRequest request, string actorUserId, CancellationToken ct = default);
    Task<GenreDetailDto> UpdateGenreAsync(Guid id, UpsertGenreRequest request, string actorUserId, CancellationToken ct = default);
    Task DeleteGenreAsync(Guid id, string actorUserId, CancellationToken ct = default);
}

public interface ILanguageService
{
    Task<IReadOnlyList<LanguageDto>> GetLanguagesAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<LanguageDto> CreateLanguageAsync(UpsertLanguageRequest request, string actorUserId, CancellationToken ct = default);
    Task<LanguageDto> UpdateLanguageAsync(Guid id, UpsertLanguageRequest request, string actorUserId, CancellationToken ct = default);
}

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task LogoutAsync(string userId, string? refreshToken, CancellationToken ct = default);
    Task<UserProfileDto> GetProfileAsync(string userId, CancellationToken ct = default);
    Task<UserProfileDto> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default);
    Task<PagedResult<AdminUserDto>> GetUsersAsync(int page, int pageSize, CancellationToken ct = default);
    Task UpdateUserRoleAsync(string targetUserId, string roleName, string actorUserId, CancellationToken ct = default);
}

public interface IBookRequestService
{
    Task<UserBookRequestDto> CreateRequestAsync(string userId, CreateBookRequestDto request, CancellationToken ct = default);
    Task<PagedResult<UserBookRequestDto>> GetMyRequestsAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    Task<UserBookRequestDto> GetMyRequestByIdAsync(string userId, Guid requestId, CancellationToken ct = default);
    Task CancelMyRequestAsync(string userId, Guid requestId, CancellationToken ct = default);
    Task<PagedResult<AdminBookRequestDto>> GetAllRequestsAsync(int page, int pageSize, string? status, CancellationToken ct = default);
    Task<AdminBookRequestDto> UpdateRequestStatusAsync(Guid requestId, UpdateBookRequestStatusDto dto, string actorUserId, CancellationToken ct = default);
}

public interface IContactMessageService
{
    Task SubmitMessageAsync(CreateContactMessageDto dto, CancellationToken ct = default);
    Task<PagedResult<AdminContactMessageDto>> GetMessagesAsync(int page, int pageSize, string? status, CancellationToken ct = default);
    Task<AdminContactMessageDto> UpdateMessageAsync(Guid id, UpdateContactMessageDto dto, string actorUserId, CancellationToken ct = default);
}

public interface IBookmarkService
{
    Task<PagedResult<BookSummaryDto>> GetUserBookmarksAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    Task AddBookmarkAsync(string userId, Guid bookId, CancellationToken ct = default);
    Task RemoveBookmarkAsync(string userId, Guid bookId, CancellationToken ct = default);
}

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken ct = default);
}
