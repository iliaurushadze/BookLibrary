using BookLibrary.Domain.Enums;

namespace BookLibrary.Domain.Entities;

public class BookFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookId { get; set; }
    public Book? Book { get; set; }
    public BookFileFormat Format { get; set; }
    public string GoogleDriveFileId { get; set; } = string.Empty;
    public string? OriginalSharingUrl { get; set; }
    public string? OriginalFileName { get; set; }
    public long? FileSizeBytes { get; set; }
    public Guid? LanguageId { get; set; }
    public Language? Language { get; set; }
    public string? EditionNote { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
