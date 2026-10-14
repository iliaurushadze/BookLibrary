using BookLibrary.Domain.Enums;

namespace BookLibrary.Application.DTOs.Catalog;

public record LanguageDto(
    Guid Id,
    string Name,
    string Code,
    string? NativeName,
    bool IsActive);

public record UpsertLanguageRequest(
    string Name,
    string Code,
    string? NativeName,
    bool IsActive = true);

public record AuthorSummaryDto(
    Guid Id,
    string DisplayName,
    string Slug,
    string? PortraitImageUrl);

public record AuthorDetailDto(
    Guid Id,
    string DisplayName,
    string Slug,
    string? Biography,
    string? PortraitImageUrl,
    DateOnly? BirthDate,
    DateOnly? DeathDate,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record UpsertAuthorRequest(
    string DisplayName,
    string? Slug,
    string? Biography,
    string? PortraitImageUrl,
    DateOnly? BirthDate,
    DateOnly? DeathDate);

public record GenreSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentGenreId);

public record GenreDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentGenreId,
    string? ParentGenreName,
    IReadOnlyList<GenreSummaryDto> SubGenres);

public record UpsertGenreRequest(
    string Name,
    string? Slug,
    string? Description,
    Guid? ParentGenreId);

public record BookFileDto(
    Guid Id,
    BookFileFormat Format,
    string FormatName,
    string? OriginalFileName,
    long? FileSizeBytes,
    Guid? LanguageId,
    string? LanguageCode,
    string? LanguageName,
    string? EditionNote,
    string DownloadUrl,
    string DirectViewUrl,
    DateTime CreatedAtUtc);

public record AdminBookFileDto(
    Guid Id,
    BookFileFormat Format,
    string GoogleDriveFileId,
    string? OriginalSharingUrl,
    string? OriginalFileName,
    long? FileSizeBytes,
    Guid? LanguageId,
    string? LanguageCode,
    string? EditionNote,
    bool IsActive,
    string DownloadUrl,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record AssociateBookFileRequest(
    string SharingUrl,
    BookFileFormat Format,
    string? OriginalFileName,
    long? FileSizeBytes,
    Guid? LanguageId,
    string? EditionNote,
    bool IsActive = true);

public record BookSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string? CoverImageUrl,
    int? PublicationYear,
    LanguageDto OriginalLanguage,
    IReadOnlyList<AuthorSummaryDto> Authors,
    IReadOnlyList<GenreSummaryDto> Genres,
    IReadOnlyList<string> AvailableFormats,
    DateTime CreatedAtUtc);

public record BookDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Synopsis,
    string? Isbn10,
    string? Isbn13,
    string? CoverImageUrl,
    int? PublicationYear,
    DateOnly? PublicationDate,
    string? Publisher,
    LanguageDto OriginalLanguage,
    string? SeriesName,
    decimal? SeriesOrder,
    string? Edition,
    BookStatus Status,
    IReadOnlyList<AuthorSummaryDto> Authors,
    IReadOnlyList<GenreSummaryDto> Genres,
    IReadOnlyList<BookFileDto> Files,
    IReadOnlyList<BookSummaryDto> RelatedBooks,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record UpsertBookRequest(
    string Title,
    string? Slug,
    string Synopsis,
    string? Isbn10,
    string? Isbn13,
    string? CoverImageUrl,
    int? PublicationYear,
    DateOnly? PublicationDate,
    string? Publisher,
    Guid OriginalLanguageId,
    string? SeriesName,
    decimal? SeriesOrder,
    string? Edition,
    IReadOnlyList<Guid> AuthorIds,
    IReadOnlyList<Guid> GenreIds);

public record BookSearchQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Genre = null,
    string? Author = null,
    string? Language = null,
    BookFileFormat? Format = null,
    int? PublicationYear = null,
    string SortBy = "dateAdded",
    string SortDirection = "desc");
