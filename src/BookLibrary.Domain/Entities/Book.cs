using BookLibrary.Domain.Enums;

namespace BookLibrary.Domain.Entities;

public class Book
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Synopsis { get; set; } = string.Empty;
    public string? Isbn10 { get; set; }
    public string? Isbn13 { get; set; }
    public string? CoverImageUrl { get; set; }
    public int? PublicationYear { get; set; }
    public DateOnly? PublicationDate { get; set; }
    public string? Publisher { get; set; }
    public Guid OriginalLanguageId { get; set; }
    public Language? OriginalLanguage { get; set; }
    public string? SeriesName { get; set; }
    public decimal? SeriesOrder { get; set; }
    public string? Edition { get; set; }
    public BookStatus Status { get; set; } = BookStatus.Draft;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }

    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    public ICollection<BookGenre> BookGenres { get; set; } = new List<BookGenre>();
    public ICollection<BookFile> BookFiles { get; set; } = new List<BookFile>();
    public ICollection<UserBookmark> Bookmarks { get; set; } = new List<UserBookmark>();
}
