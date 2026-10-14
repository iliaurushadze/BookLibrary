namespace BookLibrary.Domain.Entities;

public class UserBookmark
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid BookId { get; set; }
    public Book? Book { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
