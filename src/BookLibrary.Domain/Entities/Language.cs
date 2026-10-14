namespace BookLibrary.Domain.Entities;

public class Language
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // ISO 639-1 e.g., "en", "ka", "fr"
    public string? NativeName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Book> OriginalLanguageBooks { get; set; } = new List<Book>();
    public ICollection<BookFile> BookFiles { get; set; } = new List<BookFile>();
}
