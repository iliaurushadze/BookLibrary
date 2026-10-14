using BookLibrary.Domain.Enums;

namespace BookLibrary.Domain.Entities;

public class BookRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BookTitle { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public RequestedBookFormat PreferredFormat { get; set; } = RequestedBookFormat.Either;
    public string? Language { get; set; }
    public string? IsbnOrEdition { get; set; }
    public string? AdditionalDetails { get; set; }
    public string RequesterId { get; set; } = string.Empty;
    public BookRequestStatus Status { get; set; } = BookRequestStatus.Pending;
    public string? UserVisibleResponse { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
