using BookLibrary.Domain.Enums;

namespace BookLibrary.Application.DTOs.Requests;

public record CreateBookRequestDto(
    string BookTitle,
    string? AuthorName,
    RequestedBookFormat PreferredFormat = RequestedBookFormat.Either,
    string? Language = null,
    string? IsbnOrEdition = null,
    string? AdditionalDetails = null);

public record UserBookRequestDto(
    Guid Id,
    string BookTitle,
    string? AuthorName,
    RequestedBookFormat PreferredFormat,
    string? Language,
    string? IsbnOrEdition,
    string? AdditionalDetails,
    BookRequestStatus Status,
    string? UserVisibleResponse,
    DateTime SubmittedAtUtc,
    DateTime UpdatedAtUtc);

public record AdminBookRequestDto(
    Guid Id,
    string BookTitle,
    string? AuthorName,
    RequestedBookFormat PreferredFormat,
    string? Language,
    string? IsbnOrEdition,
    string? AdditionalDetails,
    string RequesterId,
    BookRequestStatus Status,
    string? UserVisibleResponse,
    string? AdminNotes,
    DateTime SubmittedAtUtc,
    DateTime UpdatedAtUtc);

public record UpdateBookRequestStatusDto(
    BookRequestStatus Status,
    string? UserVisibleResponse,
    string? AdminNotes);

public record CreateContactMessageDto(
    string Name,
    string Email,
    string Subject,
    string Message);

public record AdminContactMessageDto(
    Guid Id,
    string Name,
    string Email,
    string Subject,
    string Message,
    ContactMessageStatus Status,
    string? AdminNotes,
    DateTime SubmittedAtUtc,
    DateTime UpdatedAtUtc);

public record UpdateContactMessageDto(
    ContactMessageStatus Status,
    string? AdminNotes);

public record ReportBrokenLinkRequest(
    string Reason);

public record CatalogStatisticsDto(
    int TotalPublishedBooks,
    int TotalDraftBooks,
    int TotalArchivedBooks,
    int TotalAuthors,
    int TotalGenres,
    int TotalActiveFiles,
    int PendingBookRequests,
    int UnresolvedContactMessages,
    int UnresolvedBrokenLinks);
